using BlazorCore.Services.AI;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Moq;
using p11.UI.Services;
using pFemmeExample.Shared.Global;
using pFemmeExample.Shared.Models.Tests;
using pFemmeExample.Shared.Pages.Components;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.EventAggregatorProject;
using pFemmeExample.Shared.Services.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace pFemmeExample.XUnit.UI.Components.Header
{
    public class Donate_Button_Tests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppStateBase _appState;
        private readonly string _testUserUnixTS;

        public Donate_Button_Tests()
        {
            // ====================================================================
            // 1. TEST-KONFIGURATION (komplett isoliert)
            // ====================================================================
            _testUserUnixTS = $"test_user_{Guid.NewGuid():N}";

            var testConfigGeneral = new ConfigurationGeneral
            {
                ApplicationName = Configuration.ConfigGeneral.ApplicationName,
                ApplicationDescription = Configuration.ConfigGeneral.ApplicationDescription,
                DefaultLanguage = Configuration.ConfigGeneral.DefaultLanguage,
                AllSupportedLanguageCodes = Configuration.ConfigGeneral.AllSupportedLanguageCodes,
                AppVersion = Configuration.ConfigGeneral.AppVersion,
                Company = Configuration.ConfigGeneral.Company,
                TableSchema = Configuration.ConfigGeneral.TableSchema,
                FileExtensionJson = Configuration.ConfigGeneral.FileExtensionJson,
                EpochYear = Configuration.ConfigGeneral.EpochYear,
                StorageLocation = STORAGE_LOCATION.LOCAL,
                LocalStorageType = LOCAL_STORAGE_TYPE.MEMORY,
                LocalStorageEncrypt = false,
                ApplicationDomain = Configuration.ConfigGeneral.ApplicationDomain,
                ApplicationUrl = Configuration.ConfigGeneral.ApplicationUrl,
                ApplicationApiUrl = Configuration.ConfigGeneral.ApplicationApiUrl,
                Aumid = Configuration.ConfigGeneral.Aumid,
                CLSID = Configuration.ConfigGeneral.CLSID,
                WindowsBorderBckGrColor = Configuration.ConfigGeneral.WindowsBorderBckGrColor,
                WindowsBorderForeColor = Configuration.ConfigGeneral.WindowsBorderForeColor,
                AllowLocalRegistration = Configuration.ConfigGeneral.AllowLocalRegistration,
                TaskDelay_Capacitor = Configuration.ConfigGeneral.TaskDelay_Capacitor,
                EncryptDecryptWebApi = false,
                IsShowingReconnectModal = Configuration.ConfigGeneral.IsShowingReconnectModal,
                DefaultFontFamily = Configuration.ConfigGeneral.DefaultFontFamily,
                ConnectionsServerFolder = Configuration.ConfigGeneral.ConnectionsServerFolder,
                SecurityConfigJsonFilename = Configuration.ConfigGeneral.SecurityConfigJsonFilename,
                PepperApp = Configuration.ConfigGeneral.PepperApp,
                PepperAppWasm = Configuration.ConfigGeneral.PepperAppWasm,
                IsInspectionEnabled = false,
                IsDebugEnabled = true,
                SetDefaultDevice = Configuration.ConfigGeneral.SetDefaultDevice,
                ErrorText = Configuration.ConfigGeneral.ErrorText,
                WebapiExceptionError = Configuration.ConfigGeneral.WebapiExceptionError,
                ShowConnectionStringByError = false,
            };

            // ====================================================================
            // 2. SERVICES REGISTRIEREN (basierend auf MainWindows)
            // ====================================================================
            var services = new ServiceCollection();

            // Mock für IJSRuntime
            var mockJSRuntime = new Mock<IJSRuntime>();
            services.AddSingleton(mockJSRuntime.Object);

            // HttpClient (für ApiBase)
            services.AddSingleton(_ => new HttpClient());

            // Core Services
            services.AddSingleton<IPlatformBase, PlatformTestMinimal>();
            services.AddSingleton<IPlatformStorageBase, PlatformStorage>();
            services.AddSingleton<IGlobalStateBase, GlobalStateBase>();
            services.AddSingleton<IAppStateBase, AppStateBase>();
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<ILogging, Logging>();
            services.AddSingleton<ITranslation, Translation>();
            services.AddSingleton<IIdGenerator, IdGenerator>();

            // Storage Services
            services.AddSingleton<ILocalStorage, LocalStorage>();
            services.AddSingleton<ILocalQueryExecutor, LocalQueryExecutor>();

            // Mock für ILocalJsonFile (statt echter Implementierung)
            var mockLocalJsonFile = new Mock<ILocalJsonFile>();
            mockLocalJsonFile.Setup(x => x.WritePhysicalFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ScalarModel { out_value_bool = true });
            mockLocalJsonFile.Setup(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string?)null);
            mockLocalJsonFile.Setup(x => x.ReadTableFilesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<string>());
            mockLocalJsonFile.Setup(x => x.DeletePhysicalFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            mockLocalJsonFile.Setup(x => x.DeleteAllFilesAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            mockLocalJsonFile.Setup(x => x.DeletePhysicalStorageAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            services.AddSingleton(mockLocalJsonFile.Object);

            // API & DAM
            services.AddSingleton<IApiBase, ApiBase>();
            services.AddSingleton<IDamBase, DamBase>();
            services.AddSingleton<IAI, AI>();

            // Project Services
            services.AddSingleton<CyclesService>();
            services.AddSingleton<TrendsService>();
            services.AddSingleton<CyclesCalService>();
            services.AddSingleton<LogicService>();
            services.AddSingleton<IEventAggregatorProject, EventAggregatorProject>();

            _serviceProvider = services.BuildServiceProvider();

            // ====================================================================
            // 3. GLOBAL STATE INITIALISIEREN
            // ====================================================================
            var globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();
            globalState.GlobalInit(testConfigGeneral, Configuration.ConfigWebapi, Catalog.Sections);

            // ====================================================================
            // 4. APPSTATE INITIALISIEREN
            // ====================================================================
            _appState = _serviceProvider.GetRequiredService<IAppStateBase>();
            _appState.GlobalInit(Catalog.Sections, Data.Dataset);
            _appState.UpdateUnixTS(_testUserUnixTS);
            _appState.InitializeUnixTSDeviceId("test_device_ui_header");
            _appState.UpdateStorageLocation(STORAGE_LOCATION.LOCAL);

            // ====================================================================
            // 5. KUNDENSPEZIFISCHE QUERIES REGISTRIEREN
            // ====================================================================
            var executor = _serviceProvider.GetRequiredService<ILocalQueryExecutor>();
            var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
            var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();
            var appState = _serviceProvider.GetRequiredService<IAppStateBase>();

            pFemmeExample.Shared.Db.LocalDbQueryRegistry.Register(executor, localStorage, localJsonFile, appState);
        }

        public void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }

        // ==================== TESTS ====================

        [Fact]
        public void DonateButton_Exists()
        {
            using var ctx = new BunitContext();

            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Header>();

            // Button über ID finden
            var button = cut.Find("#donateButton");
            Assert.NotNull(button);
        }

        [Fact]
        public async Task DonateButton_Click_DoesNotCrash()
        {
            using var ctx = new BunitContext();
            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Header>();
            var button = cut.Find("#donateButton");
            Assert.NotNull(button);

            var exception = await Record.ExceptionAsync(() => button.ClickAsync());
            Assert.Null(exception);
        }

        [Fact]
        public async Task DonateButton_Click_FiresModalOpenEvent()
        {
            using var ctx = new BunitContext();
            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());

            var realEventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
            ctx.Services.AddSingleton<IEventAggregator>(realEventAggregator);

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            ModalNativeEventArgs? receivedEventArgs = null;
            Func<ModalNativeEventArgs, Task> handler = async (e) =>
            {
                receivedEventArgs = e;
                await Task.CompletedTask;
            };
            realEventAggregator.OnModalNativeChanged += handler;

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Header>();
            var button = cut.Find("#donateButton");
            Assert.NotNull(button);

            button.Click();

            Assert.NotNull(receivedEventArgs);
            Assert.Equal(ModalNativeType.Donate, receivedEventArgs.Modal);
            Assert.True(receivedEventArgs.IsOpen);

            realEventAggregator.OnModalNativeChanged -= handler;
        }

        [Fact]
        public async Task Modals_WhenDonateEventFired_OpensDonateModal()
        {
            // ====================================================================
            // ARRANGE
            // ====================================================================
            using var ctx = new BunitContext();

            // 1. AUTHORISIERUNG (damit <AuthorizeView> funktioniert)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var authState = Task.FromResult(new AuthenticationState(user));
            var authStateProvider = new FakeAuthenticationStateProvider(authState);
            ctx.Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
            ctx.Services.AddAuthorizationCore();
            ctx.Services.AddScoped<IAuthorizationService, DefaultAuthorizationService>();

            // 2. P11 SERVICES
            var mockEventStateService = new Mock<IEventStateService>();
            ctx.Services.AddSingleton<IEventStateService>(mockEventStateService.Object);

            // 3. CORE SERVICES (aus deinem bestehenden Container)
            ctx.Services.AddSingleton<IGlobalStateBase>(_serviceProvider.GetRequiredService<IGlobalStateBase>());
            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());

            // 4. EVENT AGGREGATOR (ECHT – kein Mock!)
            var realEventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
            ctx.Services.AddSingleton<IEventAggregator>(realEventAggregator);

            // 5. EVENT AGGREGATOR PROJECT (ECHT – für Modals)
            var realEventAggregatorProject = _serviceProvider.GetRequiredService<IEventAggregatorProject>();
            ctx.Services.AddSingleton<IEventAggregatorProject>(realEventAggregatorProject);

            // 6. MESSAGE BOX (Mock – Browser-Dialog)
            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            // 7. TOAST SERVICE (Mock)
            var mockToastService = new Mock<IToastService>();
            ctx.Services.AddSingleton<IToastService>(mockToastService.Object);

            // 8. OTP (Mock)
            var mockOtp = new Mock<IOtpBase>();
            ctx.Services.AddSingleton<IOtpBase>(mockOtp.Object);

            // 9. PLATFORM BASE (wichtig für Donate Komponente!)
            ctx.Services.AddSingleton<IPlatformBase>(_serviceProvider.GetRequiredService<IPlatformBase>());

            // 10. MODALS RENDERN (mit CascadingAuthenticationState)
            var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Modals>());

            // ====================================================================
            // ACT
            // ====================================================================
            // Event manuell feuern (simuliert Header-Klick)
            await realEventAggregator.ChangeModalNativeAsync(ModalNativeType.Donate, true);

            // Kurze Verzögerung für StateHasChanged
            await Task.Delay(100);

            // ====================================================================
            // ASSERT
            // ====================================================================
            // Warte auf DOM-Änderung (max. 1 Sekunde)
            cut.WaitForState(() => cut.FindAll("#modalTitle1Donate").Any(), TimeSpan.FromSeconds(1));

            // Modal existiert und hat den richtigen Titel
            var modal = cut.Find("#modalTitle1Donate");
            Assert.NotNull(modal);
            Assert.Equal("Donate", modal.TextContent);
        }

        // Hilfsklasse für Authentication
        public class FakeAuthenticationStateProvider : AuthenticationStateProvider
        {
            private readonly Task<AuthenticationState> _authenticationState;

            public FakeAuthenticationStateProvider(Task<AuthenticationState> authenticationState)
            {
                _authenticationState = authenticationState;
            }

            public override Task<AuthenticationState> GetAuthenticationStateAsync()
            {
                return _authenticationState;
            }
        }
    }
}