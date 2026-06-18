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

namespace pFemmeExample.XUnit.UI.Components.Footer
{
    public class Synchronize_Button_Tests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppStateBase _appState;
        private readonly string _testUserUnixTS;

        public Synchronize_Button_Tests()
        {
            _testUserUnixTS = $"test_user_{Guid.NewGuid():N}";

            var testConfigGeneral = new ConfigurationGeneral
            {
                // ... deine Konfiguration (wie in AskAI_Button_Tests)
            };

            var services = new ServiceCollection();

            var mockJSRuntime = new Mock<IJSRuntime>();
            services.AddSingleton(mockJSRuntime.Object);

            services.AddSingleton(_ => new HttpClient());

            services.AddSingleton<IPlatformBase, PlatformTestMinimal>();
            services.AddSingleton<IPlatformStorageBase, PlatformStorage>();
            services.AddSingleton<IGlobalStateBase, GlobalStateBase>();
            services.AddSingleton<IAppStateBase, AppStateBase>();
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<ILogging, Logging>();
            services.AddSingleton<ITranslation, Translation>();
            services.AddSingleton<IIdGenerator, IdGenerator>();

            services.AddSingleton<ILocalStorage, LocalStorage>();
            services.AddSingleton<ILocalQueryExecutor, LocalQueryExecutor>();

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

            services.AddSingleton<IApiBase, ApiBase>();
            services.AddSingleton<IDamBase, DamBase>();
            services.AddSingleton<IAI, AI>();

            services.AddSingleton<CyclesService>();
            services.AddSingleton<TrendsService>();
            services.AddSingleton<CyclesCalService>();
            services.AddSingleton<LogicService>();
            services.AddSingleton<IEventAggregatorProject, EventAggregatorProject>();

            _serviceProvider = services.BuildServiceProvider();

            var globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();
            globalState.GlobalInit(testConfigGeneral, Configuration.ConfigWebapi, Catalog.Sections);

            _appState = _serviceProvider.GetRequiredService<IAppStateBase>();
            _appState.GlobalInit(Catalog.Sections, Data.Dataset);
            _appState.UpdateUnixTS(_testUserUnixTS);
            _appState.InitializeUnixTSDeviceId("test_device_ui_footer");
            _appState.UpdateStorageLocation(STORAGE_LOCATION.LOCAL);

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

        [Fact]
        public void SyncButton_Exists()
        {
            using var ctx = new BunitContext();

            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());
            ctx.Services.AddSingleton<IGlobalStateBase>(_serviceProvider.GetRequiredService<IGlobalStateBase>());  // Neu!
            ctx.Services.AddSingleton<IPlatformBase>(_serviceProvider.GetRequiredService<IPlatformBase>());       // Neu!

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            // Wichtig: StorageLocation auf CLOUD_LOCAL setzen, damit Sync-Button sichtbar ist
            _appState.UpdateStorageLocation(STORAGE_LOCATION.CLOUD_LOCAL);

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Footer>();
            var button = cut.Find("#syncButton");
            Assert.NotNull(button);
        }

        [Fact]
        public async Task SyncButton_Click_DoesNotCrash()
        {
            using var ctx = new BunitContext();

            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());
            ctx.Services.AddSingleton<IGlobalStateBase>(_serviceProvider.GetRequiredService<IGlobalStateBase>());
            ctx.Services.AddSingleton<IPlatformBase>(_serviceProvider.GetRequiredService<IPlatformBase>());  // Neu!

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            // Wichtig: StorageLocation auf CLOUD_LOCAL setzen, damit Sync-Button sichtbar ist
            _appState.UpdateStorageLocation(STORAGE_LOCATION.CLOUD_LOCAL);

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Footer>();
            var button = cut.Find("#syncButton");
            Assert.NotNull(button);

            var exception = await Record.ExceptionAsync(() => button.ClickAsync());
            Assert.Null(exception);
        }

        [Fact]
        public async Task SyncButton_Click_FiresModalOpenEvent()
        {
            using var ctx = new BunitContext();

            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IGlobalStateBase>(_serviceProvider.GetRequiredService<IGlobalStateBase>());  // Neu!
            ctx.Services.AddSingleton<IPlatformBase>(_serviceProvider.GetRequiredService<IPlatformBase>());

            var realEventAggregatorProject = _serviceProvider.GetRequiredService<IEventAggregatorProject>();
            ctx.Services.AddSingleton<IEventAggregatorProject>(realEventAggregatorProject);

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            // Wichtig: StorageLocation auf CLOUD_LOCAL setzen, damit Sync-Button sichtbar ist
            _appState.UpdateStorageLocation(STORAGE_LOCATION.CLOUD_LOCAL);

            ModalNativeProjectEventArgs? receivedEventArgs = null;
            Func<ModalNativeProjectEventArgs, Task> handler = async (e) =>
            {
                receivedEventArgs = e;
                await Task.CompletedTask;
            };
            realEventAggregatorProject.OnModalNativeChanged += handler;

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Footer>();
            var button = cut.Find("#syncButton");
            Assert.NotNull(button);

            await button.ClickAsync();

            Assert.NotNull(receivedEventArgs);
            Assert.Equal(ModalNativeProjectType.Synchronization, receivedEventArgs.Modal);
            Assert.True(receivedEventArgs.IsOpen);

            realEventAggregatorProject.OnModalNativeChanged -= handler;
        }

        [Fact]
        public async Task Modals_WhenSyncEventFired_OpensSyncModal()
        {
            using var ctx = new BunitContext();

            // Authentication
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

            // P11 Services
            var mockEventStateService = new Mock<IEventStateService>();
            ctx.Services.AddSingleton<IEventStateService>(mockEventStateService.Object);

            // Core Services
            ctx.Services.AddSingleton<IGlobalStateBase>(_serviceProvider.GetRequiredService<IGlobalStateBase>());
            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());

            // Event Aggregator Project (echt)
            var realEventAggregatorProject = _serviceProvider.GetRequiredService<IEventAggregatorProject>();
            ctx.Services.AddSingleton<IEventAggregatorProject>(realEventAggregatorProject);

            // MessageBox Mock
            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            // Toast & OTP Mocks
            var mockToastService = new Mock<IToastService>();
            ctx.Services.AddSingleton<IToastService>(mockToastService.Object);
            var mockOtp = new Mock<IOtpBase>();
            ctx.Services.AddSingleton<IOtpBase>(mockOtp.Object);

            // Platform Base
            ctx.Services.AddSingleton<IPlatformBase>(_serviceProvider.GetRequiredService<IPlatformBase>());

            // Services für Synchronization Komponente
            ctx.Services.AddSingleton<LogicService>(_serviceProvider.GetRequiredService<LogicService>());
            ctx.Services.AddSingleton<CyclesService>(_serviceProvider.GetRequiredService<CyclesService>());

            // Modals rendern
            var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
                .AddChildContent<Modals>());

            // Zugriff auf die Modals-Instanz
            var modalsComponent = cut.FindComponent<Modals>();
            var modalId = modalsComponent.Instance._home.IdSynchronization.ToString();

            await realEventAggregatorProject.ChangeModalNativeAsync(ModalNativeProjectType.Synchronization, true);
            await Task.Delay(100);

            // Warte bis das Modal über seine ID im DOM erscheint (nicht über Text!)
            cut.WaitForState(() => cut.FindAll($"[id='{modalId}']").Any(), TimeSpan.FromSeconds(1));

            var modal = cut.Find($"[id='{modalId}']");
            Assert.NotNull(modal);
        }

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