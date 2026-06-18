//using BlazorCore.DbApp.pMunus.Models;
//using BlazorCore.Models;
//using BlazorCore.Services.AppState;
//using BlazorCore.Services.Dam;
//using BlazorCore.Services.GlobalState;
//using BlazorCore.Services.Platform;
//using BlazorCore.Services.SqlClient;
//using BlazorCore.Utility;
//using Realms;

//namespace BlazorCore.Services.Realm
//{
//    public partial class RealmBase : IRealmBase
//    {
//        public bool IsInitialized { get; set; } = false;

//        private readonly IGlobalStateBase _globalState;
//        private readonly IAppStateBase _appState;
//        private readonly IPlatformBase _platform;
//        private readonly p11.UI.Services.IMessageBoxService _messageBoxService;
//        private string _appDataDirectory;

//        private RealmConfiguration? _config;

//        private List<string> Tables = new();

//        private readonly string _dbPath;
//        private Realms.Realm? _realm;
//        private byte[]? _encryptionKey; // Dein 64-Byte Key aus Phase A

//        //[GeneratedRegex("@\\w+")]
//        //private static partial Regex ParameterRegex();

//        //// Private Methode, um das SQL-Insert-Statement pro Typ zu cachen
//        //private static readonly ConcurrentDictionary<Type, string> InsertSqlCache = new();
//        //private static readonly ConcurrentDictionary<Type, string> UpdateSqlCache = new();
//        //private static readonly ConcurrentDictionary<Type, string> DeleteSqlCache = new();

//        //private bool _isInitialized;
//        private Task? _initTask;
//        private readonly object _initLock = new();
//        private static readonly object _lockGetNextId = new();


//        //public RealmBase(
//        //    IGlobalStateBase globalState,
//        //    IAppStateBase appState,
//        //    IPlatformBase platform,
//        //    p11.UI.Services.IMessageBoxService messageBoxService,
//        //    string appDataDirectory)
//        //{
//        //    _globalState = globalState;
//        //    _appState = appState;
//        //    _platform = platform;
//        //    _messageBoxService = messageBoxService;
//        //    _appDataDirectory = appDataDirectory;
//        //}
//        public RealmBase(
//                IGlobalStateBase globalState,
//                IAppStateBase appState,
//                IPlatformBase platform,
//                p11.UI.Services.IMessageBoxService messageBoxService,
//                string appDataDirectory)
//        {
//            _globalState = globalState;
//            _appState = appState;
//            _platform = platform;
//            _messageBoxService = messageBoxService;
//            _appDataDirectory = appDataDirectory;

//            string dbName = BlazorCore.Utility.Appl.RealmDB;
//            if (dbName.Contains("."))
//                dbName = dbName.Substring(0, dbName.LastIndexOf('.')) + ".realm";
//            else
//                dbName += ".realm";

//            _dbPath = Path.Combine(_appDataDirectory, dbName);
//        }

//        #region INIT
//        // Diese Methode wird von deinem OnStart aufgerufen
//        public Task EnsureInitializedAsync(string appDataDirectory)
//        {
//            if (IsInitialized)
//                return Task.CompletedTask;

//            lock (_initLock)
//            {
//                _appDataDirectory = appDataDirectory;
//                // Wir behalten dein Task-Caching bei
//                return _initTask ??= InitializeInternalAsync();
//            }
//        }

//        private async Task InitializeInternalAsync()
//        {
//            try
//            {
//                // Hier passiert die Realm-spezifische Magie
//                //var dbPath = Path.Combine(_appDataDirectory!, "pMunus.realm");
//                var dbPath = _dbPath;

//                // 64-Byte Key aus SecureStorage holen/erzeugen
//                var encryptionKey = await GetOrCreateRealmKeyAsync();


//                // C:\Users\perfe\AppData\Local\Saša Daniel Simić pr True Perfect Code\ch.trueperfectcode.pMunus\Data\pMunusDB.realm
//                _config = new RealmConfiguration(dbPath)
//                {
//                    EncryptionKey = encryptionKey,
//                    SchemaVersion = 1
//                };

//                // Einmal kurz öffnen, um sicherzustellen, dass das Schema steht
//                using var realm = Realms.Realm.GetInstance(_config);

//                IsInitialized = true;
//            }
//            catch (Exception ex)
//            {
//                // Wichtig: Fehler loggen, damit OnStart im try-catch darauf reagieren kann
//                Console.WriteLine($"Realm Init Error: {ex.Message}");
//                throw;
//            }
//        }

//        private async Task<byte[]> GetOrCreateRealmKeyAsync()
//        {
//            // Wir nutzen den Pfad aus deinem GlobalState
//            string storageKey = _globalState.LocalStorage.realmkey + Appl.ApplicationName;

//            // 1. Versuche den Key aus deinem Plattform-Service zu lesen
//            var base64Key = await _platform.GetAsync(storageKey);

//            if (base64Key != null && string.IsNullOrEmpty(base64Key.out_err) && !string.IsNullOrEmpty(base64Key.out_value_str))
//            {
//                try
//                {
//                    return Convert.FromBase64String(base64Key.out_value_str);
//                }
//                catch (FormatException)
//                {
//                    // Logging oder Error-Handling
//                }
//            }

//            // 2. Generiere einen neuen 64-Byte Key (Realm Anforderung)
//            var newKey = new byte[64];
//            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
//            {
//                rng.GetBytes(newKey);
//            }

//            // 3. Speichere als Base64 => 6ZoI+dPCWZg43xoV9RZqF39KS+PrM0yY3DEsDeEw3/312B5/YTr5+82TYAlZK32LO30xL3jX04X4wvHNRySV9g==
//            //await _platform.SetAsync(storageKey, "e99a08f9d3c2599838df1a15f5166a177f4a4be3eb334c98dc312c0de130dffdf5d81e7f613af9fbcd936009592b7d8b3b7d312f78d7d385f8c2f1cd472495f6\r\n");
//            await _platform.SetAsync(storageKey, Convert.ToBase64String(newKey));

//            return newKey;
//        }
//        #endregion

//        #region LOGIN_REGISTER
//public async Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> _db_para)
//{
//    ClientStorageModel res = new();
//    try
//    {
//        // Wir holen die verschlüsselte Realm-Instanz
//        var realm = await GetRealmAsync();

//        // Parameter extrahieren
//        string case_ = _db_para.GetValueOrDefault("@Case_", "");
//        string unixts = _db_para.GetValueOrDefault("@UnixTS", "");
//        bool registration = _db_para.GetValueOrDefault("@Int__Registration", "0") == "1";
//        bool createUser = !_db_para.ContainsKey("cmd_nocreateuser");
//        bool updateUser = !_db_para.ContainsKey("cmd_noupdateuser");
//        bool case_mssql = _db_para.GetValueOrDefault("case_mssql", "0") == "1";

//        string emailHash = _db_para.GetValueOrDefault("@EmailHash", "");
//        string passwordHash = _db_para.GetValueOrDefault("@PasswordHash", "");

