using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Services.Platform;
using TestSolution4.XUnit.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TestSolution4.XUnit.Capa.Units
{
    public class PlatformTests : UnitTestsBase
    {
        private readonly Platform _sut;

        // Wir machen es exakt wie im Web-Projekt: Ein lokaler, frischer Mock!
        private readonly Mock<IJSRuntime> _localMockJs = new();
        private readonly Mock<NavigationManager> _mockNav = new();

        public PlatformTests() : base(PLATFORMS.WASM)
        {
            // BEVOR wir den Mock irgendwo übergeben, erweitern wir ihn um das synchrone Interface!
            _localMockJs.As<IJSInProcessRuntime>();

            _sut = new Platform(
                _mockServiceProvider.Object,
                _mockGlobal.Object as Shared.Services.GlobalState.IGlobalState,
                _mockNav.Object,
                _localMockJs.Object // Hier geben wir den lokal erweiterten Mock rein
            );
        }

        [Theory]
        [InlineData("android", PLATFORMS.ANDROID)]
        [InlineData("ios", PLATFORMS.IOS)]
        [InlineData("wasm", PLATFORMS.WASM)]
        [InlineData("unknown", PLATFORMS.WASM)]
        public void GetCurrPlatform_ShouldMapJsResponseCorrectly(string jsResponse, PLATFORMS expected)
        {
            // ARRANGE
            // Da wir .As im Konstruktor gerufen haben, klappt der Cast jetzt fehlerfrei!
            var mockInProcess = _localMockJs.As<IJSInProcessRuntime>();
            mockInProcess.Setup(js => js.Invoke<string>("pE_Capacitor.getPlatform", It.IsAny<object[]>()))
                         .Returns(jsResponse);

            // ACT
            var result = _sut.GetCurrPlatform();

            // ASSERT
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SetAsync_OnAndroid_ShouldInvokeCapacitorStorage()
        {
            // ARRANGE
            var mockInProcess = _localMockJs.As<IJSInProcessRuntime>();
            mockInProcess.Setup(js => js.Invoke<string>("pE_Capacitor.getPlatform", It.IsAny<object[]>()))
                         .Returns("android");

            var expectedResult = new ScalarModel { out_value_bool = true };
            _localMockJs.Setup(js => js.InvokeAsync<ScalarModel>("pE_Capacitor.setStorage", It.IsAny<object[]>()))
                        .ReturnsAsync(expectedResult);

            // ACT
            var result = await _sut.SetAsync("testKey", "testVal");

            // ASSERT
            _localMockJs.Verify(js => js.InvokeAsync<ScalarModel>(
                "pE_Capacitor.setStorage",
                It.Is<object[]>(args => args[0].ToString() == "testKey" && args[1].ToString() == "testVal")),
                Times.Once);

            Assert.True(result.out_value_bool);
        }

        [Fact]
        public async Task GetFormFactor_OnWasm_ShouldReturnWasmString()
        {
            // ARRANGE
            var mockInProcess = _localMockJs.As<IJSInProcessRuntime>();
            mockInProcess.Setup(js => js.Invoke<string>("pE_Capacitor.getPlatform", It.IsAny<object[]>()))
                         .Returns("wasm");

            // ACT
            var result = await _sut.GetFormFactor();

            // ASSERT
            Assert.Equal("Wasm", result);
        }
    }
}