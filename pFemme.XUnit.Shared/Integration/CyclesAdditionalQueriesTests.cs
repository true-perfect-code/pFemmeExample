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
using pFemmeExample.Shared.Global;
using pFemmeExample.Shared.Models;
using pFemmeExample.Shared.Models.Tests;
using pFemmeExample.Shared.Services.Components;
using pFemmeExample.Shared.Services.Platform;


namespace pFemmeExample.XUnit.Shared.Integration;

public class CyclesAdditionalQueriesTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAppStateBase _appState;
    private readonly CyclesService _cyclesService;
    private readonly TrendsService _trendsService;
    private readonly CyclesCalService _cyclesCalService;
    private readonly string _testUserUnixTS;

    public CyclesAdditionalQueriesTests()
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
        _appState.InitializeUnixTSDeviceId("test_device_integration_test");
        _appState.UpdateStorageLocation(STORAGE_LOCATION.LOCAL);

        // ====================================================================
        // 5. KUNDENSPEZIFISCHE QUERIES REGISTRIEREN
        // ====================================================================
        var executor = _serviceProvider.GetRequiredService<ILocalQueryExecutor>();
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();
        var appState = _serviceProvider.GetRequiredService<IAppStateBase>();

        pFemmeExample.Shared.Db.LocalDbQueryRegistry.Register(executor, localStorage, localJsonFile, appState);

        // ====================================================================
        // 6. SERVICES HOLEN
        // ====================================================================
        _cyclesService = _serviceProvider.GetRequiredService<CyclesService>();
        _cyclesCalService = _serviceProvider.GetRequiredService<CyclesCalService>();
        _trendsService = _serviceProvider.GetRequiredService<TrendsService>();
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Fact]
    public async Task SelectDayCycle_ReturnsCorrectDay()
    {
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(_testUserUnixTS);

        // CREATE Testdaten
        var targetDate = new DateTime(2024, 5, 15);
        var expectedCycle = new CyclesModel
        {
            RecordDate = targetDate,
            bleeding = true,
            intensity = 4,
            pain = 3,
            headache = 2,
            fatigue = 1,
            nausea = 0,
            cramps = 2,
            Details = "Test für SelectDay"
        };

        var anotherCycle = new CyclesModel
        {
            RecordDate = new DateTime(2024, 5, 20),
            bleeding = false,
            intensity = 2,
            pain = 1,
            Details = "Anderer Tag"
        };

        await _cyclesService.Save(expectedCycle, STORAGE_LOCATION.LOCAL);
        await _cyclesService.Save(anotherCycle, STORAGE_LOCATION.LOCAL);

        // ACT - über CyclesCalService.LoadDay (wie im Original)
        var result = await _cyclesCalService.LoadDay(targetDate);

        // ASSERT
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.out_err), $"LoadDay failed: {result.out_err}");
        Assert.NotNull(result.out_list);
        Assert.Single(result.out_list);

        var loadedCycle = result.out_list.First();
        Assert.Equal(expectedCycle.UnixTS, loadedCycle?.UnixTS);
        Assert.Equal(expectedCycle.bleeding, loadedCycle?.bleeding);
        Assert.Equal(expectedCycle.intensity, loadedCycle?.intensity);
        Assert.Equal(expectedCycle.pain, loadedCycle?.pain);
        Assert.Equal(expectedCycle.Details, loadedCycle?.Details);
    }

    [Fact]
    public async Task SelectDayCycle_ReturnsEmptyList_WhenNoDataForDate()
    {
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(_testUserUnixTS);

        // Arrange: Kein Cycle für den 10. Mai
        var existingCycle = new CyclesModel
        {
            RecordDate = new DateTime(2024, 5, 15),
            bleeding = true,
            intensity = 3
        };
        await _cyclesService.Save(existingCycle, STORAGE_LOCATION.LOCAL);

        // ACT
        var result = await _cyclesCalService.LoadDay(new DateTime(2024, 5, 10));

        // ASSERT
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.out_err));
        Assert.NotNull(result.out_list);
        Assert.Empty(result.out_list);
    }

    [Fact]
    public async Task SelectTrendsBleeding_ReturnsCorrectTrends()
    {
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(_testUserUnixTS);

        // CREATE Testdaten
        var cycles = new List<CyclesModel>
        {
            new() { RecordDate = new DateTime(2024, 1, 1), bleeding = true, intensity = 3 },
            new() { RecordDate = new DateTime(2024, 1, 2), bleeding = true, intensity = 2 },
            new() { RecordDate = new DateTime(2024, 1, 3), bleeding = false, intensity = 0 },
            new() { RecordDate = new DateTime(2024, 2, 1), bleeding = true, intensity = 4 },
            new() { RecordDate = new DateTime(2024, 2, 2), bleeding = true, intensity = 3 },
            new() { RecordDate = new DateTime(2024, 2, 3), bleeding = false, intensity = 0 }
        };

        foreach (var cycle in cycles)
        {
            await _cyclesService.Save(cycle, STORAGE_LOCATION.LOCAL);
        }

        // ACT - über TrendsService.LoadChartBleeding (wie im Original)
        var result = await _trendsService.LoadChartBleeding();

        // ASSERT
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.out_err), $"LoadChartBleeding failed: {result.out_err}");
        Assert.NotNull(result.out_list);
        Assert.Equal(4, result.out_list.Count);
    }

    [Fact]
    public async Task SelectTrendsPain_ReturnsCorrectTrends()
    {
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(_testUserUnixTS);

        // CREATE Testdaten
        var cycles = new List<CyclesModel>
        {
            new() { RecordDate = new DateTime(2024, 1, 1), pain = 1 },
            new() { RecordDate = new DateTime(2024, 1, 2), pain = 2 },
            new() { RecordDate = new DateTime(2024, 1, 3), pain = 3 },
            new() { RecordDate = new DateTime(2024, 2, 1), pain = 2 },
            new() { RecordDate = new DateTime(2024, 2, 2), pain = 1 },
            new() { RecordDate = new DateTime(2024, 2, 3), pain = 0 }
        };

        foreach (var cycle in cycles)
        {
            await _cyclesService.Save(cycle, STORAGE_LOCATION.LOCAL);
        }

        // ACT - über TrendsService.LoadChartPain (wie im Original)
        var result = await _trendsService.LoadChartPain();

        // ASSERT
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.out_err), $"LoadChartPain failed: {result.out_err}");
        Assert.NotNull(result.out_list);
        Assert.Equal(5, result.out_list.Count);
    }

    [Fact]
    public async Task SelectTrendsBleeding_ReturnsEmptyList_WhenNoBleedingData()
    {
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(_testUserUnixTS);

        // CREATE: Nur Zyklen ohne Blutungen
        var cycles = new List<CyclesModel>
        {
            new() { RecordDate = new DateTime(2024, 1, 1), bleeding = false, intensity = 0 },
            new() { RecordDate = new DateTime(2024, 1, 2), bleeding = false, intensity = 0 }
        };

        foreach (var cycle in cycles)
        {
            await _cyclesService.Save(cycle, STORAGE_LOCATION.LOCAL);
        }

        // ACT
        var result = await _trendsService.LoadChartBleeding();

        // ASSERT
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.out_err));
        Assert.NotNull(result.out_list);
        Assert.Empty(result.out_list);
    }

    [Fact]
    public async Task SelectTrendsPain_ReturnsEmptyList_WhenNoPainData()
    {
        var localStorage = _serviceProvider.GetRequiredService<ILocalStorage>();
        await localStorage.InitializeAsync(_testUserUnixTS);

        // CREATE: Nur Zyklen mit pain = 0
        var cycles = new List<CyclesModel>
        {
            new() { RecordDate = new DateTime(2024, 1, 1), pain = 0 },
            new() { RecordDate = new DateTime(2024, 1, 2), pain = 0 }
        };

        foreach (var cycle in cycles)
        {
            await _cyclesService.Save(cycle, STORAGE_LOCATION.LOCAL);
        }

        // ACT
        var result = await _trendsService.LoadChartPain();

        // ASSERT
        Assert.NotNull(result);
        Assert.True(string.IsNullOrEmpty(result.out_err));
        Assert.NotNull(result.out_list);
        Assert.Empty(result.out_list);
    }
}