//        // WICHTIG: Wir suchen jetzt in der 'AuthUsersEntity' Tabelle!
//        var authUser = realm.All<AuthUsersEntity>()
//                            .FirstOrDefault(x => x.EmailHash == emailHash && x.PasswordHash == passwordHash);

//        if (case_ == "Register>>AuthUsers")
//        {
//            if (authUser != null) // Account existiert bereits
//            {
//                // UnixTS Abgleich (Sync-Logik) => falls Account/Passwort vorhanden, aber UnixTS unterschiedlich, dann abgleichen
//                if (!string.IsNullOrEmpty(unixts) && authUser.UnixTS != unixts)
//                {
//                    if (updateUser)
//                    {
//                        await realm.WriteAsync(() =>
//                        {
//                            authUser.UnixTS = unixts; // Realm schreibt direkt in die verschlüsselte Datei
//                        });
//                    }

//                    res.UnixTS = unixts;
//                    res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
//                }
//                else
//                {
//                    if (!string.IsNullOrEmpty(authUser.UnixTS))
//                    {
//                        res.UnixTS = authUser.UnixTS;
//                        res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
//                    }
//                    else
//                    {
//                        res.UnixTS = "";
//                        res.WebApiToken = "";
//                        res.out_err = "no_user";
//                    }
//                }
//            }
//            else // Account existiert nicht lokal
//            {
//                if (registration)
//                {
//                    if (!string.IsNullOrEmpty(unixts))
//                    {
//                        // Fallback: Suche via Email + UnixTS (falls Passwort auf anderem Gerät geändert wurde)
//                        var userByTS = realm.All<AuthUsersEntity>()
//                                            .FirstOrDefault(x => x.EmailHash == emailHash && x.UnixTS == unixts);

//                        if (userByTS != null)
//                        {
//                            if (updateUser)
//                            {
//                                await realm.WriteAsync(() =>
//                                {
//                                    userByTS.PasswordHash = passwordHash;
//                                    if (_db_para.TryGetValue("@LastUpdateUnixTS", out var lastUpdate))
//                                        userByTS.LastUpdateUnixTS = long.Parse(lastUpdate);
//                                });
//                            }
//                            res.UnixTS = userByTS.UnixTS;
//                            res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
//                        }
//                        else if (createUser)
//                        {
//                            // Ruft die Methode auf, die eine neue AuthUsersEntity anlegt
//                            res = await CreateLocalAccount(_db_para);
//                        }
//                    }
//                    else // Falls keine UnixTS, dann nicht registrieren
//                    {
//                        res.UnixTS = "";
//                        res.WebApiToken = "";
//                        res.out_err = "no_user";
//                    }
//                }
//                else // Falls keine Registrierung, dann melden, dass Accoutn nicht existiert
//                {
//                    // Prüfen, ob Cloud Account existiert (via case_mssql Parameter), dann lokalen Account erstellen
//                    if (case_mssql)
//                    {
//                        // Ruft die Methode auf, die eine neue AuthUsersEntity anlegt
//                        res = await CreateLocalAccount(_db_para, case_mssql);
//                    }
//                    else
//                    {
//                        res.UnixTS = "";
//                        res.WebApiToken = "";
//                        res.out_err = "no_user";
//                    }
//                }
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        res.out_err = ex.Message;
//        // Hier ggf. Logging ergänzen
//    }
//    return res;
//}

//        public async Task<ClientStorageModel> CreateLocalAccount(Dictionary<string, string> _db_para, bool case_mssql = false)
//        {
//            ClientStorageModel res = new();
//            try
//            {
//                var realm = await GetRealmAsync();

//                // Parameter extrahieren
//                string email = _db_para.GetValueOrDefault("@EmailHash", "");
//                string unixts = _db_para.GetValueOrDefault("@UnixTS", "");
//                string password = _db_para.GetValueOrDefault("@PasswordHash", "");
//                string idp = _db_para.GetValueOrDefault("@IdP", "");
//                bool termsAccepted = case_mssql ? true : (_db_para.GetValueOrDefault("@TermsAccepted", "0") == "1");

//                long.TryParse(_db_para.GetValueOrDefault("@LastUpdateUnixTS", "0"), out long lastUpdate);

//                // 1. Neues Realm-Objekt (Entity!) erstellen
//                // WICHTIG: Wir instanziieren AuthUsersEntity, NICHT AuthUsersModel
//                var newUserEntity = new AuthUsersEntity
//                {
//                    ID = GetNextId<AuthUsersEntity>(realm, unixts),
//                    UnixTS = unixts,
//                    EmailHash = email,
//                    PasswordHash = password,
//                    active = true,
//                    TermsAccepted = termsAccepted,
//                    IdP = idp,
//                    IdPClientIdent = API_CONST.TOKEN_LOCAL_ONLY,
//                    IdPToken = API_CONST.TOKEN_LOCAL_ONLY,
//                    UserRole = "",
//                    LastUpdateUnixTS = lastUpdate
//                };

//                // 2. In Realm speichern (Transaktion)
//                await realm.WriteAsync(() =>
//                {
//                    // realm.Add erkennt automatisch den PrimaryKey (UnixTS)
//                    realm.Add(newUserEntity, update: true);
//                });

//                // 3. Erfolg prüfen (LINQ Abfrage auf Entity)
//                var checkUser = realm.All<AuthUsersEntity>()
//                                     .FirstOrDefault(x => x.EmailHash == email && x.PasswordHash == password);

//                if (checkUser != null)
//                {
//                    res.UnixTS = checkUser.UnixTS;
//                    res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
//                }
//                else
//                {
//                    res.UnixTS = string.Empty;
//                }
//            }
//            catch (Exception ex)
//            {
//                res.out_err = ex.Message;
//            }
//            return res;
//        }
//        #endregion


//        #region CRUD
//        public async Task<ScalarModel> Save(Dictionary<string, string> _db_para)
//        {
//            ScalarModel RES = new();
//            try
//            {
//                // Wir holen uns die Instanz direkt über die interne Methode der Klasse
//                var realm = await GetRealmAsync();
//                string case_ = _db_para.GetValueOrDefault("@Case_", "");

//                if (string.IsNullOrEmpty(case_)) return RES;

//                // Asynchrone Transaktion starten
//                await realm.WriteAsync(() =>
//                {
//                    switch (case_)
//                    {
//                        case "UpdateIsChecked>>Tasks":
//                            HandleUpdateIsCheckedTasks(realm, _db_para, RES);
//                            break;
//                        case "Save>>Tasks":
//                            HandleSaveTasks(realm, _db_para, RES);
//                            break;
//                        case "Save>>AuthUsersExtend":
//                            HandleSaveAuthUsersExtend(realm, _db_para, RES);
//                            break;
//                        case "UpdateTermsAccepted>>AuthUsers":
//                            HandleUpdateTermsAccepted(realm, _db_para, RES);
//                            break;
//                        case "ChangePassword>>AuthUsers":
//                            HandleChangePassword(realm, _db_para, RES);
//                            break;
//                        case "Save>>SharingUsers":
//                            HandleSaveSharingUsers(realm, _db_para, RES);
//                            break;
//                        case "ChangeSharingStatus>>SharingUsers":
//                            HandleChangeSharingStatus(realm, _db_para, RES);
//                            break;
//                        case "Save>>Todo":
//                            HandleSaveTodo(realm, _db_para, RES);
//                            break;
//                        case "Save>>AppParameter":
//                            HandleSaveAppParameter(realm, _db_para, RES);
//                            break;
//                        case "SaveJson>>AppParameter":
//                            HandleSaveJsonAppParameter(realm, _db_para, RES);
//                            break;
//                        case "DeleteOtp>>AuthUsers":
//                            HandleDeleteOtp(realm, _db_para, RES);
//                            break;
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                RES.out_err = $"Error: {ex.Message} , Classname : RealmBase";
//            }

