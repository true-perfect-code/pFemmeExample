using BlazorCore.Models;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using p11.UI.Models;

namespace BlazorCore.Services.AppState
{
    /// <summary>
    /// Represents the shared application state across the entire app.
    /// Provides global properties and events for state tracking and UI updates.
    /// </summary>
    public interface IAppStateBase
    {
        // =====================================================================
        // INITIALIZATION & GLOBAL CONFIG
        // =====================================================================

        /// <summary>
        /// Global initialization of AppState with static configuration.
        /// Called once during app startup (AppStartup.razor).
        /// </summary>
        /// <param name="catalog">Static app configuration (tables, languages, fonts, localStorage keys).</param>
        /// <param name="data">Static app dataset (settings metadata, user parameters, version changelog).</param>
        void GlobalInit(Sections catalog, Dataset data);

        /// <summary>
        /// Static app configuration (delegates to catalog from GlobalInit).
        /// </summary>
        Sections Catalog { get; }

        /// <summary>
        /// Static app dataset (delegates to data from GlobalInit).
        /// </summary>
        Dataset Data { get; }

        /// <summary>
        /// A task that completes when the application state has been initialized.
        /// Can be awaited by components or services that depend on initialization.
        /// </summary>
        Task Initialization { get; }

        /// <summary>
        /// Marks the application state as initialized, completing the Initialization task.
        /// </summary>
        void MarkInitialized();

        /// <summary>
        /// Resets the initialization task to allow re-initialization of the application state.
        /// </summary>
        void ResetInitialization();

        // =====================================================================
        // UI EVENTS & NOTIFICATIONS
        // =====================================================================

        /// <summary>
        /// Event triggered whenever a state property changes.
        /// Components can subscribe to refresh UI when state changes occur.
        /// </summary>
        event Action? OnChange;

        /// <summary>
        /// Event triggered when data should be reloaded.
        /// Components can subscribe to reload data if data changed.
        /// </summary>
        event Action? OnReload;

        /// <summary>
        /// Triggers the OnReload event.
        /// </summary>
        void Reload();

        /// <summary>
        /// Event triggered for displaying a message to the user.
        /// </summary>
        event Action<string>? OnMessage;

        /// <summary>
        /// Triggers the OnMessage event with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        void Message(string message);

        /// <summary>
        /// Event triggered to hide or show native modal header/footer.
        /// </summary>
        event Action<bool>? OnHideModalNativeHeaderFooter;

        /// <summary>
        /// Triggers the OnHideModalNativeHeaderFooter event.
        /// </summary>
        /// <param name="hide">True to hide, false to show.</param>
        void HideModalNativeHeaderFooter(bool hide);

        // =====================================================================
        // SESSION STATE PROPERTIES
        // =====================================================================

        /// <summary>
        /// Indicates if app is initialized.
        /// </summary>
        bool IsAppInitialized { get; }

        /// <summary>
        /// Updates the app initialization status.
        /// </summary>
        /// <param name="value">True if initialized, false otherwise.</param>
        void UpdateIsAppInitialized(bool value);

        /// <summary>
        /// Indicates whether the current user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Updates the authentication status of the user.
        /// </summary>
        /// <param name="value">True if authenticated, false otherwise.</param>
        void UpdateIsAuthenticated(bool value);

        /// <summary>
        /// Indicates the current app loading status.
        /// </summary>
        APP_LOADING_STATUS AppLoadingStatus { get; }

        /// <summary>
        /// Updates the app loading status.
        /// </summary>
        /// <param name="appLoadingStatus">The new app loading status.</param>
        void UpdateAppLoadingStatus(APP_LOADING_STATUS appLoadingStatus);

        /// <summary>
        /// Indicates whether the cloud is currently connected.
        /// </summary>
        bool IsCloudConnected { get; }

        /// <summary>
        /// Updates the cloud connection status.
        /// </summary>
        /// <param name="value">True if connected, false otherwise.</param>
        void UpdateIsCloudConnected(bool value);

