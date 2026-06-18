If you are creating a new web project, then:

- Customize the ‘wwwroot’ folder (see ReadMe.md in wwwroot)

- Customize the ‘Components’ folder (see ReadMe.md in Components)

- Add project reference to 'Shared' project

- Update 'appsettings.json' file 

Example 'appsettings.json':
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "SymmetricSecurityKey": "eE3@eE3!E3ee*B@R@KUd@2023!wwWeE?3superSecretKey@345",
    "Issuer": "https://api.testsolution4.com",
    "Audience": "https://api.testsolution4.com"
  },

  "Authentication": {
    "Google": {
      "ClientId": "555775687511-20s3krg6ljl1em8s6itzgn44602a07vv.apps.googleusercontent.com",
      "ClientSecret": "555SPX-7HjCMPInD2ezHF2vNVr5WalxgwDm"
    },
    "Microsoft": {
      "ClientId": "55581d9f-2bf9-4fe3-c43e-b77b67bfe0cc",
      "ClientSecret": "5558Q~n-v1YTRhJFErcbDEX.SrySXPT4zAzuQdaO"
    },
    "Apple": {
      "ClientId": "ch.yourcompany.yourapplicationname.service",
      "TeamId": "555554NYYH",
      "KeyId": "555V42LWY6",
      "ClientSecret": "555hbGciOiJFUzI1NiIsImtpZCI6IjhROVY0M0xXWDYiLCJ0eXAiOiJKV1QifQ.eyJuYmYiOjE3NzMwNTQ4MDMsImV4cCI6MTc4ODYwNjgwMywiaXNzIjoiOTM5NTU0TllYSCIsImF1ZCI6Imh0dHBzOi8vYXBwbGVpZC5hcHBsZS5jb20iLCJzdWIiOiJjaC50cnVlcGVyZmVjdGNv1GUucE11bnVzLnNlFnZpY2UifQ.lue4B_mqoC2GsVm8jKXl1EWqG9ioP7maY79YCCso2Xcpccpe5g89ydoCsTRNtvELt4JP4O4MOgjD9beliB3555"
    }
  }
}
```

- Update file 'Program.cs'

Examle Program.cs:
```
using TestSolution4.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Components.Authorization;
using p11.UI.Services;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Authentication;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.ImageOptimizer;
using BlazorCore.Services.LocalNotification;
using BlazorCore.Services.Media;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Global;
using Shared.Pages.Common.Media;
using System.Security.Claims;
using System.Threading.RateLimiting;
using TestSolution4.Web.Apis;
//using Web.Components;
using TestSolution4.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ERNEUERUNG DES APPLE SCHLUESSELS:
// 1. Im Developer Apple Portal neuen Schlüssel generieren (siehe Doku '05 05 Ext. Authentifizierung')
// 2. Generierten Schlüssel Herunterladen (z.B. lokal 'Dokumente > Dev > [APP-NAME] > Auth > Apple')
// 3. heruntergeladenen Schlüssel auslesen und zeile für Zeile unten in die Methode 'CreateAppleClientSecret' einfügen
// 4. In der 'appsettings.json' ('Web' und 'pWebApi' Projekte) bei Apple den Json-Parameter 'Key ID' anpassen bzw. von neu generiertem Schlüssel (Apple Portal) abschreiben
// 5. Deaktivierten code hier 'var (jwt, err) = CreateAppleClientSecret(builder);' aktivieren und ausführen (Stopp gleich danach setzen)
// 5. Ausgelesenen 'jwt' in die 'appsettings.json' der Projekte 'Web' und 'pWebapi' einfügen, so wie auch auf dem Firestorm Server
// 6. Auf dem Firestorm Server die beide Apps ('Web' und 'pWebApi') neu starten, damit der Schlüssel eingelesen wird
// 7. Anmeldung über Apple sowohl über web-Browser wie einem Gerät austesten.
//
// ACHTUNG: Nicht vergessen zuerst 'Key ID' in 'appsettings.json' anzupassen, erst dann 'jwt' generieren !!!
// Logs:
// ab 08.03.2026 nach 6 Monaten wieder ausführen um JWT für Apple Anmeldung zu generieren
// ab 19.09.2025 nach 6 Monaten wieder ausführen um JWT für Apple Anmeldung zu generieren
//
//var (jwt, err) = CreateAppleClientSecret(builder);