//            return RES;
//        }
//        private void HandleUpdateIsCheckedTasks(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var unixTS = para["@UnixTS"];
//            // Wir suchen in der TasksEntity (Datenbank-Tabelle)
//            var task = realm.All<TasksEntity>().FirstOrDefault(u => u.UnixTS == unixTS);

//            if (task != null)
//            {
//                // 1. Task-Status aktualisieren
//                task.IsChecked = _globalState.ConvertStrPara<bool>(para["@IsChecked"]);
//                task.LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"]);

//                // 2. Das zugehörige Todo-Projekt finden (ebenfalls als Entity)
//                var todo = realm.All<TodoEntity>().FirstOrDefault(t => t.UnixTS == task.Todo_UnixTS);

//                if (todo != null)
//                {
//                    // Alle Tasks dieses Projekts abrufen (IQueryable)
//                    var allTasks = realm.All<TasksEntity>().Where(t => t.Todo_UnixTS == todo.UnixTS);

//                    // LOGIK-UMKEHR: 
//                    // Wir prüfen, ob es KEINEN Task gibt (!...Any), der NICHT abgehakt ist (!t.IsChecked)
//                    todo.IsChecked = !allTasks.Any(t => !t.IsChecked);

//                    // Zeitstempel synchronisieren
//                    todo.LastUpdateUnixTS = task.LastUpdateUnixTS;
//                }

//                // Rückgabe-String für die UI/Logik
//                res.out_value_str = $"updated:{unixTS}:{task.AuthUsers_UnixTS}";
//            }
//        }
//        private void HandleSaveTasks(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var unixTS = para["@UnixTS"];
//            var task = new TasksEntity
//            {
//                ID = GetNextId<TasksEntity>(realm, unixTS),
//                UnixTS = unixTS,
//                AuthUsers_UnixTS = para["@AuthUsers_UnixTS"],
//                DisplayName = para["@DisplayName"],
//                IsChecked = _globalState.ConvertStrPara<bool>(para.GetValueOrDefault("@IsChecked", "0")),
//                imgJpeg = para.GetValueOrDefault("@imgJpeg", ""),
//                imgJpegThumbnail = para.GetValueOrDefault("@imgJpegThumbnail", ""),
//                Todo_UnixTS = para.GetValueOrDefault("@Todo_UnixTS", ""),
//                LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"])
//            };

//            realm.Add(task, update: true);

//            var todo = realm.All<TodoEntity>().FirstOrDefault(u => u.UnixTS == task.Todo_UnixTS);
//            if (todo != null)
//            {
//                var taskNames = realm.All<TasksEntity>()
//                                     .Where(t => t.Todo_UnixTS == todo.UnixTS)
//                                     .ToList() // Notwendig für string.Join außerhalb von Realm-LINQ
//                                     .Select(t => t.DisplayName);

//                todo.Tasks = string.Join(", ", taskNames);
//                todo.LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"]);
//            }
//            res.out_value_str = $"updated:{unixTS}:{task.AuthUsers_UnixTS}";
//        }
//        private void HandleSaveTodo(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var unixTS = para["@UnixTS"];
//            var todo = new TodoEntity
//            {
//                ID = GetNextId<TodoEntity>(realm, unixTS),
//                UnixTS = unixTS,
//                DisplayName = para["@DisplayName"],
//                AuthUsers_UnixTS = para["@AuthUsers_UnixTS"],
//                IsChecked = _globalState.ConvertStrPara<bool>(para.GetValueOrDefault("@IsChecked", "false")),
//                Tasks = para.GetValueOrDefault("@Tasks", ""),
//                IsNotifyActivated = _globalState.ConvertStrPara<bool>(para.GetValueOrDefault("@IsNotifyActivated", "false")),
//                RecordDateTime = _globalState.ConvertStrPara<DateTimeOffset>(para.GetValueOrDefault("@RecordDateTime", DateTimeOffset.Now.ToString())),
//                CategoryColor = para.GetValueOrDefault("@CategoryColor", ""),
//                LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"])
//            };

//            realm.Add(todo, update: true);

//            var master = realm.All<AuthUsersTodoEntity>()
//                              .FirstOrDefault(x => x.Todo_UnixTS == unixTS && (x.AuthUsers_ShareFrom_UnixTS == null || x.AuthUsers_ShareFrom_UnixTS == ""));

//            if (master != null)
//            {
//                master.DisplayName = _appState.Alias;
//                master.LastUpdateUnixTS = todo.LastUpdateUnixTS;
//            }
//            else
//            {
//                realm.Add(new AuthUsersTodoEntity
//                {
//                    ID = GetNextId<AuthUsersTodoEntity>(realm, para["@UnixTS2"]),
//                    UnixTS = para["@UnixTS2"],
//                    Todo_UnixTS = unixTS,
//                    DisplayName = _appState.Alias,
//                    AuthUsers_UnixTS = todo.AuthUsers_UnixTS,
//                    LastUpdateUnixTS = todo.LastUpdateUnixTS
//                });
//            }
//            res.out_value_str = $"saved:{unixTS}:{todo.AuthUsers_UnixTS}";
//        }
//        private void HandleSaveAuthUsersExtend(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var authTS = para["@AuthUsers_UnixTS"];

//            var existing = realm.All<AuthUsersExtendEntity>()
//                .FirstOrDefault(u =>
//                    u.DisplayName == para["@DisplayName"] &&
//                    u.AuthUsers_UnixTS != authTS);

//            if (existing != null)
//            {
//                res.out_err = "record_exists_no_update";
//                return;
//            }

//            byte[]? jpegBytes = DecodeBase64Image(
//                para.GetValueOrDefault("@imgJpegThumbnail", "")
//            );

//            var extend = new AuthUsersExtendEntity
//            {
//                ID = GetNextId<AuthUsersExtendEntity>(realm, para["@UnixTS"]),
//                UnixTS = para["@UnixTS"],
//                AuthUsers_UnixTS = authTS,
//                DisplayName = para["@DisplayName"],
//                ImgJpegThumbnail = jpegBytes,
//                LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"])
//            };

//            realm.Add(extend, update: true);
//            res.out_value_str = $"updated:{extend.UnixTS}:{authTS}";
//        }
//        private void HandleChangePassword(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var emailHash = para["@EmailHash"];
//            var oldHash = para["@PasswordHash"];
//            var user = realm.All<AuthUsersEntity>().FirstOrDefault(u => u.EmailHash == emailHash && u.PasswordHash == oldHash);

