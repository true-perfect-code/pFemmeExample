using Xunit;
using Moq;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using TestSolution4.Shared.Services.SqLite;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace TestSolution4.XUnit.Capa.Integration
{
    public class SqLiteTests : IAsyncLifetime
    {
        private SqLiteWasm _sqliteService; // Wir testen die spezifische Capacitor-Klasse
        private ServiceProvider _serviceProvider;
        private readonly Mock<IJSRuntime> _mockJs = new();
        private const string TEST_USER = "capa_integration_user";

        public SqLiteTests()
        {
            var services = new ServiceCollection();
            var platformMock = new Mock<IPlatformBase>();
            var globalStateMock = new Mock<IGlobalStateBase>();
            var appStateMock = new Mock<IAppStateBase>();

            // 1. Setup Platform (Muss ungleich WASM sein für den nativen Pfad)
            platformMock.Setup(p => p.GetCurrPlatform()).Returns(PLATFORMS.ANDROID);

            // 2. Setup Config
            var config = new ConfigurationGeneral { ApplicationName = "pEngine_Capa_Test" };
            globalStateMock.Setup(g => g.ConfigGeneral).Returns(config);

            // 3. DI-Container
            services.AddSingleton(platformMock.Object);
            services.AddSingleton(globalStateMock.Object);
            services.AddSingleton(_mockJs.Object);
            services.AddSingleton<IAppStateBase>(appStateMock.Object);

            _serviceProvider = services.BuildServiceProvider();
            _sqliteService = new SqLiteWasm(_serviceProvider, _mockJs.Object);
        }

        public async Task InitializeAsync()
        {
            // --- SIMULATION DER CAPACITOR STATUS-MASCHINE ---

            // Schritt 1: initConnection (Erwartet JSON-String laut deinem Code)
            var initResponse = JsonSerializer.Serialize(new ScalarModel { out_value_bool = true });
            _mockJs.Setup(js => js.InvokeAsync<string>(It.Is<string>(s => s.Contains("initConnection")), It.IsAny<object[]>()))
                   .ReturnsAsync(initResponse);

            // Schritt 2: getDatabaseStatus (Wir simulieren den Übergang zu READY oder NEW)
            // out_value_int = 1 entspricht READY (oder 0 für NEW, je nach Enum-Definition)
            var statusResponse = JsonSerializer.Serialize(new ScalarModel { out_value_int = 1, out_value_bool = true });
            _mockJs.Setup(js => js.InvokeAsync<string>(It.Is<string>(s => s.Contains("getDatabaseStatus")), It.IsAny<object[]>()))
                   .ReturnsAsync(statusResponse);

            // Schritt 3: getVersion
            var versionResponse = JsonSerializer.Serialize(new ScalarModel { out_value_int = 1, out_value_bool = true });
            _mockJs.Setup(js => js.InvokeAsync<string>(It.Is<string>(s => s.Contains("getVersion")), It.IsAny<object[]>()))
                   .ReturnsAsync(versionResponse);

            // Schritt 4: CheckAllTablesExist (Muss true liefern, damit Initialisierung erfolgreich abschließt)
            // Da dein Code VerifyJsonToScalarModel nutzt, simulieren wir hier auch den JSON-String
            var tablesExistResponse = JsonSerializer.Serialize(new ScalarModel { out_value_bool = true });
            _mockJs.Setup(js => js.InvokeAsync<string>(It.Is<string>(s => s.Contains("scalar")), It.IsAny<object[]>()))
                   .ReturnsAsync(tablesExistResponse);

            // ACT
            var result = await _sqliteService.InitializeAsync(register: true, userAccount: TEST_USER);

            // ASSERT
            Assert.True(result.out_value_bool, $"Capa-DB-Init fehlgeschlagen: {result.out_err}");
        }

        [Fact]
        public async Task Database_Should_Handle_Timeout_If_Status_Stays_Initializing()
        {
            // ARRANGE: Wir simulieren, dass die Bridge im Status INITIALIZING (z.B. 2) hängen bleibt
            var initializingResponse = JsonSerializer.Serialize(new ScalarModel { out_value_int = 2, out_value_bool = true });

            _mockJs.Setup(js => js.InvokeAsync<string>(It.Is<string>(s => s.Contains("getDatabaseStatus")), It.IsAny<object[]>()))
                   .ReturnsAsync(initializingResponse);

            // ACT
            var result = await _sqliteService.InitializeAsync(register: true, userAccount: TEST_USER);

            // ASSERT: Dein Code hat ein Limit von 15 Retries (ca. 3 Sekunden)
            Assert.False(result.out_value_bool);
            Assert.Contains("INITIALIZING OR ERROR", result.out_err);
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