// Rate Limiting auf IP-Basis
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
    //o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
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
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.SaveTokens = true;
    AddCommonOAuthEvents(options.Events, "google");
})
.AddMicrosoftAccount("Microsoft", options =>
{
    options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    options.CallbackPath = "/signin-microsoft";
    options.AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    options.TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("https://graph.microsoft.com/User.Read");
    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.SaveTokens = true;
    AddCommonOAuthEvents(options.Events, "microsoft");
})
.AddApple("Apple", options =>
{
    //var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    //var jwtPath = Path.Combine(Path.GetDirectoryName(baseDir!)!, "_Connections", Appl.ApplicationName + ".txt");

    //if (File.Exists(jwtPath))
    //{
    options.ClientId = builder.Configuration["Authentication:Apple:ClientId"];
    options.KeyId = builder.Configuration["Authentication:Apple:KeyId"];
    options.TeamId = builder.Configuration["Authentication:Apple:TeamId"];
    options.CallbackPath = "/signin-apple";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.GenerateClientSecret = false;
    //options.ClientSecret = File.ReadAllText(jwtPath);
    options.ClientSecret = builder.Configuration["Authentication:Apple:ClientSecret"]; // Created ClientSecret on 21.08.2025 
    options.SaveTokens = true;

    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

    AddCommonOAuthEvents(options.Events, "apple");
    //}
});
;

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        // Erhöht das Limit für eingehende Nachrichten (z.B. auf 2 MB)
        // Dies ist notwendig, damit die Bilddaten per InvokeMethodAsync 
        // sicher am Server ankommen.
        options.MaximumReceiveMessageSize = 2 * 1024 * 1024;
    });

// Platform
builder.Services.AddScoped<TestSolution4.Shared.Services.Platform.IPlatform, Platform>();
builder.Services.AddScoped<IPlatformBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.Platform.IPlatform>());

// Global State
builder.Services.AddSingleton<TestSolution4.Shared.Services.GlobalState.IGlobalState, TestSolution4.Shared.Services.GlobalState.GlobalState>();
builder.Services.AddSingleton<IGlobalStateBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>());

//// Global State Initialisierung
////builder.Services.AddSingleton<GlobalStateInitializer>();
//builder.Services.AddSingleton<TestSolution4.Shared.Services.GlobalStateInitializer.GlobalStateInitializer>();

// Global State Initialisierung
builder.Services.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

// App State
builder.Services.AddScoped<TestSolution4.Shared.Services.AppState.IAppState, TestSolution4.Shared.Services.AppState.AppState>();
builder.Services.AddScoped<IAppStateBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>());

// Initializer
builder.Services.AddScoped<TestSolution4.Shared.Services.AppInitializer.IAppInitializer, TestSolution4.Shared.Services.AppInitializer.AppInitializer>();
builder.Services.AddScoped<BlazorCore.Services.AppInitializer.IAppInitializerBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.AppInitializer.IAppInitializer>());

// DAM
builder.Services.AddScoped<IDamBase, DamBase>();

// Authentication
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<TestSolution4.Web.Services.Authentication>();
builder.Services.AddScoped<TestSolution4.Shared.Services.Authentication.IAuthentication>(sp => sp.GetRequiredService<TestSolution4.Web.Services.Authentication>());
builder.Services.AddScoped<IAuthenticationBase>(sp => sp.GetRequiredService<TestSolution4.Web.Services.Authentication>());
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<TestSolution4.Web.Services.Authentication>());

// UI Services
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IMessageBoxService, MessageBoxService>();
builder.Services.AddScoped<IEventStateService, EventStateService>();

// SqlClient
builder.Services.AddScoped<ISqlClientBase, SqlClient>();

// Initialisierung von SQL Client => Mapping von allen Tabellen
builder.Services.AddHostedService<TestSolution4.Web.Services.SqlClientInitializer>();

// Lokale Notifikation
builder.Services.AddScoped<ILocalNotification, LocalNotification>();

// Image optimizer
builder.Services.AddScoped<IImageOptimizer, ImageOptimizer>();

// Media
builder.Services.AddScoped<IMediaBase, Media>();

//// CRUD
//builder.Services.AddScoped<TasksService>();
//builder.Services.AddScoped<TodoService>();


// System
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

var app = builder.Build();

//// Initialisierung von Global State => ConnectionString, XML-Sprachtabelle ...
//using (var scope = app.Services.CreateScope())
//{
//    //var initializer = scope.ServiceProvider.GetRequiredService<GlobalStateInitializer>();
//    var initializer = scope.ServiceProvider.GetRequiredService<TestSolution4.Shared.Services.GlobalStateInitializer.GlobalStateInitializer>();
//    // Hier ist der "Trick": Synchroner Aufruf der Initialisierungslogik.
//    // .GetAwaiter().GetResult() blockiert den Thread, bis die Initialisierung fertig ist.
//    initializer.InitializeAsync().GetAwaiter().GetResult();
//}

