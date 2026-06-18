using BlazorCore;
using BlazorCore.Services.Apis;
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
using BlazorCore.Services.Translation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using p11.UI.Services;
using pFemmeExample.JsonContexts;
using pFemmeExample.Shared.Models;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.Email;
using pFemmeExample.Shared.Services.EventAggregatorProject;
using pFemmeExample.Shared.Services.LocalJsonFile;
using pFemmeExample.Shared.Services.Otp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ========================================================================
// ROOT COMPONENTS
// ========================================================================
builder.RootComponents.Add<pFemmeExample.Shared.Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ========================================================================
// CORE SERVICES
// ========================================================================

// Platform & Storage
builder.Services.AddSingleton<IPlatformBase, pFemmeExample.Shared.Services.Platform.Platform>();
builder.Services.AddSingleton<IPlatformStorageBase, pFemmeExample.Shared.Services.Platform.PlatformStorage>();

// Global State
builder.Services.AddSingleton<IGlobalStateBase, GlobalStateBase>();
builder.Services.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

// App State
builder.Services.AddSingleton<IAppStateBase, AppStateBase>();

// App Initializer
builder.Services.AddSingleton<IAppInitializerBase, AppInitializerBase>();

// ========================================================================
// INFRASTRUCTURE SERVICES
// ========================================================================

builder.Services.AddSingleton<IEventAggregator, EventAggregator>();
builder.Services.AddSingleton<IEventAggregatorProject, EventAggregatorProject>();
builder.Services.AddSingleton<ILogging, Logging>();
builder.Services.AddSingleton<ITranslation, Translation>();
builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
builder.Services.AddSingleton<IApiBase, ApiBase>();
builder.Services.AddSingleton<IDamBase, DamBase>();

// ========================================================================
// STORAGE SERVICES (WASM/Capacitor)
// ========================================================================
builder.Services.AddSingleton<ILocalStorage, LocalStorage>();
builder.Services.AddSingleton<ILocalQueryExecutor, LocalQueryExecutor>();
builder.Services.AddSingleton<ILocalJsonFile, LocalJsonFile>(); // Shared service for local JSON file handling (not for WEB only WASM!)

// ========================================================================
// AUTHENTICATION
// ========================================================================
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<pFemmeExample.Shared.Services.Authentication.Authentication>();
builder.Services.AddSingleton<IAuthenticationBase>(sp => sp.GetRequiredService<pFemmeExample.Shared.Services.Authentication.Authentication>());
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<pFemmeExample.Shared.Services.Authentication.Authentication>());

// ========================================================================
// APPLICATION SERVICES
// ========================================================================
builder.Services.AddSingleton<IOtpBase, Otp>();
builder.Services.AddSingleton<IEmailBase, Email>();

// ========================================================================
// UI SERVICES (p11)
// ========================================================================
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<IToastService, ToastService>();
builder.Services.AddSingleton<IMessageBoxService, MessageBoxService>();
builder.Services.AddSingleton<IEventStateService, EventStateService>();

// ========================================================================
// PROJECT CRUD
// ========================================================================
builder.Services.AddSingleton<CyclesService>();
builder.Services.AddSingleton<TrendsService>();
builder.Services.AddSingleton<CyclesCalService>();
builder.Services.AddSingleton<CyclePhasesService>();
builder.Services.AddSingleton<LogicService>();
builder.Services.AddSingleton<IAIService, AIService>();

// ========================================================================
// HTTP & EMAIL CONFIGURATION
// ========================================================================
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.Configure<SmtpSettingsModel>(builder.Configuration.GetSection("SmtpSettings"));

// ========================================================================
// LOGGING
// ========================================================================
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// ========================================================================
// BUILD & RUN
// ========================================================================
var host = builder.Build();

// ========================================================================
// AOT REGISTRIERUNG FÜR KUNDEN-MODELLE (WASM/Capacitor)
// ========================================================================
// Alle projekt-spezifischen Modelle hier registrieren.
// Dies muss NACH builder.Build() aber VOR await host.RunAsync() erfolgen.

AotTypeRegistry.RegisterList<CyclesModel>(pFemmeJsonContext.Default.ListCyclesModel);
AotTypeRegistry.RegisterList<CyclesCompareModel>(pFemmeJsonContext.Default.ListCyclesCompareModel);
AotTypeRegistry.RegisterList<CyclePhasesModel>(pFemmeJsonContext.Default.ListCyclePhasesModel);
AotTypeRegistry.RegisterList<ChartsModel>(pFemmeJsonContext.Default.ListChartsModel);

// Weitere Modelle hier hinzufügen, falls nötig
// AotTypeRegistry.RegisterList<MeinAnderesModel>(pFemmeJsonContext.Default.ListMeinAnderesModel);
// ========================================================================

// Security client registration with JS interop.
// On WASM/WebView, AES has restrictions, so crypto is delegated to JavaScript.
var jsRuntime = host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
var platform = host.Services.GetRequiredService<IPlatformBase>();
var globalState = host.Services.GetRequiredService<IGlobalStateBase>();
var appState = host.Services.GetRequiredService<IAppStateBase>();

BlazorCore.Services.ServerShared.SecurityServerFactory.Register(() =>
    new pFemmeExample.Shared.Services.Security.SecurityClient(jsRuntime, platform, globalState, appState));

await host.RunAsync();