namespace TestSolution4.Shared.Services.AppState
{
    /// <inheritdoc />
    public class AppState : BlazorCore.Services.AppState.AppStateBase, IAppState //IAppState
    {
        public AppState(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            // Der Körper des Konstruktors kann leer bleiben
            // oder eigene Initialisierungslogik enthalten.
        }
    }
}