//            if (user != null)
//            {
//                user.PasswordHash = para["@PasswordHashNew"];
//                user.LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"]);
//                res.out_value_str = $"updated:{user.UnixTS}:{user.UnixTS}";
//            }
//            else
//            {
//                res.out_err = "not_updated:0:0";
//            }
//        }
//        private void HandleUpdateTermsAccepted(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var user = realm.All<AuthUsersEntity>().FirstOrDefault(u => u.UnixTS == para["@UnixTS"]);
//            if (user != null)
//            {
//                user.TermsAccepted = true;
//                user.LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"]);
//                res.out_value_str = $"updated:{user.UnixTS}:{user.UnixTS}";
//            }
//        }
//        private void HandleSaveSharingUsers(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var share = new SharingUsersEntity
//            {
//                ID = GetNextId<SharingUsersEntity>(realm, para["@UnixTS"]),
//                UnixTS = para["@UnixTS"],
//                AuthUsers_UnixTS = para["@AuthUsers_UnixTS"],
//                AuthUsers_ShareTo_UnixTS = para["@AuthUsers_ShareTo_UnixTS"],
//                SharingStatus = int.Parse(para["@SharingStatus"]),
//                LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"])
//            };

//            realm.Add(share, update: true);
//            res.out_value_str = $"saved:{share.UnixTS}:{share.AuthUsers_UnixTS}";
//        }
//        private void HandleChangeSharingStatus(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var share = realm.All<SharingUsersEntity>().FirstOrDefault(u => u.UnixTS == para["@UnixTS"]);
//            if (share != null)
//            {
//                share.SharingStatus = int.Parse(para["@SharingStatus"]);
//                share.LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"]);
//                res.out_value_str = $"updated:{share.UnixTS}:0";
//            }
//        }
//        private void HandleSaveAppParameter(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var item = new AppParameterEntity
//            {
//                ID = GetNextId<AppParameterEntity>(realm, para["@UnixTS"]),
//                UnixTS = para["@UnixTS"],
//                ParameterName = para["@ParameterName"],
//                ParameterValue = para["@ParameterValue"],
//                Details = para.GetValueOrDefault("@Details", ""),
//                Scope = para.GetValueOrDefault("@Scope", ""),
//                AuthUsers_UnixTS = para["@AuthUsers_UnixTS"],
//                LastUpdateUnixTS = long.Parse(para["@LastUpdateUnixTS"])
//            };

//            realm.Add(item, update: true);
//            res.out_value_str = $"updated:{item.UnixTS}:{item.AuthUsers_UnixTS}";
//        }
//        private void HandleSaveJsonAppParameter(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            string json = para.GetValueOrDefault("@Json", "");
//            var items = System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.ListAppParameterModel);

//            if (items != null)
//            {
//                foreach (var item in items)
//                {
//                    // Da wir in die Datenbank schreiben, konvertieren wir das Model in die Entity
//                    var entity = new AppParameterEntity
//                    {
//                        ID = item.ID == 0 ? GetNextId<AppParameterEntity>(realm, item.UnixTS) : item.ID,
//                        UnixTS = item.UnixTS,
//                        ParameterName = item.ParameterName,
//                        ParameterValue = item.ParameterValue,
//                        Details = item.Details,
//                        Scope = item.Scope,
//                        AuthUsers_UnixTS = item.AuthUsers_UnixTS,
//                        LastUpdateUnixTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
//                    };

//                    realm.Add(entity, update: true);
//                }
//                res.out_value_str = "updated:-1:json_bulk";
//            }
//        }
//        private void HandleDeleteOtp(Realms.Realm realm, Dictionary<string, string> para, ScalarModel res)
//        {
//            var authTS = para["@AuthUsers_UnixTS"];
//            var user = realm.All<AuthUsersEntity>().FirstOrDefault(u => u.UnixTS == authTS);
//            if (user != null)
//            {
//                // otp ist in der Entity nicht vorhanden (nur im Model/RAM), 
//                // daher konzentrieren wir uns auf das Entfernen des Backup-Codes aus der DB.

//                var param = realm.All<AppParameterEntity>()
//                                 .FirstOrDefault(p => p.AuthUsers_UnixTS == authTS && p.ParameterName == "OtpBackupCode");

//                if (param != null)
//                {
//                    realm.Remove(param);
//                }

//                res.out_value_str = $"updated:{para["@UnixTS"]}:{authTS}";
//            }
//        }

//        public async Task<ReaderModel<T>> Read<T>(Dictionary<string, string> _db_para) where T : class, new()
//        {
//            ReaderModel<T> RES = new();
//            try
//            {
//                var realm = await GetRealmAsync();
//                string case_ = _db_para.GetValueOrDefault("@Case_", "");

//                if (string.IsNullOrEmpty(case_)) return RES;

//                string authUsers_UnixTS = _db_para.GetValueOrDefault("@AuthUsers_UnixTS", "");
//                string todo_UnixTS = _db_para.GetValueOrDefault("@Todo_UnixTS", "");

//                switch (case_)
//                {
//                    // AuthUsersExtendEntity
//                    case "Select>>AuthUsersExtend":
//                        //var aue = realm.All<AuthUsersExtendEntity>()
//                        //               .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS)
//                        //               .ToList()
//                        //               .Select(e => MapToModel<AuthUsersExtendModel, AuthUsersExtendEntity>(e))
//                        //               .Cast<T>().ToList();
//                        //FillReaderModel(RES, aue);
//                        var list = realm.All<AuthUsersExtendEntity>()
//                            .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS)
//                            .ToList()
//                            .Select(e =>
//                            {
//                                var model = MapToModel<AuthUsersExtendModel, AuthUsersExtendEntity>(e);

//                                // WICHTIG: byte[] → Base64
//                                model.imgJpegThumbnail = ConvertImageToBase64(e.ImgJpegThumbnail);

//                                return model;
//                            })
//                            .Cast<T>()
//                            .ToList();

//                        FillReaderModel(RES, list);
//                        break;

//                    case "SelectByDisplayName>>AuthUsersExtend":
//                        //string displayName = _db_para.GetValueOrDefault("@DisplayName", "");
//                        //var list_AUE = realm.All<AuthUsersExtendEntity>()
//                        //                    .Where(x => x.DisplayName == displayName)
//                        //                    .ToList()
//                        //                    .Select(e => MapToModel<AuthUsersExtendModel, AuthUsersExtendEntity>(e))
//                        //                    .Cast<T>().ToList();
//                        //FillReaderModel(RES, list_AUE);
//                        string displayName = _db_para.GetValueOrDefault("@DisplayName", "");
//                        var list_AUE = realm.All<AuthUsersExtendEntity>()
//                            .Where(x => x.DisplayName == displayName)
//                            .ToList()
//                            .Select(e =>
//                            {
//                                var model = MapToModel<AuthUsersExtendModel, AuthUsersExtendEntity>(e);

//                                // WICHTIG: byte[] → Base64
//                                model.imgJpegThumbnail = ConvertImageToBase64(e.ImgJpegThumbnail);

