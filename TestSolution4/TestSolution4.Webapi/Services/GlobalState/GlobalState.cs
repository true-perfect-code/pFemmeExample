namespace pWebApi.Services.GlobalState
{
    public class GlobalState : BlazorCore.Services.GlobalState.GlobalStateBase, IGlobalState
    {
        public GlobalState(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            // Der Körper des Konstruktors kann leer bleiben
            // oder eigene Initialisierungslogik enthalten.
        }

    }
}
