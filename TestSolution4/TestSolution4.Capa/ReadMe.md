If you are creating a new web project, then:
- Customize the ‘wwwroot’ folder (see ReadMe.md in wwwroot)
- Remove folder 'Layout'
- Remove folder 'Pages'
- Remove file 'App.razor'
- Remove '@using TestSolution4.Capa.Layout' in file '_Imports.razor'
- Add project reference to 'Shared' project
- Add Folder 'Services' and add your services there

- Update file 'Program.cs'

Examle Program.cs:
```
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Exact namespaces from your MAUI definition
using p11.UI.Services;
using pE.Services.Apis;
using pE.Services.AppState;
using pE.Services.Authentication;
using pE.Services.Dam;
using pE.Services.GlobalState;
using pE.Services.ImageOptimizer;
using pE.Services.LocalNotification;
using pE.Services.Media;
using pE.Services.Platform;
using Shared.Pages.Common.Media;
using Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Root Components
//builder.RootComponents.Add<pMunus.App>("#app");
builder.RootComponents.Add<Shared.Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- Platform ---
builder.Services.AddSingleton<Shared.Services.Platform.IPlatform, TestSolution4.Capa.Services.Platform>();
builder.Services.AddSingleton<IPlatformBase>(sp => sp.GetRequiredService<Shared.Services.Platform.IPlatform>());

// --- Global State ---
builder.Services.AddSingleton<Shared.Services.GlobalState.IGlobalState, Shared.Services.GlobalState.GlobalState>();
builder.Services.AddSingleton<IGlobalStateBase>(sp => sp.GetRequiredService<Shared.Services.GlobalState.IGlobalState>());

// Global State Initialisierung
builder.Services.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

// --- App State ---
builder.Services.AddSingleton<Shared.Services.AppState.IAppState, Shared.Services.AppState.AppState>();
builder.Services.AddSingleton<IAppStateBase>(sp => sp.GetRequiredService<Shared.Services.AppState.IAppState>());

// --- Initializer ---
builder.Services.AddSingleton<Shared.Services.AppInitializer.IAppInitializer, Shared.Services.AppInitializer.AppInitializer>();
builder.Services.AddSingleton<pE.Services.AppInitializer.IAppInitializerBase>(sp => sp.GetRequiredService<Shared.Services.AppInitializer.IAppInitializer>());

// --- Api ---
builder.Services.AddSingleton<IApiBase, ApiBase>();
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

//// --- Realm ---
//builder.Services.AddSingleton<IRealmBase>(sp =>
//{
//    var globalState = sp.GetRequiredService<IGlobalStateBase>();
//    var appState = sp.GetRequiredService<IAppStateBase>();
//    var platform = sp.GetRequiredService<IPlatformBase>();
//    var messageBoxService = sp.GetRequiredService<p11.UI.Services.IMessageBoxService>();

//    // Virtual identifier for WASM
//    var appDataDir = "wasm_storage";

//    return new RealmBase(globalState, appState, platform, messageBoxService, appDataDir);
//});

// --- SQLite ---
// Using the platform-specific class name "SqLite" as per your convention
builder.Services.AddSingleton<pE.Services.SqLite.ISqLiteBase, TestSolution4.Capa.Services.SqLite>();

// --- DAM ---
builder.Services.AddSingleton<IDamBase, DamBase>();

// --- Authentication ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<TestSolution4.Capa.Services.Authentication>();
builder.Services.AddSingleton<Shared.Services.Authentication.IAuthentication>(sp => sp.GetRequiredService<TestSolution4.Capa.Services.Authentication>());
builder.Services.AddSingleton<IAuthenticationBase>(sp => sp.GetRequiredService<TestSolution4.Capa.Services.Authentication>());
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<TestSolution4.Capa.Services.Authentication>());

// --- UI Services (p11) ---
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<IToastService, ToastService>();
builder.Services.AddSingleton<IMessageBoxService, MessageBoxService>();
builder.Services.AddSingleton<IEventStateService, EventStateService>();

// --- Media ---
builder.Services.AddSingleton<IMediaBase, Media>();

// --- Platform Specific Services (Using exactly the same class names as in MAUI) ---
builder.Services.AddSingleton<IImageOptimizer, TestSolution4.Capa.Services.ImageOptimizer>();
builder.Services.AddSingleton<ILocalNotification, TestSolution4.Capa.Services.LocalNotification>();

// --- CRUD APP SERVICES ---
// builder.Services.AddSingleton<XourService>();

// Setzt das Standard-Logging auf 'Warning', damit 'Information' (info) ausgeblendet wird
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();

// Security-Server über JS realisiert wegen WebView AES Einschränkungen
var jsRuntime = host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
var platform = host.Services.GetRequiredService<Shared.Services.Platform.IPlatform>();
var globalState = host.Services.GetRequiredService<Shared.Services.GlobalState.IGlobalState>();
var appState = host.Services.GetRequiredService<Shared.Services.AppState.IAppState>();
pE.Services.ServerShared.SecurityServerFactory.Register(() => new TestSolution4.Capa.Services.SecurityServer(jsRuntime, platform, globalState, appState));

await host.RunAsync();
```