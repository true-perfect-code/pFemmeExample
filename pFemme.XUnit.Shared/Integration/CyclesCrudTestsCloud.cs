//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace pFemmeExample.XUnit.Shared.Integration
//{
//    internal class CyclesCrudTestsCloud
//    {
//    }
//}

using BlazorCore;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using pFemmeExample.JsonContexts;
using pFemmeExample.Shared.Global;
using pFemmeExample.Shared.Models;
using pFemmeExample.Shared.Models.Tests;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.Platform;

namespace pFemmeExample.XUnit.Shared.Integration;

/// <summary>
/// Cloud integration tests for Cycles CRUD operations.
/// Uses existing test user "test" with password "123".
/// Requires internet connection and running WebAPI.
/// </summary>
public class CyclesCrudTestsCloud : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAppStateBase _appState;
    private readonly CyclesService _cyclesService;

    public CyclesCrudTestsCloud()
    {
        // ====================================================================
        // 1. TEST-KONFIGURATION (CLOUD)
        // ====================================================================
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
            StorageLocation = STORAGE_LOCATION.CLOUD,           // ← CLOUD
            LocalStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID,  // ← JSON_HYBRID
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
        // 2. SERVICES REGISTRIEREN
        // ====================================================================
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

        // ====================================================================
        // AOT REGISTRIERUNG FÜR KUNDEN-MODELLE
        // ====================================================================
        // Alle projekt-spezifischen Modelle hier registrieren
        // Damit das Framework (BlazorCore) sie kennt, obwohl sie nicht im Framework sind

        AotTypeRegistry.RegisterList<CyclesModel>(pFemmeJsonContext.Default.ListCyclesModel);
        AotTypeRegistry.RegisterList<CyclesCompareModel>(pFemmeJsonContext.Default.ListCyclesCompareModel);
        AotTypeRegistry.RegisterList<CyclePhasesModel>(pFemmeJsonContext.Default.ListCyclePhasesModel);
        AotTypeRegistry.RegisterList<ChartsModel>(pFemmeJsonContext.Default.ListChartsModel);


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

        // ====================================================================
        // 5. KUNDENSPEZIFISCHE QUERIES REGISTRIEREN
        // ====================================================================
        var executor = _serviceProvider.GetRequiredService<ILocalQueryExecutor>();
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();
        var appState = _serviceProvider.GetRequiredService<IAppStateBase>();

        pFemmeExample.Shared.Db.LocalDbQueryRegistry.Register(executor, localStorage, localJsonFile, appState);

        // ====================================================================
        // 6. CYCLES SERVICE HOLEN
        // ====================================================================
        _cyclesService = _serviceProvider.GetRequiredService<CyclesService>();
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    //[Fact(Skip = "Requires internet connection and running WebAPI")]
    [Fact]
    public async Task FullCrudCycle_CyclesTable_CloudStorage()
    {
        var dam = _serviceProvider.GetRequiredService<IDamBase>();

        // ====================================================================
        // 1. LOGIN AND GET TOKEN
        // ====================================================================
        var loginParams = new Dictionary<string, string>
        {
            { "@Case_", "Register>>AuthUsers" },
            { "@EmailHash", "test" },
            { "@PasswordHash", "123" },
            { "@Int__Registration", "0" },
            { "@IdP", "tpc" },
            { "@active", "1" },
            { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() },  // ← NUR Cloud, kein lokaler Versuch
        };

        var tokenData = await dam.GetTokenTPC(loginParams);

        Assert.NotNull(tokenData);
        Assert.True(string.IsNullOrEmpty(tokenData.out_err), $"Login failed: {tokenData.out_err}");
        Assert.False(string.IsNullOrEmpty(tokenData.WebApiToken), "WebApiToken should not be empty");
        Assert.False(string.IsNullOrEmpty(tokenData.UnixTS), "UnixTS should not be empty");

        // ====================================================================
        // 2. CONFIGURE APPSTATE FOR CLOUD
        // ====================================================================
        _appState.UpdateWebApiToken(tokenData.WebApiToken);
        _appState.UpdateUnixTS(tokenData.UnixTS);
        _appState.UpdateStorageLocation(STORAGE_LOCATION.CLOUD);

        // ====================================================================
        // 3. INITIALIZE LOCAL STORAGE
        // ====================================================================
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(tokenData.UnixTS);

        // ====================================================================
        // 4. CREATE
        // ====================================================================
        var newCycle = new CyclesModel
        {
            RecordDate = DateTime.Now.Date,
            bleeding = true,
            intensity = 3,
            pain = 2,
            headache = 1,
            fatigue = 1,
            nausea = 0,
            cramps = 2,
            Details = "Cloud integration test cycle - created"
        };

        var saveResult = await _cyclesService.Save(newCycle, STORAGE_LOCATION.CLOUD);

        Assert.NotNull(saveResult);
        Assert.True(string.IsNullOrEmpty(saveResult.out_err), $"Create failed: {saveResult.out_err}");
        Assert.False(string.IsNullOrEmpty(newCycle.UnixTS), "UnixTS should have been generated");

        var createdUnixTS = newCycle.UnixTS;

        // ====================================================================
        // 5. READ
        // ====================================================================
        var loadResult = await _cyclesService.Load();

        Assert.NotNull(loadResult);
        Assert.True(string.IsNullOrEmpty(loadResult.out_err), $"Load failed: {loadResult.out_err}");
        Assert.NotNull(loadResult.out_list);

        var savedCycle = loadResult.out_list.FirstOrDefault(x => x?.UnixTS == createdUnixTS);
        Assert.NotNull(savedCycle);
        Assert.Equal(newCycle.intensity, savedCycle?.intensity);
        Assert.Equal(newCycle.bleeding, savedCycle?.bleeding);
        Assert.Equal(newCycle.Details, savedCycle?.Details);

        // ====================================================================
        // 6. UPDATE
        // ====================================================================
        savedCycle!.intensity = 5;
        savedCycle.pain = 4;
        savedCycle.Details = "Cloud integration test cycle - updated";

        var updateResult = await _cyclesService.Save(savedCycle, STORAGE_LOCATION.CLOUD);

        Assert.NotNull(updateResult);
        Assert.True(string.IsNullOrEmpty(updateResult.out_err), $"Update failed: {updateResult.out_err}");

        // ====================================================================
        // 7. READ after UPDATE
        // ====================================================================
        var reloadResult = await _cyclesService.Load();
        var updatedCycle = reloadResult.out_list?.FirstOrDefault(x => x?.UnixTS == createdUnixTS);

        Assert.NotNull(updatedCycle);
        Assert.Equal(5, updatedCycle?.intensity);
        Assert.Equal(4, updatedCycle?.pain);
        Assert.Equal("Cloud integration test cycle - updated", updatedCycle?.Details);

        // ====================================================================
        // 8. DELETE
        // ====================================================================
        var deleteResult = await _cyclesService.DeleteUnixTS(createdUnixTS!, tokenData.UnixTS);

        Assert.NotNull(deleteResult);
        Assert.True(string.IsNullOrEmpty(deleteResult.out_err), $"Delete failed: {deleteResult.out_err}");

        // ====================================================================
        // 9. VERIFY DELETE
        // ====================================================================
        var finalLoadResult = await _cyclesService.Load();
        var deletedCycle = finalLoadResult.out_list?.FirstOrDefault(x => x?.UnixTS == createdUnixTS);

        Assert.Null(deletedCycle);
    }

}