//                                return model;
//                            })
//                            .Cast<T>()
//                            .ToList();

//                        FillReaderModel(RES, list_AUE);
//                        break;

//                    case "SelectAuthUsersData>>AuthUsersExtend":
//                        //var extendEntity = realm.All<AuthUsersExtendEntity>()
//                        //                        .FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
//                        //if (extendEntity != null)
//                        //{
//                        //    var extendModel = MapToModel<AuthUsersExtendModel, AuthUsersExtendEntity>(extendEntity);
//                        //    var baseUserEntity = realm.All<AuthUsersEntity>()
//                        //                              .FirstOrDefault(x => x.UnixTS == extendEntity.AuthUsers_UnixTS);
//                        //    if (baseUserEntity != null)
//                        //    {
//                        //        extendModel.Int__AuthUsers = MapToModel<AuthUsersModel, AuthUsersEntity>(baseUserEntity);
//                        //    }
//                        //    RES.out_data = extendModel as T;
//                        //    RES.out_list = new List<T> { extendModel as T };
//                        //}
//                        // 1️⃣ Extend laden (TOP 1)
//                        var extendEntity = realm.All<AuthUsersExtendEntity>()
//                            .FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);

//                        // 2️⃣ Kein Treffer → leerer, gültiger Contract
//                        if (extendEntity == null)
//                        {
//                            RES.out_list = new List<T?>();
//                            RES.out_data = null;
//                            break;
//                        }

//                        // 3️⃣ Extend → Model
//                        var extendModel = MapToModel<AuthUsersExtendModel, AuthUsersExtendEntity>(extendEntity);

//                        // byte[] → Base64
//                        extendModel.imgJpegThumbnail = ConvertImageToBase64(extendEntity.ImgJpegThumbnail);

//                        // 4️⃣ Base AuthUser laden (JOIN-Ersatz)
//                        var baseUserEntity = realm.All<AuthUsersEntity>()
//                            .FirstOrDefault(x => x.UnixTS == extendEntity.AuthUsers_UnixTS);

//                        if (baseUserEntity != null)
//                        {
//                            extendModel.Int__AuthUsers =
//                                MapToModel<AuthUsersModel, AuthUsersEntity>(baseUserEntity);
//                        }

//                        // 5️⃣ MSSQL-konformes Ergebnis
//                        RES.out_data = (T)(object)extendModel;
//                        RES.out_list = new List<T?> { (T)(object)extendModel };
//                        break;

//                    // SharingUsers
//                    case "Select>>SharingUsers":
//                        var list_sharing = realm.All<SharingUsersEntity>()
//                                                .ToList()
//                                                .Select(e => MapToModel<SharingUsersModel, SharingUsersEntity>(e))
//                                                .Cast<T>().ToList();
//                        FillReaderModel(RES, list_sharing);
//                        break;
//                    case "SelectRequest>>SharingUsers":
//                        var list_req = realm.All<SharingUsersEntity>()
//                                            .Where(x => x.SharingStatus == 0)
//                                            .ToList()
//                                            .Select(e => MapToModel<SharingUsersModel, SharingUsersEntity>(e))
//                                            .Cast<T>().ToList();
//                        FillReaderModel(RES, list_req);
//                        break;

//                    // Todo
//                    case "Select>>Todo":
//                        var linkedUnixTS = realm.All<AuthUsersTodoEntity>()
//                                                .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS)
//                                                .Select(x => x.Todo_UnixTS)
//                                                .ToList();

//                        var todoList = realm.All<TodoEntity>()
//                                            .Where(x => linkedUnixTS.Contains(x.UnixTS))
//                                            .ToList()
//                                            .Select(e => MapToModel<TodoModel, TodoEntity>(e))
//                                            .Cast<T>().ToList();
//                        FillReaderModel(RES, todoList);
//                        break;
//                    case "SelectByFilter>>Todo":
//                        // -------------------------------
//                        // 1. Parameter lesen
//                        // -------------------------------
//                        string searchFields = _db_para.GetValueOrDefault("@SearchFields", "");

//                        var searchParams = System.Text.Json.JsonSerializer.Deserialize<SearchForCategoryColorModel>(
//                            searchFields,
//                            JsonContext.Default.SearchForCategoryColorModel
//                        );

//                        string searchStr = searchParams?.SearchFor?.Trim() ?? string.Empty;
//                        string colorStr = searchParams?.CategoryColor?.Trim() ?? string.Empty;

//                        bool isChecked = _db_para.GetValueOrDefault("@IsChecked", "0") == "1";
//                        string authUserUnixTS = _db_para.GetValueOrDefault("@AuthUsers_UnixTS", "");
//                        int top = int.Parse(_db_para.GetValueOrDefault("@TOP", "100"));

//                        bool hasSearch = !string.IsNullOrWhiteSpace(searchStr);
//                        bool hasColor = !string.IsNullOrWhiteSpace(colorStr);

//                        // -------------------------------
//                        // 2. Basisabfrage (IMMER)
//                        // -------------------------------
//                        IQueryable<TodoEntity> baseQuery = realm.All<TodoEntity>()
//                            .Where(t =>
//                                t.AuthUsers_UnixTS == authUserUnixTS &&
//                                t.IsChecked == isChecked);

//                        // -------------------------------
//                        // 3. Count ALL (wie SQL @tmpCountAll)
//                        // -------------------------------
//                        int countAll = baseQuery.Count();
//                        //_db_para["@ID"] = countAll.ToString();

//                        // -------------------------------
//                        // 4. FALLUNTERSCHEIDUNG
//                        // -------------------------------
//                        IQueryable<TodoEntity> filteredQuery;

//                        // FALL 1: KEINE Filter → ALLES anzeigen
//                        if (!hasSearch && !hasColor)
//                        {
//                            filteredQuery = baseQuery;
//                        }
//                        // FALL 2: MINDESTENS EIN FILTER → einschränken
//                        else
//                        {
//                            filteredQuery = baseQuery;

//                            if (hasSearch)
//                            {
//                                filteredQuery = filteredQuery.Where(t =>
//                                    (t.DisplayName != null && t.DisplayName.Contains(searchStr)) ||
//                                    (t.Tasks != null && t.Tasks.Contains(searchStr)));
//                            }

//                            if (hasColor)
//                            {
//                                filteredQuery = filteredQuery.Where(t =>
//                                    t.CategoryColor != null && t.CategoryColor.Contains(colorStr));
//                            }
//                        }

//                        // -------------------------------
//                        // 5. Count FILTER (wie SQL @tmpCountFilter)
//                        // -------------------------------
//                        int countFilter = filteredQuery.Count();
//                        //_db_para["@sorter"] = countFilter.ToString();

//                        // -------------------------------
//                        // 6. Sortieren + TOP
//                        // -------------------------------
//                        var finalEntities = filteredQuery
//                            .OrderByDescending(t => t.UnixTS)
//                            .AsEnumerable()   // Wechsel von Realm → In-Memory
//                            .Take(top)
//                            .ToList();

