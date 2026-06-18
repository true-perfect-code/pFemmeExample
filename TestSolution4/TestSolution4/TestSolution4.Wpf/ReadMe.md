If you are creating a new web project, then:

- Add project reference to 'Shared' project

- Create file '_Imports.razor' and add '@using Microsoft.AspNetCore.Components.Web'

- Create a 'Services' folder and add the relevant services.

- Create folder 'wwwroot'

- Create file 'index.html' in 'wwwroot' folder with the following content:
index.html
```

<!DOCTYPE html>
<html lang="en">
<head>
    <title>TestSolution4</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover" />
    <base href="/" />

    <link rel="stylesheet" href="_content/TestSolution4.Shared/fonts.css" />

    <link rel="stylesheet" href="_content/P11/p11.css" />
    <script src="_content/P11/p11.js" type="module"></script>

    <link rel="stylesheet" href="_content/BlazorCore/nativewebview.css" />

    <link rel="icon" href="data:,">
</head>

<body>
    <div class="status-bar-safe-area"></div>

    <div id="app">
        <div class="d-flex flex-column justify-content-center align-items-center vh-100 bg-light">
            <div class="text-center">
                <div class="spinner-border text-secondary" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div class="loading-progress-text mt-3 text-secondary" style="font-family: sans-serif; font-size: 0.85rem;"></div>
            </div>
        </div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="." class="reload">Reload</a>
        <span class="dismiss">🗙</span>
    </div>

    <!--<script src="_content/P11/p11.js" type="module"></script>-->
    <script src="_content/BlazorCore/app.js"></script>

    <script src="_framework/blazor.webview.js" autostart="false"></script>

    <script>
        // 1. Zentrale Start-Logik
        function attemptBlazorStart() {
            // Falls bereits gestartet, nichts tun
            if (window.BlazorStarted) return;

            // BEDINGUNG 1: Unsere Library muss bereit sein (CSS/JS geladen)
            const isP11Ready = !!window.p11Ready;

            // BEDINGUNG 2: Das Microsoft Blazor Framework muss geladen sein
            const isBlazorLoaded = !!window.Blazor;

            if (!isP11Ready || !isBlazorLoaded) {
                // Noch nicht alles da? Wir loggen es nur einmalig leise.
                return;
            }

            try {
                window.BlazorStarted = true;
                console.log("[WPF] Alle Systeme bereit. Starte Blazor Framework...");

                // Blazor manuell starten (da autostart="false" in der index.html sein sollte)
                window.Blazor.start();

            } catch (err) {
                console.error("[WPF] CRITICAL: Fehler beim Aufruf von Blazor.start():", err);
                window.BlazorStarted = false;
            }
        }

        // 2. Registrierung der "Zünder"

        // Zünder A: Falls p11 meldet, dass es fertig ist
        window.addEventListener('p11-js-ready', () => {
            console.log("[WPF] Event: p11-js-ready empfangen.");
            attemptBlazorStart();
        });

        // Zünder B: Der Sicherheits-Poller (extrem wichtig für WPF/WebView2)
        // Er prüft alle 50ms, ob BEIDE Bedingungen erfüllt sind.
        const startValidator = setInterval(() => {
            attemptBlazorStart();

            // Wenn gestartet, Poller stoppen
            if (window.BlazorStarted) {
                clearInterval(startValidator);
                console.log("[WPF] Start-Poller beendet.");
            }
        }, 50);

        // Timeout-Schutz: Nach 10 Sekunden geben wir auf, um Ressourcen zu sparen
        setTimeout(() => clearInterval(startValidator), 10000);

        // --- Dein restlicher Code (WPF Interop) ---
        window.pE_Web = window.pE_Web || {};
        window.pE_Web.registerWebViewReadyCallback = function (dotNetRef) {
            window.pE_Web._readyDotNetRef = dotNetRef;
            window.pE_Web._webViewReady = true;
            dotNetRef.invokeMethodAsync("OnWebViewReady");
        };
    </script>

    <style>
        /* 1. STRUKTUR: Definiert nur das Flex-Verhältnis */
        .page {
            display: flex;
            flex-direction: column;
            height: 100vh;
            height: 100dvh;
        }

        /* 2. DER CONTENT nimmt den restlichen Platz ein */
        main {
            flex: 1;
        }

        /* 3. BLAZOR ERROR UI (Sollte hier bleiben, da es spezifisch für Blazor ist) */
        #blazor-error-ui {
            background: lightyellow;
            bottom: 0;
            box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
            box-sizing: border-box;
            display: none;
            left: 0;
            padding: 0.6rem 1.25rem 0.7rem 1.25rem;
            position: fixed;
            width: 100%;
            z-index: 10000;
        }

            #blazor-error-ui .dismiss {
                cursor: pointer;
                position: absolute;
                right: 0.75rem;
                top: 0.5rem;
            }
    </style>

</body>
</html>
```

