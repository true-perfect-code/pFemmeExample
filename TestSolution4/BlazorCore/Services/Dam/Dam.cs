#pragma warning disable CA1416 // Disables CA1416 for the Encrypt call
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.JsonHybridStorage;
using BlazorCore.Services.MemoryStorage;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.SqLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Data.SqlTypes;

namespace BlazorCore.Services.Dam
{
    public class DamBase : IDamBase
    {
        private readonly IServiceProvider _serviceProvider;
        private IGlobalStateBase? _globalState;
        private IAppStateBase? _appState;
        private IPlatformBase _platform;
        private IApiBase? _api;
        private ISqlClientBase? _sqlClient;
        private ISqLiteBase? _sqLite;
        private IMemoryStorageBase? _memoryStorage;
        private IJsonHybridStorageBase? _jsonStorage;

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

            _currPlatform = _platform.GetCurrPlatform();
            switch (_currPlatform)
            {
                case PLATFORMS.WINDOWS_SERVER:
                case PLATFORMS.WINDOWS_API:
                    _sqlClient = serviceProvider.GetRequiredService<ISqlClientBase>();
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
                            _sqLite = serviceProvider.GetRequiredService<ISqLiteBase>();
                        }
                    }
                    _api = serviceProvider.GetRequiredService<IApiBase>();
                    _memoryStorage = serviceProvider.GetRequiredService<IMemoryStorageBase>();
                    _jsonStorage = serviceProvider.GetRequiredService<IJsonHybridStorageBase>();
                    break;
            }
        }

        //// Interner Konstruktor für Unit Tests
        //public DamBase(
        //    IAppStateBase appState,
        //    IGlobalStateBase globalState,
        //    IPlatformBase platform,
        //    IApiBase? api = null,
        //    ISqLiteBase? sqLite = null,
        //    ISqlClientBase? sqlClient = null)
        //{
        //    _serviceProvider = null!;

        //    _appState = appState;
        //    _globalState = globalState;
        //    _platform = platform;
        //    _api = api;
        //    _sqLite = sqLite;
        //    _sqlClient = sqlClient;

        //    // Die Plattform-Zuweisung muss auch hier erfolgen
        //    _currPlatform = _platform.GetCurrPlatform();
        //}

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

        /// <summary>
        /// Liefert nach der Authentifizierung ein Token vom WebApi Server
        /// </summary>
        /// <param name="_db_para">Enthält die Informationen über den Benutzer (für die Registrierung des Accounts)</param>
        /// <returns>Rückgabewert ist ein Objekt vom Typ ClientStorageModel</returns>
        public async Task<ClientStorageModel> GetTokenTPC(Dictionary<string, string> _db_para)
        {
            // JS-LOG
            await _appState!.Log("START [Dam] GetTokenTPC");
            await _appState.Log($"Current platform = {_currPlatform}");

            if (_appState == null || _platform == null || _globalState == null)
                return new ClientStorageModel { out_err = "pEngine error: AppState, _platform or _globalState is null" };

            ClientStorageModel tokendata = new();
            //string exec = Appl.CreateExec(_db_para); // zum Testen
            try
            {
                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Prüfen, ob DB oder WebApi  PLATFORM.Current = (int)PLATFORMS.WINDOWS_API
                //switch (_appState!.Platforms)
                switch (_currPlatform)
                {
                    // Beim Web-Server oder Web-Api Server wird CLoud-DB direkt angesprochen
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        // Bei Server oder WebApi-Host wird kein token benötigt
                        break;

                    // Bei Windows, Android oder iOS erfolgt die Verbindung zur CLoud-DB Datenbank über den Web-Api Server
                    case PLATFORMS.WINDOWS_CLIENT:
                    case PLATFORMS.WASM:
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        bool case_cloud = true;
                        if (_db_para.ContainsKey(DB_CMD.NO_CLOUD))
                        {
                            case_cloud = false;
                            _db_para.Remove(DB_CMD.NO_CLOUD);
                        }

                        bool case_local = true;
                        if (_db_para.ContainsKey(DB_CMD.NO_LOCAL))
                        {
                            case_local = false;
                            _db_para.Remove(DB_CMD.NO_LOCAL);
                        }

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

                        if(_currPlatform == PLATFORMS.WASM) // Wenn WASM lokal ausgeführt wird, dann keine lokale DB
                            case_local = false;

                        // Parameter zu String serialisieren
                        string jsonpara = _globalState.SerializeDictionaryTpc(_db_para);
                        await _appState.Log($"Jsonpara = {jsonpara}");

                        if (!String.IsNullOrEmpty(jsonpara))
                        {
                            string email = "";
                            if (_db_para.ContainsKey("@EmailHash"))
                            {
                                email = _globalState.ConvertStrPara<string>(_db_para["@EmailHash"]);
                            }


                            string passwordhash = "";
                            if (_db_para.ContainsKey("@PasswordHash"))
                            {
                                passwordhash = _globalState.ConvertStrPara<string>(_db_para["@PasswordHash"]);
                            }


                            string unixts = string.Empty;
                            if (_db_para.ContainsKey("@UnixTS"))
                            {
                                unixts = _globalState.ConvertStrPara<string>(_db_para["@UnixTS"]);
                            }

                            bool registration = false;
                            if (_db_para.ContainsKey("@Int__Registration"))
                            {
                                registration = _globalState.ConvertStrPara<string>(_db_para["@Int__Registration"]) == "1" ? true : false;
                            }

                            UserWebApi user = new();
                            //user.JsonPara = jsonpara;
                            //user.EncryptDecrypt = "0";
                            if(_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                            {
                                using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                {
                                    user.JsonPara = aes.Encrypt(jsonpara);
                                    user.EncryptDecrypt = "1";
                                    //user.DisplayError = "1";
                                }
                            }
                            else
                            {
                                // Algorithm 'AesGcm' is not supported on WASM platform
                                user.JsonPara = jsonpara;
                                user.EncryptDecrypt = "0";
                            }


                            // Prüfen, ob Email und Passworthash vorhanden
                            if (!String.IsNullOrEmpty(email) && !String.IsNullOrEmpty(passwordhash))
                            {
                                bool logIn = true;

                                // Registrierung nur wenn Internetverbindung (beachte hier Datentyp bool)
                                if (registration)
                                {
                                    logIn = false;
                                    tokendata.out_err = "no_internet_connection";

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
                                            _db_para["case_cloud"] = "1";
                                        }

                                        if (String.IsNullOrEmpty(tokendata.out_err))
                                        {
                                            if (case_local)
                                            {
                                                //////////////////////////
                                                // L o c a l
                                                //////////////////////////
                                                ClientStorageModel tokendatalocal = await _sqLite!.GetTokenDataTPC(_db_para);
                                                await _appState.Log($"Tokendata local UnixTS = {tokendatalocal.UnixTS}", data: tokendatalocal);
                                                if (String.IsNullOrEmpty(tokendatalocal.out_err))
                                                {
                                                    // Wurde der Account Lokal erstellt
                                                    if (string.IsNullOrEmpty(tokendatalocal.UnixTS))
                                                    {
                                                        if (!_db_para.ContainsKey("cmd_nocreateuser"))
                                                            tokendata.out_err = "no_local_account_created";
                                                    }
                                                    else
                                                    {
                                                        if (case_cloud)
                                                        {
                                                            if (string.IsNullOrEmpty(tokendata.UnixTS) && !string.IsNullOrEmpty(tokendatalocal.UnixTS))
                                                                tokendata.UnixTS = tokendatalocal.UnixTS;
                                                            else
                                                            {
                                                                if (string.IsNullOrEmpty(tokendata.UnixTS) && string.IsNullOrEmpty(tokendatalocal.UnixTS))
                                                                    tokendata.out_err = "no_local_account_created";
                                                            }
                                                        }
                                                        else
                                                        {
                                                            tokendata = tokendatalocal;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    tokendata.out_err = tokendatalocal.out_err;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //////////////////////////
                                        // L o c a l
                                        //////////////////////////
                                        tokendata = await _sqLite!.GetTokenDataTPC(_db_para);
                                        await _appState.Log($"Tokendata local UnixTS = {tokendata.UnixTS}", data: tokendata);
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
                // AOT-sichere Fehlerprotokollierung: Entfernung der Reflection
                tokendata.out_err += (String.IsNullOrEmpty(tokendata.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
                // Optional, aber weniger hilfreich als Caller-Attribute: 
                // + "." + nameof(GetTokenTPC); 
            }

            await _appState.Log($"Tokendata error = {tokendata.out_err}", data: tokendata);
            await _appState.Log("END [Dam] GetTokenTPC");
            return tokendata;
        }

        /// <summary>
        /// Generiert einen otp-Schlüssel
        /// </summary>
        /// <returns>Rückgabewert ist ein Scalar Objekt</returns>
        public async Task<ClientStorageModel> GetTokenIDP(Dictionary<string, string> _db_para)
        {
            if (_appState == null || _platform == null || _globalState == null)
                return new ClientStorageModel { out_err = "pEngine error: AppState, _platform or _globalState is null" };

            ClientStorageModel tokendata = new();
            //string exec = Appl.CreateExec(_db_para); // zum Testen

            try
            {
                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Prüfen, ob DB oder WebApi  PLATFORM.Current = (int)PLATFORMS.WINDOWS_API
                //switch (_appState!.Platforms)
                switch (_currPlatform)
                {
                    // Beim Web-Server oder Web-Api Server wird Cloud direkt angesprochen
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WINDOWS_API:
                        // Bei Server oder WebApi-Host wird kein token benötigt
                        break;

                    // Bei Windows, Android oder iOS erfolgt die Verbindung zur Cloud Datenbank über den Web-Api Server
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
                                pollingId = _globalState.ConvertStrPara<string>(_db_para["@IdPClientIdent"]);
                            }

                            UserWebApi user = new();
                            if (_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                            {
                                using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                {
                                    user.JsonPara = aes.Encrypt(jsonpara);
                                    user.EncryptDecrypt = "1";
                                    //user.DisplayError = "1";
                                }
                            }
                            else
                            {
                                user.JsonPara = jsonpara;
                                user.EncryptDecrypt = "0";
                            }


                            // Prüfen, ob pollingId vorhanden
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

                                        //// Hier Savemodus bestimmen, damit über ext. anmeldung auch der lokale Benutzer in GetTokenTPC(..) erstellt werden kann
                                        //if (string.IsNullOrEmpty(_appState!.SecureStorageID))
                                        //    _appState.UpdateSecureStorageID(SecurityHelp.ExtractCookieSuffix(SecurityHelp.ComputeSha256Hash(Appl.ApplicationName + tokendata.UnixTS)));
                                        //string? StorageLocation = await _platform!.GetAsync(_globalState.LocalStorage.storageLocation + Appl.ApplicationName + _appState!.SecureStorageID);
                                        //if (StorageLocation != null)
                                        //    _appState.UpdateStorageLocation(int.TryParse(StorageLocation, out int execModus) ? (STORAGE_LOCATION)execModus : STORAGE_LOCATION.CLOUD_LOCAL);

                                        //WebApiToken setzen, damit Parameter abgefragt werden kann bis Authentifikation aufgerufen wird
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
                // AOT-sichere Fehlerprotokollierung: Entfernung der Reflection
                tokendata.out_err += (String.IsNullOrEmpty(tokendata.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam"; 
            }

            return tokendata;
        }

        /// <summary>
        /// Methode, um das Benutzerpasswort zu ändern
        /// </summary>
        /// <param name="_db_para">SP Parameter-Paare (EXEC)</param>
        /// <param name="_token">Token für dne Zugriff auf WebApi</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> _db_para) // Verwendung bei MAUI Blazor
        {
            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            //string exec = Appl.GetSpParameters(_sp_para);

            //string exec = Appl.CreateExec(_db_para);

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Prüfen, ob DB (Blazor Web) oder WebApi (MAUI Blazor)
                //switch (_appState!.Platforms)
                switch (_currPlatform)
                {
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
                                // 1. Technischer Fallback: In WASM gibt es kein SQLite. 
                                // Wenn AUTO (SQLite-Ziel) gewählt ist, biegen wir es auf JSON_HYBRID um.
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

                                if (_appState.IsInternetConnected)
                                {
                                    // Verschlüsseln
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
                                            user.EncryptDecrypt = "1";
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = "0";
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
                                        else //...wenn WebAPi Aufruf erfolgreich
                                        {
                                            if (String.IsNullOrEmpty(result_cloud.out_err))
                                            {
                                                // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
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
                                        //ScalarModel result_local = await _sqLite!.Save(_db_para);
                                        ScalarModel result_local = new(); // await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result_local = await _sqLite!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result_local = await _jsonStorage!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result_local = await _memoryStorage!.Save(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
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
                                        //result = await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result = await _sqLite!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result = await _jsonStorage!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result = await _memoryStorage!.Save(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    else
                                        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                    //////////////////////////
                                }





                                //// Verschlüsseln
                                //user.Token = _appState.WebApiToken;


                                //if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                //{
                                //    case_cloud = false;
                                //    case_local = true; 
                                //}

                                //if (_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                //{
                                //    using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                //    {
                                //        user.JsonPara = aes.Encrypt(jsonpara);
                                //        user.EncryptDecrypt = "1";
                                //    }
                                //}
                                //else
                                //{
                                //    user.JsonPara = jsonpara;
                                //    user.EncryptDecrypt = "0";
                                //}

                                //// Ist Internetverbindung vorhanden
                                //if (_appState.IsInternetConnected)
                                //{
                                //    //////////////////////////
                                //    // W E B A P I
                                //    //////////////////////////
                                //    if (case_cloud)
                                //    {
                                //        ScalarModel result_cloud = case_cloud ? await _api!.ChangePassword(user) : new();

                                //        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                //        {
                                //            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                //            {
                                //                result_cloud = case_cloud ? await _api!.ChangePassword(user) : new();
                                //                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                //                    await Task.Delay(500);
                                //                else
                                //                    break;
                                //            }
                                //        }
                                //        else //...wenn WebAPi Aufruf erfolgreich
                                //        {
                                //            if (String.IsNullOrEmpty(result_cloud.out_err))
                                //            {
                                //                // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
                                //                string[] arrRes_cloud = result_cloud.out_value_str!.Split(":");

                                //                if (arrRes_cloud.Length == 3)
                                //                {
                                //                    result.out_value_str = result_cloud.out_value_str;
                                //                    result.out_cloud = result_cloud.out_value_str;
                                //                }

                                //                //////////////////////////
                                //                // L o c a l
                                //                //////////////////////////
                                //                if (case_local)
                                //                {
                                //                    ScalarModel result_local = await _sqLite!.Save(_db_para);
                                //                    if (String.IsNullOrEmpty(result_local.out_err))
                                //                    {
                                //                        // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
                                //                        string[] arrRes_sqlite = result_local.out_value_str!.Split(":");

                                //                        if (arrRes_sqlite.Length == 3)
                                //                        {
                                //                            result.out_local = result_local.out_value_str;
                                //                        }
                                //                    }
                                //                    else
                                //                        result.out_err = result_local.out_err;
                                //                }
                                //                //////////////////////////
                                //            }
                                //            else
                                //                result.out_err = result_cloud.out_err;
                                //        }
                                //    }
                                //    else // Wenn kein Case_ für Cloud vorhanden, dann nur lokal speichern
                                //    {
                                //        //////////////////////////
                                //        // L o c a l
                                //        //////////////////////////
                                //        if (case_local)
                                //        {
                                //            ScalarModel result_local = await _sqLite!.Save(_db_para);
                                //            if (String.IsNullOrEmpty(result_local.out_err))
                                //            {
                                //                // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
                                //                string[] arrRes_sqlite = result_local.out_value_str!.Split(":");

                                //                if (arrRes_sqlite.Length == 3)
                                //                {
                                //                    result.out_value_str = result_local.out_value_str;
                                //                    result.out_local = result_local.out_value_str;
                                //                }
                                //            }
                                //            else
                                //                result.out_err = result_local.out_err;
                                //        }
                                //        //////////////////////////
                                //    }
                                //    //////////////////////////
                                //}
                                //else // ...wenn nicht, dann nur lokal Daten speichern (SqLite)
                                //{
                                //    //////////////////////////
                                //    // L o c a l
                                //    //////////////////////////
                                //    if (case_local)
                                //    {
                                //        result = await _sqLite!.ExecQuery(_db_para);
                                //        result.out_local = result.out_value_str;
                                //    }
                                //    else
                                //        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                //    //////////////////////////
                                //}
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

            return result;
        }
   
        /// <summary>
        /// Methode, um einen Scalar-Wert zu liefern
        /// </summary>
        /// <param name="_db_para">Parameter für die Erstellung von Abfragen (Cloud und Local)</param>
        /// <param name="_token">Token für dne Zugriff auf WebApi</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
        public async Task<ScalarModel> Scalar(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START Scalar aufruf:", data: _db_para);

            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Prüfen, ob DB oder WebApi
                //switch (_appState!.Platforms)
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
                                // 1. Technischer Fallback: In WASM gibt es kein SQLite. 
                                // Wenn AUTO (SQLite-Ziel) gewählt ist, biegen wir es auf JSON_HYBRID um.
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
                                        user.EncryptDecrypt = "1";
                                        // SP verschlüsseln
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = "0";
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
                                        else //...wenn WebAPi Aufruf erfolgreich
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
                                        //ScalarModel result_local = await _sqLite!.Scalar(_db_para);
                                        ScalarModel result_local = new(); // await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result_local = await _sqLite!.Scalar(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result_local = await _jsonStorage!.Scalar(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result_local = await _memoryStorage!.Scalar(_db_para);
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
                                        //result = await _sqLite!.Scalar(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result = await _sqLite!.Scalar(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result = await _jsonStorage!.Scalar(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result = await _memoryStorage!.Scalar(_db_para);
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
                // AOT-sichere Fehlerprotokollierung: Entfernung der Reflection
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Scalar aufruf");

            return result;
        }

        /// <summary>
        /// Methode, die Daten von Cloud und Lokal selektiert und diese vergleicht
        /// </summary>
        /// <param name="_db_para">SP Parameter-Paare (EXEC)</param>
        /// <param name="_token">Token für dne Zugriff auf WebApi</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
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
                            bool ComparetionData = true;
                            if (_db_para.ContainsKey("cmd_nocomparetion"))
                                ComparetionData = false;

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
                                if (ComparetionData)
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
        public async Task<ReadModel<T?>>? ReadData<T>(Dictionary<string, string> _db_para) where T : class, new() //where T : new()
        {
            if (_appState == null)
                return new ReadModel<T?> { out_err = "pEngine error: AppState is null" };

            ReadModel<T?>? result = new();
            result.out_list = new();

            result = await Read<T>(_db_para);

            return result;
        }
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
                        ReaderModel<T> RESserver = await _sqlClient!.Reader<T>(_db_para);
                        if (RESserver.out_list != null)
                        {
                            result.out_err = RESserver.out_err;
                            result.out_list = RESserver.out_list;
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
                                // 1. Technischer Fallback: In WASM gibt es kein SQLite. 
                                // Wenn AUTO (SQLite-Ziel) gewählt ist, biegen wir es auf JSON_HYBRID um.
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
                                        user.EncryptDecrypt = "1";
                                        // SP verschlüsseln
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = "0";
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
                                        //ReaderModel<T> RESlocal = await _sqLite!.Read<T>(_db_para);
                                        ReaderModel<T> result_local = new(); // await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result_local = await _sqLite!.Read<T>(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result_local = await _jsonStorage!.Read<T>(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result_local = await _memoryStorage!.Read<T>(_db_para);
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
                                        //ReaderModel<T> RESlocal = await _sqLite!.Read<T>(_db_para);
                                        ReaderModel<T> result_local = new(); // await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result_local = await _sqLite!.Read<T>(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result_local = await _jsonStorage!.Read<T>(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result_local = await _memoryStorage!.Read<T>(_db_para);
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

                                //if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                //{
                                //    case_cloud = false;
                                //    case_local = true;
                                //}

                                //// Verschlüsselung aktiviert?
                                //if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                //{
                                //    user.EncryptDecrypt = (_globalState.ConfigGeneral.EncryptDecryptWebApi ? "1" : "0");
                                //    // SP verschlüsseln
                                //    using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                //    {
                                //        user.JsonPara = aes.Encrypt(jsonpara);
                                //    }
                                //}
                                //else
                                //{
                                //    user.JsonPara = jsonpara;
                                //    user.EncryptDecrypt = (_globalState.ConfigGeneral.EncryptDecryptWebApi ? "1" : "0");
                                //}

                                //if (_appState.IsInternetConnected)
                                //{
                                //    //////////////////////////
                                //    // W E B A P I
                                //    //////////////////////////
                                //    if (case_cloud)
                                //    {
                                //        ReaderModel<T> RESwebapi = await _api!.GetRows<T>(
                                //            user,
                                //            AotUtility.GetListTypeInfo<T>() // AOT-sicher
                                //        );
                                //        if (RESwebapi.out_err == "Error 401: Unauthorized request.")
                                //        {
                                //            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                //            {
                                //                RESwebapi = await _api!.GetRows<T>(
                                //                    user,
                                //                    AotUtility.GetListTypeInfo<T>() // AOT-sicher
                                //                );
                                //                if (RESwebapi.out_err == "Error 401: Unauthorized request.")
                                //                    await Task.Delay(500);
                                //                else
                                //                    break;
                                //            }
                                //        }
                                //        if (String.IsNullOrEmpty(RESwebapi.out_err))
                                //        {
                                //            result.out_list = RESwebapi.out_list;
                                //            result.out_list_cloud = RESwebapi.out_list;
                                //        }
                                //        else
                                //            result.out_err = RESwebapi.out_err;
                                //    }

                                //    //////////////////////////
                                //    // L o c a l
                                //    //////////////////////////
                                //    if (case_local)
                                //    {
                                //        //ReaderModel<T> RESlocal = await _sqLite!.Read<T>(_db_para);
                                //        ReaderModel<T> result_local = new(); // await _sqLite!.Save(_db_para);
                                //        switch (localStorageType)
                                //        {
                                //            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                // Nur hier rufen wir den echten SQLite-Service auf
                                //                result_local = await _sqLite!.Read<T>(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                // Unser neuer File-basierter Cache
                                //                result_local = await _jsonStorage!.Read<T>(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.MEMORY:
                                //                // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                result_local = await _memoryStorage!.Read<T>(_db_para);
                                //                break;
                                //        }
                                //        if (String.IsNullOrEmpty(result_local.out_err))
                                //        {
                                //            // Wenn kein Cloud, dann Lokal-Daten übernehmen
                                //            if (case_cloud == false)
                                //                result.out_list = result_local.out_list ?? new List<T?>();
                                //            else
                                //                result.out_list_local = result_local.out_list ?? new List<T?>();
                                //        }
                                //        else
                                //        {
                                //            result.out_err = result_local.out_err;

                                //            // Wenn bei WebApi ein Fehler ausgelöst wird, sollen die Daten dann aus der lokalen DB angezeigt werden
                                //            if (case_cloud == false)
                                //            {
                                //                //result.out_list = RESlocal.out_list;
                                //                result.out_list = result_local.out_list ?? new List<T?>();
                                //            }
                                //        }
                                //    }
                                //    //////////////////////////
                                //}
                                //else // Keine Internetverbindung
                                //{
                                //    //////////////////////////
                                //    // L o c a l
                                //    //////////////////////////
                                //    if (case_local)
                                //    {
                                //        //ReaderModel<T> RESlocal = await _sqLite!.Read<T>(_db_para);
                                //        ReaderModel<T> result_local = new(); // await _sqLite!.Save(_db_para);
                                //        switch (localStorageType)
                                //        {
                                //            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                // Nur hier rufen wir den echten SQLite-Service auf
                                //                result_local = await _sqLite!.Read<T>(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                // Unser neuer File-basierter Cache
                                //                result_local = await _jsonStorage!.Read<T>(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.MEMORY:
                                //                // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                result_local = await _memoryStorage!.Read<T>(_db_para);
                                //                break;
                                //        }
                                //        if (String.IsNullOrEmpty(result_local.out_err))
                                //        {
                                //            result.out_list = result_local.out_list ?? new List<T?>();
                                //            result.out_list_local = result_local.out_list ?? new List<T?>();
                                //        }
                                //        else
                                //            result.out_err = result_local.out_err;
                                //    }
                                //}
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
                // Wenn das ReadModel selbst null ist, liegt wahrscheinlich ein schwerwiegender
                // Fehler in der Datenzugriffsschicht vor. Geben Sie ein leeres/Fehler-Modell zurück.
                return new ReadModel<T?> { out_err = "pEngine error: ReadModel returned null after await." };
            }
            else
                return result;
        }

        /// <summary>
        /// Methode, um eine Save Abfrage bei MAUI Blazor auszuführen
        /// </summary>
        /// <param name="_db_para">SP Parameter-Paare (EXEC)</param>
        /// <param name="_token">Token für dne Zugriff auf WebApi</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
        public async Task<ScalarModel> Save(Dictionary<string, string> _db_para) // Verwendung bei MAUI Blazor
        {
            await _appState!.Log("[BLAZOR - DAM] START Save aufruf:", data: _db_para);

            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Prüfen, ob DB (Blazor Web) oder WebApi (MAUI Blazor)
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
                                // 1. Technischer Fallback: In WASM gibt es kein SQLite. 
                                // Wenn AUTO (SQLite-Ziel) gewählt ist, biegen wir es auf JSON_HYBRID um.
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

                                if (_appState.IsInternetConnected)
                                {
                                    user.Token = _appState.WebApiToken;

                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    // Verschlüsseln
                                    if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        user.EncryptDecrypt = "1";
                                        // SP verschlüsseln
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = "0";
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
                                        else //...wenn WebAPi Aufruf erfolgreich
                                        {
                                            if (String.IsNullOrEmpty(result_cloud.out_err))
                                            {
                                                // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
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
                                        ScalarModel result_local = new(); // await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result_local = await _sqLite!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result_local = await _jsonStorage!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result_local = await _memoryStorage!.Save(_db_para);
                                                break;
                                        }
                                        if (String.IsNullOrEmpty(result_local.out_err))
                                        {
                                            // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
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
                                        //result = await _sqLite!.Save(_db_para);
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result = await _sqLite!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result = await _jsonStorage!.Save(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result = await _memoryStorage!.Save(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    else
                                        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                    //////////////////////////
                                }

                                //user.Token = _appState.WebApiToken;

                                //if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                //{
                                //    case_cloud = false;
                                //    case_local = true;
                                //}

                                //// Verschlüsseln
                                //if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                //{
                                //    user.EncryptDecrypt = (_globalState.ConfigGeneral.EncryptDecryptWebApi ? "1" : "0");
                                //    // SP verschlüsseln
                                //    using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                //    {
                                //        user.JsonPara = aes.Encrypt(jsonpara);
                                //    }
                                //}
                                //else
                                //{
                                //    user.JsonPara = jsonpara;
                                //    user.EncryptDecrypt = (_globalState.ConfigGeneral.EncryptDecryptWebApi ? "1" : "0");
                                //}

                                //if (_appState.IsInternetConnected)
                                //{
                                //    //////////////////////////
                                //    // W E B A P I
                                //    //////////////////////////
                                //    if (case_cloud)
                                //    {
                                //        ScalarModel result_cloud = case_cloud ? await _api!.PostData(user) : new();

                                //        await _appState!.Log("[BLAZOR - DAM] _api!.PostData:", data: result_cloud);

                                //        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                //        {
                                //            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                //            {
                                //                result_cloud = case_cloud ? await _api!.PostData(user) : new();
                                //                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                //                    await Task.Delay(500);
                                //                else
                                //                    break;
                                //            }
                                //        }
                                //        else //...wenn WebAPi Aufruf erfolgreich
                                //        {
                                //            if (String.IsNullOrEmpty(result_cloud.out_err))
                                //            {
                                //                // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
                                //                string[] arrRes_cloud = result_cloud.out_value_str!.Split(":");

                                //                if (arrRes_cloud.Length == 3)
                                //                {
                                //                    result.out_value_str = result_cloud.out_value_str;
                                //                    result.out_cloud = result_cloud.out_value_str;
                                //                }

                                //                //////////////////////////
                                //                // L o c a l
                                //                //////////////////////////
                                //                if (case_local)
                                //                {
                                //                    ScalarModel result_local = new(); // await _sqLite!.Save(_db_para);
                                //                    switch (localStorageType)
                                //                    {
                                //                        case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                            // Nur hier rufen wir den echten SQLite-Service auf
                                //                            result_local = await _sqLite!.Save(_db_para);
                                //                            break;

                                //                        case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                            // Unser neuer File-basierter Cache
                                //                            result_local = await _jsonStorage!.Save(_db_para);
                                //                            break;

                                //                        case LOCAL_STORAGE_TYPE.MEMORY:
                                //                            // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                            result_local = await _memoryStorage!.Save(_db_para);
                                //                            break;
                                //                    }
                                //                    if (String.IsNullOrEmpty(result_local.out_err))
                                //                    {
                                //                        // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
                                //                        string[] arrRes_sqlite = result_local.out_value_str!.Split(":");

                                //                        if (arrRes_sqlite.Length == 3)
                                //                        {
                                //                            result.out_local = result_local.out_value_str;
                                //                        }
                                //                    }
                                //                    else
                                //                        result.out_err = result_local.out_err;
                                //                }
                                //                //////////////////////////
                                //            }
                                //            else
                                //                result.out_err = result_cloud.out_err;
                                //        }
                                //    }
                                //    else // Wenn kein Case_ für Cloud vorhanden, dann nur lokal speichern
                                //    {
                                //        //////////////////////////
                                //        // L o c a l
                                //        //////////////////////////
                                //        if (case_local)
                                //        {
                                //            ScalarModel result_local = new(); // await _sqLite!.Save(_db_para);
                                //            switch (localStorageType)
                                //            {
                                //                case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                    // Nur hier rufen wir den echten SQLite-Service auf
                                //                    result_local = await _sqLite!.Save(_db_para);
                                //                    break;

                                //                case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                    // Unser neuer File-basierter Cache
                                //                    result_local = await _jsonStorage!.Save(_db_para);
                                //                    break;

                                //                case LOCAL_STORAGE_TYPE.MEMORY:
                                //                    // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                    result_local = await _memoryStorage!.Save(_db_para);
                                //                    break;
                                //            }
                                //            if (String.IsNullOrEmpty(result_local.out_err))
                                //            {
                                //                // Rückgabewert auslesen (Status, ID und AuthUsers_ID)
                                //                string[] arrRes_sqlite = result_local.out_value_str!.Split(":");

                                //                if (arrRes_sqlite.Length == 3)
                                //                {
                                //                    result.out_value_str = result_local.out_value_str;
                                //                    result.out_local = result_local.out_value_str;
                                //                }
                                //            }
                                //            else
                                //                result.out_err = result_local.out_err;
                                //        }
                                //        //////////////////////////
                                //    }
                                //    //////////////////////////
                                //}
                                //else // ...wenn nicht, dann nur lokal Daten speichern (Realm)
                                //{
                                //    //////////////////////////
                                //    // L o c a l
                                //    //////////////////////////
                                //    if (case_local)
                                //    {
                                //        //result = await _sqLite!.Save(_db_para);
                                //        switch (localStorageType)
                                //        {
                                //            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                // Nur hier rufen wir den echten SQLite-Service auf
                                //                result = await _sqLite!.Save(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                // Unser neuer File-basierter Cache
                                //                result = await _jsonStorage!.Save(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.MEMORY:
                                //                // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                result = await _memoryStorage!.Save(_db_para);
                                //                break;
                                //        }
                                //        result.out_local = result.out_value_str;
                                //    }
                                //    else
                                //        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                //    //////////////////////////
                                //}
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

            await _appState!.Log("[BLAZOR - DAM] ENDE Save aufruf");

            return result;
        }

        /// <summary>
        /// Methode, um eine SQL Abfrage auszuführen (scalar wie Update, Delete usw.)
        /// </summary>
        /// <param name="_db_para">SP Parameter-Paare (EXEC)</param>
        /// <param name="_token">Token für dne Zugriff auf WebApi</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
        public async Task<ScalarModel> ExecQuery(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START ExecQuery aufruf:", data: _db_para);

            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;

                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
                if (!_db_para.ContainsKey("@LastUpdateUnixTS"))
                    _db_para["@LastUpdateUnixTS"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                // Prüfen, ob DB oder WebApi
                //switch (_appState!.Platforms)
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
                                // 1. Technischer Fallback: In WASM gibt es kein SQLite. 
                                // Wenn AUTO (SQLite-Ziel) gewählt ist, biegen wir es auf JSON_HYBRID um.
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

                                if (_appState.IsInternetConnected)
                                {
                                    user.Token = _appState.WebApiToken;

                                    if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        case_cloud = false;
                                        case_local = true;
                                    }

                                    // Verschlüsseln
                                    if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                    {
                                        user.EncryptDecrypt = "1";
                                        // SP verschlüsseln
                                        using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                        {
                                            user.JsonPara = aes.Encrypt(jsonpara);
                                        }
                                    }
                                    else
                                    {
                                        user.JsonPara = jsonpara;
                                        user.EncryptDecrypt = "0";
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
                                        else //...wenn WebAPi Aufruf erfolgreich
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
                                        //ScalarModel result_local = await _sqLite!.ExecQuery(_db_para);
                                        ScalarModel result_local = new();
                                        switch (localStorageType)
                                        {
                                            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result_local = await _sqLite!.ExecQuery(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result_local = await _jsonStorage!.ExecQuery(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result_local = await _memoryStorage!.ExecQuery(_db_para);
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
                                                // Nur hier rufen wir den echten SQLite-Service auf
                                                result = await _sqLite!.ExecQuery(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                                // Unser neuer File-basierter Cache
                                                result = await _jsonStorage!.ExecQuery(_db_para);
                                                break;

                                            case LOCAL_STORAGE_TYPE.MEMORY:
                                                // Daten landen NUR in der flüchtigen Liste (RAM)
                                                result = await _memoryStorage!.ExecQuery(_db_para);
                                                break;
                                        }
                                        result.out_local = result.out_value_str;
                                    }
                                    else
                                        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                    //////////////////////////
                                }


                                //user.Token = _appState.WebApiToken;

                                //if (user.Token == API_CONST.TOKEN_LOCAL_ONLY)
                                //{
                                //    case_cloud = false;
                                //    case_local = true;
                                //}

                                //// Verschlüsseln
                                //if (_globalState!.ConfigGeneral.EncryptDecryptWebApi && _currPlatform == PLATFORMS.WINDOWS_CLIENT)
                                //{
                                //    user.EncryptDecrypt = (_globalState.ConfigGeneral.EncryptDecryptWebApi ? "1" : "0");
                                //    // SP verschlüsseln
                                //    using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                //    {
                                //        user.JsonPara = aes.Encrypt(jsonpara);
                                //    }
                                //}
                                //else
                                //{
                                //    user.JsonPara = jsonpara;
                                //    user.EncryptDecrypt = (_globalState.ConfigGeneral.EncryptDecryptWebApi ? "1" : "0");
                                //}

                                //if (_appState.IsInternetConnected)
                                //{
                                //    //////////////////////////
                                //    // W E B A P I
                                //    //////////////////////////
                                //    if (case_cloud)
                                //    {
                                //        ScalarModel result_cloud = await _api!.PostData(user);
                                //        if (result_cloud.out_err == "Error 401: Unauthorized request.") // Falls sich der WebApi Server nicht sofort meldet 
                                //        {
                                //            for (int attempt = 1; attempt <= attemptWebApi; attempt++)
                                //            {
                                //                result_cloud = await _api.PostData(user);
                                //                if (result_cloud.out_err == "Error 401: Unauthorized request.")
                                //                    await Task.Delay(500);
                                //                else
                                //                    break;
                                //            }
                                //        }
                                //        else //...wenn WebAPi Aufruf erfolgreich
                                //        {
                                //            result.out_value_str = result_cloud.out_value_str;

                                //            if (String.IsNullOrEmpty(result_cloud.out_err))
                                //            {
                                //                //////////////////////////
                                //                // L o c a l
                                //                //////////////////////////
                                //                if (case_local)
                                //                {
                                //                    //ScalarModel result_local = await _sqLite!.ExecQuery(_db_para);
                                //                    ScalarModel result_local = new();
                                //                    switch (localStorageType)
                                //                    {
                                //                        case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                            // Nur hier rufen wir den echten SQLite-Service auf
                                //                            result_local = await _sqLite!.ExecQuery(_db_para);
                                //                            break;

                                //                        case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                            // Unser neuer File-basierter Cache
                                //                            result_local = await _jsonStorage!.ExecQuery(_db_para);
                                //                            break;

                                //                        case LOCAL_STORAGE_TYPE.MEMORY:
                                //                            // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                            result_local = await _memoryStorage!.ExecQuery(_db_para);
                                //                            break;
                                //                    }
                                //                    if (String.IsNullOrEmpty(result_local.out_err))
                                //                        result.out_local = String.IsNullOrEmpty(result.out_value_str) ? result_local.out_value_str : result.out_value_str;
                                //                    else
                                //                        result.out_err = result_local.out_err;
                                //                }
                                //                //////////////////////////
                                //            }
                                //            else
                                //                result.out_err = result_cloud.out_err;
                                //        }
                                //    }
                                //    else
                                //    {
                                //        //////////////////////////
                                //        // L o c a l
                                //        //////////////////////////
                                //        if (case_local)
                                //        {
                                //            //ScalarModel result_local = await _sqLite!.ExecQuery(_db_para);
                                //            ScalarModel result_local = new();
                                //            switch (localStorageType)
                                //            {
                                //                case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                    // Nur hier rufen wir den echten SQLite-Service auf
                                //                    result_local = await _sqLite!.ExecQuery(_db_para);
                                //                    break;

                                //                case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                    // Unser neuer File-basierter Cache
                                //                    result_local = await _jsonStorage!.ExecQuery(_db_para);
                                //                    break;

                                //                case LOCAL_STORAGE_TYPE.MEMORY:
                                //                    // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                    result_local = await _memoryStorage!.ExecQuery(_db_para);
                                //                    break;
                                //            }
                                //            if (String.IsNullOrEmpty(result_local.out_err))
                                //            {
                                //                result.out_value_str = result_local.out_value_str;
                                //                result.out_local = result_local.out_value_str;
                                //            }
                                //            else
                                //                result.out_err = result_local.out_err;
                                //        }
                                //        //////////////////////////
                                //    }
                                //    //////////////////////////
                                //}
                                //else
                                //{
                                //    //////////////////////////
                                //    // L o c a l
                                //    //////////////////////////
                                //    if (case_local)
                                //    {
                                //        //result = await _sqLite!.Save(_db_para);
                                //        switch (localStorageType)
                                //        {
                                //            case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                                //                // Nur hier rufen wir den echten SQLite-Service auf
                                //                result = await _sqLite!.ExecQuery(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                                //                // Unser neuer File-basierter Cache
                                //                result = await _jsonStorage!.ExecQuery(_db_para);
                                //                break;

                                //            case LOCAL_STORAGE_TYPE.MEMORY:
                                //                // Daten landen NUR in der flüchtigen Liste (RAM)
                                //                result = await _memoryStorage!.ExecQuery(_db_para);
                                //                break;
                                //        }
                                //        result.out_local = result.out_value_str;
                                //    }
                                //    else
                                //        result.out_err += (!String.IsNullOrEmpty(result.out_err) ? " , " : "") + "no_case_local";
                                //    //////////////////////////
                                //}
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

            await _appState!.Log("[BLAZOR - DAM] ENDE Save aufruf");

            return result;
        }

        /// <summary>
        /// Generiert einen otp-Schlüssel
        /// </summary>
        /// <returns>Rückgabewert ist ein Scalar Objekt</returns>
        public async Task<ScalarModel> AnonymousQuery(Dictionary<string, string> _db_para)
        {
            await _appState!.Log("[BLAZOR - DAM] START AnonymousQuery aufruf:", data: _db_para);

            ScalarModel result = new();
            //string exec = Appl.CreateExec(_db_para); // zum Testen

            try
            {
                // LastUpdateUnixTS setzen, um später Migrationsrichtung bestimmen zu können (Cloud/lokal)
                // Bemerkung: Bei migration müssen 'LastUpdateUnixTS' (und ev. 'LastUpdateUnixTS2') gleich gesetzt werden mit dem
                // migrierten Wert LastUpdateUnixTS/LastUpdateUnixTS2 (damit es keine Zierkelfluss gibt indem sich der migrierte
                // Datensatz wegen 'LastUpdateUnixTS' vom ursprünglichem Datensatz unterscheiden würde)!
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


                // Prüfen, ob DB oder WebApi  PLATFORM.Current = (int)PLATFORMS.WINDOWS_API
                //switch (_appState!.Platforms)
                switch (_currPlatform)
                {
                    // Beim Web-Server oder Web-Api Server wird Cloud direkt angesprochen
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

                    // Bei Windows, Android oder iOS erfolgt die Verbindung zur Cloud Datenbank über den Web-Api Server
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
                            UserWebApi user = new();
                            if (_currPlatform == PLATFORMS.WINDOWS_CLIENT)
                            {
                                using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                                {
                                    user.JsonPara = aes.Encrypt(jsonpara);
                                    user.EncryptDecrypt = "1";
                                    //user.DisplayError = "1";
                                }
                            }
                            else
                            {
                                user.JsonPara = jsonpara;
                                user.EncryptDecrypt = "0";
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
                // AOT-sichere Fehlerprotokollierung: Entfernung der Reflection
                result.out_err += (String.IsNullOrEmpty(result.out_err) ? "" : "\r\n")
                    + "Error: " + ex.Message
                    + " , Classname : Dam";
            }

            await _appState!.Log("[BLAZOR - DAM] ENDE Save aufruf");

            return result;
        }

        /// <summary>
        /// Vergleicht lokale und Cloud Daten
        /// </summary>
        /// <returns>Rückgabewert ist die Liste der Datensätzen</returns>
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

            var cloudDict = cloudItems.ToDictionary(x => x?.UnixTS ?? "0");
            var localDict = localItems.ToDictionary(x => x?.UnixTS ?? "0");

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
            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                //result = await _sqLite!.ClearAllData();
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;
                switch (localStorageType)
                {
                    case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                        // Nur hier rufen wir den echten SQLite-Service auf
                        result = await _sqLite!.ClearAllData();
                        break;

                    case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                        // Unser neuer File-basierter Cache
                        result = await _jsonStorage!.ClearAllData();
                        break;

                    case LOCAL_STORAGE_TYPE.MEMORY:
                        // Daten landen NUR in der flüchtigen Liste (RAM)
                        result = await _memoryStorage!.ClearAllData();
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
            if (_appState == null || _globalState == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                //result = await _sqLite!.DeleteDB();
                var localStorageType = _globalState.ConfigGeneral.LocalStorageType;
                switch (localStorageType)
                {
                    case LOCAL_STORAGE_TYPE.LOCAL_DB_AUTO:
                        // Nur hier rufen wir den echten SQLite-Service auf
                        result = await _sqLite!.DeleteDB();
                        break;

                    case LOCAL_STORAGE_TYPE.JSON_HYBRID:
                        // Unser neuer File-basierter Cache
                        result = await _jsonStorage!.DeleteDB();
                        break;

                    case LOCAL_STORAGE_TYPE.MEMORY:
                        // Daten landen NUR in der flüchtigen Liste (RAM)
                        result = await _memoryStorage!.DeleteDB();
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"[Dam] DeleteRealmDB: {ex.Message}";
            }

            return result;
        }

        public async Task<ScalarModel> LoadJsonStorage()
        {
            if (_appState == null || _jsonStorage == null)
                return new ScalarModel { out_err = "pEngine error: AppState is null" };

            ScalarModel result = new();

            try
            {
                var user = _appState.HasUserAccount() ? _appState.UserAccount : _appState.UnixTS;
                result = await _jsonStorage.InitializeAsync(true, user);
            }
            catch (Exception ex)
            {
                result.out_err = $"[Dam] DeleteSqLiteData: {ex.Message}";
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
                        _db_para[DB_CMD.NO_CLOUD] = string.Empty;
                        break;

                    case STORAGE_LOCATION.CLOUD:
                        _db_para[DB_CMD.NO_LOCAL] = string.Empty;
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