//                        // -------------------------------
//                        // 7. Mapping
//                        // -------------------------------
//                        var finalTodos = finalEntities.Select(e =>
//                        {
//                            var model = MapToModel<TodoModel, TodoEntity>(e);
//                            model.Int__IsEmpty = string.IsNullOrEmpty(e.Tasks);
//                            model.Int__CountAll = countAll;
//                            model.Int__CountFilter = countFilter;
//                            return model;
//                        }).Cast<T>().ToList();

//                        FillReaderModel(RES, finalTodos);
//                        break;

//                    // Tasks
//                    case "SelectByTodo_UnixTS>>Tasks":
//                        var tasks = realm.All<TasksEntity>()
//                                         .Where(x => x.Todo_UnixTS == todo_UnixTS)
//                                         .OrderByDescending(x => x.ID)
//                                         .ToList()
//                                         .Select(e => MapToModel<TasksModel, TasksEntity>(e))
//                                         .Cast<T>().ToList();
//                        FillReaderModel(RES, tasks);
//                        break;

//                    // AppParameter
//                    case "Select>>AppParameter":
//                        string scope = _db_para.GetValueOrDefault("@Scope", "");
//                        var appParams = realm.All<AppParameterEntity>()
//                                             .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS && x.Scope == scope)
//                                             .ToList()
//                                             .Select(e => MapToModel<AppParameterModel, AppParameterEntity>(e))
//                                             .Cast<T>().ToList();
//                        FillReaderModel(RES, appParams);
//                        break;
//                    case "SelectStoreUrl>>AppParameter":
//                        var storeUrls = realm.All<AppParameterEntity>()
//                                             .Where(x => x.AuthUsers_UnixTS == "" && x.Scope == "app" && x.ParameterName.StartsWith("StoreUrl_"))
//                                             .ToList()
//                                             .Select(e => MapToModel<AppParameterModel, AppParameterEntity>(e))
//                                             .Cast<T>().ToList();
//                        FillReaderModel(RES, storeUrls);
//                        break;
//                }
//            }
//            catch (Exception ex)
//            {
//                RES.out_err = $"Realm Read Error: {ex.Message}";
//            }
//            return RES;
//        }
//        private TModel MapToModel<TModel, TEntity>(TEntity entity)
//            where TModel : class, new()
//            where TEntity : Realms.IRealmObject
//        {
//            if (entity == null) return null;

//            // Wir nutzen Reflexion für ein automatisches Mapping der Properties mit gleichem Namen
//            var model = new TModel();
//            var modelProps = typeof(TModel).GetProperties();
//            var entityProps = typeof(TEntity).GetProperties();

//            foreach (var mProp in modelProps)
//            {
//                var eProp = entityProps.FirstOrDefault(p => p.Name == mProp.Name && p.PropertyType == mProp.PropertyType);
//                if (eProp != null && mProp.CanWrite)
//                {
//                    mProp.SetValue(model, eProp.GetValue(entity));
//                }
//            }
//            return model;
//        }
//        // Hilfsmethode zum Befüllen des Result-Models
//        private void FillReaderModel<T>(ReaderModel<T> res, List<T> sourceList) where T : class, new()
//        {
//            if (sourceList != null && sourceList.Count > 0)
//            {
//                res.out_list = sourceList;
//                res.out_data = sourceList.FirstOrDefault();
//            }
//        }

//        public async Task<ScalarModel> Scalar(Dictionary<string, string> _db_para)
//        {
//            ScalarModel RES = new();
//            try
//            {
//                var realm = await GetRealmAsync();
//                string case_ = _db_para.GetValueOrDefault("@Case_", "");

//                if (string.IsNullOrEmpty(case_)) return RES;

//                string unixts = _db_para.GetValueOrDefault("@UnixTS", "");
//                string todo_UnixTS = _db_para.GetValueOrDefault("@Todo_UnixTS", "");
//                string authUsers_UnixTS = _db_para.GetValueOrDefault("@AuthUsers_UnixTS", "");
//                string parametername = _db_para.GetValueOrDefault("@ParameterName", "");
//                string scope = _db_para.GetValueOrDefault("@Scope", "");

//                switch (case_)
//                {
//                    // Todo
//                    case "SharingInfoByUnixTS>>Todo":
//                        var todo = realm.All<TodoEntity>().FirstOrDefault(x => x.UnixTS == unixts);
//                        if (todo != null)
//                        {
//                            string taskPart = string.IsNullOrEmpty(todo.Tasks) ? "" : ": " + todo.Tasks;
//                            RES.out_value_str = todo.DisplayName + taskPart;
//                        }
//                        break;

//                    // Tasks
//                    case "SelectImgJpeg>>Tasks":
//                        var task = realm.All<TasksEntity>().FirstOrDefault(x => x.UnixTS == unixts);
//                        RES.out_value_str = task?.imgJpeg ?? "";
//                        break;
//                    case "ExistsTodoTasks>>Tasks":
//                        RES.out_value_bool = realm.All<TasksEntity>().Any(x => x.Todo_UnixTS == todo_UnixTS);
//                        break;

//                    // AppParameter
//                    case "SelectAppSettings>>AppParameter":
//                        var appPara = realm.All<AppParameterEntity>()
//                                           .FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS &&
//                                                                x.ParameterName == parametername &&
//                                                                x.Scope == scope);
//                        RES.out_value_str = appPara?.ParameterValue ?? "";
//                        break;
//                    case "ExistsStoreUrl>>AppParameter":
//                        RES.out_value_bool = realm.All<AppParameterEntity>()
//                            .Where(x =>
//                                x.Scope == "app" &&
//                                x.ParameterName.StartsWith("StoreUrl_"))
//                            .AsEnumerable() // Wechsel von Realm → LINQ-to-Objects
//                            .Any(x => x.AuthUsers_UnixTS.Length < 35);
//                        break;

//                    // AuthUsers
//                    case "SelectTermsAccepted>>AuthUsers":
//                        var user = realm.All<AuthUsersEntity>().FirstOrDefault(x => x.UnixTS == authUsers_UnixTS);
//                        if (user != null)
//                        {
//                            int terms = user.TermsAccepted ? 1 : 0;
//                            RES.out_value_int = terms;
//                            RES.out_value_str = terms.ToString();
//                            RES.out_value_bool = user.TermsAccepted;
//                        }
//                        break;
//                    case "SelectAuthUsersEmail":
//                        string emailHash = _db_para.GetValueOrDefault("@EmailHash", "");
//                        string passHash = _db_para.GetValueOrDefault("@PasswordHash", "");
//                        var authUser = realm.All<AuthUsersEntity>()
//                                            .FirstOrDefault(x => x.EmailHash == emailHash && x.PasswordHash == passHash);
//                        RES.out_value_str = authUser != null ? authUser.EmailHash : "no_user";
//                        break;
//                    case "ExistsUnixTS>>AuthUsers":
//                        RES.out_value_bool = realm.All<AuthUsersEntity>().Any(x => x.UnixTS == unixts);
//                        break;
//                }
//            }
//            catch (Exception ex)
//            {
//                RES.out_err = $"Realm Scalar Error: {ex.Message}";
//            }

//            return RES;
//        }