- Recommendation: Add a splash screen here under wwwroot and define it as 'SplashScreen' in the properties during the build process.

- Add project reference to 'Shared' project

- Update your 'csproj' file as shown below:

Examle TestSolution4.Wpf.csproj:
```
<Project Sdk="Microsoft.NET.Sdk.Razor">

	<PropertyGroup>
		<OutputType>WinExe</OutputType>
		<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
		<UseWPF>true</UseWPF>
		<SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
		<ApplicationIcon>wpficon.ico</ApplicationIcon>
		<Platforms>AnyCPU;x64</Platforms>

		<Title>TestSolution4 Desktop</Title>
		<Product>TestSolution4</Product>
		<Description>TestSolution4 - Desktop Version (Store/Portable)</Description>
		<Company>https://true-perfect-code.ch/</Company>
		<Authors>D. Simic, J. Simic, A. Simic</Authors>
		<Copyright>Copyright © 2026</Copyright>
		<AssemblyVersion>1.0.0.0</AssemblyVersion>
		<FileVersion>1.0.0.0</FileVersion>
		<NeutralLanguage>en</NeutralLanguage>
		<ApplicationManifest></ApplicationManifest>
	</PropertyGroup>

	<ItemGroup>
	  <Content Include="wpficon.ico" />
	</ItemGroup>
	<ItemGroup>
		<Folder Include="Services\" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Dapper" Version="2.1.66" />
		<PackageReference Include="Microsoft.AspNetCore.Components.WebView.Wpf" Version="10.0.51" />
		<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.5" />
	</ItemGroup>

	<ItemGroup>
	  <ProjectReference Include="..\TestSolution4.Shared\TestSolution4.Shared.csproj" />
	</ItemGroup>

</Project>
```

- Update your 'MainWindow.xaml' file as shown below:

Example MainWindow.xaml:
```
<Window x:Class="TestSolution4.Wpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:blazor="clr-namespace:Microsoft.AspNetCore.Components.WebView.Wpf;assembly=Microsoft.AspNetCore.Components.WebView.Wpf"
        xmlns:shared="clr-namespace:TestSolution4.Shared;assembly=TestSolution4.Shared"
        xmlns:local="clr-namespace:TestSolution4.Wpf"
        mc:Ignorable="d"
        Height="800" 
        Width="1100"
        MinHeight="800"
        MinWidth="1100"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <blazor:BlazorWebView 
            x:Name="blazorWebView" 
            HostPage="wwwroot\index.html" 
            Services="{DynamicResource services}"
            BlazorWebViewInitializing="BlazorWebView_Initializing">
            <blazor:BlazorWebView.RootComponents>
                <blazor:RootComponent Selector="#app" ComponentType="{x:Type shared:Routes}" />
            </blazor:BlazorWebView.RootComponents>
        </blazor:BlazorWebView>
    </Grid>
</Window>
```


- Update your 'MainWindow.xaml.cs' file as shown below:

