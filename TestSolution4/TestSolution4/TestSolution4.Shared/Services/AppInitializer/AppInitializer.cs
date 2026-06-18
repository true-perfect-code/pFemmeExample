using Microsoft.JSInterop;

namespace TestSolution4.Shared.Services.AppInitializer
{
    public class AppInitializer : BlazorCore.Services.AppInitializer.AppInitializerBase, IAppInitializer
    {
        public AppInitializer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            // Der Körper des Konstruktors kann leer bleiben
            // oder eigene Initialisierungslogik enthalten.
        }
    }
}