        /// <summary>
        /// Indicates whether the root page is currently loaded.
        /// </summary>
        bool IsRootPageLoaded { get; }

        /// <summary>
        /// Updates the root page status.
        /// </summary>
        /// <param name="value">True if currently loaded, false otherwise.</param>
        void UpdateIsRootPageLoaded(bool value);

        /// <summary>
        /// Indicates whether the user has activated two-factor authentication.
        /// </summary>
        bool Is2FAActivated { get; }

        /// <summary>
        /// Updates the two-factor authentication activated status.
        /// </summary>
        /// <param name="value">True if activated, false otherwise.</param>
        void UpdateIs2FAActivated(bool value);

        /// <summary>
        /// Provides the central configuration model for accessibility.
        /// Components bind to the properties of this model.
        /// </summary>
        AccessibilityModel AccessibilityConfig { get; set; }

        // =====================================================================
        // USER CREDENTIALS & TOKENS
        // =====================================================================

        /// <summary>
        /// Resets user-specific credentials (login, tokens, etc.).
        /// </summary>
        void ResetUserCredential();

        /// <summary>
        /// Gets the current WebApiToken.
        /// </summary>
        string WebApiToken { get; }

        /// <summary>
        /// Updates the WebApiToken.
        /// </summary>
        /// <param name="webApiToken">The new WebApiToken.</param>
        void UpdateWebApiToken(string webApiToken);

        /// <summary>
        /// Checks if a valid WebApiToken exists (not default string.empty).
        /// </summary>
        /// <returns>True if the WebApiToken is valid, otherwise false.</returns>
        bool HasWebApiToken();

        /// <summary>
        /// Gets the current UnixTS (unique identifier for user session).
        /// </summary>
        string UnixTS { get; }

        /// <summary>
        /// Updates the UnixTS.
        /// </summary>
        /// <param name="unixTS">The new UnixTS.</param>
        void UpdateUnixTS(string unixTS);

        /// <summary>
        /// Checks if a valid UnixTS exists (not default string.empty).
        /// </summary>
        /// <returns>True if the UnixTS is valid, otherwise false.</returns>
        bool HasUnixTS();

        /// <summary>
        /// Gets the current UserAccount (email or username).
        /// </summary>
        string UserAccount { get; }

        /// <summary>
        /// Updates the UserAccount.
        /// </summary>
        /// <param name="userAccount">The new user account identifier.</param>
        void UpdateUserAccount(string userAccount);

        /// <summary>
        /// Checks if a valid UserAccount exists (not default string.empty).
        /// </summary>
        /// <returns>True if the UserAccount is valid, otherwise false.</returns>
        bool HasUserAccount();

        /// <summary>
        /// Gets the current IdP (external Identity Provider).
        /// </summary>
        string IdP { get; }

        /// <summary>
        /// Updates the IdP.
        /// </summary>
        /// <param name="idp">The new Identity Provider identifier.</param>
        void UpdateIdP(string idp);

        /// <summary>
        /// Checks if a valid IdP exists (not default string.empty).
        /// </summary>
        /// <returns>True if the IdP is valid, otherwise false.</returns>
        bool HasIdP();

        /// <summary>
        /// Gets the current Pepper (security key).
        /// </summary>
        string Pepper { get; }

        /// <summary>
        /// Updates the Pepper.
        /// </summary>
        /// <param name="pepper">The new pepper value.</param>
        void UpdatePepper(string pepper);

        /// <summary>
        /// Checks if a valid Pepper exists (not default string.empty).
        /// </summary>
        /// <returns>True if the Pepper is valid, otherwise false.</returns>
        bool HasPepper();

        // =====================================================================
        // USER PREFERENCES
        // =====================================================================

        /// <summary>
        /// Gets the current TOP (count of loaded DB records).
        /// </summary>
        int TOP { get; }

