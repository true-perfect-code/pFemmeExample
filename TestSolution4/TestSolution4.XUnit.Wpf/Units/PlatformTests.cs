using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Otp;
using BlazorCore.Services.SqlClient;
using TestSolution4.Wpf.Services;
using TestSolution4.XUnit.Common;
using Moq;
using Xunit;

namespace TestSolution4.XUnit.Wpf.Units
{
    public class PlatformTests : UnitTestsBase
    {
        private readonly Platform _sut; // System Under Test

        public PlatformTests() : base(BlazorCore.Services.Platform.PLATFORMS.WINDOWS_CLIENT)
        {
            // 1. Erstelle die echte Konfiguration (kein Mock für die Daten-Klasse!)
            var realConfigGeneral = new ConfigurationGeneral
            {
                EncryptDecryptWebApi = true,
                ApplicationName = "pEngine", // Wichtig für die Pfadbildung im Platform-Konstruktor
                TableSchema = "tpc"
            };

            // 2. Mocke das IGlobalState Interface
            var mockGlobalShared = new Mock<Shared.Services.Platform.IPlatform>();
            // Hinweis: Falls Platform gegen Shared.Services.GlobalState.IGlobalState geht, nutze dieses Interface:
            var mockGlobalState = new Mock<Shared.Services.GlobalState.IGlobalState>();

            // Sage dem GlobalState-Mock, dass er die echte Config-Instanz zurückgeben soll
            mockGlobalState.SetupGet(g => g.ConfigGeneral).Returns(realConfigGeneral);

            // 3. System Under Test initialisieren
            // Wir übergeben unseren vorbereiteten Mock und den ServiceProvider aus der UnitTestsBase
            _sut = new Platform(_mockServiceProvider.Object, mockGlobalState.Object);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnSecret_WhenDatabaseReturnsUpdated()
        {
            // ARRANGE
            var otpParams = new OtpParametersModel
            {
                UnixTS = "123456789",
                OtpBackupCode = "123",
                AuthUsers_UnixTS = "987"
            };

            // Wir simulieren die Antwort der Datenbank (AnonymousQuery)
            var expectedSecret = "updated:ABCDEF123456";
            _mockDam.Setup(d => d.AnonymousQuery(It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(new ScalarModel { out_value_str = expectedSecret, out_err = "" });

            // ACT
            var result = await _sut.GenerateOtpAsync(otpParams);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(expectedSecret, result.secret);
            Assert.Empty(result.err);

            // Verifizieren, ob die Parameter korrekt an die DB übergeben wurden
            _mockDam.Verify(d => d.AnonymousQuery(It.Is<Dictionary<string, string>>(dict =>
                dict["@Case_"] == "SaveOtp>>AuthUsers" &&
                dict["@UnixTS"] == "123456789"
            )), Times.Once);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnError_WhenDatabaseFails()
        {
            // ARRANGE
            _mockDam.Setup(d => d.AnonymousQuery(It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(new ScalarModel { out_value_str = null, out_err = "db_error" });

            // ACT
            var result = await _sut.GenerateOtpAsync(new OtpParametersModel());

            // ASSERT
            Assert.Equal("no_otp", result.err); // Laut deinem Code-Zweig bei null
        }

        [Fact]
        public async Task GetFormFactor_ShouldReturnDesktop()
        {
            // Ein simpler Test für die Identität der Plattform
            var result = await _sut.GetFormFactor();
            Assert.Equal("Desktop", result);
        }
    }
}