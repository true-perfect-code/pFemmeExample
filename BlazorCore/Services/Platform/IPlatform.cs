using BlazorCore.Services.Otp;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Platform
{
    /// <summary>
    /// Provides platform-specific services, such as accessing the base directory of the application.
    /// </summary>
    public interface IPlatformBase
    {
        // ========================================================================
        // 1. BASIS-INFORMATIONEN
        // ========================================================================

        /// <summary>
        /// Gets the base directory of the application.
        /// </summary>
        string GetBaseDirectory();

        /// <summary>
        /// Gets current platform from xml-file for the application.
        /// </summary>
        PLATFORMS GetCurrPlatform();

        /// <summary>
        /// Gets current platform from xml-file for the application.
        /// </summary>
        p11.UI.NativeDevice GetCurrDevice();

        /// <summary>
        /// Gets device information (OS, version, etc.).
        /// </summary>
        string GetDeviceInfo();

        /// <summary>
        /// Gets the form factor (Phone, Tablet, Desktop, etc.).
        /// </summary>
        Task<string> GetFormFactor();

        /// <summary>
        /// Gets the current window width in pixels.
        /// </summary>
        Task<double> GetWindowWidth();

        /// <summary>
        /// Gets the current window height in pixels.
        /// </summary>
        Task<double> GetWindowHeight();

        /// <summary>
        /// Gets the idiom platform (iOS, Android, Web, etc.).
        /// </summary>
        Task<string> GetIdiomPlatform();


        // ========================================================================
        // 2. ZWISCHENABLAGE & TEILEN
        // ========================================================================

        /// <summary>
        /// Copies text to the clipboard.
        /// </summary>
        Task CopyTextToClipboard(string text);

        /// <summary>
        /// Shares text using the native sharing dialog.
        /// </summary>
        Task ShareText(string title, string text);


        // ========================================================================
        // 3. DATEI- & VERZEICHNISAUSWAHL
        // ========================================================================

        /// <summary>
        /// Opens a native directory picker dialog.
        /// </summary>
        Task<string?> DirectoryPicker();

        /// <summary>
        /// Opens a native file picker dialog.
        /// </summary>
        Task<ScalarModel> FilePicker(string Filter, string Title);

        /// <summary>
        /// Saves a file natively on the current platform.
        /// </summary>
        Task<ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "");


        // ========================================================================
        // 4. EXTERNE URLS & AUTHENTIFIZIERUNG
        // ========================================================================

        /// <summary>
        /// Opens an external URL in the default browser.
        /// </summary>
        Task OpenExternalUrl(string url);

        /// <summary>
        /// Starts the authentication flow for native platform (opens external browser).
        /// </summary>
        Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false);


        // ========================================================================
        // 5. NAVIGATION (NATIV)
        // ========================================================================

        /// <summary>
        /// Registers the bridge for native navigation events (e.g., Android Back-Button).
        /// </summary>
        Task RegisterNativeNavigationAsync<T>(Microsoft.JSInterop.DotNetObjectReference<T> dotNetRef) where T : class;

        /// <summary>
        /// Controls the swipe-back gesture state (primarily for iOS/Web).
        /// </summary>
        Task SetSwipeBackStateAsync(bool enabled);

        /// <summary>
        /// Forces a reset of the swipe gesture (cleanup after navigation).
        /// </summary>
        Task ForceResetSwipeAsync();

        /// <summary>
        /// Closes the app or the current window (platform-dependent).
        /// </summary>
        Task ExitAppAsync();

        /// <summary>
        /// Navigates back natively.
        /// </summary>
        Task NavigateBackAsync();


        // ========================================================================
        // 6. INITIALISIERUNG
        // ========================================================================

        /// <summary>
        /// Initializes the JS bridge (required for WASM/PWA).
        /// </summary>
        Task<ScalarModel> InitializeJSAsync();


    }
}
