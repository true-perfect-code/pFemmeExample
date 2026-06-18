using BlazorCore.Services.Dam;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Global; // Hier liegen Catalog und Configuration
using TestSolution4.Wpf.Services;
using TestSolution4.XUnit.Common;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TestSolution4.XUnit.Wpf.Units
{
    public class AuthenticationTest : UnitTestsBase
    {
        public AuthenticationTest() : base(PLATFORMS.WINDOWS_CLIENT)
        {
        }

        private Authentication CreateAuth()
        {
            // Nutzt den in UnitTestsBase trainierten Mock-ServiceProvider
            return new Authentication(_mockServiceProvider.Object);
        }

        [Fact]
        public async Task Logout_ShouldRemoveTokenAndResetCredentials()
        {
            // --- ARRANGE ---
            // Nutzt die echten statischen Daten deiner Schmiede
            var sections = Catalog.Sections;
            string expectedTokenKey = sections.LocalStorage.oauth_token;

            // Mock-Setup: Der AppState muss das Catalog-Objekt kennen
            _mockApp.SetupGet(a => a.Catalog).Returns(sections);

            var authService = CreateAuth();

            // --- ACT ---
            await authService.Logout();

            // --- ASSERT ---
            // Prüfen, ob der Token-Key (z.B. "oauth_token__pEngine") gelöscht wurde
            _mockPlatform.Verify(p => p.RemoveAsync(expectedTokenKey), Times.Once);
            _mockApp.Verify(a => a.ResetUserCredential(), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldStoreTokenOnPlatform()
        {
            // --- ARRANGE ---
            var sections = Catalog.Sections;
            string expectedTokenKey = sections.LocalStorage.oauth_token;
            string testToken = "unit-test-token-123";

            _mockApp.SetupGet(a => a.Catalog).Returns(sections);

            // WICHTIG: out_value_bool muss auf true stehen, damit der Test-Assert besteht!
            _mockPlatform.Setup(p => p.SetAsync(expectedTokenKey, testToken))
                         .ReturnsAsync(new ScalarModel { out_err = "", out_value_bool = true });

            var authService = CreateAuth();

            // --- ACT ---
            var result = await authService.Login(testToken);

            // --- ASSERT ---
            // Verifizieren, dass der Token auf der Plattform (Windows/WPF) gespeichert wurde
            _mockPlatform.Verify(p => p.SetAsync(expectedTokenKey, testToken), Times.Once);

            Assert.NotNull(result);
            Assert.True(result.out_value_bool);
        }
    }
}