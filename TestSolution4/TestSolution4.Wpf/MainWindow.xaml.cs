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

            // --- SQLite ---
            // Using the platform-specific class name "SqLite" as per your convention
            serviceCollection.AddSingleton<BlazorCore.Services.SqLite.ISqLiteBase, TestSolution4.Wpf.Services.SqLite>();
            // --- Memory ---
            serviceCollection.AddSingleton<BlazorCore.Services.MemoryStorage.IMemoryStorageBase, BlazorCore.Services.MemoryStorage.MemoryStorageBase>();
            // --- Json ---
            serviceCollection.AddSingleton<BlazorCore.Services.JsonHybridStorage.IJsonHybridStorageBase, TestSolution4.Wpf.Services.JsonHybridStorage>();

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
            serviceCollection.AddSingleton<IMediaBase, TestSolution4.Shared.Pages.Common.Media.Media>();

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