        /// <summary>
        /// Updates the TOP value.
        /// </summary>
        /// <param name="top">The new TOP value.</param>
        void UpdateTOP(int top);

        /// <summary>
        /// Checks if TOP is valid (not default 0).
        /// </summary>
        /// <returns>True if TOP is valid, otherwise false.</returns>
        bool HasTOP();

        /// <summary>
        /// Gets the current Alias (display name).
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// Updates the Alias.
        /// </summary>
        /// <param name="alias">The new alias.</param>
        void UpdateAlias(string alias);

        /// <summary>
        /// Checks if a valid Alias exists (not default string.empty).
        /// </summary>
        /// <returns>True if the Alias is valid, otherwise false.</returns>
        bool HasAlias();

        /// <summary>
        /// Gets the current AliasImg (profile image identifier).
        /// </summary>
        string AliasImg { get; }

        /// <summary>
        /// Updates the AliasImg.
        /// </summary>
        /// <param name="aliasImg">The new alias image identifier.</param>
        void UpdateAliasImg(string aliasImg);

        /// <summary>
        /// Checks if a valid AliasImg exists (not default string.empty).
        /// </summary>
        /// <returns>True if the AliasImg is valid, otherwise false.</returns>
        bool HasAliasImg();

        /// <summary>
        /// Gets the current SecureStorageID.
        /// </summary>
        string SecureStorageID { get; }

        /// <summary>
        /// Updates the SecureStorageID.
        /// </summary>
        /// <param name="secureStorageID">The new SecureStorageID.</param>
        void UpdateSecureStorageID(string secureStorageID);

        /// <summary>
        /// Checks if a valid SecureStorageID exists (not default string.empty).
        /// </summary>
        /// <returns>True if the SecureStorageID is valid, otherwise false.</returns>
        bool HasSecureStorageID();

        /// <summary>
        /// Gets the current Display (display mode/size preference).
        /// </summary>
        string Display { get; }

        /// <summary>
        /// Updates the Display.
        /// </summary>
        /// <param name="display">The new display preference.</param>
        void UpdateDisplay(string display);

        /// <summary>
        /// Checks if a valid Display exists (not default string.empty).
        /// </summary>
        /// <returns>True if the Display is valid, otherwise false.</returns>
        bool HasDisplay();

        /// <summary>
        /// Gets the current StorageLocation (Cloud, Local, or Cloud_Local).
        /// </summary>
        STORAGE_LOCATION StorageLocation { get; }

        /// <summary>
        /// Updates the StorageLocation.
        /// </summary>
        /// <param name="storageLocation">The new storage location.</param>
        void UpdateStorageLocation(STORAGE_LOCATION storageLocation);

        /// <summary>
        /// Checks if StorageLocation is valid (not STORAGE_LOCATION.Unknown).
        /// </summary>
        /// <returns>True if StorageLocation is valid, otherwise false.</returns>
        bool HasStorageLocation();

        /// <summary>
        /// Gets the current SelectedLanguage (e.g., "EN", "DE").
        /// </summary>
        string SelectedLanguage { get; }

        /// <summary>
        /// Updates the SelectedLanguage.
        /// </summary>
        /// <param name="selectedLanguage">The new language code.</param>
        void UpdateSelectedLanguage(string selectedLanguage);

        /// <summary>
        /// Checks if a valid SelectedLanguage exists (not default string.empty).
        /// </summary>
        /// <returns>True if SelectedLanguage is valid, otherwise false.</returns>
        bool HasSelectedLanguage();

        /// <summary>
        /// Gets the current Ltr-Rtl (text direction preference).
        /// </summary>
        LTR_RTL LtrRtl { get; }

        /// <summary>
        /// Updates the LtrRtl.
        /// </summary>
        /// <param name="ltrRtl">The new text direction preference.</param>
        void UpdateLtrRtl(LTR_RTL ltrRtl);