//        public async Task<ScalarModel> ExecQuery(Dictionary<string, string> _db_para)
//        {
//            ScalarModel RES = new();
//            try
//            {
//                var realm = await GetRealmAsync();

//                string case_ = _db_para.GetValueOrDefault("@Case_", "");
//                if (string.IsNullOrEmpty(case_)) return RES;

//                // Parameter extrahieren
//                string unixTS = _db_para.GetValueOrDefault("@UnixTS", "");
//                string authUsers_UnixTS = _db_para.GetValueOrDefault("@AuthUsers_UnixTS", "");
//                string todo_UnixTS = _db_para.GetValueOrDefault("@Todo_UnixTS", "");
//                long lastUpdateUnixTS = long.TryParse(_db_para.GetValueOrDefault("@LastUpdateUnixTS", "0"), out var l) ? l : 0;

//                // In Realm führen wir Löschungen in einem Write-Block aus
//                await realm.WriteAsync(() =>
//                {
//                    switch (case_)
//                    {
//                        // AppParameter
//                        case "DeleteAuthUsers_UnixTS>>AppParameter":
//                            var paramsToDelete = realm.All<AppParameterEntity>().Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
//                            realm.RemoveRange(paramsToDelete);
//                            RES.out_value_str = $"deleted:0:{authUsers_UnixTS}";
//                            break;

//                        // AuthUsersExtend
//                        case "Delete>>AuthUsersExtend":
//                            // 1. Extend-Eintrag löschen
//                            var extendEntry = realm.All<AuthUsersExtendEntity>().FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
//                            if (extendEntry != null) realm.Remove(extendEntry);

//                            // 2. Sharing Verknüpfungen (AuthUsersTodo) löschen
//                            var links = realm.All<AuthUsersTodoEntity>().Where(x =>
//                                (x.AuthUsers_UnixTS == authUsers_UnixTS && (x.AuthUsers_ShareFrom_UnixTS != null && x.AuthUsers_ShareFrom_UnixTS != "")) ||
//                                (x.AuthUsers_ShareFrom_UnixTS == authUsers_UnixTS && (x.AuthUsers_UnixTS != null && x.AuthUsers_UnixTS != "")));
//                            realm.RemoveRange(links);

//                            // 3. User aus SharingUsers löschen
//                            var sUsers = realm.All<SharingUsersEntity>().Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
//                            realm.RemoveRange(sUsers);

//                            RES.out_value_str = $"deleted:0:{authUsers_UnixTS}";
//                            break;

//                        // SharingUsers
//                        case "Delete>>SharingUsers":
//                            var sUser = realm.All<SharingUsersEntity>().FirstOrDefault(u => u.UnixTS == unixTS);
//                            if (sUser != null)
//                            {
//                                realm.Remove(sUser);
//                                // Verknüpfungen analog zu oben entfernen
//                                var sLinks = realm.All<AuthUsersTodoEntity>().Where(x =>
//                                    (x.AuthUsers_UnixTS == authUsers_UnixTS && (x.AuthUsers_ShareFrom_UnixTS != null && x.AuthUsers_ShareFrom_UnixTS != "")) ||
//                                    (x.AuthUsers_ShareFrom_UnixTS == authUsers_UnixTS && (x.AuthUsers_UnixTS != null && x.AuthUsers_UnixTS != "")));
//                                realm.RemoveRange(sLinks);
//                                RES.out_value_str = $"deleted:{unixTS}:{authUsers_UnixTS}";
//                            }
//                            break;

//                        // Todo
//                        case "Delete>>Todo":
//                            // Prüfen ob Tasks hängen (SELECT EXISTS Ersatz)
//                            bool hasTasks = realm.All<TasksEntity>().Any(x => x.Todo_UnixTS == unixTS);
//                            if (hasTasks)
//                            {
//                                RES.out_err = "record_exists_no_delete";
//                            }
//                            else
//                            {
//                                // Verknüpfung löschen
//                                var todoLink = realm.All<AuthUsersTodoEntity>().FirstOrDefault(x =>
//                                    x.Todo_UnixTS == unixTS && x.AuthUsers_UnixTS == _appState.UnixTS && (x.AuthUsers_ShareFrom_UnixTS == null || x.AuthUsers_ShareFrom_UnixTS == ""));
//                                if (todoLink != null) realm.Remove(todoLink);

//                                // Todo selbst löschen
//                                var todo = realm.All<TodoEntity>().FirstOrDefault(u => u.UnixTS == unixTS);
//                                if (todo != null) realm.Remove(todo);

//                                RES.out_value_str = $"deleted:{unixTS}:{_appState.UnixTS}";
//                            }
//                            break;

//                        // Tasks
//                        case "Delete>>Tasks":
//                            var task = realm.All<TasksEntity>().FirstOrDefault(u => u.UnixTS == unixTS);
//                            if (task != null)
//                            {
//                                string t_todoUnixTS = task.Todo_UnixTS;
//                                realm.Remove(task);

//                                // Todo-Eintrag aktualisieren
//                                var parentTodo = realm.All<TodoEntity>().FirstOrDefault(u => u.UnixTS == t_todoUnixTS);
//                                if (parentTodo != null)
//                                {
//                                    var remainingTasks = realm.All<TasksEntity>()
//                                                              .Where(x => x.Todo_UnixTS == t_todoUnixTS)
//                                                              .ToList() // ToList() um string.Join außerhalb von LINQ-to-Realm auszuführen
//                                                              .Select(x => x.DisplayName);

//                                    parentTodo.Tasks = string.Join(", ", remainingTasks);
//                                    if (parentTodo.Tasks.Length > 1024) parentTodo.Tasks = parentTodo.Tasks.Substring(0, 1024);
//                                    parentTodo.LastUpdateUnixTS = lastUpdateUnixTS;
//                                }
//                                RES.out_value_str = $"deleted:{unixTS}:{authUsers_UnixTS}";
//                            }
//                            break;
//                        case "DeleteTodoTasks>>Tasks":
//                            var tasksToDelete = realm.All<TasksEntity>().Where(x => x.Todo_UnixTS == todo_UnixTS);
//                            realm.RemoveRange(tasksToDelete);

//                            var targetTodo = realm.All<TodoEntity>().FirstOrDefault(u => u.UnixTS == todo_UnixTS);
//                            if (targetTodo != null)
//                            {
//                                targetTodo.Tasks = "";
//                                targetTodo.LastUpdateUnixTS = lastUpdateUnixTS;
//                            }
//                            RES.out_value_str = $"deleted:{todo_UnixTS}:{todo_UnixTS}";
//                            break;

//                        // AuthUsers Cases werden laut Kommentar nur in Cloud oder über separate Methoden behandelt
//                        case "DeleteCloudData":
//                            break;
//                        case "DeleteLocalData":
//                            break;
//                        case "DeleteLocalAccount":
//                            break;
//                        case "DeleteAccount":
//                            break;
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                RES.out_err = "Error: " + ex.Message + " , Classname : RealmService";
//            }

//            return RES;
//        }
//        #endregion


//        #region DELETE_DATA_DB
//        public async Task<string> DeleteData()
//        {
//            string err = "";

