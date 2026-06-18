//using System;

//public class Class1
//{
//	public Class1()
//	{
//	}
//}

using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqLite;
using TestSolution4.Shared.Services.AppState;
using TestSolution4.Shared.Services.Platform;
using Microsoft.JSInterop;
using Moq;
using System;
using System.Collections.Generic;

namespace TestSolution4.XUnit.Common
{
    public abstract class UnitTestsBase
    {
        protected readonly Mock<IAppStateBase> _mockApp = new();
        protected readonly Mock<IGlobalStateBase> _mockGlobal = new();
        protected readonly Mock<IPlatformBase> _mockPlatform = new();
        protected readonly Mock<IApiBase> _mockApi = new();
        protected readonly Mock<ISqLiteBase> _mockSqlite = new();
        protected readonly Mock<IServiceProvider> _mockServiceProvider = new();
        protected readonly Mock<IDamBase> _mockDam = new();
        protected readonly Mock<IJSRuntime> _mockJs = new();

        protected UnitTestsBase(PLATFORMS defaultPlatform)
        {
            // 1. DER FIX FÜR DEN CAST-FEHLER:
            // Wir erweitern den Mock so, dass er beide Interfaces implementiert.
            _mockPlatform.As<IPlatform>();
            _mockApp.As<IAppState>();

            // 2. Standard pEngine Setup (Default: Server)
            _mockPlatform.Setup(p => p.GetCurrPlatform()).Returns(defaultPlatform);
            _mockApp.SetupGet(a => a.IsInternetConnected).Returns(true);
            _mockApp.SetupGet(a => a.IsCloudConnected).Returns(true);
            _mockApp.SetupGet(a => a.WebApiToken).Returns("Unit-Test-Token");

            // 3. ServiceProvider Training:
            // Registrierung der Mocks für alle Interface-Varianten.

            // Platform (Base + Spezifisch)
            RegisterService<IPlatformBase>(_mockPlatform);
            RegisterService<IPlatform>(_mockPlatform);

            // AppState (Base + Spezifisch)
            RegisterService<IAppStateBase>(_mockApp);
            RegisterService<IAppState>(_mockApp);

            // Restliche Dienste
            RegisterService<IGlobalStateBase>(_mockGlobal);
            RegisterService<IApiBase>(_mockApi);
            RegisterService<ISqLiteBase>(_mockSqlite);
            RegisterService<IDamBase>(_mockDam);
            RegisterService<IJSRuntime>(_mockJs);
        }

        /// <summary>
        /// Registriert den Mock im ServiceProvider. 
        /// Durch das 'object' Casten stellen wir sicher, dass auch die 
        /// via .As<T>() hinzugefügten Interfaces gefunden werden.
        /// </summary>
        protected void RegisterService<T>(Mock mock) where T : class
        {
            _mockServiceProvider
                .Setup(x => x.GetService(typeof(T)))
                .Returns(mock.Object);
        }
    }
}
