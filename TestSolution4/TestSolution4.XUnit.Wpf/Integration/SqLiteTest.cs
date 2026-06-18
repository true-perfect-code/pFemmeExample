using Xunit;
using Moq;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using TestSolution4.Wpf.Services;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.GlobalState;
// Füge hier den Namespace für deine ConfigurationGeneral Klasse hinzu, falls nötig
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestSolution4.XUnit.Wpf.Integration
{
    public class SqLiteTest : IAsyncLifetime
    {
        private SqLite _sqliteService;
        private ServiceProvider _serviceProvider;
        private const string TEST_USER = "xunit_integration_user";

        public SqLiteTest()
        {
            var services = new ServiceCollection();

            // 1. Mocks erstellen
            var platformMock = new Mock<IPlatformBase>();
            var globalStateMock = new Mock<IGlobalStateBase>();
            var jsRuntimeMock = new Mock<IJSRuntime>();
            var appStateMock = new Mock<IAppStateBase>();

            // 2. Configuration mit 'init' Eigenschaften korrekt erstellen
            // Wir nutzen hier deine Vorgabe für die Test-Umgebung
            var config = new ConfigurationGeneral
            {
                EncryptDecryptWebApi = true,
                ApplicationName = "pEngine_Test", // Der Name, der in DbName landet
                TableSchema = "testsolution4"
            };

            // Den Mock so konfigurieren, dass er dieses fertige Objekt zurückgibt
            globalStateMock.Setup(g => g.ConfigGeneral).Returns(config);

            // 3. Registrierung im DI-Container
            services.AddSingleton(platformMock.Object);
            services.AddSingleton(globalStateMock.Object);
            services.AddSingleton(jsRuntimeMock.Object);

            // Beide Varianten des AppState auf den gleichen Mock mappen
            services.AddSingleton<IAppStateBase>(appStateMock.Object);
            // Falls dein Code an anderer Stelle IAppState (ohne Base) verlangt:
            // services.AddSingleton<IAppState>(sp => (IAppState)appStateMock.Object);

            // 4. Den ServiceProvider bauen
            _serviceProvider = services.BuildServiceProvider();

            // 5. Den ECHTEN SQLite-Service instanziieren
            // Hier wird im Basis-Konstruktor nun DbName = "pEngine_Test" gesetzt
            _sqliteService = new SqLite(_serviceProvider, jsRuntimeMock.Object);
        }

        public async Task InitializeAsync()
        {
            // Initialisierung triggert das CreateTablesScript
            var result = await _sqliteService.InitializeAsync(register: true, userAccount: TEST_USER);
            Assert.True(result.out_value_bool, $"DB-Initialisierung fehlgeschlagen: {result.out_err}");
        }

        public async Task DisposeAsync()
        {
            // Datenbank löschen, um den Test-Ordner sauber zu halten
            await _sqliteService.Scalar(new Dictionary<string, string>
            {
                { "@Case_", "deleteDatabase" }
            });
            await _serviceProvider.DisposeAsync();
        }

        [Fact]
        public async Task Database_Should_Create_Framework_Tables_On_Initialize()
        {
            // Arrange: Die 4 Standardtabellen des Frameworks
            string frameworkTables = "AuthUsers,AppParameter,AuthUsersExtend,SharingUsers";

            // Act: Wir nutzen den Case 'CheckMultipleTablesExist'
            var result = await _sqliteService.Scalar(new Dictionary<string, string>
            {
                { "@Case_", "CheckMultipleTablesExist" },
                { "@TableList", frameworkTables },
                { BlazorCore.Services.Dam.DB_CMD.NO_CLOUD, string.Empty }
            });

            // Assert
            // 1. War der Aufruf technisch erfolgreich?
            //Assert.True(result.out_value_bool, $"SQLite-Fehler: {result.out_err}");

            // 2. Wurden alle 4 Tabellen in der sqlite_master gefunden?
            // Der Case 'CheckMultipleTablesExist' gibt die Anzahl in 'out_value_int' zurück
            Assert.Equal(4, result.out_value_int);

            // Optional: Logge das Ergebnis für den Test-Output
            //await _appState.Log($"[XUnit] Framework-Integrationstest: {result.out_value_int} von 4 Tabellen erfolgreich verifiziert.");
        }
    }
}