//            try
//            {
//                var realm = await GetRealmAsync();

//                // Wir führen alle Löschvorgänge in einer einzigen Transaktion durch
//                await realm.WriteAsync(() =>
//                {
//                    // Wir löschen alle Daten aus den Entity-Tabellen
//                    DeleteUserSpecificData<TasksEntity>(realm);
//                    DeleteUserSpecificData<TodoEntity>(realm);
//                    DeleteUserSpecificData<SharingUsersEntity>(realm);
//                    DeleteUserSpecificData<AppParameterEntity>(realm);
//                    DeleteUserSpecificData<AuthUsersTodoEntity>(realm);

//                    // Hinweis: AuthUsersEntity und AuthUsersExtendEntity werden hier 
//                    // bewusst nicht gelöscht, um den Account-Kern zu erhalten.
//                });
//            }
//            catch (Exception ex)
//            {
//                err = ex.Message;
//            }

//            return err;
//        }

//        /// <summary>
//        /// Hilfsmethode, um Redundanz zu vermeiden (Generisches Löschen nach UnixTS)
//        /// </summary>
//        private void DeleteUserSpecificData<T>(Realms.Realm realm) where T : Realms.IRealmObject
//        {
//            var items = realm.All<T>();

//            if (!string.IsNullOrEmpty(_appState.UnixTS))
//            {
//                // Sicherer Weg mit Platzhaltern: $0 wird durch unixTs ersetzt.
//                // Realm kümmert sich um das korrekte Quoting.
//                var toDelete = items.Filter("AuthUsers_UnixTS == $0", _appState.UnixTS);
//                realm.RemoveRange(toDelete);
//            }
//            else
//            {
//                realm.RemoveRange(items);
//            }
//        }

//        //public async Task<string> DeleteDB()
//        //{
//        //    string err = "";

//        //    try
//        //    {
//        //        // 1. Realm-Instanz schließen
//        //        if (_realm != null)
//        //        {
//        //            _realm.Dispose();
//        //            _realm = null;
//        //        }

//        //        // 2. Löschen mit der Realm-API
//        //        if (_config != null)
//        //        {
//        //            // Nutzt die existierende Konfiguration
//        //            Realms.Realm.DeleteRealm(_config);
//        //        }
//        //        else if (!string.IsNullOrEmpty(_dbPath))
//        //        {
//        //            // Da _dbPath readonly ist, lesen wir ihn einfach aus.
//        //            // Wir erstellen eine temporäre Config nur für das Löschen.
//        //            var tempConfig = new Realms.RealmConfiguration(_dbPath);
//        //            Realms.Realm.DeleteRealm(tempConfig);
//        //        }

//        //        // 3. Den Encryption-Key aus dem sicheren Speicher löschen
//        //        string storageKey = _globalState.LocalStorage.realmkey + Appl.ApplicationName;
//        //        await _platform.RemoveAsync(storageKey);

//        //        // 4. Status zurücksetzen
//        //        _isInitialized = false;
//        //        _initTask = null;

//        //        await Task.Delay(200);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        err = $"Error deleting Realm: {ex.Message}";
//        //    }

//        //    return err;
//        //}
//        public async Task<string> DeleteDB()
//        {
//            try
//            {
//                _platform.SetPreference(BlazorCore.Utility.Appl.Realmdelete, "1");

//                // Daten löschen
//                await DeleteData();

//                // Eigene Realm-Instanz freigeben
//                if (_realm != null)
//                {
//                    _realm.Dispose();
//                    _realm = null;
//                }

//                // GC erzwingen (wichtig!)
//                GC.Collect();
//                GC.WaitForPendingFinalizers();
//                GC.Collect();

//                // Realm-Dateien löschen
//                try
//                {
//                    if (_config != null)
//                    {
//                        Realms.Realm.DeleteRealm(_config);
//                    }
//                    else if (!string.IsNullOrEmpty(_dbPath))
//                    {
//                        var tempConfig = new RealmConfiguration(_dbPath);
//                        Realms.Realm.DeleteRealm(tempConfig);
//                    }
//                }
//                catch
//                {
//                    // optional: logging
//                }

//                // Encryption-Key entfernen
//                string storageKey = _globalState.LocalStorage.realmkey + Appl.ApplicationName;
//                await _platform.RemoveAsync(storageKey);

//                // Status reset
//                IsInitialized = false;
//                _initTask = null;

//                return "";
//            }
//            catch (Exception ex)
//            {
//                return $"Error deleting Realm: {ex.Message}";
//            }
//        }

//        #endregion


//        /// <summary>
//        /// Thread-safe Access to the encrypted Realm instance.
//        /// </summary>
//        private async Task<Realms.Realm> GetRealmAsync()
//        {
//            if (_initTask != null)
//            {
//                await _initTask;
//            }

//            // Wenn _realm null ist oder die Instanz geschlossen wurde (z.B. nach DeleteDB)
//            if (_realm == null || _realm.IsClosed)
//            {
//                if (_config == null)
//                {
//                    // Nutzt den Pfad, den wir beim App-Start festgelegt haben
//                    await EnsureInitializedAsync(_dbPath);
//                }

//                // GetInstanceAsync ist bei verschlüsselten Realms sicherer als GetInstance
//                _realm = await Realms.Realm.GetInstanceAsync(_config!);
//            }
//            return _realm;
//        }

//        private static byte[]? DecodeBase64Image(string? base64)
//        {
//            if (string.IsNullOrWhiteSpace(base64))
//                return null;

//            // data:image/jpeg;base64, entfernen
//            int commaIndex = base64.IndexOf(',');
//            if (commaIndex >= 0)
//                base64 = base64[(commaIndex + 1)..];

//            return Convert.FromBase64String(base64);
//        }

//        private static string ConvertImageToBase64(byte[]? data)
//        {
//            if (data == null || data.Length == 0)
//                return string.Empty;

//            return "data:image/jpeg;base64," + Convert.ToBase64String(data);
//        }

//        //public static int GetNextId<T>(Realms.Realm realm) where T : RealmObject, IAutoIncrementEntity
//        //{
//        //    lock (_lockGetNextId)
//        //    {
//        //        var last = realm.All<T>()
//        //                        .OrderByDescending(e => e.ID)
//        //                        .FirstOrDefault();

//        //        return last == null ? 1 : last.ID + 1;
//        //    }
//        //}
//        public static int GetNextId<T>(Realms.Realm realm, string unixTS) where T : RealmObject, IAutoIncrementEntity
//        {
//            lock (_lockGetNextId)
//            {
//                var existing = realm.All<T>().FirstOrDefault(u => u.UnixTS == unixTS);
//                if (existing == null)
//                {
//                    var last = realm.All<T>()
//                                    .OrderByDescending(e => e.ID)
//                                    .FirstOrDefault();

//                    return last == null ? 1 : last.ID + 1;
//                }
//                else
//                {
//                    {
//                        // Es existiert bereits ein Eintrag mit gleicher UnixTS -> ID beibehalten
//                        return existing.ID;
//                    }
//                }
//            }
//        }


//    }
//}
