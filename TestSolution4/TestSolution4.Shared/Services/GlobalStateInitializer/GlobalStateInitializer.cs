// GlobalStateInitializer.cs
using Microsoft.Extensions.DependencyInjection;

namespace TestSolution4.Shared.Services.GlobalStateInitializer
{
    // Keine IHostedService Implementierung mehr
    public class GlobalStateInitializer
    {
        private readonly IServiceProvider _provider;

        public GlobalStateInitializer(IServiceProvider provider)
        {
            _provider = provider;
        }

        // Neue, public Methode für den expliziten Aufruf
        public async Task InitializeAsync()
        {
            //Console.WriteLine("[Blazor GlobalStateInitializer InitializeAsync] START");
            // Erzeugt einen neuen Scope, da wir ihn außerhalb der Services-Registrierung aufrufen.
            // Beachten Sie, dass Sie hier _provider anstatt _app.Services verwenden.
            using var scope = _provider.CreateScope();
            var globalState = scope.ServiceProvider.GetRequiredService<BlazorCore.Services.GlobalState.IGlobalStateBase>();

            // Dies ist die asynchrone Initialisierungslogik, die nun synchron geblockt wird.
            await globalState.EnsureInitializedAsync();

            //Console.WriteLine("[Blazor GlobalStateInitializer InitializeAsync] END");
        }

    }
}

