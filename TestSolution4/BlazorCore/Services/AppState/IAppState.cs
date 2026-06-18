using Microsoft.Identity.Client;
using p11.UI.Models;
using BlazorCore.Models;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.AppState
{
    /// <summary>
    /// Represents the shared application state across the entire app.
    /// Provides global properties and events for state tracking and UI updates.
    /// </summary>
    public interface IAppStateBase
    {
        Sections Catalog { get; }
        Dataset Data { get; }
        void GlobalInit(Sections catalog, Dataset data);





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


        /// <summary>
        /// Event triggered whenever a state property changes.
        /// Components can subscribe to refresh UI when state changes occur.
        /// </summary>
        event Action? OnChange;

        /// <summary>
        /// Event triggered reloading data.
        /// Components can subscribe to reload data if data changed.
        /// </summary>
        event Action? OnReload;
        void Reload();

        /// <summary>
        /// Event triggered displaying message.
        /// Components can subscribe to reload data if data changedisplay a messaged.
        /// </summary>
        //event Action? OnMessage;
        public event Action<string>? OnMessage;
        void Message(string message);


        /// <summary>
        /// Event triggered.
        /// Components can subscribe to trigger an event.    Notifies all subscribers to trigger set parameter in AuthenticationExtend.razor
        /// </summary>
        //event Action? OnTriggerEvent01;
        event Action? OnAddNewTodo;
        void AddNewTodo();

        event Action? OnRefreshLandingpage;
        void RefreshLandingpage();

        event Action? OnRoutesStateHasChanged;
        void RoutesStateHasChanged();

        event Action? OnLanguageHasChanged;
        void LanguageHasChanged();


        // Das Event akzeptiert nun einen bool-Parameter
        event Action<bool>? OnHideModalNativeHeaderFooter;
        // Die Methode nimmt den Status entgegen und gibt ihn an das Event weiter
        void HideModalNativeHeaderFooter(bool hide);

        //event Func<Task>? OnRoutesInitializeAppAsync;
        //Task RoutesInitializeAppAsync();

        event Action? OnOpenAboutHome;
        void OpenAboutHome();

        event Action? OnToggleSettingsHome;
        void ToggleSettingsHome();

        event Action? OnSearchTodo;
        void SearchTodo();
        event Action? OnSortTodo;
        void SortTodo();


        event Func<Task>? OnTriggerEvent01Async;
        Task TriggerEvent01Async();

        //event Func<TodoModel, Task>? OnRefreshTodoAsync;
        //Task RefreshTodoAsync(TodoModel item);

        event Func<string, Task>? OnRefreshTodoUnixTSAsync;
        Task RefreshTodoUnixTSAsync(string unixTS);

        /// <summary>
        /// Event triggered.
        /// AuthenticationExtend.razor Component subscribe to trigger set parameter
        /// </summary>
        event Action? OnParametersSetAuthenticationExtend;
        void ParametersSetAuthenticationExtend();


        //p11.UI.Models.FontSizeModel FontSize { get; set; }
        /// <summary>
        /// Provides the central configuration model for accessibility.
        /// Components bind to the properties of this model.
        /// </summary>
        AccessibilityModel AccessibilityConfig { get; set; }

        ///// <summary>
        ///// Indicates whether the user has successfully passed two-factor authentication.
        ///// </summary>
        //bool Is2FAVerified { get; }

        ///// <summary>
        ///// Sets two-factor authentication as verified.
        ///// </summary>
        //void Verify2FA();

        ///// <summary>
        ///// Revokes two-factor authentication verification.
        ///// </summary>
        //void Revoke2FA();

        ///// <summary>
        ///// Indicates whether the user has successfully passed two-factor authentication.
        ///// </summary>
        //bool Is2FAFailed { get; }

        ///// <summary>
        ///// Updates the authentication status of the user.
        ///// </summary>
        ///// <param name="value">True if authenticated, false otherwise.</param>
        //void UpdateIs2FAFailed(bool value);

        /// <summary>
        /// Indicates whether the user has activated two-factor authentication.
        /// </summary>
        bool Is2FAActivated { get; }

        /// <summary>
        /// Updates the authentication status of the user.
        /// </summary>
        /// <param name="value">True if authenticated, false otherwise.</param>
        void UpdateIs2FAActivated(bool value);


        /// <summary>
        /// Indicates if app is initialized.
        /// </summary>
        bool IsAppInitialized { get; }

        /// <summary>
        /// Updates the app initializion status.
        /// </summary>
        /// <param name="value">True if initialized, false otherwise.</param>
        void UpdateIsAppInitialized(bool value);


        ///// <summary>
        ///// Indicates whether the user was allready authenticated.
        ///// </summary>
        //bool ExistedAuthCookie { get; }

        ///// <summary>
        ///// Updates the authentication status of the user.
        ///// </summary>
        ///// <param name="value">True if authenticated, false otherwise.</param>
        //void UpdateExistedAuthCookie(bool value);

        ///// <summary>
        ///// Gets the current authenticated user's ID.
        ///// </summary>
        //string UserId { get; }

        ///// <summary>
        ///// Updates the authenticated user's ID.
        ///// </summary>
        ///// <param name="userId">The new user ID.</param>
        //void UpdateUserId(string userId);

        ///// <summary>
        ///// Checks if a valid user ID exists (not the default "0").
        ///// </summary>
        ///// <returns>True if the user ID is valid, otherwise false.</returns>
        //bool HasUserId();

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
        /// Indicates whether the current App Loading Status.
        /// </summary>
        APP_LOADING_STATUS AppLoadingStatus { get; }

        /// <summary>
        /// Updates the AppLoading status of the user.
        /// </summary>
        /// <param name="value">AppLoading Status</param>
        void UpdateAppLoadingStatus(APP_LOADING_STATUS appLoadingStatus);


        /// <summary>
        /// Indicates whether the current user is authenticated.
        /// </summary>
        bool IsCloudConnected { get; }

        /// <summary>
        /// Updates the authentication status of the user.
        /// </summary>
        /// <param name="value">True if authenticated, false otherwise.</param>
        void UpdateIsCloudConnected(bool value);
        //Task InitializeCloudMonitorAsync();

        ///// <summary>
        ///// Gets the current font size.
        ///// </summary>
        //string FontSize { get; }

        ///// <summary>
        ///// Updates the font size.
        ///// </summary>
        ///// <param name="fontSize">The new font size.</param>
        //void UpdateFontSize(string fontSize);

        ///// <summary>
        ///// Checks if a valid font size exists (not the default string.empty).
        ///// </summary>
        ///// <returns>True if the font size is valid, otherwise false.</returns>
        //bool HasFontSize();


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
        /// Gets the current SecureStorageID.
        /// </summary>
        string SecureStorageID { get; }

        /// <summary>
        /// Updates the SecureStorageID
        /// </summary>
        /// <param name="SecureStorageID">The new SecureStorageID.</param>
        void UpdateSecureStorageID(string secureStorageID);

        /// <summary>
        /// Checks if a valid font SecureStorage ID (not the default string.empty).
        /// </summary>
        /// <returns>True if the SecureStorageIDis valid, otherwise false.</returns>
        bool HasSecureStorageID();


        /// <summary>
        /// Gets the current Display.
        /// </summary>
        string Display { get; }

        /// <summary>
        /// Updates the Display
        /// </summary>
        /// <param name="Display">The new font size.</param>
        void UpdateDisplay(string display);

        /// <summary>
        /// Checks if a valid Display (not the default string.empty).
        /// </summary>
        /// <returns>True if the Display is valid, otherwise false.</returns>
        bool HasDisplay();


        /// <summary>
        /// Gets the current Pepper.
        /// </summary>
        string Pepper { get; }

        /// <summary>
        /// Updates the Pepper
        /// </summary>
        /// <param name="Pepper">The new font size.</param>
        void UpdatePepper(string pepper);

        /// <summary>
        /// Checks if a valid Pepper (not the default string.empty).
        /// </summary>
        /// <returns>True if the Pepper is valid, otherwise false.</returns>
        bool HasPepper();



        /// <summary>
        /// Gets the current StorageLocation.
        /// </summary>
        STORAGE_LOCATION StorageLocation { get; }

        /// <summary>
        /// Updates the StorageLocation
        /// </summary>
        /// <param name="StorageLocation">The new StorageLocation.</param>
        void UpdateStorageLocation(STORAGE_LOCATION storageLocation);

        /// <summary>
        /// Checks if a valid StorageLocation (not the default STORAGE_LOCATION.Unknown).
        /// </summary>
        /// <returns>True if the StorageLocation is valid, otherwise false.</returns>
        bool HasStorageLocation();

        /// <summary>
        /// Gets the current SelectedLanguage.
        /// </summary>
        string SelectedLanguage { get; }

        /// <summary>
        /// Updates the SelectedLanguage
        /// </summary>
        /// <param name="SelectedLanguage">The new SelectedLanguage.</param>
        void UpdateSelectedLanguage(string selectedLanguage);

        /// <summary>
        /// Checks if a valid SelectedLanguage (not the default string.empty).
        /// </summary>
        /// <returns>True if the SelectedLanguage is valid, otherwise false.</returns>
        bool HasSelectedLanguage();


        /// <summary>
        /// Gets the current Local SqLite Db-Path.
        /// </summary>
        string LocalSqLiteDbPath { get; }

        /// <summary>
        /// Updates the Local SqLite Db-Path
        /// </summary>
        /// <param name="LocalSqLiteDbPath">The new Local SqLite Db-Path.</param>
        void UpdateLocalSqLiteDbPath(string localSqLiteDbPath);

        /// <summary>
        /// Checks if a valid SelectedLanguage (not the default string.empty).
        /// </summary>
        /// <returns>True if the SelectedLanguage is valid, otherwise false.</returns>
        bool HasLocalSqLiteDbPath();


        /// <summary>
        /// Gets the current Idp (ext. Identity Provider).
        /// </summary>
        string IdP { get; }

        /// <summary>
        /// Updates the Idp
        /// </summary>
        /// <param name="Idp">The new Idp.</param>
        void UpdateIdP(string idp);

        /// <summary>
        /// Checks if a valid Idp (not the default string.empty).
        /// </summary>
        /// <returns>True if the Idp is valid, otherwise false.</returns>
        bool HasIdP();



        /// <summary>
        /// Gets the current Ltr-Rtl.
        /// </summary>
        LTR_RTL LtrRtl { get; }

        /// <summary>
        /// Updates the LtrRtl
        /// </summary>
        /// <param name="LtrRtl">The new LtrRtl.</param>
        void UpdateLtrRtl(LTR_RTL ltrRtl);

        /// <summary>
        /// Checks if a valid LtrRtl (not the default string.empty).
        /// </summary>
        /// <returns>True if the LtrRtl is valid, otherwise false.</returns>
        bool HasLtrRtl();


        ///// <summary>
        ///// Gets the current app css theme.
        ///// </summary>
        //string CssTheme { get; }

        ///// <summary>
        ///// Updates the css theme
        ///// </summary>
        ///// <param name="CssTheme">The new CssTheme.</param>
        //void UpdateCssTheme(string cssTheme);

        ///// <summary>
        ///// Checks if a valid CssTheme (not the default string.empty).
        ///// </summary>
        ///// <returns>True if the CssTheme is valid, otherwise false.</returns>
        //bool HasCssTheme();


        /// <summary>
        /// Gets the current Alias.
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// Updates the Alias
        /// </summary>
        /// <param name="Alias">The new Alias.</param>
        void UpdateAlias(string alias);

        /// <summary>
        /// Checks if a valid Alias (not the default string.empty).
        /// </summary>
        /// <returns>True if the Alias is valid, otherwise false.</returns>
        bool HasAlias();


        ///// <summary>
        ///// Gets the current AppProjectTyBlazorCore.
        ///// </summary>
        //APP_PROJECT_TYPE AppProjectType { get; }

        ///// <summary>
        ///// Updates the AppProjectType
        ///// </summary>
        ///// <param name="AppProjectType">The new AppProjectTyBlazorCore.</param>
        //void UpdateAppProjectType(APP_PROJECT_TYPE appProjectType);

        ///// <summary>
        ///// Checks if a valid AppProjectType (not the default string.empty).
        ///// </summary>
        ///// <returns>True if the AppProjectType is valid, otherwise false.</returns>
        //bool HasAppProjectType();


        /// <summary>
        /// Gets the current AliasImg.
        /// </summary>
        string AliasImg { get; }

        /// <summary>
        /// Updates the AliasImg
        /// </summary>
        /// <param name="AliasImg">The new AliasImg.</param>
        void UpdateAliasImg(string aliasImg);

        /// <summary>
        /// Checks if a valid AliasImg (not the default string.empty).
        /// </summary>
        /// <returns>True if the AliasImg is valid, otherwise false.</returns>
        bool HasAliasImg();


        /// <summary>
        /// Gets the current WebApiToken.
        /// </summary>
        string WebApiToken { get; }

        /// <summary>
        /// Updates the WebApiToken
        /// </summary>
        /// <param name="WebApiToken">The new WebApiToken.</param>
        void UpdateWebApiToken(string webApiToken);

        /// <summary>
        /// Checks if a valid WebApiToken (not the default string.empty).
        /// </summary>
        /// <returns>True if the WebApiToken is valid, otherwise false.</returns>
        bool HasWebApiToken();


        /// <summary>
        /// Gets the current UnixTS.
        /// </summary>
        string UnixTS { get; }

        /// <summary>
        /// Updates the UnixTS
        /// </summary>
        /// <param name="UnixTS">The new UnixTS.</param>
        void UpdateUnixTS(string unixTS);

        /// <summary>
        /// Checks if a valid UnixTS (not the default string.empty).
        /// </summary>
        /// <returns>True if the UnixTS is valid, otherwise false.</returns>
        bool HasUnixTS();


        /// <summary>
        /// Gets the current UserAccount.
        /// </summary>
        string UserAccount { get; }

        /// <summary>
        /// Updates the UserAccount
        /// </summary>
        /// <param name="UserAccount">The new UnixTS.</param>
        void UpdateUserAccount(string userAccount);

        /// <summary>
        /// Checks if a valid UserAccount (not the default string.empty).
        /// </summary>
        /// <returns>True if the UserAccount is valid, otherwise false.</returns>
        bool HasUserAccount();


        /// <summary>
        /// Gets the current TOP (count of loaded DB-Records).
        /// </summary>
        int TOP { get; }

        /// <summary>
        /// Updates the TOP
        /// </summary>
        /// <param name="TOP">The new TOP.</param>
        void UpdateTOP(int top);

        /// <summary>
        /// Checks if a valid TOP (not the default 0).
        /// </summary>
        /// <returns>True if the TOP is valid, otherwise false.</returns>
        bool HasTOP();


        ///// <summary>
        ///// Gets the current Pin-Info for local login.
        ///// </summary>
        //List<PinsModel> Pin { get; set; }

        ///// <summary>
        ///// Updates the Pin
        ///// </summary>
        ///// <param name="Pin">The new Pin.</param>
        //void UpdatePin(string pin);

        ///// <summary>
        ///// Checks if a valid Pin (not the default empty).
        ///// </summary>
        ///// <returns>True if the IsPin is valid, otherwise empty.</returns>
        //bool HasPin();


        /// <summary>
        /// Gets the current IsNativeApp status.
        /// </summary>
        bool? IsNativeApp { get; }

        /// <summary>
        /// Updates the IsNativeApp status
        /// </summary>
        /// <param name="IsNativeApp">The new IsNativeApp status.</param>
        //void UpdateIsNativeApp(bool? isNativeApp);
        //void UpdateIsNativeApp();
        /// <summary>
        /// Aktualisiert den Status, ob die App nativ läuft oder im Web.
        /// Da die Abfrage über die Capacitor-Bridge asynchron erfolgt, ist dies ein Task.
        /// </summary>
        Task UpdateIsNativeApp(); // Geändert von void zu Task

        /// <summary>
        /// Checks if a valid IsNativeApp (not the default null).
        /// </summary>
        /// <returns>True if the font size is valid, otherwise false.</returns>
        bool HasIsNativeApp();


        /// <summary>
        /// Gets the Native Device tyBlazorCore.
        /// </summary>
        p11.UI.NativeDevice NativeDeviceType { get; }

        /// <summary>
        /// Aktualisiert den Status, ob die App nativ läuft oder im Web.
        /// Da die Abfrage über die Capacitor-Bridge asynchron erfolgt, ist dies ein Task.
        /// </summary>
        void UpdateNativeDeviceType(); // Geändert von void zu Task


        /// <summary>
        /// Gets the current IsPhone status.
        /// </summary>
        bool IsPhone { get; }

        /// <summary>
        /// Aktualisiert den Status, ob die App auf phone läuft.
        /// </summary>
        //Task UpdateIsPhone();

        ///// <summary>
        ///// Checks if a valid IsPhone (not the default null).
        ///// </summary>
        ///// <returns>True if is valid, otherwise false.</returns>
        //bool HasIsPhone();


        /// <summary>
        /// Gets the current IsDesktop status.
        /// </summary>
        bool IsDesktop { get; }

        /// <summary>
        /// Aktualisiert den Status, ob die App auf desktop läuft.
        /// </summary>
        void UpdateIsPhoneOrDesktop();

        ///// <summary>
        ///// Checks if a valid IsDesktop (not the default null).
        ///// </summary>
        ///// <returns>True if is valid, otherwise false.</returns>
        //bool HasIsDesktop();


        /// <summary>
        /// Gets the current Settings.
        /// </summary>
        List<AppParameterModel> Settings { get; }

        /// <summary>
        /// Updates the Settings
        /// </summary>
        /// <param name="Settings">The new Settings.</param>
        void UpdateSettings(List<AppParameterModel> settings);

        /// <summary>
        /// Checks if a valid Settings (not the default new()).
        /// </summary>
        /// <returns>True if the Settings is valid, otherwise null.</returns>
        bool HasSettings();


        ///// <summary>
        ///// Gets the current number of reminders.
        ///// </summary>
        //HashSet<string> Reminders { get; }

        ///// <summary>
        ///// Add new reminders
        ///// </summary>
        //void AddReminder(string reminder);

        ///// <summary>
        ///// Remove the reminders
        ///// </summary>
        //void RemoveReminder(string reminder);

        /////// <summary>
        /////// Checks if a valid number of current reminders (not the default null).
        /////// </summary>
        /////// <returns>True if the NumberCurrentReminders is valid, otherwise false.</returns>
        ////bool HasNumberCurrentReminders();


        /// <summary>
        /// Gets the current Meta-Settings.
        /// </summary>
        List<SettingMetadataModel> SettingsMeta { get; }

        /// <summary>
        /// Updates the Meta-Settings
        /// </summary>
        /// <param name="Settings">The new Meta-Settings.</param>
        void UpdateSettingsMeta(List<SettingMetadataModel> settings);

        /// <summary>
        /// Checks if a valid Meta-Settings (not the default new()).
        /// </summary>
        /// <returns>True if the Meta-Settings is valid, otherwise null.</returns>
        bool HasSettingsMeta();


        TodoCountModel RecordCounts { get; set; }


        Task<int> GetScreenWidthLevel();



        //Task<bool> IsInternetConnected();

        /// <summary>
        /// Gets the current IsInternetConnected status.
        /// Used for synchronous access in Blazor components (caching).
        /// </summary>
        bool IsInternetConnected { get; }

        /// <summary>
        /// Updates the IsInternetConnected status cache.
        /// This method is called by the Platform service when a native connectivity change occurs.
        /// </summary>
        /// <param name="isConnected">The new IsInternetConnected status.</param>
        void SetInternetStatus(bool isConnected);



        /// <summary>
        /// Resets user-specific credentials (login, tokens, etc.).
        /// </summary>
        void ResetUserCredential();

        /// <summary>
        /// Load selected language
        /// </summary>
        Task LoadTranslations(bool routesStateHasChanged, string selectedLanguage = "");

        /// <summary>
        /// Get translated text
        /// </summary>
        string T(string englishTerm);

        /// <summary>
        /// Resets the complete application state to defaults.
        /// </summary>
        //void ResetAppState();


        Task<ReadModel<AppParameterModel?>?> ReadAppParameter(STORAGE_LOCATION storagelocation);


        Task<List<AppParameterModel>> GetAppParameter();

        Task LoadStorageLocation();

        //void InitializeUnixTSAuthsuser_UnixTS(string unixTS);
        void InitializeUnixTSUser(string unixTS);
        void InitializeUnixTSDeviceId(string deviceInfo);
        string GenerateUniqueId();


        // Js-Logs
        void Initialize(Microsoft.JSInterop.IJSRuntime js);
        Task Log(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null, bool isDebugEnabled = false);
        Task Warn(string msg, object? data = null);
        Task Error(string msg, object? data = null);

    }
}
