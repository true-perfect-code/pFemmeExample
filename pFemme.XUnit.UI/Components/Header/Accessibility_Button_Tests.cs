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
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.Translation;
using Microsoft.JSInterop;
using Moq;
using p11.UI.Services;
using pFemmeExample.Shared.Global;
using pFemmeExample.Shared.Models.Tests;
using pFemmeExample.Shared.Pages.Common;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.EventAggregatorProject;
using pFemmeExample.Shared.Services.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace pFemmeExample.XUnit.UI.Components.Header
{
    public class Accessibility_Button_Tests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppStateBase _appState;
        private readonly string _testUserUnixTS;

        public Accessibility_Button_Tests()
        {
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
            _appState.InitializeUnixTSDeviceId("test_device_ui_header");
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
        public async Task AccessibilityButton_Click_DoesNotCrash()
        {
            using var ctx = new BunitContext();

            // Header Services
            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            var mockEventStateService = new Mock<IEventStateService>();
            ctx.Services.AddSingleton<IEventStateService>(mockEventStateService.Object);

            // Accessibility-Komponente durch Stub ersetzen (nicht rendern)
            ctx.ComponentFactories.AddStub<Accessibility>();

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Header>();
            var button = cut.Find("#accessibilityButton");
            Assert.NotNull(button);

            var exception = await Record.ExceptionAsync(() => button.ClickAsync());
            Assert.Null(exception);
        }

        [Fact]
        public void AccessibilityButton_Exists()
        {
            using var ctx = new BunitContext();

            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Header>();
            var button = cut.Find("#accessibilityButton");
            Assert.NotNull(button);
        }

        [Fact]
        public async Task AccessibilityButton_Click_OpensModal()
        {
            using var ctx = new BunitContext();

            // Header Services
            ctx.Services.AddSingleton<IAppStateBase>(_appState);
            ctx.Services.AddSingleton<IEventAggregator>(_serviceProvider.GetRequiredService<IEventAggregator>());
            ctx.Services.AddSingleton<IEventAggregatorProject>(_serviceProvider.GetRequiredService<IEventAggregatorProject>());

            var mockMessageBox = new Mock<IMessageBoxService>();
            ctx.Services.AddSingleton<IMessageBoxService>(mockMessageBox.Object);

            var mockEventStateService = new Mock<IEventStateService>();
            ctx.Services.AddSingleton<IEventStateService>(mockEventStateService.Object);

            // Accessibility-Komponente durch Stub ersetzen (nicht rendern)
            ctx.ComponentFactories.AddStub<Accessibility>();

            var cut = ctx.Render<pFemmeExample.Shared.Pages.Components.Header>();
            var button = cut.Find("#accessibilityButton");
            Assert.NotNull(button);

            await button.ClickAsync();

            cut.WaitForState(() => cut.FindAll("#accessibilityModalTitle").Any(), TimeSpan.FromSeconds(1));

            var modal = cut.Find("#accessibilityModalTitle");
            Assert.NotNull(modal);
            Assert.Equal("Accessibility Settings", modal.TextContent);
        }
    }
}