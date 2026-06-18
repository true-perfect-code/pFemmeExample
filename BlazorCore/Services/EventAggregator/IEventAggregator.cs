namespace BlazorCore.Services.EventAggregator
{
    public interface IEventAggregator
    {
        // === Navigation / UI Events ===

        /// <summary>
        /// Triggered when the landing page needs to be refreshed.
        /// </summary>
        event Action? OnRefreshLandingpage;
        void RefreshLandingpage();

        /// <summary>
        /// Triggered when routes state has changed.
        /// </summary>
        event Action? OnRoutesStateHasChanged;
        void RoutesStateHasChanged();

        /// <summary>
        /// Triggered when language has changed.
        /// </summary>
        event Action? OnLanguageHasChanged;
        void LanguageHasChanged();

        // === Navigation Modal-Native ===

        /// <summary>
        /// Triggered when a native modal should open or close.
        /// </summary>
        event Func<ModalNativeEventArgs, Task>? OnModalNativeChanged;

        /// <summary>
        /// Opens or closes a native modal.
        /// </summary>
        /// <param name="modal">Target modal type.</param>
        /// <param name="isOpen">Optional True = open, False = close.</param>
        /// <param name="data">Optional payload.</param>
        Task ChangeModalNativeAsync(
            ModalNativeType modal,
            bool? isOpen = null,
            object? data = null);


        /// <summary>
        /// Triggered when ParametersSet in AuthenticationExtend component is needed.
        /// </summary>
        event Action? OnParametersSetAuthenticationExtend;
        void ParametersSetAuthenticationExtend();

        // === Generic / Legacy Events ===

        /// <summary>
        /// Generic trigger event (legacy). Consider replacing with specific events.
        /// </summary>
        event Func<Task>? OnTriggerEvent01Async;
        Task TriggerEvent01Async();
    }
}