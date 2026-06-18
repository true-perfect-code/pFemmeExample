If you are creating a new web project, then:
- Customize the ‘wwwroot’ folder (see ReadMe.md in wwwroot)
- Remove folder 'Layout'
- Remove folder 'Pages'
- Remove file 'App.razor'
- Remove '@using TestSolution4.Capa.Layout' in file '_Imports.razor'
- Add project reference to 'Shared' project
- Add Folder 'Services' and add your services there

- Add file 'web.config'

Examle web.config:
```
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <staticContent>
      <remove fileExtension=".webmanifest" />
      <remove fileExtension=".blat" />
      <remove fileExtension=".dat" />
      <remove fileExtension=".dll" />
      <remove fileExtension=".webcil" />
      <remove fileExtension=".json" />
      <remove fileExtension=".wasm" />
      <remove fileExtension=".woff" />
      <remove fileExtension=".woff2" />
      <mimeMap fileExtension=".webmanifest" mimeType="application/manifest+json" />
      <mimeMap fileExtension=".blat" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".dll" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".webcil" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".dat" mimeType="application/octet-stream" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <mimeMap fileExtension=".wasm" mimeType="application/wasm" />
      <mimeMap fileExtension=".woff" mimeType="application/font-woff" />
      <mimeMap fileExtension=".woff2" mimeType="application/font-woff" />
    </staticContent>
    <httpCompression>
      <dynamicTypes>
        <add mimeType="application/octet-stream" enabled="true" />
        <add mimeType="application/wasm" enabled="true" />
      </dynamicTypes>
    </httpCompression>
    <rewrite>
      <rules>
        <rule name="Serve subdir">
          <match url=".*" />
          <action type="Rewrite" url="wwwroot\{R:0}" />
        </rule>
        <rule name="SPA fallback routing" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
          </conditions>
          <action type="Rewrite" url="wwwroot\" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

- Update file 'Program.cs'

Examle Program.cs:
```
using TestSolution4.Shared.Services.GlobalState;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Namespaces aus deiner Architektur
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

// Root Components - Wir nutzen deine Shared Routes wie in Capacitor
builder.RootComponents.Add<Shared.Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- Platform ---
// WICHTIG: Hier referenzieren wir die Services aus dem .Wasm (Capacitor) Projekt, 
// da die Logik für PWA identisch ist.
builder.Services.AddSingleton<Shared.Services.Platform.IPlatform, TestSolution4.Pwa.Services.Platform>();
builder.Services.AddSingleton<IPlatformBase>(sp => sp.GetRequiredService<Shared.Services.Platform.IPlatform>());

// --- Global State ---
builder.Services.AddSingleton<Shared.Services.GlobalState.IGlobalState, TestSolution4.Shared.Services.GlobalState.GlobalState>();
builder.Services.AddSingleton<IGlobalStateBase>(sp => sp.GetRequiredService<Shared.Services.GlobalState.IGlobalState>());
builder.Services.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

// --- App State ---
builder.Services.AddSingleton<Shared.Services.AppState.IAppState, TestSolution4.Shared.Services.AppState.AppState>();
builder.Services.AddSingleton<IAppStateBase>(sp => sp.GetRequiredService<Shared.Services.AppState.IAppState>());

// --- Initializer ---
builder.Services.AddSingleton<Shared.Services.AppInitializer.IAppInitializer, TestSolution4.Shared.Services.AppInitializer.AppInitializer>();
builder.Services.AddSingleton<pE.Services.AppInitializer.IAppInitializerBase>(sp => sp.GetRequiredService<Shared.Services.AppInitializer.IAppInitializer>());

// --- Api ---
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IApiBase, ApiBase>();

// --- SQLite (WASM Version) ---
builder.Services.AddSingleton<pE.Services.SqLite.ISqLiteBase, TestSolution4.Pwa.Services.SqLite>();

// --- DAM ---
builder.Services.AddSingleton<IDamBase, DamBase>();

// --- Authentication ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<TestSolution4.Pwa.Services.Authentication>();
builder.Services.AddSingleton<Shared.Services.Authentication.IAuthentication>(sp => sp.GetRequiredService<TestSolution4.Pwa.Services.Authentication>());
builder.Services.AddSingleton<IAuthenticationBase>(sp => sp.GetRequiredService<TestSolution4.Pwa.Services.Authentication>());
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<TestSolution4.Pwa.Services.Authentication>());

// --- UI Services (p11) ---
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<IToastService, ToastService>();
builder.Services.AddSingleton<IMessageBoxService, MessageBoxService>();
builder.Services.AddSingleton<IEventStateService, EventStateService>();

// --- Media & Platform Specific ---
builder.Services.AddSingleton<IMediaBase, Media>();
builder.Services.AddSingleton<IImageOptimizer, TestSolution4.Pwa.Services.ImageOptimizer>();
builder.Services.AddSingleton<ILocalNotification, TestSolution4.Pwa.Services.LocalNotification>();

// --- CRUD ---
//builder.Services.AddSingleton<YourService>();

// Logging
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();

// Security-Server Initialisierung (identisch zu Capacitor WASM)
var jsRuntime = host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
var platform = host.Services.GetRequiredService<Shared.Services.Platform.IPlatform>();
var globalState = host.Services.GetRequiredService<Shared.Services.GlobalState.IGlobalState>();
var appState = host.Services.GetRequiredService<Shared.Services.AppState.IAppState>();
pE.Services.ServerShared.SecurityServerFactory.Register(() => new TestSolution4.Pwa.Services.SecurityServer(jsRuntime, platform, globalState, appState));

await host.RunAsync();
```