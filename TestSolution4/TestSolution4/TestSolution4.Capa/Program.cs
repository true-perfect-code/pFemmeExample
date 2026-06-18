using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Exact namespaces from your MAUI definition
using p11.UI.Services;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Authentication;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.ImageOptimizer;
using BlazorCore.Services.LocalNotification;
using BlazorCore.Services.Media;
using BlazorCore.Services.Platform;
using TestSolution4.Shared.Pages.Common.Media;
using TestSolution4.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Root Components
//builder.RootComponents.Add<pMunus.App>("#app");
builder.RootComponents.Add<TestSolution4.Shared.Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- Platform ---
builder.Services.AddSingleton<TestSolution4.Shared.Services.Platform.IPlatform, TestSolution4.Shared.Services.Platform.Platform>();
builder.Services.AddSingleton<IPlatformBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.Platform.IPlatform>());

// --- Global State ---
builder.Services.AddSingleton<TestSolution4.Shared.Services.GlobalState.IGlobalState, TestSolution4.Shared.Services.GlobalState.GlobalState>();
builder.Services.AddSingleton<IGlobalStateBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>());

// Global State Initialisierung
builder.Services.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

// --- App State ---
builder.Services.AddSingleton<TestSolution4.Shared.Services.AppState.IAppState, TestSolution4.Shared.Services.AppState.AppState>();
builder.Services.AddSingleton<IAppStateBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>());

// --- Initializer ---
builder.Services.AddSingleton<TestSolution4.Shared.Services.AppInitializer.IAppInitializer, TestSolution4.Shared.Services.AppInitializer.AppInitializer>();
builder.Services.AddSingleton<BlazorCore.Services.AppInitializer.IAppInitializerBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.AppInitializer.IAppInitializer>());

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
builder.Services.AddSingleton<BlazorCore.Services.SqLite.ISqLiteBase, TestSolution4.Shared.Services.SqLite.SqLiteWasm>();

// --- DAM ---
builder.Services.AddSingleton<IDamBase, DamBase>();

// --- Authentication ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<TestSolution4.Shared.Services.Authentication.Authentication>();
builder.Services.AddSingleton<TestSolution4.Shared.Services.Authentication.IAuthentication>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.Authentication.Authentication>());
builder.Services.AddSingleton<IAuthenticationBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.Authentication.Authentication>());
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.Authentication.Authentication>());

// --- UI Services (p11) ---
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<IToastService, ToastService>();
builder.Services.AddSingleton<IMessageBoxService, MessageBoxService>();
builder.Services.AddSingleton<IEventStateService, EventStateService>();

// --- Media ---
builder.Services.AddSingleton<IMediaBase, Media>();

// --- Platform Specific Services (Using exactly the same class names as in MAUI) ---
builder.Services.AddSingleton<IImageOptimizer, TestSolution4.Shared.Services.ImageOptimizer.ImageOptimizer>();
builder.Services.AddSingleton<ILocalNotification, TestSolution4.Shared.Services.LocalNotification.LocalNotification>();

// --- CRUD APP SERVICES ---
// builder.Services.AddSingleton<XourService>();

// Setzt das Standard-Logging auf 'Warning', damit 'Information' (info) ausgeblendet wird
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();

// Security-Server über JS realisiert wegen WebView AES Einschränkungen
var jsRuntime = host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
var platform = host.Services.GetRequiredService<TestSolution4.Shared.Services.Platform.IPlatform>();
var globalState = host.Services.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>();
var appState = host.Services.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>();
BlazorCore.Services.ServerShared.SecurityServerFactory.Register(() => new TestSolution4.Shared.Services.Security.SecurityClient(jsRuntime, platform, globalState, appState));

await host.RunAsync();
