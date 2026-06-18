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
using Microsoft.Extensions.DependencyInjection;
using p11.UI.Services;
using pFemmeExample.JsonContexts;
using pFemmeExample.Shared.Models;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.Email;
using pFemmeExample.Shared.Services.EventAggregatorProject;
using pFemmeExample.Wpf.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;

namespace pFemmeExample.Wpf
{
    public partial class MainWindow : Window
    {
        public IServiceProvider Services { get; }

        private Microsoft.Web.WebView2.Wpf.WebView2? _webView2Cache;

        public MainWindow()
        {
            var serviceCollection = new ServiceCollection();

            // WPF Blazor
            serviceCollection.AddWpfBlazorWebView();       

            // ========================================================================
            // CORE SERVICES
            // ========================================================================

            // Platform & Storage
            serviceCollection.AddSingleton<IPlatformBase, Platform>();
            serviceCollection.AddSingleton<IPlatformStorageBase, PlatformStorage>();

            // Global State
            serviceCollection.AddSingleton<IGlobalStateBase, GlobalStateBase>();
            serviceCollection.AddSingleton<IGlobalStateInitializer, GlobalStateInitializer>();

            // App State
            serviceCollection.AddSingleton<IAppStateBase, AppStateBase>();

            // App Initializer
            serviceCollection.AddSingleton<IAppInitializerBase, AppInitializerBase>();

            // ========================================================================
            // INFRASTRUCTURE SERVICES
            // ========================================================================
            serviceCollection.AddSingleton<IEventAggregator, EventAggregator>();
            serviceCollection.AddSingleton<IEventAggregatorProject, EventAggregatorProject>();
            serviceCollection.AddSingleton<ILogging, Logging>();
            serviceCollection.AddSingleton<ITranslation, Translation>();
            serviceCollection.AddSingleton<IIdGenerator, IdGenerator>();
            serviceCollection.AddSingleton<IApiBase, ApiBase>();
            serviceCollection.AddSingleton<IDamBase, DamBase>();

            // ========================================================================
            // STORAGE SERVICES
            // ========================================================================
            serviceCollection.AddSingleton<ILocalStorage, LocalStorage>();
            serviceCollection.AddSingleton<ILocalQueryExecutor, LocalQueryExecutor>();
            serviceCollection.AddSingleton<ILocalJsonFile, LocalJsonFile>(); // Shared service for local JSON file handling (not for WEB only WASM!)

            // ========================================================================
            // AUTHENTICATION
            // ========================================================================
            serviceCollection.AddAuthorizationCore();
            serviceCollection.AddCascadingAuthenticationState();
            serviceCollection.AddSingleton<Authentication>();
            serviceCollection.AddSingleton<IAuthenticationBase>(sp => sp.GetRequiredService<Authentication>());
            serviceCollection.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<Authentication>());

            // ========================================================================
            // APPLICATION SERVICES
            // ========================================================================
            serviceCollection.AddSingleton<IOtpBase, Shared.Services.Otp.Otp>();
            serviceCollection.AddSingleton<IEmailBase, Email>();

            // ========================================================================
            // UI SERVICES (p11)
            // ========================================================================
            serviceCollection.AddSingleton<IThemeService, ThemeService>();
            serviceCollection.AddSingleton<IToastService, ToastService>();
            serviceCollection.AddSingleton<IMessageBoxService, MessageBoxService>();
            serviceCollection.AddScoped<IEventStateService, EventStateService>();

            // ========================================================================
            // PROJECT CRUD
            // ========================================================================
            serviceCollection.AddSingleton<CyclesService>();
            serviceCollection.AddSingleton<TrendsService>();
            serviceCollection.AddSingleton<CyclesCalService>();
            serviceCollection.AddSingleton<CyclePhasesService>();
            serviceCollection.AddSingleton<LogicService>();
            serviceCollection.AddSingleton<IAIService, AIService>();

            // ========================================================================
            // HTTP CLIENT
            // ========================================================================