Examle MainWindow.xaml.cs:
```
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using p11.UI.Services;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.ImageOptimizer;
using BlazorCore.Services.LocalNotification;
using BlazorCore.Services.Media;
using BlazorCore.Services.Platform;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;

namespace TestSolution4.Wpf
{
    public partial class MainWindow : Window
    {
        public IServiceProvider Services { get; }

        private Microsoft.Web.WebView2.Wpf.WebView2? _webView2Cache;

        public MainWindow()
        {
            var serviceCollection = new ServiceCollection();

            // 1. WPF Blazor
            serviceCollection.AddWpfBlazorWebView();
#if DEBUG
            serviceCollection.AddBlazorWebViewDeveloperTools();
#endif

            // --- SQLite ---
            // Using the platform-specific class name "SqLite" as per your convention
            serviceCollection.AddSingleton<BlazorCore.Services.SqLite.ISqLiteBase, TestSolution4.Wpf.Services.SqLite>();

            // --- DAM ---
            serviceCollection.AddSingleton<IDamBase, DamBase>();

            // 2. Authentication
            serviceCollection.AddAuthorizationCore();
            serviceCollection.AddCascadingAuthenticationState();
            serviceCollection.AddSingleton<TestSolution4.Wpf.Services.Authentication>();
            // Die Interfaces zeigen auf die oben registrierte Instanz
            serviceCollection.AddSingleton<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<TestSolution4.Wpf.Services.Authentication>());
            serviceCollection.AddSingleton<TestSolution4.Shared.Services.Authentication.IAuthentication>(sp =>
                sp.GetRequiredService<TestSolution4.Wpf.Services.Authentication>());
            serviceCollection.AddSingleton<BlazorCore.Services.Authentication.IAuthenticationBase>(sp =>
                sp.GetRequiredService<TestSolution4.Wpf.Services.Authentication>());

            // 3. WPF-spezifische Platform Services
            serviceCollection.AddSingleton<TestSolution4.Shared.Services.Platform.IPlatform, TestSolution4.Wpf.Services.Platform>();
            serviceCollection.AddSingleton<IPlatformBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.Platform.IPlatform>());

            // 4. Global State (kann gleich bleiben, wenn keine JSInterop)
            serviceCollection.AddSingleton<TestSolution4.Shared.Services.GlobalState.IGlobalState, TestSolution4.Shared.Services.GlobalState.GlobalState>();
            serviceCollection.AddSingleton<IGlobalStateBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>());

            // Global State Initialisierung
            serviceCollection.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

            // 5. App State
            serviceCollection.AddSingleton<TestSolution4.Shared.Services.AppState.IAppState, TestSolution4.Shared.Services.AppState.AppState>();
            serviceCollection.AddSingleton<IAppStateBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>());

            // --- Initializer ---
            serviceCollection.AddSingleton<TestSolution4.Shared.Services.AppInitializer.IAppInitializer, TestSolution4.Shared.Services.AppInitializer.AppInitializer>();
            serviceCollection.AddSingleton<BlazorCore.Services.AppInitializer.IAppInitializerBase>(sp => sp.GetRequiredService<TestSolution4.Shared.Services.AppInitializer.IAppInitializer>());

            // 6. API & HttpClient
            serviceCollection.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5001/")
            });
            serviceCollection.AddSingleton<IApiBase, ApiBase>();

            // 7. UI Services
            serviceCollection.AddSingleton<IThemeService, ThemeService>();
            serviceCollection.AddSingleton<IToastService, ToastService>();
            serviceCollection.AddSingleton<IMessageBoxService, MessageBoxService>();
            serviceCollection.AddScoped<IEventStateService, EventStateService>();

            // 9. Platform specific services
            serviceCollection.AddSingleton<IImageOptimizer, TestSolution4.Wpf.Services.ImageOptimizer>();
            serviceCollection.AddSingleton<ILocalNotification, TestSolution4.Wpf.Services.LocalNotification>();
            serviceCollection.AddSingleton<IMediaBase, TestSolution4.Wpf.Services.MediaWpf>();

            // Security-Service 
            BlazorCore.Services.ServerShared.SecurityServerFactory.Register(() => new TestSolution4.Shared.Services.Security.SecurityServer());

            Services = serviceCollection.BuildServiceProvider();
            Resources.Add("services", Services);

            InitializeComponent();

            // Warte bis das Window geladen ist
            this.Loaded += MainWindow_Loaded;
            BlazorCore.HostBridge.InspectionChanged += OnInspectionChanged;

            this.Title = $"{TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName} {TestSolution4.Shared.Global.Configuration.ConfigGeneral.AppVersion}";
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Delay(250);

                var webView2 = await WaitForWebViewAsync();

                if (webView2 == null)
                {
                    Debug.WriteLine("WebView2 not ready");
                    return;
                }

                SetupPermissions(webView2.CoreWebView2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task<Microsoft.Web.WebView2.Wpf.WebView2?> WaitForWebViewAsync()
        {
            // Falls wir die WebView schon mal gefunden haben, nutzen wir den Cache
            if (_webView2Cache != null && _webView2Cache.CoreWebView2 != null)
            {
                return _webView2Cache;
            }

            for (int i = 0; i < 30; i++)
            {
                // Suche nur, wenn der Cache noch leer ist
                _webView2Cache ??= FindWebView2Control(blazorWebView);

                if (_webView2Cache != null)
                {
                    try
                    {
                        // EnsureCoreWebView2Async ist idempotent, kann also sicher 
                        // mehrfach aufgerufen werden, falls es beim ersten Mal noch nicht bereit war.
                        await _webView2Cache.EnsureCoreWebView2Async();

                        if (_webView2Cache.CoreWebView2 != null)
                        {
                            return _webView2Cache;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[WebView2 Retry {i}] {ex.Message}");
                        // Bei einem Fehler (z.B. WebView noch im Prozess-Setup) kurz warten und weitermachen
                    }
                }

                // 100ms warten vor dem nächsten Versuch
                await Task.Delay(100);
            }

            return null;
        }


        private void SetupPermissions(Microsoft.Web.WebView2.Core.CoreWebView2 coreWebView)
        {
            coreWebView.PermissionRequested += (sender, e) =>
            {
                // Kamera, Mikrofon und Geolocation erlauben
                if (e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Camera ||
                    e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Microphone ||
                    e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Geolocation)
                {
                    e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                    //Console.WriteLine($"[WebView2] Permission allowed: {e.PermissionKind}");
                }
                else
                {
                    e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Deny;
                }
            };

            // Weitere Einstellungen
            coreWebView.Settings.IsWebMessageEnabled = true;
            coreWebView.Settings.AreDefaultScriptDialogsEnabled = true;
            coreWebView.Settings.IsStatusBarEnabled = false;

            //Console.WriteLine("[WebView2] Permissions setup completed");
        }

        // Hilfsmethode um die innere WebView2 Instanz zu finden
        private Microsoft.Web.WebView2.Wpf.WebView2 FindWebView2Control(DependencyObject parent)
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Microsoft.Web.WebView2.Wpf.WebView2 webView2)
                {
                    return webView2;
                }

                var result = FindWebView2Control(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private async void OnInspectionChanged(bool enabled)
        {
            var webView2 = FindWebView2Control(blazorWebView);
            if (webView2 == null) return;

            // Auch hier: Wir müssen sicherstellen, dass die Umgebung stimmt
            // Da die Initialisierung meist schon in MainWindow_Loaded durch ist, 
            // reicht hier oft das normale await, aber sicherheitshalber:
            await webView2.EnsureCoreWebView2Async();

            webView2.CoreWebView2.Settings.AreDevToolsEnabled = enabled;

            if (enabled)
                webView2.CoreWebView2.OpenDevToolsWindow();
        }


        private void BlazorWebView_Initializing(object sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializingEventArgs e)
        {
            // Pfad in LocalAppData definieren - hier hat die MSIX-App volle Schreibrechte
            string userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName,
                "WebView2Data");

            // Sicherstellen, dass der Ordner existiert
            if (!System.IO.Directory.Exists(userDataFolder))
            {
                System.IO.Directory.CreateDirectory(userDataFolder);
            }

            // Dem Framework den Pfad mitteilen, BEVOR die WebView2 gestartet wird
            e.UserDataFolder = userDataFolder;
        }

    }
}
```

