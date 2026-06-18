using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using TestSolution4.XUnit.Common;
using TestSolution4.XUnit.Web.Units;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace TestSolution4.XUnit.Web.Integration
{
    public class SqlClientTests : UnitTestsBase
    {
        private readonly SqlClient _sut;
        private readonly GlobalStateBase _realGlobal;

        public SqlClientTests() : base(PLATFORMS.WINDOWS_CLIENT)
        {
            // 1. ServiceProvider für GlobalState aufbauen
            // Da GlobalStateBase im Konstruktor einen IServiceProvider will:
            var services = new ServiceCollection();

            // Wir fügen die Mocks aus der UnitTestsBase hinzu, falls GlobalState sie braucht
            services.AddSingleton(_mockPlatform.Object);

            var serviceProvider = services.BuildServiceProvider();

            // 2. Echtes GlobalState Objekt erstellen und initialisieren
            _realGlobal = new GlobalStateBase(serviceProvider);

            var configGeneral = new ConfigurationGeneral
            {
                ApplicationName = "TestSolution4",
                TableSchema = "dbuser_TestSolution4",
                ConnectionsServerFolder = "_Connections",
                FileExtensionJson = ".json"
            };

            // Initialisiere die Config-Werte, damit GenerateConnectionString() sie nutzen kann
            _realGlobal.GlobalInit(
                configGeneral,
                new ConfigurationWebapi(),
                new Sections()
            );

            // 3. Den SUT (SqlClient) mit dem ECHTEN GlobalState instanziieren
            // Hier wird jetzt im Konstruktor die echte GenerateConnectionString Methode aufgerufen!
            _sut = new SqlClient(_realGlobal);
        }

        [Fact]
        public void Connection_ShouldBeEstablished()
        {
            // Wenn dieser Test fehlschlägt, ist der Pfad-Algorithmus im Konstruktor 
            // im xUnit-Kontext gescheitert.
            Assert.True(_sut.isConnected,
                $"Verbindung fehlgeschlagen. Generierter ConnectionString war: '{_sut.connectionString}'");
        }

        [Fact]
        public async Task Scalar_ShouldReturnData_FromRealDatabase()
        {
            // ARRANGE
            var dbPara = new Dictionary<string, string>
            {
                { "@Case_", "TestConnection" }
            };

            // ACT
            var result = await _sut.Scalar(dbPara);

            // ASSERT
            Assert.NotNull(result);
            // Wir prüfen auf out_err, da dies dein Standard-Fehlerfeld ist
            Assert.True(string.IsNullOrEmpty(result.out_err), $"SQL Fehler: {result.out_err}");
        }

        //[Fact]
        //public async Task Reader_ShouldReturnDynamicJson()
        //{
        //    // ARRANGE
        //    // Wir nutzen einen Case, der definitiv eine Zeile (z.B. die Server-Zeit) liefert
        //    var dbPara = new Dictionary<string, string>
        //    {
        //        { "@Case_", "GetServerTime" }
        //    };

        //    // ACT
        //    var result = await _sut.Reader(dbPara);

        //    // ASSERT
        //    Assert.NotNull(result);

        //    // Prüfung des spezifischen ReaderDynamicModel
        //    Assert.True(string.IsNullOrEmpty(result.out_err), $"Reader Fehler: {result.out_err}");

        //    // Da es ein Reader ist, erwarten wir bei Erfolg ein gültiges JSON-Array oder Objekt
        //    Assert.False(string.IsNullOrEmpty(result.out_json), "Das zurückgegebene JSON darf nicht leer sein.");

        //    // Optional: Prüfen, ob es valides JSON ist (startet meist mit [ oder {)
        //    Assert.True(result.out_json!.Trim().StartsWith("[") || result.out_json!.Trim().StartsWith("{"),
        //        "out_json sollte ein gültiges JSON-Format haben.");
        //}

        //[Fact]
        //public async Task NonQuery_ShouldExecuteWithoutError()
        //{
        //    // ARRANGE
        //    // Ein Case, der z.B. nur einen Log-Eintrag schreibt oder eine Session aktualisiert
        //    var dbPara = new Dictionary<string, string>
        //    {
        //        { "@Case_", "PingSession" }
        //    };

        //    // ACT
        //    var result = await _sut.NonQuery(dbPara);

        //    // ASSERT
        //    Assert.NotNull(result);
        //    Assert.True(string.IsNullOrEmpty(result.out_err), $"NonQuery Fehler: {result.out_err}");
        //}
    }
}