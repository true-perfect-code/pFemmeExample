namespace BlazorCore.Services.EventAggregator
{
    public class EventAggregator : IEventAggregator
    {
        // === Navigation / UI Events ===

        /// <inheritdoc />
        public event Action? OnRefreshLandingpage;
        public void RefreshLandingpage() => OnRefreshLandingpage?.Invoke();

        /// <inheritdoc />
        public event Action? OnRoutesStateHasChanged;
        public void RoutesStateHasChanged() => OnRoutesStateHasChanged?.Invoke();

        /// <inheritdoc />
        public event Action? OnLanguageHasChanged;
        public void LanguageHasChanged() => OnLanguageHasChanged?.Invoke();


        // === Navigation Modal-Native ===

        public event Func<ModalNativeEventArgs, Task>? OnModalNativeChanged;

        /// <summary>
        /// Opens or closes a native modal and dispatches the event to all subscribers.
        /// </summary>
        public async Task ChangeModalNativeAsync(
            ModalNativeType modal,
            bool? isOpen = null,
            object? data = null)
        {
            // No subscribers
            if (OnModalNativeChanged is null)
                return;

            // Create event payload
            var args = new ModalNativeEventArgs
            {
                Modal = modal,
                IsOpen = isOpen,
                Data = data
            };

            // Execute all subscribers safely
            foreach (Func<ModalNativeEventArgs, Task> handler
                     in OnModalNativeChanged.GetInvocationList())
            {
                try
                {
                    await handler(args);
                }
                catch (Exception ex)
                {
                    // Optional:
                    // Replace with your logging system
                    Console.WriteLine(
                        $"[EventAggregatorProject] Modal event error: {ex}");
                }
            }
        }


        /// <inheritdoc />
        public event Action? OnParametersSetAuthenticationExtend;
        public void ParametersSetAuthenticationExtend() => OnParametersSetAuthenticationExtend?.Invoke();

        // === Generic / Legacy Events ===

        public event Func<Task>? OnTriggerEvent01Async;
        public async Task TriggerEvent01Async()
        {
            if (OnTriggerEvent01Async != null)
            {
                await OnTriggerEvent01Async.Invoke();
            }
        }
    }
}