        /// <summary>
        /// Checks if a valid LtrRtl exists.
        /// </summary>
        /// <returns>True if LtrRtl is valid, otherwise false.</returns>
        bool HasLtrRtl();

        // =====================================================================
        // PLATFORM & DEVICE INFO
        // =====================================================================

        /// <summary>
        /// Gets the current IsNativeApp status (true for MAUI/Capacitor, false for Web).
        /// </summary>
        bool? IsNativeApp { get; }

        /// <summary>
        /// Updates the IsNativeApp status asynchronously.
        /// </summary>
        Task UpdateIsNativeApp();

        /// <summary>
        /// Checks if IsNativeApp is valid (not null).
        /// </summary>
        /// <returns>True if IsNativeApp has a value, otherwise false.</returns>
        bool HasIsNativeApp();

        /// <summary>
        /// Gets the Native Device type (Phone, Tablet, Desktop, etc.).
        /// </summary>
        p11.UI.NativeDevice NativeDeviceType { get; }

        /// <summary>
        /// Updates the Native Device type.
        /// </summary>
        void UpdateNativeDeviceType(p11.UI.NativeDevice? nativeDeviceType = null);

        /// <summary>
        /// Gets the current IsPhone status.
        /// </summary>
        bool IsPhone { get; }

        /// <summary>
        /// Gets the current IsDesktop status.
        /// </summary>
        bool IsDesktop { get; }

        /// <summary>
        /// Updates both IsPhone and IsDesktop statuses based on current device.
        /// </summary>
        void UpdateIsPhoneOrDesktop();

        /// <summary>
        /// Gets the current Local SQLite Db-Path.
        /// </summary>
        string LocalSqLiteDbPath { get; }

        /// <summary>
        /// Updates the Local SQLite Db-Path.
        /// </summary>
        /// <param name="localSqLiteDbPath">The new database path.</param>
        void UpdateLocalSqLiteDbPath(string localSqLiteDbPath);

        /// <summary>
        /// Checks if a valid LocalSqLiteDbPath exists (not default string.empty).
        /// </summary>
        /// <returns>True if LocalSqLiteDbPath is valid, otherwise false.</returns>
        bool HasLocalSqLiteDbPath();

        /// <summary>
        /// Gets the current IsInternetConnected status cache.
        /// Used for synchronous access in Blazor components.
        /// </summary>
        bool IsInternetConnected { get; }

        /// <summary>
        /// Updates the IsInternetConnected status cache.
        /// Called by Platform service when a native connectivity change occurs.
        /// </summary>
        /// <param name="isConnected">The new internet connection status.</param>
        void SetInternetStatus(bool isConnected);

        // =====================================================================
        // LOGGING (delegated to ILogging service)
        // =====================================================================

        /// <summary>
        /// Initializes the logging service with JavaScript runtime.
        /// Called once during app startup, typically in App.razor or Program.cs.
        /// </summary>
        /// <param name="js">The JavaScript runtime instance for console logging.</param>
        void Initialize(Microsoft.JSInterop.IJSRuntime js);

        /// <summary>
        /// Logs a message to the browser console or native debug output.
        /// Messages may be filtered based on debug mode configuration.
        /// </summary>
        /// <param name="msg">The message text to log.</param>
        /// <param name="level">Log level (Log, Warn, Error). Default is Log.</param>
        /// <param name="data">Optional data object to serialize and log alongside the message.</param>
        /// <param name="isDebugEnabled">Force logging even when debug mode is disabled.</param>
        Task Log(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null, bool isDebugEnabled = false);

        /// <summary>
        /// Logs a warning message to the console or native debug output.
        /// Convenience wrapper around Log() with Warn level.
        /// </summary>
        /// <param name="msg">The warning message text.</param>
        /// <param name="data">Optional data object to serialize and log.</param>
        Task Warn(string msg, object? data = null);

        /// <summary>
        /// Logs an error message to the console or native debug output.
        /// Convenience wrapper around Log() with Error level (always shown regardless of debug mode).
        /// </summary>
        /// <param name="msg">The error message text.</param>
        /// <param name="data">Optional data object to serialize and log.</param>
        Task Error(string msg, object? data = null);

