using BlazorCore;
using BlazorCore.Services.AI;
using BlazorCore.Services.AppInitializer;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Authentication;
using BlazorCore.Services.Dam;
using BlazorCore.Services.Email;
using BlazorCore.Services.EventAggregator;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.IdGenerator;
using BlazorCore.Services.LocalJsonFile;
using BlazorCore.Services.LocalQueryExecutor;
using BlazorCore.Services.LocalStorage;
using BlazorCore.Services.Logging;
using BlazorCore.Services.Otp;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.Translation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using p11.UI.Services;
using pFemmeExample.JsonContexts;
using pFemmeExample.Shared.Global;
using pFemmeExample.Shared.Models;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.EventAggregatorProject;
using pFemmeExample.Shared.Services.LocalJsonFile;
using pFemmeExample.Web.Apis;
using pFemmeExample.Web.Components;
using pFemmeExample.Web.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// IP-based rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        )
    );
});

// Cookie Policy
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, m =>
{
    m.LogoutPath = "/";
    m.Cookie.Name = Catalog.Sections.LocalStorage!.oauth_token;
    m.Cookie.SameSite = SameSiteMode.None;
    m.Cookie.SecurePolicy = CookieSecurePolicy.Always; // WICHTIG für Apple
    m.ExpireTimeSpan = TimeSpan.FromDays(30);
    m.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            // Verhindere Redirect-Loops bei Fehlern
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
    };
})
;

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        // Increases the limit for incoming messages (e.g., to 2 MB)
        // This is necessary to ensure that the image data is successfully received by the server 
        // via InvokeMethodAsync.
        options.MaximumReceiveMessageSize = 2 * 1024 * 1024;
    });

// ========================================================================
// CORE SERVICES
// ========================================================================

// Platform & Storage
builder.Services.AddScoped<IPlatformBase, Platform>();
builder.Services.AddScoped<IPlatformStorageBase, PlatformStorage>();

// Global State
builder.Services.AddSingleton<IGlobalStateBase, GlobalStateBase>();
builder.Services.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

// App State
builder.Services.AddScoped<IAppStateBase, AppStateBase>();

// App Initializer
builder.Services.AddScoped<IAppInitializerBase, AppInitializerBase>();

// ========================================================================
// INFRASTRUCTURE SERVICES
// ========================================================================
builder.Services.AddScoped<IEventAggregator, EventAggregator>();
builder.Services.AddScoped<IEventAggregatorProject, EventAggregatorProject>();
builder.Services.AddScoped<ILogging, Logging>();
builder.Services.AddScoped<ITranslation, Translation>();
builder.Services.AddScoped<IIdGenerator, IdGenerator>();
builder.Services.AddScoped<IDamBase, DamBase>();
builder.Services.AddScoped<ISqlClientBase, SqlClient>();

// ========================================================================
// STORAGE SERVICES
// ========================================================================
builder.Services.AddScoped<ILocalStorage, LocalStorage>();
builder.Services.AddScoped<ILocalQueryExecutor, LocalQueryExecutor>();
builder.Services.AddScoped<ILocalJsonFile, LocalJsonFile>(); // Shared service for local JSON file handling (not for WEB only WASM!)

// ========================================================================
// AUTHENTICATION
// ========================================================================
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<Authentication>();
builder.Services.AddScoped<IAuthenticationBase>(sp => sp.GetRequiredService<Authentication>());
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<Authentication>());

// ========================================================================
// APPLICATION SERVICES
// ========================================================================
builder.Services.AddScoped<IOtpBase, Otp>();
builder.Services.AddScoped<IEmailBase, Email>();

// ========================================================================
// UI SERVICES (p11)
// ========================================================================
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IMessageBoxService, MessageBoxService>();
builder.Services.AddScoped<IEventStateService, EventStateService>();

// ========================================================================
// HTTP & EMAIL CONFIGURATION
// ========================================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.Configure<SmtpSettingsModel>(builder.Configuration.GetSection("SmtpSettings"));

// ========================================================================
// SQL CLIENT, AI INITIALIZATION
// ========================================================================
builder.Services.AddHostedService<ServiceInitializer>();


// ========================================================================
// AI SERVICE (Azure OpenAI)
// ========================================================================

// AI Configuration aus appsettings.json lesen
var aiConfig = new AIConfiguration
{
    Endpoint = builder.Configuration["AI:Endpoint"] ?? builder.Configuration["OpenAIapiURL"],
    ApiKey = builder.Configuration["AI:ApiKey"] ?? builder.Configuration["OpenAIkey"],
    ModelName = builder.Configuration["AI:ModelName"] ?? "gpt-35-turbo",
    Temperature = double.TryParse(builder.Configuration["AI:Temperature"], out double temp) ? temp : 0.7
};

// AI Service registrieren
builder.Services.AddSingleton(aiConfig);
builder.Services.AddSingleton<IAI, AI>();


// ========================================================================
// PROJECT CRUD
// ========================================================================
builder.Services.AddScoped<CyclesService>();
builder.Services.AddScoped<TrendsService>();
builder.Services.AddScoped<CyclesCalService>();
builder.Services.AddScoped<CyclePhasesService>();
builder.Services.AddScoped<LogicService>();
builder.Services.AddScoped<IAIService, AIService>();

// System
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

var app = builder.Build();

// ========================================================================
// AOT REGISTRIERUNG FÜR KUNDEN-MODELLE (Blazor Server)
// ========================================================================
// Alle projekt-spezifischen Modelle hier registrieren.
// Dies muss NACH builder.Build() aber VOR app.Run() erfolgen.

AotTypeRegistry.RegisterList<CyclesModel>(pFemmeJsonContext.Default.ListCyclesModel);
AotTypeRegistry.RegisterList<CyclesCompareModel>(pFemmeJsonContext.Default.ListCyclesCompareModel);
AotTypeRegistry.RegisterList<CyclePhasesModel>(pFemmeJsonContext.Default.ListCyclePhasesModel);
AotTypeRegistry.RegisterList<ChartsModel>(pFemmeJsonContext.Default.ListChartsModel);

// Weitere Modelle hier hinzufügen, falls nötig
// AotTypeRegistry.RegisterList<MeinAnderesModel>(pFemmeJsonContext.Default.ListMeinAnderesModel);
// ========================================================================

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(pFemmeExample.Shared._Imports).Assembly);

app.MapEndPoints();
app.SetPepper();

app.Run();