////// Static Service Locator initialisieren
////pE.Utility.PlatformHelper.Initialize(app.Services);

////// GlobalState initialisieren
////using (var scope = app.Services.CreateScope())
////{
////    var globalState = scope.ServiceProvider.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>();
////    await globalState.EnsureInitializedAsync();
////}

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
    .AddAdditionalAssemblies(typeof(Shared._Imports).Assembly);

app.MapEndPoints();
app.SetPepper();

app.Run();




void AddCommonOAuthEvents(OAuthEvents events, string provider)
{
    events.OnCreatingTicket = async context =>
    {
        context.RunClaimActions();

        var identity = new ClaimsIdentity(context.Principal.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Expiration, DateTimeOffset.UtcNow.AddDays(30).ToString("O")));

        var principal = new ClaimsPrincipal(identity);

        await context.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        Console.WriteLine($"[{provider.ToUpper()} Auth] Authenticated: {context.Principal.Identity?.IsAuthenticated}");
        foreach (var claim in context.Identity.Claims)
            Console.WriteLine($"[{provider.ToUpper()} Claim] {claim.Type} = {claim.Value}");

        context.Response.Redirect("/");
        await Task.CompletedTask;
    };

    events.OnRemoteFailure = async context =>
    {
        var error = context.Failure;
        string msg = error?.Message ?? "Ein unbekannter Fehler ist aufgetreten.";
        if (error?.InnerException != null)
            msg += " (Inner: " + error.InnerException.Message + ")";

        Console.WriteLine($"[{provider.ToUpper()} AUTH ERROR] {msg}");
        context.Response.Redirect("/Error?message=" + Uri.EscapeDataString(msg));
        context.HandleResponse();
        await Task.CompletedTask;
    };

    events.OnTicketReceived = context =>
    {
        context.Response.Redirect("/");
        context.HandleResponse();
        return Task.CompletedTask;
    };
}

//// Hilfsfunktion zur Erstellung des JWT + Fehler-Übergabe
//(string token, string error) CreateAppleClientSecret(WebApplicationBuilder builder)
//{
//    //string teamId = "939554NYXH";
//    //string clientId = "ch.trueperfectcode.pfemme.service";
//    //string keyId = "CNVCL4M62C";
//    string clientId = builder.Configuration["Authentication:Apple:ClientId"];
//    string keyId = builder.Configuration["Authentication:Apple:KeyId"];
//    string teamId = builder.Configuration["Authentication:Apple:TeamId"];

//    string privateKeyPem = string.Join('\n', new[]
//    {
//        "-----BEGIN PRIVATE KEY-----",
//        "MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg5y9AkIMsZzwdANMW",
//        "0rJ8MC/gzhQ7tCAiEI6vGhiyRgKgCgYIKoZIzj0DAQehRANCAASsoSq6qFpsn82w",
//        "kzbHNUl2pPNEV9bWp2wSOceFqHt1hssuX9NYGsUIfGBqkboWzUuDvCTQpro+yRXA",
//        "xSn1rOSx",
//        "-----END PRIVATE KEY-----"
//    });

//    try
//    {
//        var ecdsa = ECDsa.Create();
//        ecdsa.ImportFromPem(privateKeyPem.ToCharArray());

//        var credentials = new SigningCredentials(
//            new ECDsaSecurityKey(ecdsa) { KeyId = keyId },
//            SecurityAlgorithms.EcdsaSha256);

//        var now = DateTime.UtcNow;
//        var jwt = new JwtSecurityToken(
//            issuer: teamId,
//            audience: "https://appleid.apple.com",
//            claims: null,
//            notBefore: now,
//            //expires: now.AddMinutes(30),
//            expires: now.AddDays(180),
//            signingCredentials: credentials);

//        jwt.Payload["sub"] = clientId;

//        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
//        Console.WriteLine("[JWT Created] Length: " + token.Length);
//        return (token, "");
//    }
//    catch (Exception ex)
//    {
//        var errorMsg = "ClientSecret Fehler: " + ex.Message;
//        if (ex.InnerException != null)
//        {
//            errorMsg += " | Inner: " + ex.InnerException.Message;
//        }
//        Console.WriteLine("[JWT ERROR] " + errorMsg);
//        return (string.Empty, errorMsg);
//    }
//}
```