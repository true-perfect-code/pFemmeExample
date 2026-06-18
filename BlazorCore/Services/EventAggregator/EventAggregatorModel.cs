namespace BlazorCore.Services.EventAggregator
{
    internal class EventAggregatorModel
    {
    }

    /// <summary>
    /// Central modal registry.
    /// </summary>
    public enum ModalNativeType
    {
        None,

        // Common
        About,
        Account,
        Settings,
        SettingsToggle,
        Sharing,
        Donate,
        Cookies,
    }

    /// <summary>
    /// Event payload for native modal navigation.
    /// </summary>
    public sealed class ModalNativeEventArgs
    {
        /// <summary>
        /// Which modal should be affected.
        /// </summary>
        public ModalNativeType Modal { get; init; }

        /// <summary>
        /// True = open modal, False = close modal.
        /// </summary>
        public bool? IsOpen { get; init; }

        /// <summary>
        /// Optional payload for the modal.
        /// Example:
        /// Cycle model, DTO, navigation data, etc.
        /// </summary>
        public object? Data { get; init; }
    }
}
