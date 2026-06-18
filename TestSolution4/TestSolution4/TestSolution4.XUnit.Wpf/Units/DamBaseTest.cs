using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Global;
using TestSolution4.XUnit.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TestSolution4.XUnit.Wpf.Units
{
    public class DamApiTests : UnitTestsBase
    {
        public DamApiTests() : base(PLATFORMS.WINDOWS_CLIENT)
        {
        }

        private DamBase CreateDam()
        {
            return new DamBase(
                _mockApp.Object,
                _mockGlobal.Object,
                _mockPlatform.Object,
                _mockApi.Object,
                _mockSqlite.Object
            );
        }

        [Fact]
        public async Task Scalar_WhenOnline_ShouldOrchestrateHybridData()
        {
            // --- ARRANGE ---
            // Wir erstellen eine lokale Config-Instanz für den Test
            var config = new ConfigurationGeneral
            {
                EncryptDecryptWebApi = true, // Damit u.EncryptDecrypt = "1" gesetzt wird
                ApplicationName = "pEngine",
                TableSchema = "tpc"
            };
            _mockGlobal.SetupGet(g => g.ConfigGeneral).Returns(config);

            // Notwendige AppState-Werte für den Cloud-Zweig
            _mockApp.SetupGet(a => a.IsInternetConnected).Returns(true);
            _mockApp.SetupGet(a => a.WebApiToken).Returns("Unit-Test-Token");
            _mockApp.SetupGet(a => a.StorageLocation).Returns(STORAGE_LOCATION.CLOUD);

            // Mock für die Serialisierung (muss einen String liefern, sonst Abbruch)
            _mockGlobal.Setup(g => g.SerializeDictionaryTpc(It.IsAny<Dictionary<string, string>>()))
                       .Returns("{\"test\":\"json\"}");

            // API Response: Wir befüllen out_value_str, da DamBase dies auf out_mssql mappt
            var fakeApiResponse = new ScalarModel
            {
                out_value_str = "WertVomServer",
                out_err = ""
            };

            _mockApi.Setup(a => a.GetScalar(It.IsAny<UserWebApi>()))
                    .ReturnsAsync(fakeApiResponse);

            var dam = CreateDam();

            // --- ARRANGE ---
            // Wir blockieren beide Wege (API und SQLite), damit kein Fehler fliegt
            var dbPara = new Dictionary<string, string>
            {
                { "@Case_", "PureLogicTest" },
                { DB_CMD.NO_CLOUD, string.Empty },
                { DB_CMD.NO_LOCAL, string.Empty }
            };

            // --- ACT ---
            // Der Aufruf wird schnell enden, weil case_mssql und case_sqlite false sind
            var result = await dam.Scalar(dbPara);

            // --- ASSERT ---
            // Wir prüfen, ob der Zeitstempel automatisch hinzugefügt wurde
            Assert.True(dbPara.ContainsKey("@LastUpdateUnixTS"),
                "Dam sollte @LastUpdateUnixTS hinzufügen, wenn er fehlt.");

            // Wir prüfen, ob es ein valider Unix-Zeitstempel ist (Zahl als String)
            bool isLong = long.TryParse(dbPara["@LastUpdateUnixTS"], out long timestamp);
            Assert.True(isLong, "Der Zeitstempel muss eine gültige Zahl sein.");

            // Optional: Prüfen, ob der Zeitstempel "frisch" ist (nicht älter als 10 Sek)
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.True(timestamp <= now && timestamp > now - 10);
        }

        [Fact]
        public async Task Scalar_WhenOffline_ShouldOnlyQuerySqlite()
        {
            // --- ARRANGE ---
            var config = new ConfigurationGeneral { EncryptDecryptWebApi = false };
            _mockGlobal.SetupGet(g => g.ConfigGeneral).Returns(config);

            // Offline Modus
            _mockApp.SetupGet(a => a.IsInternetConnected).Returns(false);
            _mockApp.SetupGet(a => a.WebApiToken).Returns("Unit-Test-Token");

            _mockGlobal.Setup(g => g.SerializeDictionaryTpc(It.IsAny<Dictionary<string, string>>()))
                       .Returns("{\"test\":\"json\"}");

            _mockSqlite.Setup(s => s.Scalar(It.IsAny<Dictionary<string, string>>()))
                       .ReturnsAsync(new ScalarModel { out_value_str = "LokalerWert" });

            var dam = CreateDam();

            // --- ACT ---
            var result = await dam.Scalar(new Dictionary<string, string> { { "@Case_", "OfflineTest" } });

            // --- ASSERT ---
            // API darf NICHT gerufen werden, wenn IsInternetConnected = false
            _mockApi.Verify(api => api.GetScalar(It.IsAny<UserWebApi>()), Times.Never);

            // SQLite muss gerufen werden
            _mockSqlite.Verify(s => s.Scalar(It.IsAny<Dictionary<string, string>>()), Times.AtLeastOnce);
            Assert.Equal("LokalerWert", result.out_value_str);
        }
    }
}