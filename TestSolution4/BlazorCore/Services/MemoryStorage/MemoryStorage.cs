using BlazorCore.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState; // Falls IPlatformBase etc. dort liegen
using BlazorCore.Services.MemoryStorage; // Dein Interface-Namespace
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.SqLite;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorCore.Services.MemoryStorage
{
    /// <summary>
    /// Volatile Memory implementation of pEngine Storage.
    /// Operates strictly in RAM using Dictionaries and Lists.
    /// Data is lost upon application restart.
    /// </summary>
    public class MemoryStorageBase : IMemoryStorageBase
    {
        public bool IsInitialized { get; set; } = false;

        protected readonly IPlatformBase _platform;
        protected readonly IAppStateBase _appState;
        protected readonly IGlobalStateBase _globalState;

        protected readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// The RAM Cache: Key = TableName (e.g., "AuthUsers"), 
        /// Value = List of data records stored as objects.
        /// </summary>
        protected readonly Dictionary<string, List<object>> _ramCache = new();

        // Hilfsvariable für die Version im RAM
        private int _memoryDbVersion = 0;

        // ID's
        private static int ID_AppParameter = 0;

        /// <summary>
        /// Central location for all table names, consistent with SQLite implementation.
        /// </summary>
        protected static readonly string[] AllTableNames = new[]
        {
            "AuthUsers",
            "AppParameter",
            "AuthUsersExtend",
            "SharingUsers"
        };

        public MemoryStorageBase(IServiceProvider serviceProvider)
        {
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();

            // Initialize empty lists for all defined tables
            foreach (var table in AllTableNames)
            {
                if (!_ramCache.ContainsKey(table))
                {
                    _ramCache.Add(table, new List<object>());
                }
            }
        }

        ///// <summary>
        ///// Initializes the Capacitor SQLite connection and creates the schema.
        ///// Called during the AppInitializer phase.
        ///// </summary>
        public virtual async Task<ScalarModel> InitializeAsync(bool register, string userAccount)
        {
            // Wir nutzen den Lock, um Konsistenz zu garantieren, 
            // falls die App während des Inits bereits Daten schieben will.
            await _lock.WaitAsync();

            try
            {
                if (IsInitialized)
                    return new ScalarModel { out_value_bool = true };

                await _appState.Log("[MemoryStorage] START InitializeAsync");

                // Bei Memory gibt es keinen Unterschied zwischen 'register' oder 'normal'.
                // Wir stellen lediglich sicher, dass die Listen sauber sind.
                foreach (var table in AllTableNames)
                {
                    if (!_ramCache.ContainsKey(table))
                    {
                        _ramCache.Add(table, new List<object>());
                    }
                }

                IsInitialized = true;
                await _appState.Log("[MemoryStorage] Memory lists initialized and ready.");

                return new ScalarModel { out_value_bool = true };
            }
            catch (Exception ex)
            {
                await _appState.Log($"[MemoryStorage] Critical Init Error: {ex.Message}", data: ex);
                IsInitialized = false;
                return new ScalarModel { out_value_bool = false, out_err = ex.Message };
            }
            finally
            {
                await _appState.Log("[MemoryStorage] END InitializeAsync");
                _lock.Release();
            }
        }

        public virtual async Task<ScalarModel> InitializeAsync()
        {
            // Ruft die komplexe Methode mit Standardwerten auf
            return await InitializeAsync(false, "default_user");
        }

        protected virtual async Task<ScalarModel> InitNativeSqliteAsync(bool register, string userAccount)
        {
            return new ScalarModel { out_value_bool = true };
        }

        public virtual async Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[MemoryStorage] START GetTokenDataTPC");
            ClientStorageModel res = new();

            try
            {
                // Parameter extrahieren
                string emailHash = dbParams.GetValueOrDefault("@EmailHash", "");
                string passwordHash = dbParams.GetValueOrDefault("@PasswordHash", "");
                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                bool registration = dbParams.GetValueOrDefault("@Int__Registration", "0") == "1";
                bool createUser = !dbParams.ContainsKey("cmd_nocreateuser");
                bool updateUser = !dbParams.ContainsKey("cmd_noupdateuser");
                bool case_mssql = dbParams.GetValueOrDefault("case_mssql", "0") == "1";

                // Zugriff auf die vorinitialisierte Liste
                var userList = _ramCache["AuthUsers"].Cast<AuthUsersModel>().ToList();

                // Suche nach passendem User (Email & Passwort Match)
                var localUser = userList.FirstOrDefault(u => u.EmailHash == emailHash && u.PasswordHash == passwordHash);

                if (localUser != null)
                {
                    if (!string.IsNullOrEmpty(unixTS) && localUser.UnixTS != unixTS)
                    {
                        if (updateUser)
                        {
                            localUser.UnixTS = unixTS;
                            await _appState.Log($"[MemoryStorage] RAM-Update: UnixTS für {emailHash} synchronisiert.");
                        }
                        res.UnixTS = unixTS;
                    }
                    else
                    {
                        res.UnixTS = localUser.UnixTS;
                    }
                    res.WebApiToken = Apis.API_CONST.TOKEN_LOCAL_ONLY;
                }
                else
                {
                    // Simulation der Registrierungslogik
                    if (registration && !string.IsNullOrEmpty(unixTS))
                    {
                        // Prüfen ob Email/UnixTS Kombination existiert
                        bool emailExists = userList.Any(u => u.EmailHash == emailHash && u.UnixTS == unixTS);

                        if (emailExists)
                        {
                            if (updateUser)
                            {
                                var userToUpdate = userList.First(u => u.EmailHash == emailHash && u.UnixTS == unixTS);
                                userToUpdate.PasswordHash = passwordHash;
                                userToUpdate.LastUpdateUnixTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            }
                            res.UnixTS = unixTS;
                            res.WebApiToken = Apis.API_CONST.TOKEN_LOCAL_ONLY;
                        }
                        else if (createUser)
                        {
                            res = await CreateLocalAccount(dbParams);
                        }
                    }
                    else if (case_mssql)
                    {
                        res = await CreateLocalAccount(dbParams);
                    }
                    else
                    {
                        res.out_err = "no_user";
                    }
                }
            }
            catch (Exception ex)
            {
                res.out_err = $"MemoryStorage GetTokenDataTPC Error: {ex.Message}";
            }

            return res;
        }

        public async Task<ClientStorageModel> CreateLocalAccount(Dictionary<string, string> dbParams, bool case_mssql = false)
        {
            await _appState.Log("[MemoryStorage] START CreateLocalAccount");
            ClientStorageModel result = new();

            try
            {
                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");

                // Neues Model-Objekt erstellen
                var newUser = new AuthUsersModel
                {
                    UnixTS = unixTS,
                    EmailHash = dbParams.GetValueOrDefault("@EmailHash", ""),
                    PasswordHash = dbParams.GetValueOrDefault("@PasswordHash", ""),
                    TermsAccepted = (dbParams.GetValueOrDefault("@TermsAccepted", "") == "true" || dbParams.GetValueOrDefault("@TermsAccepted", "") == "1"),
                    active = (dbParams.GetValueOrDefault("@active", "") == "true" || dbParams.GetValueOrDefault("@active", "") == "1"),
                    IdP = dbParams.GetValueOrDefault("@IdP", ""),
                    IdPClientIdent = dbParams.GetValueOrDefault("@IdPClientIdent", ""),
                    IdPToken = dbParams.GetValueOrDefault("@IdPToken", ""),
                    LastUpdateUnixTS = long.TryParse(dbParams.GetValueOrDefault("@LastUpdateUnixTS", ""), out var l) ? l : DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                // INSERT OR REPLACE Logik im RAM
                var existing = _ramCache["AuthUsers"].Cast<AuthUsersModel>().FirstOrDefault(u => u.UnixTS == unixTS);
                if (existing != null)
                {
                    _ramCache["AuthUsers"].Remove(existing);
                }

                _ramCache["AuthUsers"].Add(newUser);

                result.UnixTS = unixTS;
                result.WebApiToken = Apis.API_CONST.TOKEN_LOCAL_ONLY;

                await _appState.Log($"[MemoryStorage] User registriert im RAM: {newUser.EmailHash}");
            }
            catch (Exception ex)
            {
                result.out_err = $"Memory CreateLocalAccount Error: {ex.Message}";
            }

            return result;
        }

        public virtual async Task<ScalarModel> Save(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[MemoryStorage] START Save");
            ScalarModel result = new();

            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrEmpty(case_)) return result;

                // Gemeinsame Parameter extrahieren
                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                long lastUpdateUnixTS = long.TryParse(dbParams.GetValueOrDefault("@LastUpdateUnixTS", ""), out var l) ? l : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                switch (case_)
                {
                    case "Save>>AuthUsersExtend":
                        // 1. Alias-Check (ExistsDisplayName)
                        var displayName = dbParams.GetValueOrDefault("@DisplayName", "");
                        var listExtend = _ramCache["AuthUsersExtend"].Cast<AuthUsersExtendModel>();

                        bool aliasExists = listExtend.Any(x => x.DisplayName == displayName && x.AuthUsers_UnixTS != authUsers_UnixTS);
                        if (aliasExists)
                        {
                            result.out_err = "displayname_already_exists";
                            return result;
                        }

                        // 2. Existenz-Check & Update/Insert
                        var existingExt = listExtend.FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);
                        if (existingExt != null)
                        {
                            // UPDATE
                            existingExt.DisplayName = displayName;
                            existingExt.imgJpegThumbnail = dbParams.GetValueOrDefault("@imgJpegThumbnail", "");
                            existingExt.LastUpdateUnixTS = lastUpdateUnixTS;
                            result.out_value_str = $"updated:{unixTS}:{authUsers_UnixTS}";
                        }
                        else
                        {
                            // INSERT
                            _ramCache["AuthUsersExtend"].Add(new AuthUsersExtendModel
                            {
                                UnixTS = unixTS,
                                AuthUsers_UnixTS = authUsers_UnixTS,
                                DisplayName = displayName,
                                imgJpegThumbnail = dbParams.GetValueOrDefault("@imgJpegThumbnail", ""),
                                LastUpdateUnixTS = lastUpdateUnixTS
                            });
                            result.out_value_str = $"saved:{unixTS}:{authUsers_UnixTS}";
                        }
                        break;

                    case "UpdateTermsAccepted>>AuthUsers":
                        var userTerms = _ramCache["AuthUsers"].Cast<AuthUsersModel>().FirstOrDefault(x => x.UnixTS == unixTS);
                        if (userTerms != null)
                        {
                            userTerms.TermsAccepted = true;
                            userTerms.LastUpdateUnixTS = lastUpdateUnixTS;
                            result.out_value_str = $"updated:{unixTS}:0";
                        }
                        break;

                    case "Save>>AppParameter":
                        var listParams = _ramCache["AppParameter"].Cast<AppParameterModel>().ToList();
                        string pName = dbParams.GetValueOrDefault("@ParameterName", "");
                        string pScope = dbParams.GetValueOrDefault("@Scope", "");

                        // Da "INSERT OR REPLACE": Erst löschen, dann neu hinzufügen
                        _ramCache["AppParameter"].RemoveAll(x => ((AppParameterModel)x).ParameterName == pName &&
                                                                ((AppParameterModel)x).Scope == pScope &&
                                                                ((AppParameterModel)x).AuthUsers_UnixTS == authUsers_UnixTS);

                        //// 1. Hole die Liste und caste sie sicher
                        //var currentParams = _ramCache["AppParameter"].Cast<AppParameterModel>().ToList();

                        //// 2. Berechne die neue ID (0 wenn leer, ansonsten Max + 1)
                        //int nextId = currentParams.Any() ? currentParams.Max(x => x.ID) + 1 : 1;

                        _ramCache["AppParameter"].Add(new AppParameterModel
                        {
                            ID = (ID_AppParameter += 1),
                            UnixTS = unixTS,
                            ParameterName = pName,
                            ParameterValue = dbParams.GetValueOrDefault("@ParameterValue", ""),
                            Details = dbParams.GetValueOrDefault("@Details", ""),
                            Scope = pScope,
                            AuthUsers_UnixTS = authUsers_UnixTS,
                            LastUpdateUnixTS = lastUpdateUnixTS
                        });
                        result.out_value_str = $"updated:{unixTS}:{authUsers_UnixTS}";
                        break;

                    case "SaveJson>>AppParameter":
                        string json = dbParams.GetValueOrDefault("@Json", "");
                        var items = System.Text.Json.JsonSerializer.Deserialize(json, BlazorCore.JsonContext.Default.ListAppParameterModel);
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                // Im RAM: Vorhandenes entfernen (Replace-Logik) und neu hinzufügen
                                _ramCache["AppParameter"].RemoveAll(x => ((AppParameterModel)x).UnixTS == item.UnixTS);
                                _ramCache["AppParameter"].Add(item);
                            }
                            result.out_value_str = "updated:-1:json_bulk";
                        }
                        break;

                    case "ChangePassword>>AuthUsers":
                        // Hier nutzen wir deine vorhandene CheckPassword Logik im RAM
                        var checkPass = await this.Scalar(new Dictionary<string, string> {
                            { "@Case_", "CheckPassword>>AuthUsers" },
                            { "@UnixTS", unixTS },
                            { "@PasswordHash", dbParams["@PasswordHash"] }
                        });

                        if (checkPass.out_value_bool)
                        {
                            var userPass = _ramCache["AuthUsers"].Cast<AuthUsersModel>().FirstOrDefault(x => x.UnixTS == unixTS);
                            if (userPass != null)
                            {
                                userPass.PasswordHash = dbParams.GetValueOrDefault("@PasswordHashNew", "");
                                userPass.LastUpdateUnixTS = lastUpdateUnixTS;
                                result.out_value_str = $"updated:{unixTS}:{unixTS}";
                            }
                        }
                        else
                        {
                            result.out_err = "not_updated:0:0";
                        }
                        break;

                        // ... weitere Cases analog umsetzen ...
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"MemoryStorage Save Error: {ex.Message}";
                await _appState.Log($"[MemoryStorage] ERROR: {ex.Message}");
            }

            await _appState.Log("[MemoryStorage] END Save");
            return result;
        }

        public virtual async Task<ScalarModel> Scalar(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[MemoryStorage] START Scalar");

            ScalarModel result = new();
            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrEmpty(case_)) return result;

                switch (case_)
                {
                    // --- AuthUsersExtend ---
                    case "SelectAlias>>AuthUsersExtend":
                        var authId = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                        var aliasExt = _ramCache["AuthUsersExtend"].Cast<AuthUsersExtendModel>()
                            .FirstOrDefault(x => x.AuthUsers_UnixTS == authId);
                        result.out_value_str = aliasExt?.DisplayName ?? "";
                        break;

                    case "ExistsDisplayName>>AuthUsersExtend":
                        var dName = dbParams.GetValueOrDefault("@DisplayName", "");
                        var excludeId = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                        result.out_value_bool = _ramCache["AuthUsersExtend"].Cast<AuthUsersExtendModel>()
                            .Any(x => x.DisplayName == dName && x.AuthUsers_UnixTS != excludeId);
                        break;

                    case "ExistsByAuthUsers_UnixTS>>AuthUsersExtend":
                        var aId = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                        result.out_value_bool = _ramCache["AuthUsersExtend"].Cast<AuthUsersExtendModel>()
                            .Any(x => x.AuthUsers_UnixTS == aId);
                        break;

                    // --- AppParameter ---
                    case "SelectAppSettings>>AppParameter":
                        var pName = dbParams.GetValueOrDefault("@ParameterName", "");
                        var pScope = dbParams.GetValueOrDefault("@Scope", "");
                        var pAuthId = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");

                        // Falls AuthUsers_UnixTS leer ist, müssten wir theoretisch über Email/Pass suchen (wie im SQL)
                        // Aber im RAM-Kontext sollte die ID meist schon vorliegen.
                        var param = _ramCache["AppParameter"].Cast<AppParameterModel>()
                            .FirstOrDefault(x => x.AuthUsers_UnixTS == pAuthId && x.ParameterName == pName && x.Scope == pScope);
                        result.out_value_str = param?.ParameterValue ?? "";
                        break;

                    case "ExistsStoreUrl>>AppParameter":
                        result.out_value_bool = _ramCache["AppParameter"].Cast<AppParameterModel>()
                            .Any(x => x.Scope == "app" && x.ParameterName.StartsWith("StoreUrl_") && x.AuthUsers_UnixTS.Length < 35);
                        break;

                    case "CheckBackupCode>>AppParameter":
                        var bCode = dbParams.GetValueOrDefault("@OtpBackupCode", "");
                        var bAuthId = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                        result.out_value_bool = _ramCache["AppParameter"].Cast<AppParameterModel>()
                            .Any(x => x.AuthUsers_UnixTS == bAuthId && x.ParameterName == "OtpBackupCode" && x.ParameterValue == bCode && x.Scope == "config");
                        break;

                    // --- AuthUsers ---
                    case "CheckPassword>>AuthUsers":
                        var uId = dbParams.GetValueOrDefault("@UnixTS", "");
                        var pHash = dbParams.GetValueOrDefault("@PasswordHash", "");
                        result.out_value_bool = _ramCache["AuthUsers"].Cast<AuthUsersModel>()
                            .Any(x => x.UnixTS == uId && x.PasswordHash == pHash && x.active == true);
                        break;

                    case "SelectTermsAccepted>>AuthUsers":
                        var tId = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                        var userT = _ramCache["AuthUsers"].Cast<AuthUsersModel>().FirstOrDefault(x => x.UnixTS == tId && x.active == true);
                        result.out_value_int = userT!.TermsAccepted ? 1 : 0;
                        result.out_value_bool = result.out_value_int == 1;
                        break;

                    // --- Allgemein ---
                    case "TableExists":
                        var tName = dbParams.GetValueOrDefault("@TableName", "");
                        result.out_value_bool = _ramCache.ContainsKey(tName);
                        result.out_value_str = result.out_value_bool ? "1" : "0";
                        break;

                    case "CheckMultipleTablesExist":
                        var tableList = dbParams.GetValueOrDefault("@TableList", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                        int count = tableList.Count(t => _ramCache.ContainsKey(t.Trim()));
                        result.out_value_int = count;
                        result.out_value_str = count.ToString();
                        break;

                    default:
                        result.out_err = $"Case '{case_}' not implemented in Memory Scalar.";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"Memory Scalar Error: {ex.Message}";
            }

            await _appState.Log("[MemoryStorage] END Scalar", data: result);
            return result;
        }

        public virtual async Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[MemoryStorage] START ExecQuery");

            ScalarModel result = new();
            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrEmpty(case_)) return result;

                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                int id = int.Parse(dbParams.GetValueOrDefault("@ID", "0"));

                // Status-Hilfsvariable für die Rückgabe wie im Original
                bool success = true;

                switch (case_)
                {
                    case "DeleteAuthUsers_UnixTS>>AppParameter":
                        // Löscht alle Parameter eines Users
                        _ramCache["AppParameter"].RemoveAll(x => ((AppParameterModel)x).AuthUsers_UnixTS == authUsers_UnixTS);
                        result.out_value_str = $"deleted:0:{authUsers_UnixTS}";
                        break;

                    case "Delete>>AuthUsersExtend":
                        // 1. AuthUsersExtend löschen
                        _ramCache["AuthUsersExtend"].RemoveAll(x => ((AuthUsersExtendModel)x).AuthUsers_UnixTS == authUsers_UnixTS);

                        // 2. Sharing-Einträge
                        _ramCache["SharingUsers"].RemoveAll(x => ((SharingUsersModel)x).AuthUsers_UnixTS == authUsers_UnixTS);

                        result.out_value_str = $"deleted:0:{authUsers_UnixTS}";
                        break;

                    case "Delete>>SharingUsers":
                        // 1. Spezifischen Sharing-Eintrag löschen
                        _ramCache["SharingUsers"].RemoveAll(x => ((SharingUsersModel)x).UnixTS == unixTS);

                        result.out_value_str = $"deleted:0:{authUsers_UnixTS}";
                        break;

                    case "Delete>>AppParameter":
                        // 1. Spezifischen Sharing-Eintrag löschen
                        _ramCache["AppParameter"].RemoveAll(x => ((AppParameterModel)x).UnixTS == unixTS);

                        result.out_value_str = $"deleted:0:{unixTS}";
                        break;

                    default:
                        result.out_err = $"Case '{case_}' not implemented in Memory ExecQuery.";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"Memory ExecQuery Error: {ex.Message}";
            }

            await _appState.Log("[MemoryStorage] END ExecQuery");
            return result;
        }

        public virtual async Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new()
        {
            await _appState.Log("[MemoryStorage] START Read");

            ReaderModel<T> result = new();
            result.out_list = new List<T?>();

            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrEmpty(case_)) return result;

                string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                string todo_UnixTS = dbParams.GetValueOrDefault("@Todo_UnixTS", "");
                string scope = dbParams.GetValueOrDefault("@Scope", "");
                string displayName = dbParams.GetValueOrDefault("@DisplayName", "");

                switch (case_)
                {
                    // --- AuthUsers ---
                    case "SelectByUnixTS>>AuthUsers":
                        if (_ramCache.TryGetValue("AuthUsers", out var listUsers))
                        {
                            result.out_list = listUsers.Cast<AuthUsersModel>()
                                .Where(x => x.UnixTS == unixTS && x.active == true)
                                .Select(x => (T?)(object)x) // Double Cast zu T? löst die Warnung
                                .ToList();
                        }
                        else
                        {
                            result.out_list = new List<T?>();
                        }
                        break;

                    // --- AuthUsersExtend ---
                    case "Select>>AuthUsersExtend":
                        if (_ramCache.TryGetValue("AuthUsersExtend", out var listExtend))
                        {
                            result.out_list = listExtend.Cast<AuthUsersExtendModel>()
                                .Where(x => x.AuthUsers_UnixTS == authUsers_UnixTS)
                                .Take(1)
                                .Select(x => (T?)(object)x) // Der sichere Weg über object zu T?
                                .ToList();
                        }
                        else
                        {
                            result.out_list = new List<T?>();
                        }
                        break;

                    case "SelectByDisplayName>>AuthUsersExtend":
                        // Simuliert den LEFT JOIN zu AuthUsers für die interne ID
                        var extByDisplayName = _ramCache["AuthUsersExtend"].Cast<AuthUsersExtendModel>()
                            .FirstOrDefault(ae => ae.DisplayName == displayName);

                        if (extByDisplayName != null)
                        {
                            // Wir können hier zusätzliche Logik einbauen, falls T ein spezielles View-Modell ist
                            result.out_list.Add((T)(object)extByDisplayName);
                        }
                        break;

                    case "SelectAuthUsersData>>AuthUsersExtend":
                        if (!_ramCache.ContainsKey("AuthUsers") || !_ramCache.ContainsKey("AuthUsersExtend"))
                        {
                            result.out_list = new List<T?>();
                            break;
                        }

                        var joinedData = (from au in _ramCache["AuthUsers"].Cast<AuthUsersModel>()
                                          join ae in _ramCache["AuthUsersExtend"].Cast<AuthUsersExtendModel>()
                                          on au.UnixTS equals ae.AuthUsers_UnixTS
                                          where ae.AuthUsers_UnixTS == authUsers_UnixTS
                                          select new AuthUsersAuthUsersExtendModel
                                          {
                                              active = au.active,
                                              TermsAccepted = au.TermsAccepted,
                                              IdP = au.IdP ?? string.Empty,
                                              EmailHash = "empty for security reasons",
                                              PasswordHash = "empty for security reasons",
                                              DisplayName = ae.DisplayName ?? string.Empty,
                                              imgJpegThumbnail = ae.imgJpegThumbnail ?? string.Empty,
                                              AuthUsers_UnixTS = ae.AuthUsers_UnixTS ?? string.Empty,
                                              UnixTS = ae.UnixTS,
                                              ID = ae.ID,
                                              LastUpdateUnixTS = ae.LastUpdateUnixTS
                                          })
                                          .Select(x => (T?)(object)x) // Cast auf T? über object
                                          .ToList();

                        result.out_list = joinedData;
                        break;

                    // --- SharingUsers ---
                    case "Select>>SharingUsers":
                        if (_ramCache.TryGetValue("SharingUsers", out var listSharing))
                        {
                            result.out_list = listSharing.Cast<SharingUsersModel>()
                                .Where(s => s.AuthUsers_UnixTS == authUsers_UnixTS || s.AuthUsers_ShareTo_UnixTS == authUsers_UnixTS)
                                .OrderByDescending(s => s.ID)
                                .Select(s => {
                                    // Wer ist das Gegenüber?
                                    string targetUid = s.AuthUsers_UnixTS != authUsers_UnixTS
                                                       ? (s.AuthUsers_UnixTS ?? "")
                                                       : (s.AuthUsers_ShareTo_UnixTS ?? "");

                                    // Sicherer Zugriff auf AuthUsersExtend für den Alias
                                    AuthUsersExtendModel? aliasEntry = null;
                                    if (_ramCache.TryGetValue("AuthUsersExtend", out var listExt))
                                    {
                                        aliasEntry = listExt.Cast<AuthUsersExtendModel>()
                                                     .FirstOrDefault(ae => ae.AuthUsers_UnixTS == targetUid);
                                    }

                                    // Mapping (Stabile Zuweisung)
                                    s.Int__AuthUsers_UnixTS = targetUid;
                                    s.Int__Alias = aliasEntry?.DisplayName ?? "";
                                    s.Int__AliasImgJpegThumbnail = aliasEntry?.imgJpegThumbnail ?? "";

                                    // Umwandlung in den Zieltyp T? über object
                                    return (T?)(object)s;
                                })
                                .ToList();
                        }
                        else
                        {
                            result.out_list = new List<T?>();
                        }
                        break;

                    case "SelectRequest>>SharingUsers":
                        if (_ramCache.TryGetValue("SharingUsers", out var listReq))
                        {
                            result.out_list = listReq.Cast<SharingUsersModel>()
                                .Where(x => x.SharingStatus == 0)
                                .OrderByDescending(x => x.ID)
                                .Select(x => (T?)(object)x) // Double Cast zu T? löst die Warnung
                                .ToList();
                        }
                        else
                        {
                            result.out_list = new List<T?>();
                        }
                        break;

                    // --- AppParameter ---
                    case "Select>>AppParameter":
                        if (_ramCache.TryGetValue("AppParameter", out var listParams))
                        {
                            result.out_list = listParams.Cast<AppParameterModel>()
                                .Where(x => (x.AuthUsers_UnixTS == authUsers_UnixTS || x.AuthUsers_UnixTS == "0") && x.Scope == scope)
                                .OrderBy(x => x.ID)
                                .Select(x => (T?)(object)x) // Double Cast zu T? gegen CS8619
                                .ToList();
                        }
                        else
                        {
                            result.out_list = new List<T?>();
                        }
                        break;

                    case "SelectStoreUrl>>AppParameter":
                        if (_ramCache.TryGetValue("AppParameter", out var listStoreParams))
                        {
                            result.out_list = listStoreParams.Cast<AppParameterModel>()
                                .Where(x => (string.IsNullOrEmpty(x.AuthUsers_UnixTS) || (x.AuthUsers_UnixTS?.Length ?? 0) < 35)
                                            && x.Scope == "app" && (x.ParameterName?.StartsWith("StoreUrl_") ?? false))
                                .OrderBy(x => x.ID)
                                .Select(x => (T?)(object)x) // Double Cast zu T? löst die Warnung
                                .ToList();
                        }
                        else
                        {
                            result.out_list = new List<T?>();
                        }
                        break;

                    default:
                        result.out_err = $"Case '{case_}' not implemented in Memory Read.";
                        break;
                }

                // out_data immer auf das erste Element setzen (Single-Result Komfort)
                if (result.out_list.Count > 0)
                {
                    result.out_data = result.out_list[0];
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"Memory Read Error: {ex.Message}";
            }

            await _appState.Log("[MemoryStorage] END Read", data: result.out_list?.Count);
            return result;
        }

        /// <summary>
        /// LEVEL 1: Daten im RAM löschen.
        /// Setzt alle Listen im _ramCache zurück.
        /// </summary>
        public virtual async Task<ScalarModel> ClearAllData()
        {
            await _appState.Log("[MemoryStorage] START ClearAllData");
            ScalarModel result = new();

            try
            {
                // Wir gehen alle Keys (Tabellennamen) durch und leeren die Listen
                foreach (var key in _ramCache.Keys.ToList())
                {
                    _ramCache[key] = new List<object>();
                }

                // Status zurücksetzen
                IsInitialized = false;

                result.out_value_bool = true;
                result.out_value_str = "deleted:0:0";

                await _appState.Log("[MemoryStorage] All data cleared from RAM.");
            }
            catch (Exception ex)
            {
                result.out_err = $"Memory ClearAllData Error: {ex.Message}";
                await _appState.Error($"[MemoryStorage] {result.out_err}");
                result.out_value_bool = false;
            }

            return result;
        }

        /// <summary>
        /// LEVEL 2: Drop tables - Im Memory-Ansatz löschen wir hier die gesamte Dictionary-Struktur.
        /// Muss wegen des Interfaces vorhanden sein.
        /// </summary>
        public virtual async Task<ScalarModel> DropAllTables()
        {
            await _appState.Log("[MemoryStorage] DropAllTables gerufen (Interface-Compliance)");

            ScalarModel result = new();
            try
            {
                // Im RAM bedeutet "Drop All Tables", dass wir nicht nur die Listen leeren, 
                // sondern das gesamte Dictionary platt machen.
                _ramCache.Clear();

                IsInitialized = false;

                result.out_value_bool = true;
                result.out_value_str = "deleted:0:0";

                await _appState.Log("[MemoryStorage] All RAM 'tables' (Dictionary Keys) dropped.");
            }
            catch (Exception ex)
            {
                result.out_err = $"Memory DropAllTables Error: {ex.Message}";
                result.out_value_bool = false;
            }

            return result;
        }

        /// <summary>
        /// LEVEL 3: Full Purge.
        /// Da es keine physikalische Datei gibt, setzen wir den RAM-Cache komplett zurück.
        /// Erfüllt die Schnittstelle für DSGVO-Löschvorgänge.
        /// </summary>
        public virtual async Task<ScalarModel> DeleteDB()
        {
            ScalarModel result = new();
            try
            {
                await _appState.Log("[MemoryStorage] DeleteDB -> Performing Full RAM Purge via .Clear()");

                // Da _ramCache readonly ist, können wir keine neue Instanz zuweisen.
                // .Clear() entfernt jedoch alle Keys und alle Listen-Referenzen aus dem Dictionary.
                _ramCache.Clear();

                // Wichtig: Den Initialisierungs-Status zurücksetzen, 
                // damit die App weiß, dass die Tabellen-Struktur neu aufgebaut werden muss.
                IsInitialized = false;

                result.out_value_bool = true;
                result.out_value_str = "deleted:0:0";

                await _appState.Log("[MemoryStorage] Memory Database successfully purged.");
            }
            catch (Exception ex)
            {
                result.out_err = $"Critical-Memory-Error: {ex.Message}";
                result.out_value_bool = false;
                await _appState.Error($"[MemoryStorage] DeleteDB Error: {ex.Message}");
            }

            return result;
        }



    }
}

