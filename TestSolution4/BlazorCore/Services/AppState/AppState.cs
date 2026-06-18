using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.JSInterop;
using p11.UI.Models;
using BlazorCore.Models;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlazorCore.Services.AppState
{
    /// <summary>
    /// Default implementation of <see cref="IAppState"/>.
    /// Holds the application's global state and provides mechanisms to update and notify changes.
    /// </summary>
    public class AppStateBase : IAppStateBase
    {
        public Sections Catalog { get; private set; } = new();
        public Dataset Data { get; private set; } = new();

        public void GlobalInit(Sections catalog, Dataset data)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }



        private readonly IServiceProvider _serviceProvider;
        //private readonly IAppStateBase _appstate;
        private readonly IPlatformBase _platform;
        private readonly IGlobalStateBase _globalState;
        private IDamBase? _dam; // Late Binding
        //private readonly IJSRuntime _jsRuntime;

        // Unix_TS
        //private long _lastTimestamp = (long)(DateTime.UtcNow - new DateTime(Appl.EpochYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        private readonly Random Random = new Random();
        private readonly object Lock = new object();
        // Device-ID wird einmalig berechnet und gespeichert
        private int _deviceId; // 6-stellige Geräte-ID für die Generierung von UnixTS
        // Benutzeremail-ID wird einmalig berechnet und gespeichert
        //private int _userUnixTS;
        private int _userId;  // 6-stellige User-ID für die Generierung von UnixTS (kommt von der Email)
        private HubConnection? _hubConnection;

        public AppStateBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            //_appstate = _serviceProvider.GetRequiredService<IAppStateBase>();
            _platform = _serviceProvider.GetRequiredService<IPlatformBase>();
            _globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();
            //_jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();

            // Der Aufruf des Monitorings muss hier oder in einer Initialisierungsmethode erfolgen
            //_ = InitializeConnectivityAndStatusAsync();
        }


        private TaskCompletionSource _initTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc />
        public Task Initialization => _initTcs.Task;

        /// <inheritdoc />
        public void MarkInitialized() => _initTcs.TrySetResult();

        /// <inheritdoc />
        public void ResetInitialization() =>
            _initTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);


        #region Events
        // Subscribe events
        /// <inheritdoc />
        public event Action? OnChange;

        /// <inheritdoc />
        public event Action? OnReload; // Event für "Reload" (z.B. in einer komponente)

        /// <inheritdoc />
        public event Action<string>? OnMessage; // Event für "Message" (z.B. in einer komponente)


        /// <inheritdoc />
        public event Action? OnAddNewTodo;
        public void AddNewTodo() => OnAddNewTodo?.Invoke();

        public event Action? OnRefreshLandingpage;
        public void RefreshLandingpage() => OnRefreshLandingpage?.Invoke();

        public event Action? OnRoutesStateHasChanged;
        public void RoutesStateHasChanged() => OnRoutesStateHasChanged?.Invoke();

        public event Action? OnLanguageHasChanged;
        public void LanguageHasChanged() => OnLanguageHasChanged?.Invoke();

        //public event Action? OnHideModalNativeHeaderFooter;
        //public void HideModalNativeHeaderFooter() => OnHideModalNativeHeaderFooter?.Invoke();
        public event Action<bool>? OnHideModalNativeHeaderFooter;
        public void HideModalNativeHeaderFooter(bool isHide)
        {
            // Das Fragezeichen prüft, ob Abonnenten vorhanden sind
            OnHideModalNativeHeaderFooter?.Invoke(isHide);
        }

        //public event Func<Task>? OnRoutesInitializeAppAsync;
        //public async Task RoutesInitializeAppAsync()
        //{
        //    if (OnRoutesInitializeAppAsync is not null)
        //    {
        //        var handlers = OnRoutesInitializeAppAsync.GetInvocationList().Cast<Func<Task>>();
        //        await Task.WhenAll(handlers.Select(h => h.Invoke()));
        //    }
        //}

        public event Action? OnOpenAboutHome;
        public void OpenAboutHome() => OnOpenAboutHome?.Invoke();

        public event Action? OnToggleSettingsHome;
        public void ToggleSettingsHome() => OnToggleSettingsHome?.Invoke();

        public event Action? OnSearchTodo;
        public void SearchTodo() => OnSearchTodo?.Invoke();

        public event Action? OnSortTodo;
        public void SortTodo() => OnSortTodo?.Invoke();


        public event Func<string, Task>? OnRefreshTodoUnixTSAsync;
        public async Task RefreshTodoUnixTSAsync(string unixTS)
        {
            if (OnRefreshTodoUnixTSAsync is not null)
            {
                // 1. Alle Abonnenten (Handler) als Func<TodoModel, Task> ermitteln
                var handlers = OnRefreshTodoUnixTSAsync.GetInvocationList().Cast<Func<string, Task>>();

                // 2. Jeden Handler mit dem 'item' aufrufen und auf alle warten
                await Task.WhenAll(handlers.Select(h => h.Invoke(unixTS)));
            }
        }

        public event Func<Task>? OnTriggerEvent01Async;
        public async Task TriggerEvent01Async()
        {
            if (OnTriggerEvent01Async is not null)
            {
                var handlers = OnTriggerEvent01Async.GetInvocationList().Cast<Func<Task>>();
                await Task.WhenAll(handlers.Select(h => h.Invoke()));
            }
        }


        /// <inheritdoc />
        public event Action? OnParametersSetAuthenticationExtend;



        /// <summary>
        /// Notifies all subscribers that a state change has occurred.
        /// </summary>
        //private void NotifyStateChanged() => OnChange?.Invoke();
        //private void NotifyStateChanged()
        //{
        //    // Wir fangen potenzielle Fehler ab, falls eine Komponente 
        //    // während des Debuggings in einem ungültigen Status ist
        //    try
        //    {
        //        // ?.Invoke() ist sicher, wenn niemand zuhört passiert nichts
        //        OnChange?.Invoke();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Im Release passiert hier nichts, im Debugger sehen wir es im Output
        //        System.Diagnostics.Debug.WriteLine($"NotifyStateChanged ignored: {ex.Message}");
        //    }
        //}
        private void NotifyStateChanged()
        {
            if (OnChange == null)
                return;

            foreach (var handler in OnChange.GetInvocationList())
            {
                try
                {
                    ((Action)handler).Invoke();
                }
                catch (ObjectDisposedException)
                {
                    // UI-Komponente ist weg → OK, ignorieren
                }
                catch (Exception ex)
                {
                    LogVoid($"State subscriber failed: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// Notifies all subscribers to trigger data reloading.
        /// </summary>
        public void Reload() => OnReload?.Invoke();

        /// <summary>
        /// Notifies all subscribers to display a sending message.
        /// </summary>
        public void Message(string message) => OnMessage?.Invoke(message);

        /// <summary>
        /// Notifies all subscribers to trigger set parameter in AuthenticationExtend.razor
        /// </summary>
        public void ParametersSetAuthenticationExtend() => OnParametersSetAuthenticationExtend?.Invoke();
        #endregion



        #region Properties
        // --- Properties ---

        private bool _is2FAActivated = false;

        /// <inheritdoc />
        public bool Is2FAActivated
        {
            get => _is2FAActivated;
            private set => SetProperty(ref _is2FAActivated, value);
        }
        public void UpdateIs2FAActivated(bool is2FAActivated) => Is2FAActivated = is2FAActivated;


        // --- Properties ---

        private bool _isAppInitialized = false;

        /// <inheritdoc />
        public bool IsAppInitialized
        {
            get => _isAppInitialized;
            private set => SetProperty(ref _isAppInitialized, value);
        }
        public void UpdateIsAppInitialized(bool isAppInitialized) => IsAppInitialized = isAppInitialized;


        // --- Propertie ---

        private bool _isAuthenticated;

        /// <inheritdoc />
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set => SetProperty(ref _isAuthenticated, value);
        }

        /// <inheritdoc />
        public void UpdateIsAuthenticated(bool value) => IsAuthenticated = value;

        // --- Propertie ---

        private APP_LOADING_STATUS _appLoadingStatus;

        /// <inheritdoc />
        public APP_LOADING_STATUS AppLoadingStatus
        {
            get => _appLoadingStatus;
            private set => SetProperty(ref _appLoadingStatus, value);
        }

        /// <inheritdoc />
        public void UpdateAppLoadingStatus(APP_LOADING_STATUS value) => AppLoadingStatus = value;

        // --- Propertie ---

        private bool _isCloudConnected;

        /// <inheritdoc />
        public bool IsCloudConnected
        {
            get => _isCloudConnected;
            private set => SetProperty(ref _isCloudConnected, value);
        }
        //public void UpdateIsCloudConnected(bool isCloudConnected) => IsCloudConnected = isCloudConnected;
        public void UpdateIsCloudConnected(bool value)
        {
            if (IsCloudConnected != value)
            {
                IsCloudConnected = value;
                NotifyStateChanged();
            }
        }
        //public async Task InitializeCloudMonitorAsync()
        //{
        //    // JS-Fallback immer aktivieren (schadet Server nicht)
        //    await _jsRuntime.InvokeVoidAsync("pE_Web.cloudConnectivity.init",
        //        DotNetObjectReference.Create(this));

        //    // bestehende SignalR-Logik UNVERÄNDERT
        //    if (_hubConnection != null)
        //        return;

        //    _hubConnection = new HubConnectionBuilder()
        //        .WithUrl(_globalState.tpcWebApi.url_AuthHub)
        //        .WithAutomaticReconnect()
        //        .Build();

        //    _hubConnection.Closed += _ =>
        //    {
        //        UpdateIsCloudConnected(false);
        //        return Task.CompletedTask;
        //    };

        //    _hubConnection.Reconnecting += _ =>
        //    {
        //        UpdateIsCloudConnected(false);
        //        return Task.CompletedTask;
        //    };

        //    _hubConnection.Reconnected += _ =>
        //    {
        //        UpdateIsCloudConnected(true);
        //        return Task.CompletedTask;
        //    };

        //    try
        //    {
        //        await _hubConnection.StartAsync();
        //        UpdateIsCloudConnected(
        //            _hubConnection.State == HubConnectionState.Connected);
        //    }
        //    catch
        //    {
        //        UpdateIsCloudConnected(false);
        //    }
        //}
        //[JSInvokable]
        //public void OnBrowserInternetChanged(bool isOnline)
        //{
        //    // Fallback-Logik:
        //    // Wenn Browser offline → Cloud MUSS offline sein
        //    if (!isOnline)
        //    {
        //        UpdateIsCloudConnected(false);
        //        return;
        //    }

        //    // Wenn Browser online:
        //    // Cloud-Status NICHT erzwingen, SignalR entscheidet
        //}


        // --- Propertie ---

        private bool _isRootPageLoaded;

        /// <inheritdoc />
        public bool IsRootPageLoaded
        {
            get => _isRootPageLoaded;
            private set => SetProperty(ref _isRootPageLoaded, value);
        }
        public void UpdateIsRootPageLoaded(bool value)
        {
            if (IsRootPageLoaded != value)
            {
                IsRootPageLoaded = value;
                NotifyStateChanged();
            }
        }

        

        // --- Propertie ---

        private string _secureStorageID = "0";

        /// <inheritdoc />
        public string SecureStorageID
        {
            get => _secureStorageID;
            private set => SetProperty(ref _secureStorageID, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateSecureStorageID(string secureStorageID) => SecureStorageID = secureStorageID;

        /// <inheritdoc />
        public bool HasSecureStorageID() => _secureStorageID != string.Empty;


        // --- Propertie ---

        private string _display = "0";

        /// <inheritdoc />
        public string Display
        {
            get => _display;
            private set => SetProperty(ref _display, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateDisplay(string display) => Display = display;

        /// <inheritdoc />
        public bool HasDisplay() => _display != string.Empty;


        // --- Propertie ---

        private string _pepper = "0";

        /// <inheritdoc />
        public string Pepper
        {
            get => _pepper;
            private set => SetProperty(ref _pepper, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdatePepper(string pepper) => Pepper = pepper;

        /// <inheritdoc />
        public bool HasPepper() => _pepper != string.Empty;


        // --- Propertie ---

        private STORAGE_LOCATION _storageLocation = STORAGE_LOCATION.Unknown;

        /// <inheritdoc />
        public STORAGE_LOCATION StorageLocation
        {
            get => _storageLocation;
            private set => SetProperty(ref _storageLocation, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateStorageLocation(STORAGE_LOCATION storageLocation) => StorageLocation = storageLocation;

        /// <inheritdoc />
        public bool HasStorageLocation() => _storageLocation != STORAGE_LOCATION.Unknown;
             

        // --- Propertie ---

        private string _webApiToken = string.Empty;

        /// <inheritdoc />
        public string WebApiToken
        {
            get => _webApiToken;
            private set => SetProperty(ref _webApiToken, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateWebApiToken(string webApiToken) => WebApiToken = webApiToken;

        /// <inheritdoc />
        public bool HasWebApiToken() => _webApiToken != string.Empty;


        // --- Propertie ---

        private string _selectedLanguage = string.Empty;

        /// <inheritdoc />
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            private set => SetProperty(ref _selectedLanguage, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateSelectedLanguage(string selectedLanguage) => SelectedLanguage = selectedLanguage;

        /// <inheritdoc />
        public bool HasSelectedLanguage() => _selectedLanguage != string.Empty;


        // --- Propertie ---

        private string _localSqLiteDbPath = string.Empty;

        /// <inheritdoc />
        public string LocalSqLiteDbPath
        {
            get => _localSqLiteDbPath;
            private set => SetProperty(ref _localSqLiteDbPath, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateLocalSqLiteDbPath(string localSqLiteDbPath) => LocalSqLiteDbPath = localSqLiteDbPath;

        /// <inheritdoc />
        public bool HasLocalSqLiteDbPath() => _localSqLiteDbPath != string.Empty;


        // --- Propertie ---

        private string _idP = string.Empty;

        /// <inheritdoc />
        public string IdP
        {
            get => _idP;
            private set => SetProperty(ref _idP, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateIdP(string idP) => IdP = idP;

        /// <inheritdoc />
        public bool HasIdP() => _idP != string.Empty;


        // --- Propertie ---

        private LTR_RTL _ltrRtl = LTR_RTL.LTR;

        /// <inheritdoc />
        public LTR_RTL LtrRtl
        {
            get => _ltrRtl;
            private set => SetProperty(ref _ltrRtl, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateLtrRtl(LTR_RTL ltrRtl) => LtrRtl = ltrRtl;

        /// <inheritdoc />
        public bool HasLtrRtl() => _ltrRtl != LTR_RTL.Unknown;


        // --- Propertie ---

        private string _alias = string.Empty;

        /// <inheritdoc />
        public string Alias
        {
            get => _alias;
            private set => SetProperty(ref _alias, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateAlias(string alias) => Alias = alias;

        /// <inheritdoc />
        public bool HasAlias() => _alias != string.Empty;


        //// --- Propertie ---

        //private APP_PROJECT_TYPE _appProjectType = APP_PROJECT_TYBlazorCore.Unknown;

        ///// <inheritdoc />
        //public APP_PROJECT_TYPE AppProjectType
        //{
        //    get => _appProjectType;
        //    private set => SetProperty(ref _appProjectType, value, notify: false);
        //}

        ///// <inheritdoc />
        //public void UpdateAppProjectType(APP_PROJECT_TYPE appProjectType) => AppProjectType = appProjectType;

        ///// <inheritdoc />
        //public bool HasAppProjectType() => _appProjectType != APP_PROJECT_TYBlazorCore.Unknown;


        // --- Propertie ---

        private string _aliasImg = string.Empty;

        /// <inheritdoc />
        public string AliasImg
        {
            get => _aliasImg;
            private set => SetProperty(ref _aliasImg, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateAliasImg(string aliasImg) => AliasImg = aliasImg;

        /// <inheritdoc />
        public bool HasAliasImg() => _aliasImg != string.Empty;


        // --- Propertie ---

        private string _unixTS = string.Empty;

        /// <inheritdoc />
        public string UnixTS
        {
            get => _unixTS;
            private set => SetProperty(ref _unixTS, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateUnixTS(string unixTS) => UnixTS = unixTS;

        /// <inheritdoc />
        public bool HasUnixTS() => _unixTS != string.Empty;


        // --- Propertie ---

        private string _userAccount = string.Empty;

        /// <inheritdoc />
        public string UserAccount
        {
            get => _userAccount;
            private set => SetProperty(ref _userAccount, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateUserAccount(string userAccount) => UserAccount = userAccount;

        /// <inheritdoc />
        public bool HasUserAccount() => _userAccount.Trim() != string.Empty;


        // --- Propertie ---

        private int _top = 100;

        /// <inheritdoc />
        public int TOP
        {
            get => _top;
            private set => SetProperty(ref _top, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateTOP(int top) => TOP = top;

        /// <inheritdoc />
        public bool HasTOP() => _top != 0;


        // --- Propertie ---

        //public List<PinsModel> Pin { get; set; } = new();

        ///// <inheritdoc />
        //public bool HasPin() => Pin.Any();

        //private string _pin;

        ///// <inheritdoc />
        //public string Pin
        //{
        //    get => _pin;
        //    private set => SetProperty(ref _pin, value, notify: false);
        //}

        ///// <inheritdoc />
        //public void UpdatePin(string pin) => Pin = pin;

        ///// <inheritdoc />
        //public bool HasPin() => _pin.Trim() != string.Empty;


        // --- Propertie ---

        private bool? _isNativeApp = null;

        /// <inheritdoc />
        public bool? IsNativeApp
        {
            get => _isNativeApp;
            private set => SetProperty(ref _isNativeApp, value, notify: false);
        }

        ///// <inheritdoc />
        ////public void UpdateIsNativeApp(bool? isNativeApp) => IsNativeApp = isNativeApp;
        //public void UpdateIsNativeApp()
        //{
        //    IsNativeApp = _platform.GetFormFactor()
        //        .Equals("web", StringComparison.OrdinalIgnoreCase) == false;
        //}
        /// <summary>
        /// Aktualisiert den Status asynchron.
        /// </summary>
        public async Task UpdateIsNativeApp() // Jetzt async Task
        {
            // Wir warten auf das Ergebnis der JS-Bridge
            var formFactor = await _platform.GetFormFactor();

            // Logik: Alles was nicht "Web" ist, gilt als Native (mobile, tablet, tv)
            IsNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool HasIsNativeApp() => _isNativeApp != null;


        // --- Propertie ---

        private p11.UI.NativeDevice _nativeDeviceType = p11.UI.NativeDevice.WEB;

        /// <inheritdoc />
        public p11.UI.NativeDevice NativeDeviceType
        {
            get => _nativeDeviceType;
            private set => SetProperty(ref _nativeDeviceType, value, notify: false);
        }

        /// <summary>
        /// Aktualisiert den Status asynchron.
        /// </summary>
        public void UpdateNativeDeviceType() // Jetzt async Task
        {
            // Wir warten auf das Ergebnis der JS-Bridge
            NativeDeviceType = _platform.GetCurrDevice();
        }


        // --- Propertie ---

        private bool _isPhone = false;

        /// <inheritdoc />
        public bool IsPhone
        {
            get => _isPhone;
            private set => SetProperty(ref _isPhone, value, notify: false);
        }

        ///// <inheritdoc />
        //public bool HasIsPhone() => _isPhone != null;


        // --- Propertie ---

        private bool _isDesktop = false;

        /// <inheritdoc />
        public bool IsDesktop
        {
            get => _isDesktop;
            private set => SetProperty(ref _isDesktop, value, notify: false);
        }

        ///// <inheritdoc />
        //public bool HasIsDesktop() => _isDesktop != null;

        /// <summary>
        /// Aktualisiert den Status asynchron.
        /// </summary>
        public void UpdateIsPhoneOrDesktop() // Jetzt async Task
        {
            var currPlatform = _platform.GetCurrPlatform();
            switch (currPlatform)
            {
                case PLATFORMS.WINDOWS_CLIENT:
                case PLATFORMS.MAC_CLIENT:
                    IsDesktop = true;
                    break;

                case PLATFORMS.ANDROID:
                case PLATFORMS.IOS:
                    IsPhone = true;
                    break;
            }
        }


        // --- Propertie ---

        private TodoCountModel _recordCounts = new();
        public TodoCountModel RecordCounts
        {
            get => _recordCounts;
            set
            {
                _recordCounts = value;
                NotifyStateChanged();
            }
        }


        // --- Propertie ---

        /// <inheritdoc />
        public async Task<int> GetScreenWidthLevel() => (await _platform.GetWindowWidth()) switch
        {
            < 600.0 => 0,
            <= 1200.0 => 1,
            _ => 2
        };


        // --- Propertie ---

        private bool _isInternetConnectedCache = true;

        // Die neue, synchrone Property, die den Cache-Wert zurückgibt
        public bool IsInternetConnected => _isInternetConnectedCache;

        //private async Task InitializeConnectivityAndStatusAsync()
        //{
        //    // 1. Abonnement starten (Synchroner Teil)
        //    // Die Platform-Klasse abonniert das MAUI-Event und ruft bei Änderungen SetInternetStatus(isConnected) auf.
        //    _platform.StartConnectivityMonitoring(this);

        //    // 2. Initialen Status Asynchron abrufen und den Cache setzen.
        //    // Dies setzt den ERSTEN, KORREKTEN Wert im Cache.
        //    bool initialStatus = await _platform.InternetConnectedAsync();

        //    // 3. Cache aktualisieren und UI informieren
        //    // Setzt den Cache-Wert und feuert AppState.OnChange
        //    SetInternetStatus(initialStatus);
        //}

        // ... Ihre SetInternetStatus(bool isConnected) Methode (bleibt unverändert) ...
        public void SetInternetStatus(bool isConnected)
        {
            if (_isInternetConnectedCache != isConnected)
            {
                _isInternetConnectedCache = isConnected;
                OnChange?.Invoke(); // Feuert das Blazor-Event
            }
        }


        // --- Propertie ---

        private List<AppParameterModel> _settings = new();

        /// <inheritdoc />
        public List<AppParameterModel> Settings
        {
            get => _settings;
            private set => SetProperty(ref _settings, value, notify: false);
        }

        /// <inheritdoc />
        public void UpdateSettings(List<AppParameterModel> settings) => Settings = settings;

        /// <inheritdoc />
        public bool HasSettings() => _settings.Count > 0;


        // --- Propertie ---

        private List<SettingMetadataModel> _settingsMeta = new();

        /// <inheritdoc />
        public List<SettingMetadataModel> SettingsMeta
        {
            get => _settingsMeta;
            private set => SetProperty(ref _settingsMeta, value, notify: false); // erfolgt über NotifyStateChanged(); in GetAppParameter()
        }

        /// <inheritdoc />
        public void UpdateSettingsMeta(List<SettingMetadataModel> settingsMeta) => SettingsMeta = settingsMeta;

        /// <inheritdoc />
        public bool HasSettingsMeta() => _settingsMeta.Count > 0;


        // --- Propertie ---
        #endregion



        #region Language
        private Dictionary<string, string> LangRef
        {
            get
            {
                if (_langRef == null)
                {
                    //// WICHTIG: Prüfe ob Translations überhaupt existiert!
                    //if (_globalState?.Translations == null)
                    //{
                    //    // Fallback: Leeres Dictionary zurückgeben
                    //    return new Dictionary<string, string>();
                    //}

                    // Automatisch laden falls nicht vorhanden
                    _langRef = _globalState.Translations.GetLanguageMap(SelectedLanguage)
                              ?? _globalState.Translations.GetLanguageMap("EN")
                              ?? new Dictionary<string, string>();
                }
                //LogVoid("[Blazor AppState] private Dictionary<string, string> LangRef", data: _langRef);
                return _langRef;
            }
        }

        private Dictionary<string, string> _langRef = null!;

        public async Task LoadTranslations(bool routesStateHasChanged, string selectedLanguage = "")
        {
            await Log("[Blazor AppState] START LoadTranslations");

            try
            {
                await Log($"[Blazor AppState] routesStateHasChanged: {routesStateHasChanged} , SelectedLanguage: {SelectedLanguage} , selectedLanguage: {selectedLanguage}");

                // 1. Prüfe auf Sprachwechsel
                if (SelectedLanguage != selectedLanguage)
                {
                    if (!string.IsNullOrEmpty(selectedLanguage))
                    {
                        SelectedLanguage = selectedLanguage.ToUpper();
                    }

                    await Log($"[Blazor AppState] SelectedLanguage: {SelectedLanguage}");

                    // Referenz neu setzen
                    _langRef = _globalState.Translations.GetLanguageMap(SelectedLanguage)
                              ?? _globalState.Translations.GetLanguageMap("EN");
                    //await Log("[Blazor AppState] _globalState.Translations.GetLanguageMap, _langRef", data: _langRef);

                    // 2. REFERENZ aus GlobalState holen (keine Kopie!)
                    var languageMap = _globalState.Translations.GetLanguageMap(SelectedLanguage);
                    //await Log("[Blazor AppState] _globalState.Translations.GetLanguageMap, languageMap", data: languageMap);

                    // 3. Fallback auf Englisch, falls nicht gefunden
                    if (languageMap == null && SelectedLanguage != "EN")
                    {
                        languageMap = _globalState.Translations.GetLanguageMap("EN");
                    }

                    // 4. REFERENZ speichern (8 Bytes statt 25 KB Kopie!)
                    if (languageMap != null)
                    {
                        _langRef = languageMap;
                    }

                    // 5. UI aktualisieren
                    if (routesStateHasChanged)
                    {
                        UpdateAppLoadingStatus(APP_LOADING_STATUS.LOADING_LANGUAGE);
                        ////RoutesStateHasChanged();
                        await Task.Delay(30);
                        LanguageHasChanged();
                        await Task.Delay(30);

                        UpdateAppLoadingStatus(APP_LOADING_STATUS.READY);
                        //RoutesStateHasChanged();

                        //await Log("[Blazor AppState] UpdateAppLoadingStatus, _langRef", data: _langRef, isDebugEnabled: true);
                    }
                }
            }
            catch (Exception ex)
            {
                await Error($"[Blazor AppState] ERROR LoadTranslations: {ex.Message}");
                throw;
            }

            await Log("[Blazor AppState] END LoadTranslations");
        }

        public string T(string englishTerm)
        {
            //LogVoid($"[Blazor AppState] T , englishTerm: {englishTerm} , LangRef.TryGetValue: {(LangRef.TryGetValue(englishTerm, out var translationLog) ? translationLog : englishTerm)}");
            return LangRef.TryGetValue(englishTerm, out var translation)
                ? translation
                : englishTerm;
        }
        #endregion



        #region Methods
        /// <summary>
        /// Helper method for setting property values and optionally notifying state change.
        /// </summary>
        /// <typeparam name="T">The property tyBlazorCore.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">New value to assign.</param>
        /// <param name="notify">Whether to trigger <see cref="OnChange"/> after setting the value.</param>
        /// <returns>True if the value was changed; otherwise false.</returns>
        private bool SetProperty<T>(ref T field, T value, bool notify = true)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            if (notify) NotifyStateChanged();
            return true;
        }

        // Fontsize Werte, für die Anpassung der Schriftgrösse zur laufzeit (siehe Komponente 'BlazorCore.Pages.Fontsize')
        //public p11.UI.Models.FontSizeModel FontSize { get; set; } = new();
        public AccessibilityModel AccessibilityConfig { get; set; } = new();

        // --- Reset methods ---

        /// <inheritdoc />
        public void ResetUserCredential()
        {
            //Is2FAVerified = false;
            //AuthUsers_ID = "0";
            UnixTS = string.Empty;
            IsAuthenticated = false;
            WebApiToken = string.Empty;
            UserAccount = string.Empty;
            Alias = string.Empty;
            AliasImg = string.Empty;
            IdP = string.Empty;
            //AppProjectType = APP_PROJECT_TYBlazorCore.Unknown;
            //LtrRtl = LTR_RTL.LTR;
            //FontSize = string.Empty;
            //ExistedAuthCookie = false;
        }
        #endregion



        #region Parameters
        public async Task<List<AppParameterModel>> GetAppParameter()
        {
            if (Data?.Settings == null || Data.UserParameter == null) return new List<AppParameterModel>();

            List<AppParameterModel> result = new();
            try
            {
                // --- NEU: Einmalig asynchron am Anfang holen ---
                if(IsNativeApp ?? false)
                {
                    var formFactor = await _platform.GetFormFactor();
                    IsNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
                }
                
                //bool isNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
                // -----------------------------------------------

                if (string.IsNullOrEmpty(await UpdateAppParameter())) // Zuerst prüfen, ob DB-Settings mit definierten Appl.Settings übereinstimmen.
                {
                    // Metadaten aus Appl.Settings übernehmen
                    SettingsMeta = Data.Settings.Select(s => (SettingMetadataModel)s.Clone()).ToList();

                    // Dann Settings vom Benutzer-Speicherort holen (Cloud/lokal, Cloud oder nur lokal)
                    ReadModel<AppParameterModel?>? result_appparameter = await ReadAppParameter(StorageLocation);
                    if (result_appparameter != null && string.IsNullOrEmpty(result_appparameter.out_err) && result_appparameter.out_list != null)
                        result = result_appparameter.out_list!;

                    // SettingsMeta mit ausgelsenen Settings abgleichen
                    foreach (var item in SettingsMeta)
                    {
                        // Werte in die SettingsMeta für die Komponente platzieren
                        if (item.ParaType == SETTINGS_TYPE.STRING_SELECT || item.ParaType == SETTINGS_TYPE.STRING_COLOR || item.ParaType == SETTINGS_TYPE.STRING_INPUT)
                            item.ValStr = result.Any(x => x.ParameterName.ToLower() == item.ParaName.ToLower())
                                ? result.FirstOrDefault(x => x.ParameterName.ToLower() == item.ParaName.ToLower())!.ParameterValue
                                : string.Empty;
                        if (item.ParaType == SETTINGS_TYPE.BOOL_CHECKBOX || item.ParaType == SETTINGS_TYPE.BOOL_SWITCH)
                            item.ValBool = result.Any(x => x.ParameterName.ToLower() == item.ParaName.ToLower())
                                ? (result.FirstOrDefault(x => x.ParameterName.ToLower() == item.ParaName.ToLower())!.ParameterValue == "1" ? true : false)
                                : false;

                        // SettingsMeta nachträglich noch anpassen (z.B. dynamische Listen)
                        //bool isNativeApp = (_platform.GetFormFactor().ToLower() != "web" ? true : false);
                        switch (item.ParaName)
                        {
                            //case Data.UserParameter.StorageLocation: CS9135
                            case var x when x == Data.UserParameter.StorageMode:
                                item.ValTempl = (IsNativeApp != null && IsNativeApp.Value ? item.ValTempl : "1,Cloud");
                                item.ValStr = (IsNativeApp != null && IsNativeApp.Value ? item.ValStr : "1");
                                break;
                        }
                    }

                    // An diesr Stelle gewünschte Parameter in die AppState-Properties setzen
                    if (SettingsMeta.Any(x => x.ParaName == Data.UserParameter.Top))
                    {
                        if (int.TryParse(SettingsMeta.Where(x => x.ParaName == Data.UserParameter.Top).FirstOrDefault()!.ValStr, out int top))
                        {
                            if (top != TOP)
                            {
                                UpdateTOP(top);

                                // Liste aktualisieren, da TOP sich geändert hat (string.Empty als UnixTS, spielt hier keine Rolle)
                                await RefreshTodoUnixTSAsync(string.Empty);
                            }
                        }
                    }
                    //if (SettingsMeta.Any(x => x.ParaName == UserParameter.Local2FAPin))
                    //{
                    //    UpdateIsPin(!string.IsNullOrEmpty(SettingsMeta.Where(x => x.ParaName == UserParameter.Local2FAPin).FirstOrDefault()!.ValStr));
                    //}

                    // Changed zurücksetzen
                    SettingsMeta.ForEach(s => s.ResetChanged());

                    NotifyStateChanged(); // Aufger in 'AppStateSettings.razor' von 'StateHasChanged' mit '_appState.OnChange += StateHasChanged;'
                }
                else
                {
                    // ERROR
                }
            }
            catch (Exception)
            {

                throw;
            }
            return result;
        }

        public async Task<ReadModel<AppParameterModel?>?> ReadAppParameter(STORAGE_LOCATION storagelocation)
        {
            if (Data?.UserParameter == null) return new ReadModel<AppParameterModel?>();

            ReadModel<AppParameterModel?> result = new();
            try
            {
                _dam = _serviceProvider.GetRequiredService<IDamBase>();

                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Select>>AppParameter" },
                    { "@AuthUsers_UnixTS", UnixTS },
                    { "@Scope", "set" },
                    { "cmd_nocomparetion", "no_comparetion_cloudlocal_data" }
                };
                if ((IsNativeApp.HasValue && !IsNativeApp.Value) || storagelocation == STORAGE_LOCATION.CLOUD) // nur MSSQL abfragen
                    db_para.Add(DB_CMD.NO_LOCAL, string.Empty);
                if (storagelocation == STORAGE_LOCATION.LOCAL) // nur Realm abfragen
                    db_para.Add(DB_CMD.NO_CLOUD, string.Empty);

                // Alle DB-Settings holen
                result = await _dam.ReadData<AppParameterModel>(db_para)!;

                // Cookie/SecureStorage ermitteln und Parameter 'StorageLocation' entsprechend setzen
                if (result != null && result.out_list != null && result.out_list.Any(x => x!.ParameterName == Data.UserParameter.StorageMode))
                {
                    STORAGE_LOCATION storagelocationDB = int.TryParse(result.out_list.FirstOrDefault(x => x!.ParameterName == Data.UserParameter.StorageMode)!.ParameterValue, out int _storagelocation) ? (STORAGE_LOCATION)_storagelocation : STORAGE_LOCATION.CLOUD_LOCAL;
                    if (storagelocation != storagelocationDB)
                        result.out_list.FirstOrDefault(x => x!.ParameterName == Data.UserParameter.StorageMode)!.ParameterValue = ((int)storagelocation).ToString();
                }

            }
            catch (Exception)
            {

                throw;
            }
            return result;
        }

        private async Task<string> UpdateAppParameter()
        {
            if (Data?.Settings == null) return string.Empty;

            try
            {
                // Cookie/SecureStorage prüfen
                if (StorageLocation == STORAGE_LOCATION.CLOUD_LOCAL || StorageLocation == STORAGE_LOCATION.CLOUD)
                {
                    // Parameter aus der Cloud-DB holen
                    ReadModel<AppParameterModel?>? result_mssql = await ReadAppParameter(STORAGE_LOCATION.CLOUD);

                    if (result_mssql != null && string.IsNullOrEmpty(result_mssql.out_err))
                    {
                        // Appl.Settings durchlaufen und prüfen, ob in Cloud vorhanden. Wenn nicht, dann erstellen!
                        foreach (var item in Data.Settings)
                        {
                            if (result_mssql.out_list != null &&
                                !result_mssql.out_list
                                    .Where(x => x != null)
                                    .Any(x =>
                                        string.Equals(x!.ParameterName, item.ParaName, StringComparison.OrdinalIgnoreCase)))
                            {
                                await SaveAppParameter(item, STORAGE_LOCATION.CLOUD);
                            }
                        }
                    }
                }

                // Cookie/SecureStorage prüfen
                if ((IsNativeApp.HasValue && IsNativeApp.Value) && (StorageLocation == STORAGE_LOCATION.CLOUD_LOCAL || StorageLocation == STORAGE_LOCATION.LOCAL))
                {
                    // Parameter aus der lokalen DB holen
                    ReadModel<AppParameterModel?>? result_sqlite = await ReadAppParameter(STORAGE_LOCATION.LOCAL);

                    if (result_sqlite != null && string.IsNullOrEmpty(result_sqlite.out_err))
                    {
                        // Appl.Settings durchlaufen und prüfen, ob lokal vorhanden. Wenn nicht, dann erstellen!
                        foreach (var item in Data.Settings)
                        {
                            if (result_sqlite.out_list != null &&
                                !result_sqlite.out_list
                                    .Where(x => x != null)
                                    .Any(x =>
                                        string.Equals(x!.ParameterName, item.ParaName, StringComparison.OrdinalIgnoreCase)))
                            {
                                await SaveAppParameter(item, STORAGE_LOCATION.LOCAL);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return string.Empty;
        }

        private async Task<string> SaveAppParameter(SettingMetadataModel item, STORAGE_LOCATION storagelocation)
        {
            try
            {
                string parameterValue = string.Empty;

                if (item.ParaType == SETTINGS_TYPE.STRING_SELECT || item.ParaType == SETTINGS_TYPE.STRING_COLOR || item.ParaType == SETTINGS_TYPE.STRING_INPUT)
                    parameterValue = item.ValStr;
                if (item.ParaType == SETTINGS_TYPE.BOOL_CHECKBOX || item.ParaType == SETTINGS_TYPE.BOOL_SWITCH)
                    parameterValue = item.ValBool ? "1" : "0";

                // Wenn Parameter nicht vorhanden, dann erstellen
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Save>>AppParameter" },
                    { "@UnixTS", GenerateUniqueId() }, // Datensatz UnixTS => eindeutig/ID
                    { "@AuthUsers_UnixTS", UnixTS }, // AuthUsers-UnixTS => _appState.UnixTS
                    { "@Scope", "set" },
                    { "@Details", string.Empty },
                    { "@ParameterName", item.ParaName },
                    { "@ParameterValue", parameterValue },
                };

                switch (storagelocation)
                {
                    case STORAGE_LOCATION.CLOUD:
                        db_para[DB_CMD.NO_LOCAL] = string.Empty;
                        break;

                    case STORAGE_LOCATION.LOCAL:
                        db_para[DB_CMD.NO_CLOUD] = string.Empty;
                        break;

                    default:
                        break;
                }

                ScalarModel result = await _dam!.Save(db_para)!;
                if (result != null && !string.IsNullOrEmpty(result.out_err))
                {
                    return result.out_err;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return string.Empty;
        }

        public async Task LoadStorageLocation()
        {
            if (Catalog?.LocalStorage == null) return;

            // Storage location
            if (_platform != null)
            {
                // --- FIX: Wir warten hier auf das Ergebnis ---
                //var formFactor = await _platform.GetFormFactor();
                if (IsNativeApp ?? false)
                {
                    var formFactor = await _platform.GetFormFactor();
                    IsNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
                }

                //if (formFactor.ToLower() != "web")
                UpdateStorageLocation(BlazorCore.Services.AppState.STORAGE_LOCATION.Unknown);
                if (IsNativeApp ?? false)
                {
                    // Prüfen, ob Storage-Location im SecureStorage auf dem Gerät gespeichert ist
                    var storageLocation = await _platform!.GetAsync(Catalog.LocalStorage.storagelocation);
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                    if (storageLocation != null && string.IsNullOrEmpty(storageLocation.out_err) && !string.IsNullOrEmpty(storageLocation.out_value_str))
                    {
                        int intValue = int.Parse(storageLocation.out_value_str);
                        UpdateStorageLocation((BlazorCore.Services.AppState.STORAGE_LOCATION)intValue);
                    }
                    //else
                    //{
                    //    //if (result != null && !string.IsNullOrEmpty(result.out_err))
                    //    //{
                    //    //    await _messageBoxService!.ShowOkAsync(
                    //    //        title: _appState?.T("Error") ?? "Error",
                    //    //        message: $"{_appState?.T(BlazorCore.Utility.Appl.DefaultErrorText)} {result.out_err}"
                    //    //    );
                    //    //}

                    //    // Wenn kein Cookie/SecureStorage, dann standardmässig auf CLOUD_LOCAL setzen
                    //    UpdateStorageLocation(BlazorCore.Services.AppState.STORAGE_LOCATION.Unknown);
                    //}
                }
                //else
                //{
                //    // Bei WEB ist Storage-Location CLOUD da keine lokale DB vorhanden ist
                //    UpdateStorageLocation(BlazorCore.Services.AppState.STORAGE_LOCATION.Unknown);
                //}
            }
        }
        #endregion



        #region Unix_TS
        /// <summary>
        /// Initialisiert die Benutzeremail-ID einmalig.
        /// </summary>
        /// <param name="userEmail">Die E-Mail des Benutzers.</param>
        public void InitializeUnixTSUser(string user)
        {
            try
            {
                if (!string.IsNullOrEmpty(user))
                {
                    _userId = GetUserIdlHash(user);
                }
                else if (_userId == 0) // Falls kein User übergeben wurde
                {
                    _userId = GenerateSecureRandomId(6);
                }
            }
            catch
            {
                throw;
            }
        }

        public void InitializeUnixTSDeviceId(string deviceInfo)
        {
            try
            {
                if (_deviceId == 0)
                {
                    if (!string.IsNullOrEmpty(deviceInfo))
                    {
                        _deviceId = GetDeviceIdHash(deviceInfo);
                    }
                    else // Blazor Server Fall: keine Device-Info verfügbar
                    {
                        _deviceId = GenerateSecureRandomId(6);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Generiert eine kryptografisch sichere Zufallszahl mit der angegebenen Stellenanzahl.
        /// </summary>
        private int GenerateSecureRandomId(int digits)
        {
            int maxValue = (int)Math.Pow(10, digits); // z.B. 1.000.000 für 6 Stellen
            return RandomNumberGenerator.GetInt32(0, maxValue);
        }

        /// <summary>
        /// Generiert eine eindeutige ID als string.
        /// </summary>
        /// <returns>Eine eindeutige long-Zahl.</returns>
        public string GenerateUniqueId()
        {
            var _lastTimestamp = (long)(DateTime.UtcNow - new DateTime(_globalState.ConfigGeneral.EpochYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            var epoch = new DateTime(_globalState.ConfigGeneral.EpochYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            lock (Lock)
            {
                // Berechnung des aktuellen Zeitstempels
                long timestamp = (long)(DateTime.UtcNow - epoch).TotalMilliseconds;

                int randomRange = 1_000_000_000;   // 9-stellige Reichweite (0–999_999_999)
                const string randomFormat = "D9";  // 9-stelliges Format

                // 1. Erzeuge eine KRYPTOGRAFISCH SICHERE Zufallszahl
                int randomValue = RandomNumberGenerator.GetInt32(0, randomRange);

                // 2. Kombiniere Zeit + Zufall als STRING, nicht als long (Overflow vermeiden)
                string timestampWithRandom = timestamp.ToString() + randomValue.ToString(randomFormat);

                // 3. Prüfe, ob der neue Wert gleich dem letzten ist
                while (timestampWithRandom == _lastTimestamp.ToString())
                {
                    randomValue = RandomNumberGenerator.GetInt32(0, randomRange);
                    timestampWithRandom = timestamp.ToString() + randomValue.ToString(randomFormat);
                }

                _lastTimestamp = timestamp; // bleibt vom Typ long, wie bisher

                // 4. Kombiniere alle Komponenten zur finalen ID
                string uniqueId = timestampWithRandom +
                                  _deviceId.ToString("D6") +
                                  _userId.ToString("D6");
                //_userUnixTS.ToString("D6");

                //// 5. Optional: Padding beibehalten
                //uniqueId = uniqueId.PadLeft(35, '0');
                //return uniqueId;

                // ACHTUNG: Wir müssen hier ein Buchstabe hinzufügen, da einige Systeme UnixTS fälschlicherweise als Zahl interprätieren

                // 5. Padding auf 34 reduzieren (statt 35)
                // Dadurch bleibt Platz für das Präfix-Zeichen
                uniqueId = uniqueId.PadLeft(34, '0');

                // 6. Rückgabe mit Präfix "T" -> Gesamtlänge ist exakt 35
                return "T" + uniqueId;
            }
        }

        /// <summary>
        /// Berechnet einen Hash der Geräteinformationen, um eine 6-stellige Geräte-ID zu erzeugen.
        /// </summary>
        /// <returns>Ein 6-stelliger int-Wert, der die Geräte-ID repräsentiert.</returns>
        private int GetDeviceIdHash(string deviceinfo)
        {
            // Kombiniere mehrere Geräteinformationen
            //string deviceInfo = $"{DeviceInfo.Platform}-{DeviceInfo.Model}-{DeviceInfo.Name}";
            //string deviceinfo = PlatformUtilities.GetDeviceInfo();

            // Erzeuge einen SHA256-Hash der Geräteinformationen
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceinfo));

                // Konvertiere die ersten 4 Bytes des Hashs in einen int-Wert
                int hash = BitConverter.ToInt32(hashBytes, 0);

                // Stelle sicher, dass der Hash positiv ist und auf 6 Stellen begrenzt ist
                return Math.Abs(hash) % 1000000; // Begrenze den Hash auf 6 Stellen
            }
        }

        /// <summary>
        /// Berechnet einen Hash der Benutzer-E-Mail, um eine 6-stellige Benutzeremail-ID zu erzeugen.
        /// </summary>
        /// <param name="userEmail">Die E-Mail des Benutzers.</param>
        /// <returns>Ein 6-stelliger int-Wert, der die Benutzeremail-ID repräsentiert.</returns>
        private int GetUserIdlHash(string userId)
        {
            // Erzeuge einen SHA256-Hash der Benutzer-E-Mail
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));

                // Konvertiere die ersten 4 Bytes des Hashs in einen int-Wert
                int hash = BitConverter.ToInt32(hashBytes, 0);

                // Stelle sicher, dass der Hash positiv ist und auf 6 Stellen begrenzt ist
                return Math.Abs(hash) % 1000000; // Begrenze den Hash auf 6 Stellen
            }
        }
        #endregion


        #region Js_Logs
        private IJSRuntime? _js;
        //private bool _isDebugEnabled = Appl.IsDebugEnabled; // Lokaler Cache für das JS-Flag

        public void Initialize(IJSRuntime js)
        {
            _js = js;
        }
        public async Task Log(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null, bool isDebugEnabled = false)
        {
            var _isDebugEnabled = _globalState.ConfigGeneral.IsDebugEnabled;
            if (!_isDebugEnabled && level != AppLogLevel.Error && !isDebugEnabled)
                return;

            //var prefix = "[Blazor]";
            var method = level.ToString().ToLower();

            // WICHTIG: Prüfen, ob wir überhaupt JS zur Verfügung haben
            if (_js == null)
            {
                System.Diagnostics.Debug.WriteLine($"[NATIVE-ONLY] {level}: {msg}");
                return;
            }

            try
            {
                // Wir nutzen eine extra Variable für den Task, 
                // um den Dispatcher-Fehler spezifischer zu fangen
                if (data != null)
                    await _js.InvokeVoidAsync($"console.{method}", $"{msg}", data);
                else
                    await _js.InvokeVoidAsync($"console.{method}", $"{msg}");
            }
            catch (Exception ex)
            {
                // Dies fängt nun auch den "Dispatcher" / "Thread" Fehler ab!
                System.Diagnostics.Debug.WriteLine($"[JS-LOG-FAILED] {level}: {msg} | Error: {ex.Message}");

                // Fallback auf die native Debug-Konsole (WPF Output Window)
                if (data != null)
                {
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(data);
                        System.Diagnostics.Debug.WriteLine($"   Data: {json}");
                    }
                    catch { /* Falls Serialisierung fehlschlägt */ }
                }
            }
        }
        public void LogVoid(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null)
        {
            // Starte den Task, aber fange Fehler innerhalb des Tasks ab
            Task.Run(async () => {
                try
                {
                    await Log(msg, level, data);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Fatal LogVoid Error: " + ex.Message);
                }
            });
        }

        public Task Warn(string msg, object? data = null) =>
            Log(msg, AppLogLevel.Warn, data);

        public Task Error(string msg, object? data = null) =>
            Log(msg, AppLogLevel.Error, data);
        #endregion
    }


}