            serviceCollection.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5001/")
            });

            // Security-Service 
            BlazorCore.Services.ServerShared.SecurityServerFactory.Register(() => new pFemmeExample.Shared.Services.Security.SecurityServer());

            Services = serviceCollection.BuildServiceProvider();
            Resources.Add("services", Services);

            // ========================================================================
            // AOT REGISTRIERUNG FÜR KUNDEN-MODELLE
            // ========================================================================
            // Alle projekt-spezifischen Modelle hier registrieren
            // Damit das Framework (BlazorCore) sie kennt, obwohl sie nicht im Framework sind

            // Registriere CyclesModel und seine Liste
            AotTypeRegistry.RegisterList<CyclesModel>(pFemmeJsonContext.Default.ListCyclesModel);
            AotTypeRegistry.RegisterList<CyclesCompareModel>(pFemmeJsonContext.Default.ListCyclesCompareModel);
            AotTypeRegistry.RegisterList<CyclePhasesModel>(pFemmeJsonContext.Default.ListCyclePhasesModel);
            AotTypeRegistry.RegisterList<ChartsModel>(pFemmeJsonContext.Default.ListChartsModel);

            // Weitere Modelle hier hinzufügen, falls nötig
            // AotTypeRegistry.RegisterList<MeinAnderesModel>(pFemmeJsonContext.Default.ListMeinAnderesModel);

            // ========================================================================

            InitializeComponent();

            // Wait until the window has loaded
            this.Loaded += MainWindow_Loaded;
            BlazorCore.HostBridge.InspectionChanged += OnInspectionChanged;

            this.Title = $"{pFemmeExample.Shared.Global.Configuration.ConfigGeneral.ApplicationName} {pFemmeExample.Shared.Global.Configuration.ConfigGeneral.AppVersion}";
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Small delay to allow WebView2 to finish loading
                await Task.Delay(50);

                var webView2 = await WaitForWebViewAsync();

                if (webView2 == null)
                {
                    return;
                }

                SetupPermissions(webView2.CoreWebView2);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error: {ex.Message}");
                Trace.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task<Microsoft.Web.WebView2.Wpf.WebView2?> WaitForWebViewAsync()
        {
            // If we've already found the WebView, we'll use the cache
            if (_webView2Cache != null && _webView2Cache.CoreWebView2 != null)
            {
                return _webView2Cache;
            }

            for (int i = 0; i < 30; i++)
            {
                // Search only if the cache is still empty
                _webView2Cache ??= FindWebView2Control(blazorWebView);

                if (_webView2Cache != null)
                {
                    try
                    {
                        // EnsureCoreWebView2Async is idempotent, so it can safely 
                        // be called multiple times if it wasn't ready the first time.
                        await _webView2Cache.EnsureCoreWebView2Async();

                        if (_webView2Cache.CoreWebView2 != null)
                        {
                            return _webView2Cache;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Debug.WriteLine($"[WebView2 Retry {i}] {ex.Message}");
                        // If an error occurs (e.g., WebView is still in the process setup), wait a moment and continue
                        Trace.WriteLine($"Error: {ex.Message}");
                    }
                }

                // Wait 100 ms before the next attempt
                await Task.Delay(100);
            }

            return null;
        }


        private void SetupPermissions(Microsoft.Web.WebView2.Core.CoreWebView2 coreWebView)
        {
            coreWebView.PermissionRequested += (sender, e) =>
            {
                // Allow access to the camera, microphone, and geolocation
                if (e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Camera ||
                    e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Microphone ||
                    e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Geolocation)
                {
                    e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                }
                else
                {
                    e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Deny;
                }
            };

            // Additional settings
            coreWebView.Settings.IsWebMessageEnabled = true;
            coreWebView.Settings.AreDefaultScriptDialogsEnabled = true;
            coreWebView.Settings.IsStatusBarEnabled = false;
        }

        // Helper method to find the internal WebView2 instance
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
            try
            {
                var webView2 = blazorWebView.WebView;

                if (webView2 == null)
                {
                    Trace.WriteLine("WebView2 not available");
                    return;
                }

                await webView2.EnsureCoreWebView2Async();

                if (webView2.CoreWebView2 != null)
                {
                    webView2.CoreWebView2.Settings.AreDevToolsEnabled = enabled;

                    if (enabled)
                    {
                        webView2.CoreWebView2.OpenDevToolsWindow();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to open DevTools: {ex}");
            }
        }


        private void BlazorWebView_Initializing(object sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializingEventArgs e)
        {
            // Define a path in LocalAppData—the MSIX app has full write permissions here
            string userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                pFemmeExample.Shared.Global.Configuration.ConfigGeneral.ApplicationName,
                "WebView2Data");

            // Make sure the folder exists
            if (!System.IO.Directory.Exists(userDataFolder))
            {
                System.IO.Directory.CreateDirectory(userDataFolder);
            }

            // Tell the framework the path BEFORE starting WebView2
            e.UserDataFolder = userDataFolder;
        }

    }
}