        /// <summary>
        /// Fire-and-forget log method that never throws exceptions.
        /// Useful for logging in contexts where awaiting is not possible or unwanted.
        /// </summary>
        /// <param name="msg">The message text to log.</param>
        /// <param name="level">Log level (Log, Warn, Error). Default is Log.</param>
        /// <param name="data">Optional data object to serialize and log.</param>
        void LogVoid(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null);

        // =====================================================================
        // TRANSLATION (delegated to ITranslation service)
        // =====================================================================

        /// <summary>
        /// Loads translations for the specified language.
        /// </summary>
        /// <param name="routesStateHasChanged">If true, triggers UI update after loading.</param>
        /// <param name="selectedLanguage">Language code (e.g., "EN", "DE"). If empty, uses current SelectedLanguage.</param>
        Task LoadTranslations(bool routesStateHasChanged, string selectedLanguage = "");

        /// <summary>
        /// Gets the translated text for the given English term.
        /// Returns the original term if no translation is found.
        /// </summary>
        /// <param name="englishTerm">The English term to translate.</param>
        /// <returns>Translated text or original term.</returns>
        string T(string englishTerm);

        // =====================================================================
        // APP PARAMETER / SETTINGS MANAGEMENT (complex orchestration - stays here)
        // =====================================================================

        /// <summary>
        /// Gets the current user settings (loaded from cloud/local storage).
        /// </summary>
        List<AppParameterModel> Settings { get; }

        /// <summary>
        /// Updates the current user settings.
        /// </summary>
        /// <param name="settings">The new settings list.</param>
        void UpdateSettings(List<AppParameterModel> settings);

        /// <summary>
        /// Checks if settings exist.
        /// </summary>
        /// <returns>True if settings are available, otherwise false.</returns>
        bool HasSettings();

        /// <summary>
        /// Gets the settings metadata (UI templates for settings dialog).
        /// </summary>
        List<SettingMetadataModel> SettingsMeta { get; }

        /// <summary>
        /// Updates the settings metadata.
        /// </summary>
        /// <param name="settings">The new settings metadata.</param>
        void UpdateSettingsMeta(List<SettingMetadataModel> settings);

        /// <summary>
        /// Checks if settings metadata exist.
        /// </summary>
        /// <returns>True if settings metadata are available, otherwise false.</returns>
        bool HasSettingsMeta();

        /// <summary>
        /// Reads app parameters from specified storage location (cloud/local).
        /// </summary>
        /// <param name="storagelocation">The storage location to read from.</param>
        /// <returns>A ReadModel containing the app parameters or error information.</returns>
        Task<ReadModel<AppParameterModel?>?> ReadAppParameter(STORAGE_LOCATION storagelocation);

        /// <summary>
        /// Gets all app parameters from current storage location.
        /// </summary>
        /// <returns>List of app parameters.</returns>
        Task<List<AppParameterModel>> GetAppParameter();

        /// <summary>
        /// Loads the storage location from secure storage (native) or defaults to cloud.
        /// </summary>
        Task LoadStorageLocation();

        // =====================================================================
        // ID GENERATION (delegated to IIdGenerator service)
        // =====================================================================

        /// <summary>
        /// Initializes the user ID based on email/user identifier.
        /// </summary>
        /// <param name="user">The user identifier (email or username).</param>
        void InitializeUnixTSUser(string user);

        /// <summary>
        /// Initializes the device ID based on device info.
        /// </summary>
        /// <param name="deviceInfo">The device information string.</param>
        void InitializeUnixTSDeviceId(string deviceInfo);

        /// <summary>
        /// Generates a unique 35-character ID for database records.
        /// </summary>
        /// <returns>A unique identifier string prefixed with 'T'.</returns>
        string GenerateUniqueId();
    }
}