- Update your 'App.xaml.cs' file as shown below:

Examle App.xaml.cs:
```
using TestSolution4.Wpf.Services;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace TestSolution4.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Cookie used to unregister the COM server later
        private uint _cookie;
        //private const string Clsid = "D6B6F2B4-9E92-4B2E-9B65-8E4F4A9F9C01";
        private string Clsid = Shared.Global.Configuration.ConfigGeneral.CLSID;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Setup Infrastructure (Registry & Shortcut) for Unpackaged
            // pE.Utility.Appl.Aumid is used here as requested
            string aumid = Shared.Global.Configuration.ConfigGeneral.Aumid;
            NotificationManager.Initialize(aumid);

            // 2. Register the COM Server so Windows can find this running instance
            RegisterComServer();

            // 3. Handle potential Protocol/Args Launch
            // If the app was started via a Toast click while closed, 
            // the arguments will be in e.Args
            if (e.Args.Length > 0)
            {
                // Logic to handle startup arguments (e.g. deep linking)
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up the COM registration on exit
            if (_cookie != 0)
            {
                CoRevokeClassObject(_cookie);
            }

            base.OnExit(e);
        }

        private void RegisterComServer()
        {
            // We only need to manually register the COM object in Unpackaged mode.
            // In Packaged mode, the Manifest handles the activation.
            if (ShortcutHelper.IsPackaged()) return;

            try
            {
                Guid clsidGuid = new Guid(Clsid);
                var factory = new ClassFactory(); // We need a simple ClassFactory for COM

                int hr = CoRegisterClassObject(
                    ref clsidGuid,
                    factory,
                    CLSCTX_LOCAL_SERVER,
                    REGCLS_MULTIPLEUSE,
                    out _cookie);

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            catch (Exception ex)
            {
                // Use your platform.Log here
                Console.WriteLine($"Failed to register COM server: {ex.Message}");
            }
        }

        #region COM Registration P/Invoke

        private const uint CLSCTX_LOCAL_SERVER = 4;
        private const uint REGCLS_MULTIPLEUSE = 1;

        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern int CoRegisterClassObject(
            [In] ref Guid rclsid,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
            uint dwClsContext,
            uint flags,
            out uint lpdwRegister);

        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern int CoRevokeClassObject(uint dwRegister);

        #endregion
    }

}
```

- Add 'app.manifest' file as shown below:

Example app.manifest:
```
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="TestSolution4.app"/>
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>

  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>

      <!-- Windows 10 / 11 -->
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />

    </application>
  </compatibility>
	
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
      <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
    </windowsSettings>
  </application>

  <dependency>
    <dependentAssembly>
      <assemblyIdentity
          type="win32"
          name="Microsoft.Windows.Common-Controls"
          version="6.0.0.0"
          processorArchitecture="*"
          publicKeyToken="6595b64144ccf1df"
          language="*"
        />
    </dependentAssembly>
  </dependency>

</assembly>
```