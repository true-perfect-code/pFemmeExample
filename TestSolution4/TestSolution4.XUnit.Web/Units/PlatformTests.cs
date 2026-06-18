using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Otp;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using TestSolution4.Web.Services; // Die echte Web-Klasse
using TestSolution4.XUnit.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace TestSolution4.XUnit.Web.Units
{
    public class PlatformTests : UnitTestsBase
    {
        private readonly Platform _sut; // System Under Test
        private readonly Mock<IJSRuntime> _mockJs = new();
        private readonly Mock<NavigationManager> _mockNav = new();
        private readonly Mock<IHttpClientFactory> _mockHttp = new();

        public PlatformTests() : base(BlazorCore.Services.Platform.PLATFORMS.WINDOWS_SERVER)
        {
            // Wir nutzen die Mocks aus der UnitTestsBase (z.B. _mockGlobal) 
            // und ergänzen die Web-spezifischen Mocks (JS, Nav, Http).

            // Cast für das spezifische GlobalState Interface (falls nötig)
            var globalState = _mockGlobal.Object as Shared.Services.GlobalState.IGlobalState;

            _sut = new Platform(
                _mockJs.Object,
                _mockGlobal.Object as Shared.Services.GlobalState.IGlobalState,
                _mockServiceProvider.Object,
                _mockNav.Object,
                _mockHttp.Object
            );
        }

        [Fact]
        public void GetCurrPlatform_ShouldReturnWindowsServer()
        {
            // ACT
            var result = _sut.GetCurrPlatform();

            // ASSERT
            // Im Web-Projekt liefert die Platform hartkodiert WINDOWS_SERVER
            Assert.Equal(PLATFORMS.WINDOWS_SERVER, result);
        }

        [Fact]
        public async Task GetFormFactor_ShouldReturnWeb()
        {
            // ACT
            var result = await _sut.GetFormFactor();

            // ASSERT
            Assert.Equal("Web", result);
        }

        [Fact]
        public async Task SetAsync_ShouldInvokeJS_SetCookie()
        {
            // ARRANGE
            string key = "token";
            string val = "123";

            // ACT
            var result = await _sut.SetAsync(key, val);

            // ASSERT
            // Wir prüfen, ob InvokeVoidAsync (intern) mit den richtigen Parametern gerufen wurde
            _mockJs.Verify(x => x.InvokeAsync<object>(
                "pE_Web.setCookie",
                It.Is<object[]>(args => args[0].ToString() == key && args[1].ToString() == val && args[2].ToString() == "365")),
                Times.Once);

            Assert.True(result.out_value_bool);
            Assert.Equal(val, result.out_value_str);
        }

    }
}