using BlazorCore.Models;
using BlazorCore.Services.Dam;
using BlazorCore.Services.EventAggregator;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.IdGenerator;
using BlazorCore.Services.Logging;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.Translation;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using p11.UI.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BlazorCore.Services.AppState
{
    /// <summary>
    /// Default implementation of <see cref="IAppStateBase"/>.
    /// Holds the application's global state and provides mechanisms to update and notify changes.
    /// </summary>
    public class AppStateBase : IAppStateBase
    {
        // =====================================================================
        // FIELDS
        // =====================================================================

        private readonly IServiceProvider _serviceProvider;
        private readonly IPlatformBase _platform;
        private readonly IPlatformStorageBase _platformStorage;
        private readonly IGlobalStateBase _globalState;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogging _logging;
        private readonly ITranslation _translation;
        private readonly IIdGenerator _idGenerator;
        private IDamBase? _dam; // Late binding

        private readonly object _lock = new();
        private HubConnection? _hubConnection;
        private TaskCompletionSource _initTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public AppStateBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _platform = _serviceProvider.GetRequiredService<IPlatformBase>();
            _platformStorage = _serviceProvider.GetRequiredService<IPlatformStorageBase>();
            _globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();
            _eventAggregator = _serviceProvider.GetRequiredService<IEventAggregator>();
            _logging = _serviceProvider.GetRequiredService<ILogging>();
            _translation = _serviceProvider.GetRequiredService<ITranslation>();
            _idGenerator = _serviceProvider.GetRequiredService<IIdGenerator>();
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        public Sections Catalog { get; private set; } = new();
        public Dataset Data { get; private set; } = new();

        public void GlobalInit(Sections catalog, Dataset data)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public Task Initialization => _initTcs.Task;

        public void MarkInitialized() => _initTcs.TrySetResult();

        public void ResetInitialization() =>
            _initTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // =====================================================================
        // UI EVENTS
        // =====================================================================

        public event Action? OnChange;
        public event Action? OnReload;
        public event Action<string>? OnMessage;
        public event Action<bool>? OnHideModalNativeHeaderFooter;

        public void HideModalNativeHeaderFooter(bool isHide)
        {
            OnHideModalNativeHeaderFooter?.Invoke(isHide);
        }

        public void Reload() => OnReload?.Invoke();

        public void Message(string message) => OnMessage?.Invoke(message);

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
                    // UI component is disposed → ignore
                }
                catch (Exception ex)
                {
                    LogVoid($"State subscriber failed: {ex.Message}");
                }
            }
        }

        // =====================================================================
        // PROPERTY HELPERS
        // =====================================================================

        /// <summary>
        /// Helper method for setting property values and optionally notifying state change.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
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

        // =====================================================================
        // SESSION STATE PROPERTIES
        // =====================================================================

        private bool _is2FAActivated = false;
        public bool Is2FAActivated
        {
            get => _is2FAActivated;
            private set => SetProperty(ref _is2FAActivated, value);
        }
        public void UpdateIs2FAActivated(bool is2FAActivated) => Is2FAActivated = is2FAActivated;

        private bool _isAppInitialized = false;
        public bool IsAppInitialized
        {
            get => _isAppInitialized;
            private set => SetProperty(ref _isAppInitialized, value);
        }
        public void UpdateIsAppInitialized(bool isAppInitialized) => IsAppInitialized = isAppInitialized;

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set => SetProperty(ref _isAuthenticated, value);
        }
        public void UpdateIsAuthenticated(bool value) => IsAuthenticated = value;

        private APP_LOADING_STATUS _appLoadingStatus;
        public APP_LOADING_STATUS AppLoadingStatus
        {
            get => _appLoadingStatus;
            private set => SetProperty(ref _appLoadingStatus, value);
        }
        public void UpdateAppLoadingStatus(APP_LOADING_STATUS value) => AppLoadingStatus = value;

        private bool _isCloudConnected;
        public bool IsCloudConnected
        {
            get => _isCloudConnected;
            private set => SetProperty(ref _isCloudConnected, value);
        }
        public void UpdateIsCloudConnected(bool value)
        {
            if (IsCloudConnected != value)
            {
                IsCloudConnected = value;
                NotifyStateChanged();
            }
        }

        private bool _isRootPageLoaded;
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

        public AccessibilityModel AccessibilityConfig { get; set; } = new();

        // =====================================================================
        // USER CREDENTIALS & TOKENS
        // =====================================================================

        public void ResetUserCredential()
        {
            UnixTS = string.Empty;
            IsAuthenticated = false;
            WebApiToken = string.Empty;
            UserAccount = string.Empty;
            Alias = string.Empty;
            AliasImg = string.Empty;
            IdP = string.Empty;
        }

        private string _webApiToken = string.Empty;
        public string WebApiToken
        {
            get => _webApiToken;
            private set => SetProperty(ref _webApiToken, value, notify: false);
        }
        public void UpdateWebApiToken(string webApiToken) => WebApiToken = webApiToken;
        public bool HasWebApiToken() => _webApiToken != string.Empty;

        private string _unixTS = string.Empty;
        public string UnixTS
        {
            get => _unixTS;
            private set => SetProperty(ref _unixTS, value, notify: false);
        }
        public void UpdateUnixTS(string unixTS) => UnixTS = unixTS;
        public bool HasUnixTS() => _unixTS != string.Empty;

        private string _userAccount = string.Empty;
        public string UserAccount
        {
            get => _userAccount;
            private set => SetProperty(ref _userAccount, value, notify: false);
        }
        public void UpdateUserAccount(string userAccount) => UserAccount = userAccount;
        public bool HasUserAccount() => _userAccount.Trim() != string.Empty;

        private string _idP = string.Empty;
        public string IdP
        {
            get => _idP;
            private set => SetProperty(ref _idP, value, notify: false);
        }
        public void UpdateIdP(string idP) => IdP = idP;
        public bool HasIdP() => _idP != string.Empty;

        private string _pepper = "0";
        public string Pepper
        {
            get => _pepper;
            private set => SetProperty(ref _pepper, value, notify: false);
        }
        public void UpdatePepper(string pepper) => Pepper = pepper;
        public bool HasPepper() => _pepper != string.Empty;

        // =====================================================================
        // USER PREFERENCES
        // =====================================================================

        private int _top = 100;
        public int TOP
        {
            get => _top;
            private set => SetProperty(ref _top, value, notify: false);
        }
        public void UpdateTOP(int top) => TOP = top;
        public bool HasTOP() => _top != 0;

        private string _alias = string.Empty;
        public string Alias
        {
            get => _alias;
            private set => SetProperty(ref _alias, value, notify: false);
        }
        public void UpdateAlias(string alias) => Alias = alias;
        public bool HasAlias() => _alias != string.Empty;

        private string _aliasImg = string.Empty;
        public string AliasImg
        {
            get => _aliasImg;
            private set => SetProperty(ref _aliasImg, value, notify: false);
        }
        public void UpdateAliasImg(string aliasImg) => AliasImg = aliasImg;
        public bool HasAliasImg() => _aliasImg != string.Empty;

        private string _secureStorageID = "0";
        public string SecureStorageID
        {
            get => _secureStorageID;
            private set => SetProperty(ref _secureStorageID, value, notify: false);
        }
        public void UpdateSecureStorageID(string secureStorageID) => SecureStorageID = secureStorageID;
        public bool HasSecureStorageID() => _secureStorageID != string.Empty;

        private string _display = "0";
        public string Display
        {
            get => _display;
            private set => SetProperty(ref _display, value, notify: false);
        }
        public void UpdateDisplay(string display) => Display = display;
        public bool HasDisplay() => _display != string.Empty;

        private STORAGE_LOCATION _storageLocation = STORAGE_LOCATION.Unknown;
        public STORAGE_LOCATION StorageLocation
        {
            get => _storageLocation;
            private set => SetProperty(ref _storageLocation, value, notify: false);
        }
        public void UpdateStorageLocation(STORAGE_LOCATION storageLocation) => StorageLocation = storageLocation;
        public bool HasStorageLocation() => _storageLocation != STORAGE_LOCATION.Unknown;

        private string _selectedLanguage = string.Empty;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            private set => SetProperty(ref _selectedLanguage, value, notify: false);
        }
        public void UpdateSelectedLanguage(string selectedLanguage) => SelectedLanguage = selectedLanguage;
        public bool HasSelectedLanguage() => _selectedLanguage != string.Empty;

        private LTR_RTL _ltrRtl = LTR_RTL.LTR;
        public LTR_RTL LtrRtl
        {
            get => _ltrRtl;
            private set => SetProperty(ref _ltrRtl, value, notify: false);
        }
        public void UpdateLtrRtl(LTR_RTL ltrRtl) => LtrRtl = ltrRtl;
        public bool HasLtrRtl() => _ltrRtl != LTR_RTL.Unknown;

        private string _localSqLiteDbPath = string.Empty;
        public string LocalSqLiteDbPath
        {
            get => _localSqLiteDbPath;
            private set => SetProperty(ref _localSqLiteDbPath, value, notify: false);
        }
        public void UpdateLocalSqLiteDbPath(string localSqLiteDbPath) => LocalSqLiteDbPath = localSqLiteDbPath;
        public bool HasLocalSqLiteDbPath() => _localSqLiteDbPath != string.Empty;

        // =====================================================================
        // PLATFORM & DEVICE INFO
        // =====================================================================

        private bool? _isNativeApp = null;
        public bool? IsNativeApp
        {
            get => _isNativeApp;
            private set => SetProperty(ref _isNativeApp, value, notify: false);
        }

        public async Task UpdateIsNativeApp()
        {
            var formFactor = await _platform.GetFormFactor();
            IsNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
        }

        public bool HasIsNativeApp() => _isNativeApp != null;

        private p11.UI.NativeDevice _nativeDeviceType = p11.UI.NativeDevice.WEB;
        public p11.UI.NativeDevice NativeDeviceType
        {
            get => _nativeDeviceType;
            private set => SetProperty(ref _nativeDeviceType, value, notify: false);
        }

        public void UpdateNativeDeviceType(p11.UI.NativeDevice? nativeDeviceType = null)
        {
            NativeDeviceType = nativeDeviceType ?? _platform.GetCurrDevice();
        }

        private bool _isPhone = false;
        public bool IsPhone
        {
            get => _isPhone;
            private set => SetProperty(ref _isPhone, value, notify: false);
        }

        private bool _isDesktop = false;
        public bool IsDesktop
        {
            get => _isDesktop;
            private set => SetProperty(ref _isDesktop, value, notify: false);
        }

        public void UpdateIsPhoneOrDesktop()
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

        private bool _isInternetConnectedCache = true;
        public bool IsInternetConnected => _isInternetConnectedCache;

        public void SetInternetStatus(bool isConnected)
        {
            if (_isInternetConnectedCache != isConnected)
            {
                _isInternetConnectedCache = isConnected;
                OnChange?.Invoke();
            }
        }

        // =====================================================================
        // TRANSLATION (delegated to ITranslation)
        // =====================================================================

        public Task LoadTranslations(bool routesStateHasChanged, string selectedLanguage = "")
            => _translation.LoadTranslations(routesStateHasChanged, selectedLanguage);

        public string T(string englishTerm)
            => _translation.T(englishTerm);

        // =====================================================================
        // LOGGING (delegated to ILogging)
        // =====================================================================

        public void Initialize(IJSRuntime js) => _logging.Initialize(js);

        public Task Log(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null, bool isDebugEnabled = false)
            => _logging.Log(msg, level, data, isDebugEnabled);

        public Task Warn(string msg, object? data = null) => _logging.Warn(msg, data);

        public Task Error(string msg, object? data = null) => _logging.Error(msg, data);

        public void LogVoid(string msg, AppLogLevel level = AppLogLevel.Log, object? data = null)
            => _logging.LogVoid(msg, level, data);

        // =====================================================================
        // APP PARAMETER / SETTINGS MANAGEMENT
        // =====================================================================

        private List<AppParameterModel> _settings = new();
        public List<AppParameterModel> Settings
        {
            get => _settings;
            private set => SetProperty(ref _settings, value, notify: false);
        }
        public void UpdateSettings(List<AppParameterModel> settings) => Settings = settings;
        public bool HasSettings() => _settings.Count > 0;

        private List<SettingMetadataModel> _settingsMeta = new();
        public List<SettingMetadataModel> SettingsMeta
        {
            get => _settingsMeta;
            private set => SetProperty(ref _settingsMeta, value, notify: false);
        }
        public void UpdateSettingsMeta(List<SettingMetadataModel> settingsMeta) => SettingsMeta = settingsMeta;
        public bool HasSettingsMeta() => _settingsMeta.Count > 0;

        public async Task<List<AppParameterModel>> GetAppParameter()
        {
            if (Data?.Settings == null || Data.UserParameter == null)
                return new List<AppParameterModel>();

            List<AppParameterModel> result = new();

            if (IsNativeApp ?? false)
            {
                var formFactor = await _platform.GetFormFactor();
                IsNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
            }

            string updateError = await UpdateAppParameter();
            if (!string.IsNullOrEmpty(updateError))
            {
                await Log($"UpdateAppParameter failed: {updateError}", AppLogLevel.Error);
                return result;
            }

            // Metadata from app settings
            SettingsMeta = Data.Settings.Select(s => (SettingMetadataModel)s.Clone()).ToList();

            // Load settings from current storage location
            ReadModel<AppParameterModel?>? result_appparameter = await ReadAppParameter(StorageLocation);
            if (result_appparameter != null && string.IsNullOrEmpty(result_appparameter.out_err) && result_appparameter.out_list != null)
                result = result_appparameter.out_list!;

            // Sync SettingsMeta with loaded settings
            foreach (var item in SettingsMeta)
            {
                var existingSetting = result.FirstOrDefault(x => x.ParameterName.Equals(item.ParaName, StringComparison.OrdinalIgnoreCase));

                if (item.ParaType == SETTINGS_TYPE.STRING_SELECT ||
                    item.ParaType == SETTINGS_TYPE.STRING_COLOR ||
                    item.ParaType == SETTINGS_TYPE.STRING_INPUT)
                {
                    item.ValStr = existingSetting?.ParameterValue ?? string.Empty;
                }

                if (item.ParaType == SETTINGS_TYPE.BOOL_CHECKBOX ||
                    item.ParaType == SETTINGS_TYPE.BOOL_SWITCH)
                {
                    item.ValBool = existingSetting?.ParameterValue == "1";
                }

                // Dynamic adjustments based on platform
                if (item.ParaName == Data.UserParameter.StorageMode)
                {
                    bool isNative = IsNativeApp.HasValue && IsNativeApp.Value;
                    item.ValTempl = isNative ? item.ValTempl : "1,Cloud";
                    item.ValStr = isNative ? item.ValStr : "1";
                }
            }

            // Apply TOP setting
            var topSetting = SettingsMeta.FirstOrDefault(x => x.ParaName == Data.UserParameter.Top);
            if (topSetting != null && int.TryParse(topSetting.ValStr, out int top) && top != TOP)
            {
                UpdateTOP(top);
            }

            SettingsMeta.ForEach(s => s.ResetChanged());
            NotifyStateChanged();

            return result;
        }

        public async Task<ReadModel<AppParameterModel?>?> ReadAppParameter(STORAGE_LOCATION storagelocation)
        {
            if (Data?.UserParameter == null)
                return new ReadModel<AppParameterModel?>();

            _dam = _serviceProvider.GetRequiredService<IDamBase>();

            Dictionary<string, string> db_para = new()
            {
                { "@Case_", "Select>>AppParameter" },
                { "@AuthUsers_UnixTS", UnixTS },
                { "@Scope", "set" },
                { "cmd_nocomparetion", "no_comparetion_cloudlocal_data" }
            };

            if ((IsNativeApp.HasValue && !IsNativeApp.Value) || storagelocation == STORAGE_LOCATION.CLOUD)
                db_para.Add(DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString());

            if (storagelocation == STORAGE_LOCATION.LOCAL)
                db_para.Add(DB_CMD.NO_CLOUD, DB_CMD.NO_CLOUD.ToString());

            ReadModel<AppParameterModel?> result = await _dam.ReadData<AppParameterModel>(db_para)!;

            // Ensure StorageLocation parameter matches requested location
            if (result?.out_list != null)
            {
                var storageModeSetting = result.out_list.FirstOrDefault(x => x?.ParameterName == Data.UserParameter.StorageMode);
                if (storageModeSetting != null)
                {
                    STORAGE_LOCATION currentStorage = int.TryParse(storageModeSetting.ParameterValue, out int val)
                        ? (STORAGE_LOCATION)val
                        : STORAGE_LOCATION.CLOUD_LOCAL;

                    if (storagelocation != currentStorage)
                    {
                        storageModeSetting.ParameterValue = ((int)storagelocation).ToString();
                    }
                }
            }

            return result;
        }

        private async Task<string> UpdateAppParameter()
        {
            if (Data?.Settings == null)
                return string.Empty;

            // Cloud sync
            if (StorageLocation == STORAGE_LOCATION.CLOUD_LOCAL || StorageLocation == STORAGE_LOCATION.CLOUD)
            {
                ReadModel<AppParameterModel?>? cloudResult = await ReadAppParameter(STORAGE_LOCATION.CLOUD);
                if (cloudResult != null && string.IsNullOrEmpty(cloudResult.out_err))
                {
                    foreach (var item in Data.Settings)
                    {
                        bool exists = cloudResult.out_list?.Any(x =>
                            x != null &&
                            string.Equals(x.ParameterName, item.ParaName, StringComparison.OrdinalIgnoreCase)) ?? false;

                        if (!exists)
                        {
                            string error = await SaveAppParameter(item, STORAGE_LOCATION.CLOUD);
                            if (!string.IsNullOrEmpty(error))
                                return error;
                        }
                    }
                }
            }

            // Local sync (native only)
            if ((IsNativeApp.HasValue && IsNativeApp.Value) &&
                (StorageLocation == STORAGE_LOCATION.CLOUD_LOCAL || StorageLocation == STORAGE_LOCATION.LOCAL))
            {
                ReadModel<AppParameterModel?>? localResult = await ReadAppParameter(STORAGE_LOCATION.LOCAL);
                if (localResult != null && string.IsNullOrEmpty(localResult.out_err))
                {
                    foreach (var item in Data.Settings)
                    {
                        bool exists = localResult.out_list?.Any(x =>
                            x != null &&
                            string.Equals(x.ParameterName, item.ParaName, StringComparison.OrdinalIgnoreCase)) ?? false;

                        if (!exists)
                        {
                            string error = await SaveAppParameter(item, STORAGE_LOCATION.LOCAL);
                            if (!string.IsNullOrEmpty(error))
                                return error;
                        }
                    }
                }
            }

            return string.Empty;
        }

        private async Task<string> SaveAppParameter(SettingMetadataModel item, STORAGE_LOCATION storagelocation)
        {
            string parameterValue = item.ParaType == SETTINGS_TYPE.BOOL_CHECKBOX ||
                                    item.ParaType == SETTINGS_TYPE.BOOL_SWITCH
                ? (item.ValBool ? "1" : "0")
                : item.ValStr;

            Dictionary<string, string> db_para = new()
            {
                { "@Case_", "Save>>AppParameter" },
                { "@UnixTS", GenerateUniqueId() },
                { "@AuthUsers_UnixTS", UnixTS },
                { "@Scope", "set" },
                { "@Details", string.Empty },
                { "@ParameterName", item.ParaName },
                { "@ParameterValue", parameterValue },
            };

            switch (storagelocation)
            {
                case STORAGE_LOCATION.CLOUD:
                    db_para[DB_CMD.NO_LOCAL] = DB_CMD.NO_CLOUD.ToString();
                    break;
                case STORAGE_LOCATION.LOCAL:
                    db_para[DB_CMD.NO_CLOUD] = DB_CMD.NO_CLOUD.ToString();
                    break;
            }

            ScalarModel result = await _dam!.Save(db_para)!;
            return result?.out_err ?? string.Empty;
        }

        public async Task LoadStorageLocation()
        {
            if (Catalog?.LocalStorage == null || _platform == null)
                return;

            if (IsNativeApp ?? false)
            {
                var formFactor = await _platform.GetFormFactor();
                IsNativeApp = !formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);
            }

            UpdateStorageLocation(STORAGE_LOCATION.Unknown);

            // check if web app running on server (Blazor Server) - if yes, default to cloud storage
            var currPlatform = _platform.GetCurrPlatform();
            if(currPlatform == PLATFORMS.WINDOWS_SERVER)
            {
                UpdateStorageLocation(STORAGE_LOCATION.CLOUD);
                return;
            }

            // For native apps, try to load the last used storage location from platform storage
            if (IsNativeApp ?? false)
            { 
                var storageLocationResult = await _platformStorage.GetAsync(Catalog.LocalStorage.storagelocation);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                if (storageLocationResult != null &&
                    string.IsNullOrEmpty(storageLocationResult.out_err) &&
                    !string.IsNullOrEmpty(storageLocationResult.out_value_str))
                {
                    int intValue = int.Parse(storageLocationResult.out_value_str);
                    var storageLocation = (STORAGE_LOCATION)intValue;

                    UpdateStorageLocation(storageLocation);
                }
            }

            // If storage location is still unknown, use default from config
            if (StorageLocation == STORAGE_LOCATION.Unknown)
            {
                var storageLocation = _globalState.ConfigGeneral.StorageLocation;

                // Wenn Cloud oder Cloud/Local, dann prüfen, ob Webapi erreichbar ist. Wenn nicht erreichbar, dann auf Local wechseln
                if (storageLocation != STORAGE_LOCATION.LOCAL && !IsCloudConnected)
                {
                    storageLocation = STORAGE_LOCATION.LOCAL;
                }

                UpdateStorageLocation(storageLocation);
            }
        }

        // =====================================================================
        // ID GENERATION (delegated to IIdGenerator)
        // =====================================================================

        public void InitializeUnixTSUser(string user)
            => _idGenerator.InitializeUser(user);

        public void InitializeUnixTSDeviceId(string deviceInfo)
            => _idGenerator.InitializeDevice(deviceInfo);

        public string GenerateUniqueId()
            => _idGenerator.GenerateUniqueId();
    }
}