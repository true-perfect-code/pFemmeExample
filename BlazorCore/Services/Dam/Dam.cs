#pragma warning disable CA1416 // Disables CA1416 for the Encrypt call
using BlazorCore.Services.AI;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.GlobalState;
//using BlazorCore.Services.JsonHybridStorage;
using BlazorCore.Services.LocalStorage;
//using BlazorCore.Services.MemoryStorage;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
//using BlazorCore.Services.SqLite;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorCore.Services.Dam
{
    public class DamBase : IDamBase
    {
        private readonly IServiceProvider _serviceProvider;
        private IGlobalStateBase? _globalState;
        private IAppStateBase? _appState;
        private IPlatformBase _platform;
        private IApiBase? _api;
        private IAI? _ai;
        private ISqlClientBase? _sqlClient;
        //private ISqLiteBase? _sqLite;
        //private IMemoryStorageBase? _memoryStorage;
        private ILocalStorage? _localStorage;
        //private IJsonHybridStorageBase? _jsonStorage;

        private readonly int attemptWebApi = 10;
        private PLATFORMS _currPlatform = PLATFORMS.Unknown;

        /// <summary>
        /// Konstruktor: Initialisierung von DI Services
        /// </summary>
        public DamBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            //_memoryStorage = serviceProvider.GetRequiredService<IMemoryStorageBase>();
            _localStorage = serviceProvider.GetRequiredService<ILocalStorage>();
            //_jsonStorage = serviceProvider.GetRequiredService<IJsonHybridStorageBase>();

            _currPlatform = _platform.GetCurrPlatform();
            switch (_currPlatform)
            {
                case PLATFORMS.WINDOWS_SERVER:
                case PLATFORMS.WINDOWS_API:
                    _sqlClient = serviceProvider.GetRequiredService<ISqlClientBase>();
                    _ai = serviceProvider.GetRequiredService<IAI>();
                    break;

                case PLATFORMS.WINDOWS_CLIENT:
                case PLATFORMS.WASM:
                case PLATFORMS.ANDROID:
                case PLATFORMS.IOS:
                case PLATFORMS.MAC_CLIENT:
                    if (_currPlatform != PLATFORMS.WASM)
                    {
                        var localStorageType = _globalState.ConfigGeneral.LocalStorageType;
                        if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                        {
                            //_sqLite = serviceProvider.GetRequiredService<ISqLiteBase>();
                        }
                    }
                    _api = serviceProvider.GetRequiredService<IApiBase>();
                    break;
            }
        }

        /// <summary>
        /// Initialisierung der Sql Klasse
        /// </summary>
        public bool IsInitializeSql()
        {
            return _sqlClient != null ? _sqlClient.isConnected : false;
        }
        public ScalarModel InitializeSql()
        {
            ScalarModel result = new();
            try
            {
                _sqlClient = _serviceProvider.GetRequiredService<ISqlClientBase>();
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result;
        }

        /// <inheritdoc />
        public void SetCurrPlatform(PLATFORMS platform)
        {
            _currPlatform = platform;

            // Optional: Logging für Debug-Zwecke
            //_ = _appState?.Log($"[BLAZOR - DAM] Platform manually set to: {platform}");
        }

        /// <summary>
        /// Retrieves an authentication token from the WebAPI server after user registration or login.
        /// </summary>
        /// <param name="dbPara">Dictionary containing user information (EmailHash, PasswordHash, etc.)</param>
        /// <returns>ClientStorageModel containing token data or error message</returns>
        public async Task<ClientStorageModel> GetTokenTPC(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("START [Dam] GetTokenTPC");
            await _appState.Log($"Current platform = {_currPlatform}");

            if (_appState == null || _platform == null || _globalState == null || _localStorage == null)
                return new ClientStorageModel { out_err = "pEngine error: AppState, _platform or _globalState is null" };

            ClientStorageModel tokendata = new();
            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // Set LastUpdateUnixTS for migration direction determination (cloud/local)
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    // Server platforms: direct cloud DB access, no token needed
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        // Bei Server oder WebApi-Host wird kein token benötigt
                        break;

                    // Client platforms: communicate via WebAPI + optional local storage
                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        bool case_cloud = true;
                        bool case_local = true;

                        // User setting
                        switch (_appState.StorageLocation)
                        {
                            case STORAGE_LOCATION.LOCAL:
                                case_cloud = false;
                                break;

                            case STORAGE_LOCATION.CLOUD:
                                case_local = false;
                                break;

                            case STORAGE_LOCATION.Unknown:
                                break;

                            default:
                                break;
                        }

                        // Parameter Override
                        if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                        {
                            case_cloud = false;
                            _db_para.Remove(DB_CMD.NO_CLOUD);
                        }
                        if (_db_para.ContainsKey(DB_CMD.FORCE_CLOUD))
                        {
                            case_cloud = true;
                            _db_para.Remove(DB_CMD.FORCE_CLOUD);
                        }

                        if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                        {
                            case_local = false;
                            _db_para.Remove(DB_CMD.NO_LOCAL);
                        }
                        if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL))
                        {
                            case_local = true;
                            _db_para.Remove(DB_CMD.FORCE_LOCAL);
                        }

                        if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                        {
                            case_cloud = false;
                        }

                        if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_JSON))
                        {
                            _db_para.Remove(DB_CMD.FORCE_LOCAL_JSON);
                            localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                        }

                        if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_MEMORY))
                        {
                            _db_para.Remove(DB_CMD.FORCE_LOCAL_MEMORY);
                            localStorageType = LOCAL_STORAGE_TYPE.MEMORY;
                        }


                        // Check if Wasm Pwa 
                        if (_currPlatform == PLATFORMS.WASM)
                        {
                            // 1. Technical fallback: WASM does not support SQLite. 
                            // If AUTO (SQLite target) is selected, we fall back to JSON_HYBRID.
                            if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                            {
                                localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                            }
                        }

                        // Serialize parameters
                        string jsonpara = _globalState.SerializeDictionaryTpc(_db_para);
                        //await _appState.Log($"Jsonpara = {jsonpara}");

                        if (!String.IsNullOrEmpty(jsonpara))
                        {
                            // Extract values from dictionary
                            string email = "";
                            if (_db_para.ContainsKey("@EmailHash"))
                            {
                                email = AotConverter.ConvertTo<string>(_db_para.GetValueOrDefault("@EmailHash", ""));
                            }


                            string passwordhash = "";
                            if (_db_para.ContainsKey("@PasswordHash"))
                            {
                                passwordhash = AotConverter.ConvertTo<string>(_db_para.GetValueOrDefault("@PasswordHash", ""));
                            }


                            string unixts = string.Empty;
                            if (_db_para.ContainsKey("@UnixTS"))
                            {
                                unixts = AotConverter.ConvertTo<string>(_db_para.GetValueOrDefault("@UnixTS", ""));
                            }

                            bool registration = false;
                            if (_db_para.ContainsKey("@Int__Registration"))
                            {
                                registration = AotConverter.ConvertTo<string>(_db_para.GetValueOrDefault("@Int__Registration", "")) == API_CONST.TRUE_VALUE ? true : false;
                            }

                            // Build UserWebApi object
                            UserWebApi user = new();
                            if(_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                            {
                                using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                {
                                    user.JsonPara = aes.Encrypt(jsonpara);
                                    user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                }
                            }
                            else
                            {
                                // AesGcm is not supported on WASM platform
                                user.JsonPara = jsonpara;
                                user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                            }

                            if (!String.IsNullOrEmpty(email) && !String.IsNullOrEmpty(passwordhash))
                            {
                                bool logIn = true;

                                // Registration requires internet connection
                                if (registration)
                                {
                                    logIn = false;
                                    tokendata.out_err = DB_CMD.ERR_NO_INTERNET_CONNECTION;

                                    if (_appState.IsInternetConnected)
                                    {
                                        logIn = true;
                                        tokendata.out_err = "";
                                    }
                                }

                                await _appState.Log($"Login (is internet connected) = {logIn}");
                                await _appState.Log($"case_cloud = {case_cloud}");
                                await _appState.Log($"case_local = {case_local}");

                                if (logIn)
                                {
                                    ClientStorageModel result_local = new();

                                    // Online: cloud + optional local
                                    if (_appState.IsInternetConnected)
                                    {
                                        //////////////////////////
                                        // W E B A P I
                                        //////////////////////////
                                        if (case_cloud)
                                        {
                                            tokendata = await _api!.GetTokenDataTPC(user);
                                            await _appState.Log($"Tokendata UnixTS = {tokendata.UnixTS}", data: tokendata);
                                            if (string.IsNullOrEmpty(tokendata.UnixTS) && !string.IsNullOrEmpty(unixts) && String.IsNullOrEmpty(tokendata.out_err))
                                                tokendata.UnixTS = unixts;

                                            _db_para["@UnixTS"] = tokendata.UnixTS;
                                            _db_para["case_cloud"] = API_CONST.TRUE_VALUE;

                                            // Auto-Sync Trigger: If we retrieve data from the cloud,
                                            // we set the force-update command for the local auth handler.
                                            _db_para[DB_CMD.FORCE_UPDATE_LOCAL_CREDENTIALS] = "1";
                                        }

                                        if (String.IsNullOrEmpty(tokendata.out_err))
                                        {
                                            if (case_local)
                                            {
                                                //////////////////////////
                                                // L o c a l
                                                //////////////////////////
                                                switch (localStorageType)
                                                {
                                                    case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                        break;

                                                    case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                    case LOCAL_STORAGE_TYPE.MEMORY:
                                                    default:
                                                        // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                        // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                        result_local = await _localStorage.GetTokenTPC(_db_para);
                                                        break;
                                                }
                                                if (String.IsNullOrEmpty(result_local.out_err))
                                                {
                                                    // Was the account created locally?
                                                    if (string.IsNullOrEmpty(result_local.UnixTS))
                                                    {
                                                        if (!_db_para.ContainsKey(DB_CMD.NO_CREATE_USER))
                                                            tokendata.out_err = DB_CMD.ERR_NO_LOCAL_ACCOUNT_CREATED;
                                                    }
                                                    else
                                                    {
                                                        if (case_cloud)
                                                        {
                                                            if (string.IsNullOrEmpty(tokendata.UnixTS) && !string.IsNullOrEmpty(result_local.UnixTS))
                                                                tokendata.UnixTS = result_local.UnixTS;
                                                            else
                                                            {
                                                                if (string.IsNullOrEmpty(tokendata.UnixTS) && string.IsNullOrEmpty(result_local.UnixTS))
                                                                    tokendata.out_err = DB_CMD.ERR_NO_LOCAL_ACCOUNT_CREATED;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            tokendata = result_local;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    tokendata.out_err = result_local.out_err;
                                                }
                                            }
                                        }
                                    }
                                    else // Offline: local only
                                    {
                                        //////////////////////////
                                        // L o c a l
                                        //////////////////////////
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage.GetTokenTPC(_db_para);
                                                break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                tokendata.out_err = "User email or password is missing";
                            }
                        }
                        else
                            tokendata.out_err = "no_jsonpara";
                        break;
                }
            }
            catch (Exception ex)
            {
                tokendata.out_err += (String.IsNullOrEmpty(tokendata.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState.Log($"Tokendata error = {tokendata.out_err}", data: tokendata);
            await _appState.Log("END [Dam] GetTokenTPC");
            return tokendata;
        }

        /// <summary>
        /// Retrieves an authentication token from the WebAPI server after external IDP login.
        /// Supports Google, Microsoft, and Apple Identity Providers.
        /// </summary>
        /// <param name="dbPara">Dictionary containing @IdPClientIdent (external IDP identifier)</param>
        /// <returns>ClientStorageModel containing token data or error message</returns>
        public async Task<ClientStorageModel> GetTokenIDP(Dictionary<string, string> _db_para)
        {
            if (_appState == null || _platform == null || _globalState == null)
                return new ClientStorageModel { out_err = "pEngine error: AppState, _platform or _globalState is null" };

            ClientStorageModel tokendata = new();

            try
            {
                // Set LastUpdateUnixTS for migration direction determination (cloud/local)
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    // Server platforms: direct cloud DB access, no token needed
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        break;

                    // Client platforms: communicate via WebAPI
                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        bool case_cloud = true;

                        // Parameter zu String serialisieren
                        string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                        if (!String.IsNullOrEmpty(jsonpara))
                        {
                            string pollingId = "";
                            if (_db_para.ContainsKey("@IdPClientIdent"))
                            {
                                pollingId = _db_para.GetValueOrDefault("@IdPClientIdent", "");
                            }

                            UserWebApi user = new();
                            if (_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                            {
                                using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                {
                                    user.JsonPara = aes.Encrypt(jsonpara);
                                    user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                }
                            }
                            else
                            {
                                user.JsonPara = jsonpara;
                                user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                            }


                            // Check if pollingId exists
                            if (!string.IsNullOrEmpty(pollingId))
                            {
                                if (_appState.IsInternetConnected)
                                {
                                    //////////////////////////
                                    // W E B A P I
                                    //////////////////////////
                                    if (case_cloud)
                                    {
                                        tokendata = await _api!.GetTokenDataIDP(user);

                                        if (string.IsNullOrEmpty(_appState!.WebApiToken) || string.IsNullOrEmpty(_appState!.WebApiToken))
                                            _appState.UpdateWebApiToken(tokendata.WebApiToken);
                                    }
                                }
                            }
                            else
                            {
                                tokendata.out_err = "User email or password is missing";
                            }
                        }
                        else
                            tokendata.out_err = "no_jsonpara";
                        break;
                }
            }
            catch (Exception ex)
            {
                tokendata.out_err += (String.IsNullOrEmpty(tokendata.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam"; 
            }

            return tokendata;
        }

        /// <summary>
        /// Changes the user's password in cloud and/or local storage.
        /// </summary>
        /// <param name="dbPara">Dictionary containing password change parameters</param>
        /// <returns>ScalarModel containing success status or error message</returns>
        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> _db_para) // Verwendung bei MAUI Blazor
        {
            if (_appState == null || _globalState == null || _localStorage == null)
                return new ScalarModel { out_err = "pEngine error: One or more required services are null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // Set LastUpdateUnixTS for migration direction
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    // Server platforms: direct DB access, no action needed here
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        if (!String.IsNullOrEmpty(_appState.WebApiToken))
                        {
                            bool case_cloud = true;
                            bool case_local = true;

                            // User setting
                            switch (_appState.StorageLocation)
                            {
                                case STORAGE_LOCATION.LOCAL:
                                    case_cloud = false;
                                    break;

                                case STORAGE_LOCATION.CLOUD:
                                    case_local = false;
                                    break;

                                case STORAGE_LOCATION.Unknown:
                                    break;

                                default:
                                    break;
                            }

                            // Parameter Override
                            if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                            {
                                case_cloud = false;
                                _db_para.Remove(DB_CMD.NO_CLOUD);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_CLOUD))
                            {
                                case_cloud = true;
                                _db_para.Remove(DB_CMD.FORCE_CLOUD);
                            }

                            if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                            {
                                case_local = false;
                                _db_para.Remove(DB_CMD.NO_LOCAL);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL))
                            {
                                case_local = true;
                                _db_para.Remove(DB_CMD.FORCE_LOCAL);
                            }

                            if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                case_cloud = false;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_JSON))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_JSON);
                                localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_MEMORY))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_MEMORY);
                                localStorageType = LOCAL_STORAGE_TYPE.MEMORY;
                            }


                            // Check if Wasm Pwa 
                            if (_currPlatform == PLATFORMS.WASM)
                            {
                                // 1. Technical fallback: WASM does not support SQLite. 
                                // If AUTO (SQLite target) is selected, we fall back to JSON_HYBRID.
                                if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                                {
                                    localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                                }
                            }

                            string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                            if (!String.IsNullOrEmpty(jsonpara))
                            {
                                UserWebApi user = new();

                                if (_appState.IsInternetConnected)
                                {
                                    user.Token = _appState.WebApiToken;

                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    if (_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                            user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                                    }

                                    //////////////////////////
                                    // W E B A P I
                                    //////////////////////////
                                    if (case_cloud)
                                    {
                                        ScalarModel result_cloud = case_cloud ? await _api!.ChangePassword(user) : new();

                                        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                        {
                                            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                            {
                                                result_cloud = case_cloud ? await _api!.ChangePassword(user) : new();
                                                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                                    await Task.Delay(500);
                                                else
                                                    break;
                                            }
                                        }
                                        else //...if the Web API call is successful
                                        {
                                            if (String.IsNullOrEmpty(result_cloud.out_err))
                                            {
                                                string[] arrRes_cloud = result_cloud.out_value_str!.Split(":");

                                                if (arrRes_cloud.Length == 3)
                                                {
                                                    result.out_value_str = result_cloud.out_value_str;
                                                    result.out_cloud = result_cloud.out_value_str;
                                                }
                                            }
                                            else
                                                result.out_err = result_cloud.out_err;
                                        }
                                    }
                                    //////////////////////////
                                   
                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        ScalarModel result_local = new();
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage.Save(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            string[] arrRes_sqlite = result_local.out_value_str!.Split(":");

                                            if (arrRes_sqlite.Length == 3)
                                            {
                                                result.out_local = result_local.out_value_str;
                                            }
                                        }
                                        else
                                            result.out_err = result_local.out_err;
                                    }
                                    //////////////////////////
                                }
                                else 
                                {
                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result = await _localStorage.Save(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    else
                                        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                    //////////////////////////
                                }
                            }
                            else
                                result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_exec_code";
                        }
                        else
                            result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_webapi_token";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            return result;
        }

        /// <summary>
        /// Method for returning a scalar value
        /// </summary>
        /// <param name="_db_para">Parameter for creating queries (Cloud and Local)</param>
        /// <param name="_token">Token for accessing WebAPI</param>
        /// <returns>Return value is a Reader object</returns>
        public async Task<ScalarModel> Scalar(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START Scalar aufruf:", data: _db_para);

            if (_appState == null || _globalState == null || _localStorage == null)
                return new ScalarModel { out_err = "pEngine error: One or more required services are null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // Set LastUpdateUnixTS for migration direction
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        result = await _sqlClient!.Scalar(_db_para);
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        if (!String.IsNullOrEmpty(_appState.WebApiToken) || _db_para.ContainsKey(DB_CMD.NO_CLOUD))
                        {
                            bool case_cloud = true;
                            bool case_local = true;

                            // User setting
                            switch (_appState.StorageLocation)
                            {
                                case STORAGE_LOCATION.LOCAL:
                                    case_cloud = false;
                                    break;

                                case STORAGE_LOCATION.CLOUD:
                                    case_local = false;
                                    break;

                                case STORAGE_LOCATION.Unknown:
                                    break;

                                default:
                                    break;
                            }

                            // Parameter Override
                            if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                            {
                                case_cloud = false;
                                _db_para.Remove(DB_CMD.NO_CLOUD);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_CLOUD))
                            {
                                case_cloud = true;
                                _db_para.Remove(DB_CMD.FORCE_CLOUD);
                            }

                            if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                            {
                                case_local = false;
                                _db_para.Remove(DB_CMD.NO_LOCAL);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL))
                            {
                                case_local = true;
                                _db_para.Remove(DB_CMD.FORCE_LOCAL);
                            }

                            if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                case_cloud = false;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_JSON))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_JSON);
                                localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_MEMORY))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_MEMORY);
                                localStorageType = LOCAL_STORAGE_TYPE.MEMORY;
                            }


                            // Check if Wasm Pwa 
                            if (_currPlatform == PLATFORMS.WASM)
                            {
                                // 1. Technical fallback: WASM does not support SQLite. 
                                // If AUTO (SQLite target) is selected, we fall back to JSON_HYBRID.
                                if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                                {
                                    localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                                }
                            }

                            string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                            if (!String.IsNullOrEmpty(jsonpara))
                            {
                                UserWebApi user = new();

                                if (_appState.IsInternetConnected)
                                {
                                    user.Token = _appState.WebApiToken;

                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                                    }

                                    //////////////////////////
                                    // W E B A P I
                                    //////////////////////////
                                    if (case_cloud)
                                    {
                                        ScalarModel result_cloud = await _api!.GetScalar(user);
                                        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                        {
                                            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                            {
                                                result_cloud = await _api.GetScalar(user);
                                                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                                    await Task.Delay(500);
                                                else
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(result_cloud.out_err))
                                            {
                                                result = result_cloud;
                                                result.out_cloud = result_cloud.out_value_str;
                                            }
                                            else
                                                result.out_err = result_cloud.out_err;
                                        }
                                    }
                                    //////////////////////////
                                    
                                    if (case_local)
                                    {
                                        //////////////////////////
                                        // L o c a l
                                        //////////////////////////
                                        ScalarModel result_local = new();
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage.Scalar(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            if (case_cloud)
                                                result.out_local = result_local.out_value_str;
                                            else
                                                result = result_local;

                                        }
                                        else
                                            result.out_err = result_local.out_err;
                                        //////////////////////////
                                    }
                                    //////////////////////////
                                }
                                else
                                {
                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result = await _localStorage.Scalar(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    //////////////////////////
                                }
                            }
                            else
                                result.out_err = "no_exec_code";
                        }
                        else
                            result.out_err = "no_webapi_token";

                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Scalar aufruf");
            
            return result;
        }

        /// <summary>
        /// Reads data from cloud and local storage, then compares both sources for synchronization.
        /// Sets migration flags (Int__MigrationToCloud / Int__MigrationToLocal) based on comparison.
        /// </summary>
        /// <typeparam name="T">Type implementing IBasisModel and IMigrationState (must have UnixTS and LastUpdateUnixTS)</typeparam>
        /// <param name="dbPara">Dictionary containing query parameters (must include @Case_)</param>
        /// <returns>
        /// ReadModel containing:
        /// - out_list: Merged list with migration flags set
        /// - out_list_cloud: Original cloud data (if available)
        /// - out_list_local: Original local data (if available)
        /// - out_err: Error message if operation failed
        /// </returns>
        /// <remarks>
        /// Comparison logic:
        /// - Only in cloud → Int__MigrationToLocal = true
        /// - Only in local → Int__MigrationToCloud = true
        /// - Both: Compare LastUpdateUnixTS → newer version gets migration flag
        /// - Use "cmd_nocomparetion" in dbPara to skip comparison
        /// </remarks>
        public async Task<ReadModel<T?>>? ReadCompare<T>(Dictionary<string, string> _db_para) where T : class, IBasisModel, IMigrationState, new() //where T : IBasisModel, new()
        {
            if (_appState == null)
                return new ReadModel<T?> { out_err = "pEngine error: AppState is null" };

            ReadModel<T?>? result = new();
            result.out_list = new();

            try
            {
                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        result = await Read<T>(_db_para);
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        if (!String.IsNullOrEmpty(_appState.WebApiToken))
                        {
                            bool ComparisonData = true;
                            if (_db_para.ContainsKey("cmd_nocomparetion"))
                                ComparisonData = false;

                            bool case_cloud = true;
                            if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                                case_cloud = false;

                            bool case_local = true;
                            if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                                case_local = false;

                            switch (_appState.StorageLocation)
                            {
                                case STORAGE_LOCATION.LOCAL:
                                    case_cloud = false;
                                    break;

                                case STORAGE_LOCATION.CLOUD:
                                    case_local = false;
                                    break;

                                case STORAGE_LOCATION.Unknown:
                                    break;

                                default:
                                    break;
                            }

                            if (_currPlatform == PLATFORMS.WASM) // Wenn WASM lokal ausgeführt wird, dann keine lokale DB
                                case_local = false;

                            result = await Read<T>(_db_para);

                            // Datenquellen Cloud/Local vergleichen (wegen Upload/Download)
                            if (case_cloud && case_local)
                            {
                                if (ComparisonData)
                                {
                                    result.out_list = await CompareCloudLocalData<T>(result.out_list_cloud, result.out_list_local)!;
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // AOT-sichere Fehlerprotokollierung: Entfernung der Reflection
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            return result;
        }

        /// <summary>
        /// Reads data from cloud and/or local storage without comparison.
        /// Use this method when only one data source is active (e.g., cloud-only or local-only mode).
        /// </summary>
        /// <typeparam name="T">Type to deserialize the data into (must have parameterless constructor)</typeparam>
        /// <param name="dbPara">Dictionary containing query parameters (must include @Case_)</param>
        /// <returns>
        /// ReadModel containing:
        /// - out_list: Result list from the appropriate data source
        /// - out_err: Error message if operation failed
        /// </returns>
        /// <remarks>
        /// This method does NOT perform cloud/local comparison. Use ReadCompare for synchronization scenarios.
        /// The actual data source is determined by the private Read<T> method based on platform and configuration.
        /// </remarks>
        public async Task<ReadModel<T?>>? ReadData<T>(Dictionary<string, string> _db_para) where T : class, new() //where T : new()
        {
            if (_appState == null)
                return new ReadModel<T?> { out_err = "pEngine error: AppState is null" };

            ReadModel<T?>? result = new();
            result.out_list = new();

            result = await Read<T>(_db_para);

            return result;
        }

        /// <summary>
        /// Core read method that executes the actual data retrieval from cloud (WebAPI) and/or local storage.
        /// This method is private and called by ReadData and ReadCompare.
        /// </summary>
        /// <typeparam name="T">Type to deserialize the data into (must have parameterless constructor)</typeparam>
        /// <param name="dbPara">Dictionary containing query parameters (must include @Case_)</param>
        /// <returns>
        /// ReadModel containing:
        /// - out_list: Result list (when single source or merged from cloud/local)
        /// - out_list_cloud: Cloud result (if cloud was queried)
        /// - out_list_local: Local result (if local was queried)
        /// - out_err: Error message if operation failed
        /// </returns>
        /// <remarks>
        /// Platform behavior:
        /// - WINDOWS_SERVER / WINDOWS_API: Direct SQL Server access via _sqlClient.Reader<T>
        /// - Client platforms: WebAPI (cloud) + optional local storage (SQLite/JSON/Memory)
        /// 
        /// Decision factors:
        /// - StorageLocation (CLOUD / LOCAL / CLOUD_LOCAL)
        /// - NO_CLOUD / NO_LOCAL / FORCE_CLOUD / FORCE_LOCAL flags
        /// - Internet connectivity
        /// - TOKEN_LOCAL_ONLY (forces local-only)
        /// - WASM: SQLite automatically replaced with JSON_HYBRID
        /// </remarks>
        private async Task<ReadModel<T?>> Read<T>(Dictionary<string, string> _db_para) where T : class, new() //where T : new()
        {
            await _appState!.Log("[BLAZOR - DAM] START Read aufruf:", data: _db_para);

            if (_appState == null || _globalState == null)
                return new ReadModel<T?> { out_err = "pEngine error: AppState is null" };

            ReadModel<T?>? result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        ReaderModel<T> resultserver = new();
                        resultserver = await _sqlClient!.Reader<T>(_db_para);
                        if (resultserver.out_list != null)
                        {
                            result.out_err = resultserver.out_err;
                            result.out_list = resultserver.out_list;
                        }
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        if (!String.IsNullOrEmpty(_appState.WebApiToken) || _appState.StorageLocation == STORAGE_LOCATION.LOCAL)
                        {
                            bool case_cloud = true;
                            bool case_local = true;

                            // --- User setting
                            switch (_appState.StorageLocation)
                            {
                                case STORAGE_LOCATION.LOCAL:
                                    case_cloud = false;
                                    break;

                                case STORAGE_LOCATION.CLOUD:
                                    case_local = false;
                                    break;

                                case STORAGE_LOCATION.Unknown:
                                    break;

                                default:
                                    break;
                            }

                            // --- Parameter Override
                            if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                            {
                                case_cloud = false;
                                _db_para.Remove(DB_CMD.NO_CLOUD);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_CLOUD))
                            {
                                case_cloud = true;
                                _db_para.Remove(DB_CMD.FORCE_CLOUD);
                            }

                            if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                            {
                                case_local = false;
                                _db_para.Remove(DB_CMD.NO_LOCAL);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL))
                            {
                                case_local = true;
                                _db_para.Remove(DB_CMD.FORCE_LOCAL);
                            }

                            if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                case_cloud = false;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_JSON))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_JSON);
                                localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_MEMORY))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_MEMORY);
                                localStorageType = LOCAL_STORAGE_TYPE.MEMORY;
                            }


                            // --- Check if Wasm Pwa 
                            if (_currPlatform == PLATFORMS.WASM)
                            {
                                // 1. Technical fallback: WASM does not support SQLite. 
                                // If AUTO (SQLite target) is selected, we fall back to JSON_HYBRID.
                                if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                                {
                                    localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                                }
                            }

                            // Parameter zu String serialisieren
                            string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                            if (!String.IsNullOrEmpty(jsonpara))
                            {
                                UserWebApi user = new();

                                user.Token = _appState.WebApiToken;

                                if (_appState.IsInternetConnected)
                                {
                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    // Verschlüsselung aktiviert?
                                    if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                        // SP verschlüsseln
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                                    }

                                    //////////////////////////
                                    // W E B A P I
                                    //////////////////////////
                                    if (case_cloud)
                                    {
                                        ReaderModel<T> RESwebapi = await _api!.GetRows<T>(
                                            user,
                                            AotUtility.GetListTypeInfo<T>() // AOT-sicher
                                        );
                                        if (RESwebapi.out_err == "Error 401: Unauthorized request.")
                                        {
                                            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                            {
                                                RESwebapi = await _api!.GetRows<T>(
                                                    user,
                                                    AotUtility.GetListTypeInfo<T>() // AOT-sicher
                                                );
                                                if (RESwebapi.out_err == "Error 401: Unauthorized request.")
                                                    await Task.Delay(500);
                                                else
                                                    break;
                                            }
                                        }
                                        if (String.IsNullOrEmpty(RESwebapi.out_err))
                                        {
                                            result.out_list = RESwebapi.out_list;
                                            result.out_list_cloud = RESwebapi.out_list;
                                        }
                                        else
                                            result.out_err = RESwebapi.out_err;
                                    }

                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        ReaderModel<T> result_local = new(); // await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage!.Read<T>(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            // Wenn kein Cloud, dann Lokal-Daten übernehmen
                                            if (case_cloud == false)
                                                result.out_list = result_local.out_list ?? new List<T?>();
                                            else
                                                result.out_list_local = result_local.out_list ?? new List<T?>();
                                        }
                                        else
                                            result.out_err = result_local.out_err;
                                    }
                                    //////////////////////////
                                }
                                else
                                {
                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        ReaderModel<T> result_local = new();
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage!.Read<T>(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            result.out_list = result_local.out_list ?? new List<T?>();
                                            result.out_list_local = result_local.out_list ?? new List<T?>();
                                        }
                                        else
                                            result.out_err = result_local.out_err;
                                    }
                                }
                            }
                            else
                                result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_exec_code";
                        }
                        else
                            result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_webapi_token";
                        break;
                }
            }
            catch (Exception ex)
            {
                // AOT-sichere Fehlerprotokollierung: Entfernung der Reflection
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Read aufruf");

            if (result == null)
            {
                return new ReadModel<T?> { out_err = "pEngine error: ReadModel returned null after await." };
            }
            else
                return result;
        }

        /// <summary>
        /// Saves data to cloud and/or local storage (INSERT/UPDATE operations).
        /// </summary>
        /// <param name="dbPara">Dictionary containing stored procedure parameters (must include @Case_)</param>
        /// <returns>
        /// ScalarModel containing:
        /// - out_value_str: Formatted result string (e.g., "updated:{UnixTS}:{AuthUsers_UnixTS}")
        /// - out_cloud: Cloud operation result (when both sources are used)
        /// - out_local: Local operation result (when both sources are used)
        /// - out_err: Error message if operation failed (empty on success)
        /// </returns>
        /// <remarks>
        /// Platform behavior:
        /// - WINDOWS_SERVER / WINDOWS_API: Direct SQL Server access via _sqlClient.NonQuery
        /// - Client platforms: WebAPI (cloud) + optional local storage (SQLite/JSON/Memory)
        ///
        /// Decision factors:
        /// - StorageLocation (CLOUD / LOCAL / CLOUD_LOCAL)
        /// - NO_CLOUD / NO_LOCAL / FORCE_CLOUD / FORCE_LOCAL flags
        /// - Internet connectivity (offline → local only)
        /// - TOKEN_LOCAL_ONLY (forces local-only)
        /// - WASM: SQLite automatically replaced with JSON_HYBRID
        ///
        /// Return format: "updated:{UnixTS}:{AuthUsers_UnixTS}" (split by colon)
        /// Retry logic: 3 attempts with 500ms delay on 401 Unauthorized
        /// </remarks>
        public async Task<ScalarModel> Save(Dictionary<string, string> _db_para) // Verwendung bei MAUI Blazor
        {
            await _appState!.Log("[BLAZOR - DAM] START Save aufruf:", data: _db_para);

            if (_appState == null || _globalState == null || _localStorage == null)
                return new ScalarModel { out_err = "pEngine error: One or more required services are null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // Set LastUpdateUnixTS for migration direction
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        result = await _sqlClient!.NonQuery(_db_para);
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        if (!String.IsNullOrEmpty(_appState.WebApiToken) || _appState.StorageLocation == STORAGE_LOCATION.LOCAL)
                        {
                            bool case_cloud = true;
                            bool case_local = true;

                            // User setting
                            switch (_appState.StorageLocation)
                            {
                                case STORAGE_LOCATION.LOCAL:
                                    case_cloud = false;
                                    break;

                                case STORAGE_LOCATION.CLOUD:
                                    case_local = false;
                                    break;

                                case STORAGE_LOCATION.Unknown:
                                    break;

                                default:
                                    break;
                            }

                            // Parameter Override
                            if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                            {
                                case_cloud = false;
                                _db_para.Remove(DB_CMD.NO_CLOUD);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_CLOUD))
                            {
                                case_cloud = true;
                                _db_para.Remove(DB_CMD.FORCE_CLOUD);
                            }

                            if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                            {
                                case_local = false;
                                _db_para.Remove(DB_CMD.NO_LOCAL);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL))
                            {
                                case_local = true;
                                _db_para.Remove(DB_CMD.FORCE_LOCAL);
                            }

                            if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                case_cloud = false;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_JSON))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_JSON);
                                localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                            }
                                
                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_MEMORY))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_MEMORY);
                                localStorageType = LOCAL_STORAGE_TYPE.MEMORY;
                            }


                            // Check if Wasm Pwa 
                            if (_currPlatform == PLATFORMS.WASM)
                            {
                                // 1. Technical fallback: WASM does not support SQLite. 
                                // If AUTO (SQLite target) is selected, we fall back to JSON_HYBRID.
                                if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                                {
                                    localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                                }
                            }

                            string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                            if (!String.IsNullOrEmpty(jsonpara))
                            {
                                UserWebApi user = new();

                                if (_appState.IsInternetConnected)
                                {
                                    user.Token = _appState.WebApiToken;

                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                                    }

                                    //////////////////////////
                                    // W E B A P I
                                    //////////////////////////
                                    if (case_cloud)
                                    {
                                        ScalarModel result_cloud = case_cloud ? await _api!.PostData(user) : new();

                                        await _appState!.Log("[BLAZOR - DAM] _api!.PostData:", data: result_cloud);

                                        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                        {
                                            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                            {
                                                result_cloud = case_cloud ? await _api!.PostData(user) : new();
                                                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                                    await Task.Delay(500);
                                                else
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(result_cloud.out_err))
                                            {
                                                string[] arrRes_cloud = result_cloud.out_value_str!.Split(":");

                                                if (arrRes_cloud.Length == 3)
                                                {
                                                    result.out_value_str = result_cloud.out_value_str;
                                                    result.out_cloud = result_cloud.out_value_str;
                                                }
                                            }
                                            else
                                                result.out_err = result_cloud.out_err;
                                        }
                                    }
                                    //////////////////////////

                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        ScalarModel result_local = new();
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage.Save(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            string[] arrRes_sqlite = result_local.out_value_str!.Split(":");

                                            if (arrRes_sqlite.Length == 3)
                                            {
                                                result.out_local = result_local.out_value_str;
                                            }
                                        }
                                        else
                                            result.out_err = result_local.out_err;
                                    }
                                    //////////////////////////
                                }
                                else
                                {
                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result = await _localStorage.Save(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    else
                                        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                    //////////////////////////
                                }
                            }
                            else
                                result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_exec_code";
                        }
                        else
                            result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_webapi_token";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Save aufruf");

            return result;
        }

        /// <summary>
        /// Executes a non-query SQL operation, typically DELETE operations.
        /// Use this method for deletion or other actions where the semantic is "execute" rather than "save".
        /// </summary>
        /// <param name="dbPara">Dictionary containing stored procedure parameters (must include @Case_)</param>
        /// <returns>
        /// ScalarModel containing:
        /// - out_err: Error message if operation failed (empty on success)
        /// - out_value_str: Return value from the database (e.g., "deleted:0:{UnixTS}" or "not_deleted")
        /// - out_local: Local operation result (when both cloud and local sources are used)
        /// </returns>
        /// <remarks>
        /// Typical return values from stored procedures:
        /// - Success: "deleted:0:{AuthUsers_UnixTS}"
        /// - Failure: "not_deleted"
        ///
        /// Platform behavior:
        /// - WINDOWS_SERVER / WINDOWS_API: Direct SQL Server access via _sqlClient.NonQuery
        /// - Client platforms: WebAPI (cloud) + optional local storage (SQLite/JSON/Memory)
        ///
        /// Decision factors:
        /// - StorageLocation (CLOUD / LOCAL / CLOUD_LOCAL)
        /// - NO_CLOUD / NO_LOCAL / FORCE_CLOUD / FORCE_LOCAL flags
        /// - Internet connectivity (offline → local only)
        /// - TOKEN_LOCAL_ONLY (forces local-only)
        /// - WASM: SQLite automatically replaced with JSON_HYBRID
        ///
        /// Difference from Save:
        /// - Save is used for INSERT/UPDATE operations (returns "updated:{UnixTS}:{AuthUsers_UnixTS}")
        /// - ExecQuery is used for DELETE operations (returns "deleted:0:{UnixTS}" or "not_deleted")
        ///
        /// Retry logic: 3 attempts with 500ms delay on 401 Unauthorized
        /// </remarks>
        public async Task<ScalarModel> ExecQuery(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START ExecQuery aufruf:", data: _db_para);

            if (_appState == null || _globalState == null || _localStorage == null)
                return new ScalarModel { out_err = "pEngine error: One or more required services are null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // Set LastUpdateUnixTS for migration direction
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        result = await _sqlClient!.NonQuery(_db_para);
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        if (!String.IsNullOrEmpty(_appState.WebApiToken) || _appState.StorageLocation == STORAGE_LOCATION.LOCAL)
                        {
                            bool case_cloud = true;
                            bool case_local = true;

                            // User setting
                            switch (_appState.StorageLocation)
                            {
                                case STORAGE_LOCATION.LOCAL:
                                    case_cloud = false;
                                    break;

                                case STORAGE_LOCATION.CLOUD:
                                    case_local = false;
                                    break;

                                case STORAGE_LOCATION.Unknown:
                                    break;

                                default:
                                    break;
                            }

                            // Parameter Override
                            if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                            {
                                case_cloud = false;
                                _db_para.Remove(DB_CMD.NO_CLOUD);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_CLOUD))
                            {
                                case_cloud = true;
                                _db_para.Remove(DB_CMD.FORCE_CLOUD);
                            }

                            if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                            {
                                case_local = false;
                                _db_para.Remove(DB_CMD.NO_LOCAL);
                            }
                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL))
                            {
                                case_local = true;
                                _db_para.Remove(DB_CMD.FORCE_LOCAL);
                            }

                            if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                case_cloud = false;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_JSON))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_JSON);
                                localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                            }

                            if (_db_para.ContainsKey(DB_CMD.FORCE_LOCAL_MEMORY))
                            {
                                _db_para.Remove(DB_CMD.FORCE_LOCAL_MEMORY);
                                localStorageType = LOCAL_STORAGE_TYPE.MEMORY;
                            }


                            // Check if Wasm Pwa 
                            if (_currPlatform == PLATFORMS.WASM)
                            {
                                // 1. Technical fallback: WASM does not support SQLite. 
                                // If AUTO (SQLite target) is selected, we fall back to JSON_HYBRID.
                                if (localStorageType == LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO)
                                {
                                    localStorageType = LOCAL_STORAGE_TYPE.JSON_HYBRID;
                                }
                            }

                            string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                            if (!String.IsNullOrEmpty(jsonpara))
                            {
                                UserWebApi user = new();

                                if (_appState.IsInternetConnected)
                                {
                                    user.Token = _appState.WebApiToken;

                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                                    }

                                    //////////////////////////
                                    // W E B A P I
                                    //////////////////////////
                                    if (case_cloud)
                                    {
                                        ScalarModel result_cloud = await _api!.PostData(user);
                                        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                        {
                                            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                            {
                                                result_cloud = await _api.PostData(user);
                                                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                                    await Task.Delay(500);
                                                else
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            if (String.IsNullOrEmpty(result_cloud.out_err))
                                            {
                                                result.out_value_str = result_cloud.out_value_str;
                                            }
                                            else
                                                result.out_err = result_cloud.out_err;
                                        }
                                    }
                                    //////////////////////////

                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        ScalarModel result_local = new();
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result_local = await _localStorage.ExecQuery(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                            result.out_local = String.IsNullOrEmpty(result.out_value_str) ? result_local.out_value_str : result.out_value_str;
                                        else
                                            result.out_err = result_local.out_err;
                                    }
                                    //////////////////////////
                                }
                                else
                                {
                                    //////////////////////////
                                    // L o c a l
                                    //////////////////////////
                                    if (case_local)
                                    {
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                            default:
                                                // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                                                // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                                                result = await _localStorage.ExecQuery(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    else
                                        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                    //////////////////////////
                                }
                            }
                            else
                                result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_exec_code";
                        }
                        else
                            result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_webapi_token";

                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Save aufruf");

            return result;
        }

        /// <summary>
        /// Executes an anonymous query on the cloud (WebAPI) without authentication.
        /// Use this method for operations where the user is NOT logged in (e.g., feedback forms, 2FA before login).
        /// </summary>
        /// <param name="dbPara">Dictionary containing stored procedure parameters (must include @Case_)</param>
        /// <returns>
        /// ScalarModel containing:
        /// - out_err: Error message if operation failed (empty on success)
        /// - out_value_str: For ExecuteReaderJson: JSON string of results; for ExecuteNonQuery: return value
        /// </returns>
        /// <remarks>
        /// This method has NO LOCAL FALLBACK. Since the user is not authenticated, there is no local storage context.
        /// Internet connection is required for client platforms.
        ///
        /// ExecuteMode control (add to dbPara):
        /// - "cmd_nonquery": Execute as NonQuery (INSERT, UPDATE, DELETE without result set)
        /// - "cmd_readerjson": Execute as Reader and return results as JSON string
        ///
        /// Platform behavior:
        /// - WINDOWS_SERVER / WINDOWS_API: Direct database access (_sqlClient.NonQuery / Reader)
        /// - Client platforms: WebAPI anonymous endpoint (_api.AnonymousQuery)
        ///
        /// Typical use cases:
        /// - Contact/feedback forms from landing page
        /// - 2FA verification before login
        /// - Public queries (username availability)
        /// - Password reset emails
        ///
        /// No encryption on client platforms (HTTPS only) except Windows Client where AES is used.
        /// </remarks>
        public async Task<ScalarModel> AnonymousQuery(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START AnonymousQuery aufruf:", data: _db_para);

            ScalarModel result = new();

            try
            {
                // Set LastUpdateUnixTS for migration direction
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                    _db_para.Remove(DB_CMD.NO_LOCAL);

                ExecuteMode execMode = ExecuteMode.Unknown;
                if (_db_para.ContainsKey("cmd_nonquery"))
                {
                    execMode = ExecuteMode.ExecuteNonQuery;
                    _db_para.Remove("cmd_nonquery");
                }
                if (_db_para.ContainsKey("cmd_readerjson"))
                {
                    execMode = ExecuteMode.ExecuteReaderJson;
                    _db_para.Remove("cmd_readerjson");
                }

                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        switch (execMode)
                        {
                            case ExecuteMode.ExecuteNonQuery:
                                result = await _sqlClient!.NonQuery(_db_para);
                                break;
                            case ExecuteMode.ExecuteReader:
                                break;
                            case ExecuteMode.ExecuteReaderJson:
                                ReaderDynamicModel result_dynamic = await _sqlClient!.Reader(_db_para);
                                result.out_err = result_dynamic.out_err;
                                result.out_value_str = result_dynamic.out_json;
                                break;
                            case ExecuteMode.ExecuteScalar:
                                break;
                            case ExecuteMode.ExecuteByte:
                                break;
                            case ExecuteMode.Unknown:
                                break;
                            default:
                                break;
                        }
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        bool case_cloud = true;

                        string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                        if (!String.IsNullOrEmpty(jsonpara))
                        {
                            UserWebApi user = new();
                            if (_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                            {
                                using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                {
                                    user.JsonPara = aes.Encrypt(jsonpara);
                                    user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                }
                            }
                            else
                            {
                                user.JsonPara = jsonpara;
                                user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                            }

                            if (_appState!.IsInternetConnected)
                            {
                                //////////////////////////
                                // W E B A P I
                                //////////////////////////
                                if (case_cloud)
                                {
                                    result = await _api!.AnonymousQuery(user);
                                }
                            }
                        }
                        else
                            result.out_err = "no_jsonpara";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Save aufruf");

            return result;
        }

        /// <summary>
        /// Executes an AI operation (Chat, Embedding, etc.) through the configured AI service.
        /// This method follows the same pattern as Scalar but for AI operations.
        /// 
        /// IMPORTANT: AI operations are CLOUD-ONLY. No local fallback exists because:
        /// - AI requires significant computational resources (not feasible locally)
        /// - AI models are typically cloud-based (Azure OpenAI)
        /// - Local AI will be handled by a separate AILocalService in the future
        /// </summary>
        /// <param name="_db_para">Dictionary containing AI parameters (must include @Case_ and AI-specific parameters)</param>
        /// <returns>
        /// ScalarModel containing:
        /// - out_err: Error message if operation failed (empty on success)
        /// - out_value_str: AI response text (for chat completion)
        /// </returns>
        /// <remarks>
        /// Supported @Case_ values:
        /// - DB_CMD.AI_COMPLETE_CHAT or DB_CMD.AI_COMPLETE_CHAT_ALT: Chat completion
        /// 
        /// Required AI parameters (depending on @Case_):
        /// - DB_CMD.AI_SYSTEM_PROMPT: System prompt defining AI behavior
        /// - DB_CMD.AI_USER_PROMPT: User input/instruction
        /// 
        /// Optional AI parameters:
        /// - DB_CMD.AI_TEMPERATURE: Randomness (0.0 - 1.0, default: 0.7)
        /// - DB_CMD.AI_MAX_TOKENS: Max response length (default: 500)
        /// - DB_CMD.AI_MODEL: Model override (e.g., "gpt-4", "gpt-35-turbo")
        /// 
        /// Platform behavior:
        /// - WINDOWS_SERVER / WINDOWS_API: Direct AI service call (_aiService.ExecuteAsync)
        /// - Client platforms (WASM, WPF, Android, iOS, Mac): WebAPI call (_api.Ai)
        /// 
        /// Security:
        /// - AI API keys never leave the server
        /// - Windows Client: AES encryption for sensitive prompts
        /// - Other platforms: HTTPS only
        /// </remarks>
        public async Task<ScalarModel> Ai(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START Ai aufruf:", data: _db_para);

            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                // Set LastUpdateUnixTS for migration direction (consistent with other DAM methods)
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // AI operations are cloud-only - ensure NO_LOCAL is always set
                // This prevents any accidental local storage attempts
                if (!_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                    _db_para[DB_CMD.NO_LOCAL] = DB_CMD.NO_LOCAL.ToString();

                switch (_currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        // --- S E R V E R   P A T H
                        // Direct AI service call (no HTTP, no WebAPI)
                        result = await _ai!.ExecuteAsync(_db_para);
                        break;

                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        // --- C L I E N T   P A T H
                        // WebAPI call (authentication is handled by _api.Ai)

                        string jsonpara = _globalState!.SerializeDictionaryTpc(_db_para);

                        if (!string.IsNullOrEmpty(jsonpara))
                        {
                            UserWebApi user = new();

                            // Check internet connection
                            if (_appState.IsInternetConnected)
                            {
                                // Token is set by _api.Ai internally (not needed here)
                                user.Token = _appState.WebApiToken;

                                // Windows Client: AES encryption for sensitive data
                                if (_globalState.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                {
                                    user.EncryptDecrypt = API_CONST.TRUE_VALUE;
                                    using (BlazorCore.Services.ServerShared.Security aes = new(
                                        _globalState.ConfigGeneral.ApplicationName,
                                        _globalState.ConfigGeneral.TableSchema))
                                    {
                                        user.JsonPara = aes.Encrypt(jsonpara);
                                    }
                                }
                                else
                                {
                                    user.JsonPara = jsonpara;
                                    user.EncryptDecrypt = API_CONST.FALSE_VALUE;
                                }

                                // Call WebAPI (authenticated endpoint)
                                result = await _api!.Ai(user);
                            }
                            else
                            {
                                result.out_err = DB_CMD.ERR_NO_INTERNET_CONNECTION;
                            }
                        }
                        else
                        {
                            result.out_err = "no_jsonpara";
                        }
                        break;

                    default:
                        result.out_err = $"Unsupported platform for AI operations: {_currPlatform}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err += (string.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Ai aufruf");

            return result;
        }

        /// <summary>
        /// Compares cloud and local data sets and determines which items need migration.
        /// </summary>
        /// <typeparam name="T">Type implementing IBasisModel and IMigrationState (must have UnixTS and LastUpdateUnixTS)</typeparam>
        /// <param name="cloudItems">Data from cloud (MSSQL / WebAPI) - can be null</param>
        /// <param name="localItems">Data from local storage (SQLite / JSON / Memory) - can be null</param>
        /// <returns>
        /// Merged list with migration flags set:
        /// - Item only in cloud → Int__MigrationToLocal = true (needs download to local)
        /// - Item only in local → Int__MigrationToCloud = true (needs upload to cloud)
        /// - Item in both: Compare LastUpdateUnixTS → newer version gets migration flag
        /// - Identical LastUpdateUnixTS → cloud version is kept (no migration flag)
        /// </returns>
        /// <remarks>
        /// The comparison is based on UnixTS as the unique identifier.
        /// Null collections are treated as empty (defensive programming).
        /// Migration flags are used by the synchronization logic in the client app.
        ///
        /// Use cases:
        /// - Determine which items need to be uploaded to cloud
        /// - Determine which items need to be downloaded to local storage
        /// - Show sync indicators (upload/download icons) in UI
        ///
        /// This method is called internally by ReadCompare but can also be used independently.
        /// </remarks>
        public async Task<List<T?>> CompareCloudLocalData<T>(
           IReadOnlyCollection<T?>? cloudItems,  // Nullable hier hinzufügen
           IReadOnlyCollection<T?>? localItems   // Nullable hier hinzufügen
        ) where T : class, IBasisModel, IMigrationState
        {
            await _appState!.Log("[Blazor] Dam -> CompareCloudLocalData cloudItems:", data: cloudItems);
            await _appState!.Log("[Blazor] Dam -> CompareCloudLocalData localItems:", data: localItems);

            var result = new List<T?>();

            // Defensive Defaults
            cloudItems ??= Array.Empty<T?>();
            localItems ??= Array.Empty<T?>();

            var cloudDict = cloudItems.ToDictionary(x => x?.UnixTS ?? API_CONST.FALSE_VALUE);
            var localDict = localItems.ToDictionary(x => x?.UnixTS ?? API_CONST.FALSE_VALUE);

            var allKeys = cloudDict.Keys.Union(localDict.Keys);

            foreach (var key in allKeys)
            {
                var hasCloud = cloudDict.TryGetValue(key, out var cloudItem);
                var hasLocal = localDict.TryGetValue(key, out var localItem);

                if (hasLocal && !hasCloud && localItem != null)
                {
                    localItem.Int__MigrationToCloud = true;
                    result.Add(localItem);
                    continue;
                }

                if (hasCloud && !hasLocal && cloudItem != null)
                {
                    cloudItem.Int__MigrationToLocal = true;
                    result.Add(cloudItem);
                    continue;
                }

                if (hasCloud && hasLocal && cloudItem != null && localItem != null)
                {
                    if (cloudItem.LastUpdateUnixTS > localItem.LastUpdateUnixTS)
                    {
                        cloudItem.Int__MigrationToLocal = true;
                        result.Add(cloudItem);
                    }
                    else if (cloudItem.LastUpdateUnixTS < localItem.LastUpdateUnixTS)
                    {
                        localItem.Int__MigrationToCloud = true;
                        result.Add(localItem);
                    }
                    else
                    {
                        result.Add(cloudItem);
                    }
                }
            }

            await _appState!.Log("[Blazor] Dam -> CompareCloudLocalData result:", data: result);

            return result;
        }

        /// <summary>
        /// Methode, um SqLite Daten zu löschen
        /// </summary>
        /// <returns>Rückgabewert ist Fehlerbeschreibungt</returns>
        //public async Task<ScalarModel> DeleteSqLiteData()
        public async Task<ScalarModel> DeleteData()
        {
            if (_appState == null || _globalState == null || _localStorage == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                //result = await _sqLite!.ClearAllData();
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;
                switch (localStorageType)
                {
                    case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                        break;

                    case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                    case LOCAL_STORAGE_TYPE.MEMORY:
                    default:
                        // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                        // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                        result = await _localStorage.ClearAllData();
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"[Dam] DeleteSqLiteData: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Methode, um SqLite DB zu löschen
        /// </summary>
        /// <returns>Rückgabewert ist Fehlerbeschreibungt</returns>
        //public async Task<ScalarModel> DeleteSqLiteDB()
        public async Task<ScalarModel> DeleteDB()
        {
            if (_appState == null || _globalState == null || _localStorage == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                //result = await _sqLite!.DeleteDB();
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;
                switch (localStorageType)
                {
                    case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                        break;

                    case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                    case LOCAL_STORAGE_TYPE.MEMORY:
                    default:
                        // BEIDE gehen über LocalStorageEngine (umbenannter MemoryStorage)
                        // Der QueryExecutor entscheidet intern, ob JSON oder MEMORY
                        result = await _localStorage.DeleteDB();
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"[Dam] DeleteRealmDB: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Prüft ob Token (WebApi) noch gültig ist
        /// </summary>
        /// <returns>Rückgabewert ist bool</returns>
        public async Task<bool> CheckToken(UserWebApi user)
        {
            bool result = false;

            try
            {
                switch (_currPlatform)
                {
                    // Beim Web-Server oder Web-Api Server wird Cloud direkt angesprochen
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        result = true; // Bei Blazor Server haben wir kein WebApi Token
                        break;

                    // Bei Windows, Android oder iOS erfolgt die Verbindung zur Cloud Datenbank über den Web-Api Server
                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        //////////////////////////
                        // W E B A P I
                        //////////////////////////
                        result = await _api!.CheckToken(user);
                        break;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Methode, um Migration-Flags/Parameter 'NO_LOCAL', 'NO_CLOUD' und '@IsMigration' zu setzen
        /// </summary>
        /// <returns>Rückgabewert ist Fehlerbeschreibungt</returns>
        public string SetMigrationFlags(ref Dictionary<string, string> _db_para, STORAGE_LOCATION storagelocation)
        {
            string RES = "";

            try
            {
                switch (storagelocation)
                {
                    case STORAGE_LOCATION.LOCAL:
                        _db_para[DB_CMD.NO_CLOUD] = DB_CMD.NO_CLOUD.ToString();
                        break;

                    case STORAGE_LOCATION.CLOUD:
                        _db_para[DB_CMD.NO_LOCAL] = DB_CMD.NO_LOCAL.ToString();
                        break;

                    //case STORAGE_LOCATION.Unknown:
                    //    break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                RES += (String.IsNullOrEmpty(RES) ? "" : "\r\n") + "Error: " + ex.Message + " , Classname : Dam";
            }

            return RES;
        }

        
    }
}
#pragma warning restore CA1416