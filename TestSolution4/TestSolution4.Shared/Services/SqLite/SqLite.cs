//using System;
//using System.Collections.Generic;
using BlazorCore.Services.Apis;
//using BlazorCore.DbApp.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.SqLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace TestSolution4.Shared.Services.SqLite
{
    /// <summary>
    /// Capacitor/WASM implementation of SQLite.
    /// Following the naming convention to match other platform services.
    /// </summary>
    public class SqLiteBase : ISqLiteBase
    {
        public bool IsInitialized { get; set; } = false;

        protected readonly IPlatformBase _platform;
        protected readonly IAppStateBase _appState;
        protected readonly IGlobalStateBase _globalState;
        protected readonly IJSRuntime _jsRuntime;
        protected string DbName = string.Empty; //"pMunusLocal.db";
        protected const string JsPrefix = "pE_Capacitor.sqlite";

        protected readonly SemaphoreSlim _initLock = new(1, 1);

        // Zentraler Ort für alle Tabellennamen
        protected static readonly string[] AllTables = new[]
        {
            "AuthUsers",
            "AppParameter",
            "AuthUsersExtend",
            "SharingUsers"
        };

        public class SqlRequest
        {
            public string? Sql { get; set; }

            // Das ? hinter dem Typ erlaubt 'null' Zuweisungen
            public IReadOnlyDictionary<string, object?>? Params { get; set; }
                = new Dictionary<string, object?>();
        }

        protected SqLiteBase(IServiceProvider serviceProvider, IJSRuntime jsRuntime)
        {
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
            _jsRuntime = jsRuntime;

            DbName = _globalState!.ConfigGeneral.ApplicationName;
        }
        
        ///// <summary>
        ///// Initializes the Capacitor SQLite connection and creates the schema.
        ///// Called during the AppInitializer phase.
        ///// </summary>
        public virtual async Task<ScalarModel> InitializeAsync(bool register, string userAccount)
        {
            return new ScalarModel { out_value_bool = true };
        }
                
        protected virtual async Task<ScalarModel> InitNativeSqliteAsync(bool register, string userAccount)
        {
            return new ScalarModel { out_value_bool = true };
        }

        protected async Task<ScalarModel?> GetScalarFromJson(string json)
        {
            ScalarModel? result = new();
            try
            {
                await _appState.Log($"[Blazor-SqLite] JSON response from SQLite Bridge: {json}");

                // AOT-sichere Deserialisierung
                result = System.Text.Json.JsonSerializer.Deserialize(
                    json,
                    BlazorCore.JsonContext.Default.ScalarModel
                );

                await _appState.Log($"[Blazor-SqLite] JSON convert to Scalarmodele: {json}", data: result);

                if (result != null)
                {
                    // --- 1. STATUS CHECK (Enum-Sicher) ---
                    if (result.out_err == DB_STATUS.NOT_CONNECTED.ToString())
                    {
                        result.out_err = "database_initializing";
                        await _appState.Log($"[Blazor-SqLite] Login abgebrochen: DB lädt noch.", AppLogLevel.Warn);
                    }

                    // --- 2. FEHLER CHECK ---
                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        result.out_err = "auth_query_failed";
                        await _appState.Log($"[Blazor-SqLite] Login Fehler: {result.out_err}", AppLogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                if (result == null)
                {
                    result = new();
                    result.out_err = $"SQLite GetTokenDataTPC Error: {ex.Message}";
                }
            }

            return result;
        }

        public async Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams)
        {
            // JS-LOG
            await _appState.Log("[Blazor-SqLite] START GetTokenDataTPC");

            ClientStorageModel res = new();
            try
            {
                // Parameter extrahieren
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                bool registration = dbParams.GetValueOrDefault("@Int__Registration", "0") == "1";
                bool createUser = !dbParams.ContainsKey("cmd_nocreateuser");
                bool updateUser = !dbParams.ContainsKey("cmd_noupdateuser");
                bool case_mssql = dbParams.GetValueOrDefault("case_mssql", "0") == "1";

                long lastUpdateUnixTS = long.TryParse(dbParams.GetValueOrDefault("@LastUpdateUnixTS", ""), out var l) ? l : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string emailHash = dbParams.GetValueOrDefault("@EmailHash", "");
                string passwordHash = dbParams.GetValueOrDefault("@PasswordHash", "");

                ScalarModel? resultJsonToScalar = new();
                ScalarModel? resultVerify = new();

                string? sql = string.Empty;
                //object[]? typedParams = Array.Empty<object>();
                SqlRequest sqlRequest = new();

                var resultUnixTS = await this.Scalar(new Dictionary<string, string> {
                    { "@Case_", "SelectAuthUsersEmail" }, { "@EmailHash", emailHash }, { "@PasswordHash", passwordHash }
                });
                if (resultUnixTS?.out_err == DB_STATUS.NOT_CONNECTED.ToString())
                    throw new Exception("Integrity Check failed: Database not ready.");

                if (resultUnixTS == null || !string.IsNullOrEmpty(resultUnixTS.out_err))
                {
                    throw new Exception($"User UnixTS not found or access error: {resultUnixTS?.out_err}");
                }


                if (resultUnixTS != null && string.IsNullOrEmpty(resultUnixTS.out_err))
                {
                    var UnixTSsqlite = resultUnixTS.out_value_str;

                    if (!string.IsNullOrEmpty(UnixTSsqlite)) // Account existiert bereits
                    {
                        // UnixTS Abgleich (Sync-Logik) => falls Account/Passwort vorhanden, aber UnixTS unterschiedlich, dann abgleichen
                        if (!string.IsNullOrEmpty(unixTS) && UnixTSsqlite != unixTS)
                        {
                            if (updateUser)
                            {
                                sql = "UPDATE AuthUsers SET UnixTS = @UnixTS WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash;";
                                //sqlRequest = new SqlRequest
                                //{
                                //    Sql = sql,
                                //    Params = new object[] { unixTS, emailHash, passwordHash }
                                //};
                                sqlRequest = new SqlRequest
                                {
                                    Sql = sql,
                                    Params = new Dictionary<string, object?>
                                    {
                                        ["@UnixTS"] = unixTS,
                                        ["@EmailHash"] = emailHash,
                                        ["@PasswordHash"] = passwordHash
                                    }
                                };
                                resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);

                                await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] UPDATE AuthUsers SET UnixTS = @UnixTS");
                                await _appState.Log($"[Blazor-SqLite] resultVerify:", data: resultVerify);
                                if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                {
                                    res.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "SET UnixTS: result is null";
                                    return res;
                                }


                                await _appState.Log($"[Blazor-SqLite] UPDATE AuthUsers SET UnixTS = ? WHERE EmailHash = ? AND PasswordHash = ?", data: new List<object> { unixTS, emailHash, passwordHash });

                            }

                            res.UnixTS = unixTS;
                            res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(UnixTSsqlite))
                            {
                                res.UnixTS = UnixTSsqlite;
                                res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
                            }
                            else
                            {
                                res.out_err = "no_user";
                            }
                        }
                    }
                    else // Account existiert nicht lokal
                    {
                        if (registration)
                        {
                            if (!string.IsNullOrEmpty(unixTS))
                            {
                                var resultUnixTSreg = await this.Scalar(new Dictionary<string, string> {
                                    { "@Case_", "CheckEmail>>AuthUsers" }, { "@EmailHash", emailHash }, { "@UnixTS", unixTS }
                                });
                                if (resultUnixTSreg?.out_err == DB_STATUS.NOT_CONNECTED.ToString())
                                    throw new Exception("Integrity Check failed: Database not ready.");

                                if (resultUnixTSreg == null || !string.IsNullOrEmpty(resultUnixTSreg.out_err))
                                {
                                    throw new Exception($"User UnixTS not found or access error: {resultUnixTSreg?.out_err}");
                                }


                                if (resultUnixTSreg != null && string.IsNullOrEmpty(resultUnixTSreg.out_err))
                                {
                                    if (resultUnixTSreg.out_value_bool)
                                    {
                                        if (updateUser)
                                        {
                                            sql = "UPDATE AuthUsers SET PasswordHash = @PasswordHash, LastUpdateUnixTS = @LastUpdateUnixTS WHERE EmailHash = @EmailHash AND UnixTS = @UnixTS;";
                                            //sqlRequest = new SqlRequest
                                            //{
                                            //    Sql = sql,
                                            //    Params = new object[] { passwordHash, lastUpdateUnixTS, emailHash, unixTS }
                                            //};
                                            sqlRequest = new SqlRequest
                                            {
                                                Sql = sql,
                                                Params = new Dictionary<string, object?>
                                                {
                                                    ["@PasswordHash"] = passwordHash,
                                                    ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                                    ["@EmailHash"] = emailHash,
                                                    ["@UnixTS"] = unixTS
                                                }
                                            };

                                            resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                
                                            await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                            resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] UPDATE AuthUsers SET PasswordHash = @PasswordHash...");
                                            await _appState.Log($"[Blazor-SqLite] resultVerify:", data: resultVerify);
                                            if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                            {
                                                res.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "UPDATE AuthUsers PasswordHash: result is null";
                                                return res;
                                            }
                                        }
                                        res.UnixTS = unixTS;
                                        res.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
                                    }
                                    else if (createUser)
                                    {
                                        // Ruft die Methode auf, die eine neue AuthUsersEntity anlegt
                                        res = await CreateLocalAccount(dbParams);
                                    }
                                }
                                else
                                {
                                    res.out_err = resultUnixTSreg != null && resultUnixTSreg.out_err != null ? resultUnixTSreg.out_err : "auth_query_failed";
                                    await _appState.Log($"[Blazor-SqLite] Login Fehler: {res.out_err}", AppLogLevel.Error);
                                    return res;
                                }
                            }
                            else // Falls keine UnixTS, dann nicht registrieren
                            {
                                res.UnixTS = "";
                                res.WebApiToken = "";
                                res.out_err = "no_user";
                            }
                        }
                        else // Falls keine Registrierung, dann melden, dass Accoutn nicht existiert
                        {
                            // Prüfen, ob Cloud Account existiert (via case_mssql Parameter), dann lokalen Account erstellen
                            if (case_mssql)
                            {
                                // Ruft die Methode auf, die eine neue AuthUsersEntity anlegt
                                res = await CreateLocalAccount(dbParams);
                            }
                            else
                            {
                                res.UnixTS = "";
                                res.WebApiToken = "";
                                res.out_err = "no_user";
                            }
                        }
                    }
                }
                else
                {
                    res.out_err = resultUnixTS != null && resultUnixTS.out_err != null ? resultUnixTS.out_err : "auth_query_failed";
                    await _appState.Log($"[Blazor-SqLite] Login Fehler: {res.out_err}", AppLogLevel.Error);
                    return res;
                }
            }
            catch (Exception ex)
            {
                res.out_err = $"SQLite GetTokenDataTPC Error: {ex.Message}";
            }

            await _appState.Log($"[Blazor-SqLite] res", data: res);
            await _appState.Log("[Blazor-SqLite] END GetTokenDataTPC");
            return res;
        }

        /// <summary>
        /// Die Brücken-Methode zur jeweiligen Plattform.
        /// Nutzt nun IReadOnlyDictionary für benannte Parameter-Unterstützung (Parität WPF/Dapper & WASM/Capacitor).
        /// </summary>
        /// <param name="sql">Der SQL-String (darf @Parameter enthalten)</param>
        /// <param name="parameters">Dictionary mit Keys (z.B. "@UnixTS") und Werten (int, long, string, etc.)</param>
        /// <param name="type">Art der Abfrage (Scalar, Query, Execute, etc.)</param>
        protected virtual async Task<ScalarModel> ExecuteRawSqlAsync(
            string? sql,
            IReadOnlyDictionary<string, object?>? parameters,
            QUERY_TYPE type)
        {
            // Standard-Fallback für noch nicht implementierte Plattformen
            return await Task.FromResult(new ScalarModel
            {
                out_value_bool = false,
                out_err = "Not implemented on this platform (Base Call)"
            });
        }

        protected async Task<ScalarModel?> VerifyJsonToScalarModel(string json)
        {
            ScalarModel? result = new();
            try
            {
                // Json in ScalarModel konvertieren
                result = await GetScalarFromJson(json);
                if (result != null)
                {
                    await _appState.Log($"[Blazor-SqLite] Json-BridgeRes: {json}", data: result);
                    if (result == null
                        || string.IsNullOrEmpty(json)
                        || !string.IsNullOrEmpty(result.out_err))
                    {
                        var err = $"Execute FAIL: {result!.out_err} , json={json}";
                        await _appState.Log($"[Blazor-SqLite] {err}", AppLogLevel.Error);
                        result.out_err = $"local_update_failed: {err}";
                    }
                }
                else
                {
                    if (result == null)
                    {
                        result = new();
                        result.out_err = $"local_update_failed: result is null.";
                    }
                }
            }
            catch (Exception ex)
            {
                if (result == null)
                {
                    result = new();
                    result.out_err = ex.Message;
                }
            }
            return result;
        }

        public async Task<ClientStorageModel> CreateLocalAccount(Dictionary<string, string> dbParams, bool case_mssql = false)
        {
            // JS-LOG
            await _appState.Log("[Blazor-SqLite] START CreateLocalAccount");

            ClientStorageModel result = new();
            try
            {
                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                await _appState.Log($"[Blazor-SqLite] unixTS: {unixTS}");

                int termsAccepted = dbParams.GetValueOrDefault("@TermsAccepted", "") == "true" || dbParams.GetValueOrDefault("@TermsAccepted", "") == "1" ? 1 : 0;
                await _appState.Log($"[Blazor-SqLite] TermsAccepted: {termsAccepted}");

                int active = dbParams.GetValueOrDefault("@active", "") == "true" || dbParams.GetValueOrDefault("@active", "") == "1" ? 1 : 0;
                await _appState.Log($"[Blazor-SqLite] active: {active}");

                long lastUpdateUnixTS = long.TryParse(dbParams.GetValueOrDefault("@LastUpdateUnixTS", ""), out var l) ? l : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await _appState.Log($"[Blazor-SqLite] lastUpdateUnixTS: {lastUpdateUnixTS}");

                ScalarModel? resultJsonToScalar = new();
                ScalarModel? resultVerify = new();

                string? sql = string.Empty;
                object[]? typedParams = Array.Empty<object>();
                SqlRequest sqlRequest = new();

                // SQL Insert or Replace (Sicherstellung der Eindeutigkeit über UnixTS)
                sql = @"INSERT OR REPLACE INTO AuthUsers 
                    (UnixTS, EmailHash, PasswordHash, TermsAccepted, IdP, IdPClientIdent, IdPToken, active, LastUpdateUnixTS) 
                    VALUES (@UnixTS, @EmailHash, @PasswordHash, @TermsAccepted, @IdP, @IdPClientIdent, @IdPToken, @active, @LastUpdateUnixTS)";
                //sqlRequest = new SqlRequest
                //{
                //    Sql = sql,
                //    Params = new object[] { unixTS, dbParams.GetValueOrDefault("@EmailHash", ""), dbParams.GetValueOrDefault("@PasswordHash", ""), termsAccepted, dbParams.GetValueOrDefault("@IdP", ""), dbParams.GetValueOrDefault("@IdPClientIdent", ""), dbParams.GetValueOrDefault("@IdPToken", ""), active, lastUpdateUnixTS }
                //};
                sqlRequest = new SqlRequest
                {
                    Sql = sql,
                    Params = new Dictionary<string, object?>
                    {
                        ["@UnixTS"] = unixTS,
                        ["@EmailHash"] = dbParams.GetValueOrDefault("@EmailHash", ""),
                        ["@PasswordHash"] = dbParams.GetValueOrDefault("@PasswordHash", ""),
                        ["@TermsAccepted"] = termsAccepted,
                        ["@IdP"] = dbParams.GetValueOrDefault("@IdP", ""),
                        ["@IdPClientIdent"] = dbParams.GetValueOrDefault("@IdPClientIdent", ""),
                        ["@IdPToken"] = dbParams.GetValueOrDefault("@IdPToken", ""),
                        ["@active"] = active,
                        ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                    }
                };

                resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);

                //resultVerify = await VerifyScalarModel(resultJsonToScalar, "UPDATE Todo SET Tasks = @Tasks, ");
                //if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                //{
                //    result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Insert user: result is null";
                //    return result;
                //}

                result.UnixTS = unixTS;
                result.WebApiToken = API_CONST.TOKEN_LOCAL_ONLY;
            }
            catch (Exception ex)
            {
                result.out_err = $"CreateLocalAccount Error: {ex.Message}";
            }

            await _appState.Log("[Blazor-SqLite] result", data: result);
            await _appState.Log("[Blazor-SqLite] END CreateLocalAccount");
            return result;
        }

        public async Task<ScalarModel> Save(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[Blazor-SqLite] START Save");

            ScalarModel result = new();
            try
            {
                await _appState.Log($"[Blazor-SqLite] dbParams:", data: dbParams);

                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                await _appState.Log($"[Blazor-SqLite] case_: {case_}");

                if (string.IsNullOrEmpty(case_)) return result;

                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                await _appState.Log($"[Blazor-SqLite] unixTS: {unixTS}");

                string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                await _appState.Log($"[Blazor-SqLite] authUsers_UnixTS: {authUsers_UnixTS}");

                string todo_UnixTS = dbParams.GetValueOrDefault("@Todo_UnixTS", "");
                await _appState.Log($"[Blazor-SqLite] todo_UnixTS: {todo_UnixTS}");

                long lastUpdateUnixTS = long.TryParse(dbParams.GetValueOrDefault("@LastUpdateUnixTS", ""), out var l) ? l : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await _appState.Log($"[Blazor-SqLite] lastUpdateUnixTS: {lastUpdateUnixTS}");

                int isChecked = dbParams.GetValueOrDefault("@IsChecked", "") == "true" || dbParams.GetValueOrDefault("@IsChecked", "") == "1" ? 1 : 0;
                await _appState.Log($"[Blazor-SqLite] isCheckedInt: {isChecked}");


                ScalarModel? resultJsonToScalar = new();
                ScalarModel? resultVerify = new();
                //ScalarModel? resultScalar = new();

                string? sql = string.Empty;
                //object[]? typedParams = Array.Empty<object>();
                SqlRequest sqlRequest = new();

                switch (case_)
                {
                    // Tasks
                    case "UpdateIsChecked>>Tasks":
                        sql = "UPDATE Tasks SET IsChecked = @IsChecked, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@IsChecked"] = isChecked,
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                ["@UnixTS"] = unixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);

                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM AuthUsersExtend");
                        await _appState.Log($"[Blazor-SqLite] resultVerify:", data: resultVerify);
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }

                        // Todo-UnixTS ermitteln, um offene Tasks zu prüfen
                        resultVerify = await this.Scalar(new Dictionary<string, string> {
                            { "@Case_", "SelectTodo_UnixTS>>Tasks" }, { "@UnixTS", unixTS }
                        });
                        await _appState.Log("[Blazor-SqLite] resultScalar (ExistsOpenTasks>>Tasks):", data: resultVerify);
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }
                        else
                        {
                            todo_UnixTS = resultVerify!.out_value_str ?? todo_UnixTS;

                            if (string.IsNullOrEmpty(todo_UnixTS))
                            {
                                result.out_value_str = $"updated:{unixTS}:0";
                                return result; // Es ist ein Sharing Eintrag, Todo darf nicht gesetzt werden
                            }

                            // Gibt es noch offenen Tasks
                            resultVerify = await this.Scalar(new Dictionary<string, string> {
                                { "@Case_", "ExistsOpenTasks>>Tasks" }, { "@Todo_UnixTS", todo_UnixTS }
                            });
                            await _appState.Log("[Blazor-SqLite] resultScalar (ExistsOpenTasks>>Tasks):", data: resultVerify);
                            if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                            {
                                result.out_err = resultVerify.out_err;
                                return result;
                            }

                            // Wenn keine offene Tasks, dann Todo als erledigt markieren
                            bool allChecked = !resultVerify!.out_value_bool;

                            // Aktuellen check-Status holen
                            resultVerify = await this.Scalar(new Dictionary<string, string> {
                                { "@Case_", "SelectIsChecked>>Todo" }, { "@Todo_UnixTS", todo_UnixTS }
                            });
                            await _appState.Log("[Blazor-SqLite] resultScalar (ExistsOpenTasks>>Tasks):", data: resultVerify);
                            if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                            {
                                result.out_err = resultVerify.out_err;
                                return result;
                            }
                            else
                            {
                                // Wenn keine offene Tasks, dann Todo als erledigt markieren
                                bool currCheckedStatus = resultVerify!.out_value_bool;

                                // Wenn Status geändert, dann Todo aktualisieren
                                if(allChecked != currCheckedStatus)
                                {
                                    sql = "UPDATE Todo SET IsChecked = @Status, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @Todo_UnixTS;";
                                    sqlRequest = new SqlRequest
                                    {
                                        Sql = sql,
                                        Params = new Dictionary<string, object?>
                                        {
                                            ["@Status"] = allChecked ? 1 : 0,
                                            ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                            ["@Todo_UnixTS"] = todo_UnixTS
                                        }
                                    };

                                    resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                    resultVerify = await VerifyScalarModel(resultJsonToScalar, "Update Todo Status");
                                    if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                                    {
                                        result.out_err = resultVerify.out_err;
                                        return result;
                                    }

                                    result.out_value_str = $"updated:{unixTS}:0";
                                }
                            }
                        }
                        break;

                    //case "Save>>Tasks":
                    //    sql = @"INSERT OR REPLACE INTO Tasks 
                    //        (AuthUsers_UnixTS, UnixTS, DisplayName, IsChecked, imgJpeg, imgJpegThumbnail, Todo_UnixTS, LastUpdateUnixTS) 
                    //        VALUES (@AuthUsers_UnixTS, @UnixTS, @DisplayName, @IsChecked, @imgJpeg, @imgJpegThumbnail, @Todo_UnixTS, @LastUpdateUnixTS);";
                    //    sqlRequest = new SqlRequest
                    //    {
                    //        Sql = sql,
                    //        Params = new Dictionary<string, object?>
                    //        {
                    //            ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                    //            ["@UnixTS"] = unixTS,
                    //            ["@DisplayName"] = dbParams.GetValueOrDefault("@DisplayName", ""),
                    //            ["@IsChecked"] = isChecked,
                    //            //["@imgJpeg"] = dbParams.GetValueOrDefault("@imgJpeg", ""),
                    //            ["@imgJpeg"] = PrepareBinaryParam(dbParams.GetValueOrDefault("@imgJpeg")),
                    //            //["@imgJpegThumbnail"] = dbParams.GetValueOrDefault("@imgJpegThumbnail", ""),
                    //            ["@imgJpegThumbnail"] = PrepareBinaryParam(dbParams.GetValueOrDefault("@imgJpegThumbnail")),
                    //            ["@Todo_UnixTS"] = todo_UnixTS,
                    //            ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                    //        }
                    //    };

                    //    resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                    //    resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO Tasks");
                    //    if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                    //    {
                    //        result.out_err = resultVerify.out_err;
                    //        return result;
                    //    }

                    //    dbParams["@Case_"] = "SelectByTodo_UnixTS>>Tasks";
                    //    ReaderModel<TasksModel> resultTasks = await Read<TasksModel>(dbParams);
                    //    if (resultTasks == null || resultTasks?.out_err == DB_STATUS.NOT_CONNECTED.ToString())
                    //    {
                    //        await _appState.Log("[Blazor-SqLite] Abbruch: DB initialisiert noch.", AppLogLevel.Warn);
                    //        result.out_err = resultTasks != null ? resultTasks.out_err : "Unknown error at SelectByTodo_UnixTS>>Tasks";
                    //        return result;
                    //    }
                    //    else if (resultTasks != null && !string.IsNullOrEmpty(resultTasks.out_err))
                    //    {
                    //        await _appState.Log($"[Blazor-SqLite] Fehler bei Task-Abfrage: {resultTasks.out_err}", AppLogLevel.Error);
                    //        result.out_err = resultTasks.out_err;
                    //        return result;
                    //    }

                    //    if (resultTasks != null && resultTasks.out_list != null && resultTasks.out_list.Count > 0)
                    //    {
                    //        // String zusammenbauen
                    //        var combined = string.Join(", ", resultTasks.out_list.Select(x => x!.DisplayName));

                    //        // Zurückschreiben in die Datenbank
                    //        string isMigration = dbParams.GetValueOrDefault("@IsMigration", "0");
                    //        if(isMigration == "1")
                    //            sql = "UPDATE Todo SET Tasks = @Tasks WHERE UnixTS = @UnixTS;";
                    //        else
                    //            sql = "UPDATE Todo SET Tasks = @Tasks, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @UnixTS;";
                    //        //sqlRequest = new SqlRequest
                    //        //{
                    //        //    Sql = sql,
                    //        //    Params = new object[] { combined, lastUpdateUnixTS, unixTS }
                    //        //};
                    //        sqlRequest = new SqlRequest
                    //            {
                    //                Sql = sql,
                    //                Params = new Dictionary<string, object?>
                    //                {
                    //                    ["@Tasks"] = combined,
                    //                    ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                    //                    ["@UnixTS"] = todo_UnixTS
                    //                }
                    //            };

                    //        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                    //        resultVerify = await VerifyScalarModel(resultJsonToScalar, "UPDATE Todo SET Tasks = @Tasks, ");
                    //        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                    //        {
                    //            var err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Set Todo Tasks: result is null";
                    //            await _appState.Log($"[Blazor-SqLite] Fehler beim Update des Todo-Strings: {err}", AppLogLevel.Error);
                    //        }
                    //    }

                    //    result.out_value_str = $"updated:{unixTS}:{authUsers_UnixTS}";
                    //    break;


                    // AuthUsersExtend
                    case "Save>>AuthUsersExtend":
                        // Prüfen, ob Alias bereits verwendet wird bei einem anderen User
                        var existingAlias = await this.Scalar(new Dictionary<string, string> {
                            { "@Case_", "ExistsDisplayName>>AuthUsersExtend" },
                            { "@DisplayName", dbParams["@DisplayName"] },
                            { "@AuthUsers_UnixTS", authUsers_UnixTS }
                        });

                        await _appState.Log("[Blazor-SqLite] resultScalar (ExistsDisplayName>>AuthUsersExtend):", data: existingAlias);

                        if (!string.IsNullOrEmpty(existingAlias.out_err))
                        {
                            result.out_err = existingAlias.out_err;
                            return result;
                        }

                        // Wenn Alias 'frei' ist
                        if (!existingAlias.out_value_bool)
                        {
                            // Prüfen ob Datensatz existiert
                            var existing = await this.Scalar(new Dictionary<string, string> {
                                { "@Case_", "ExistsByAuthUsers_UnixTS>>AuthUsersExtend" },
                                { "@AuthUsers_UnixTS", authUsers_UnixTS }
                            });

                            await _appState.Log("[Blazor-SqLite] resultScalar (ExistsByAuthUsers_UnixTS>>AuthUsersExtend):", data: existing);

                            if (!string.IsNullOrEmpty(existing.out_err))
                            {
                                result.out_err = existing.out_err;
                                return result;
                            }

                            string logLabel;

                            if (existing.out_value_bool)
                            {
                                // UPDATE - Datensatz existiert bereits
                                sql = @"UPDATE AuthUsersExtend 
                                SET DisplayName = @DisplayName,
                                    imgJpegThumbnail = @imgJpegThumbnail,
                                    LastUpdateUnixTS = @LastUpdateUnixTS
                                WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                                logLabel = "UPDATE AuthUsersExtend";
                                result.out_value_str = $"updated:{unixTS}:{authUsers_UnixTS}";
                            }
                            else
                            {
                                // INSERT - Neuer Datensatz
                                sql = @"INSERT OR REPLACE INTO AuthUsersExtend 
                                    (UnixTS, AuthUsers_UnixTS, DisplayName, imgJpegThumbnail, LastUpdateUnixTS) 
                                    VALUES (@UnixTS, @AuthUsers_UnixTS, @DisplayName, @imgJpegThumbnail, @LastUpdateUnixTS);";
                                logLabel = "INSERT INTO AuthUsersExtend";
                                result.out_value_str = $"saved:{unixTS}:{authUsers_UnixTS}";
                            }

                            sqlRequest = new SqlRequest
                            {
                                Sql = sql,
                                Params = new Dictionary<string, object?>
                                {
                                    ["@UnixTS"] = unixTS,
                                    ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                                    ["@DisplayName"] = dbParams.GetValueOrDefault("@DisplayName", ""),
                                    //["@imgJpegThumbnail"] = dbParams.GetValueOrDefault("@imgJpegThumbnail", ""),
                                    ["@imgJpegThumbnail"] = PrepareBinaryParam(dbParams.GetValueOrDefault("@imgJpegThumbnail")),
                                    ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                                }
                            };

                            resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);

                            await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);

                            resultVerify = await VerifyScalarModel(resultJsonToScalar, logLabel);
                            await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);

                            if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                            {
                                result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err)
                                    ? resultVerify.out_err
                                    : $"{logLabel}: result is null";
                                return result;
                            }
                            result.out_value_str = $"updated:{unixTS}:{authUsers_UnixTS}";
                        }
                        break;


                    // AuthUsers
                    case "DeleteOtp>>AuthUsers":
                        var userCheck = await this.Scalar(new Dictionary<string, string> {
                            { "@Case_", "CheckPassword>>AuthUsers" },
                            { "@UnixTS", authUsers_UnixTS },
                            { "@PasswordHash", dbParams["@PasswordHash"] }
                        });
                        await _appState.Log("[Blazor-SqLite] resultScalar (CheckPassword>>AuthUsers):", data: userCheck);
                        if (!string.IsNullOrEmpty(userCheck.out_err))
                        {
                            result.out_err = userCheck.out_err;
                            return result;
                        }

                        if (userCheck.out_value_bool)
                        {
                            var backupCheck = await this.Scalar(new Dictionary<string, string> {
                                { "@Case_", "CheckBackupCode>>AppParameter" },
                                { "@AuthUsers_UnixTS", authUsers_UnixTS },
                                { "@OtpBackupCode", dbParams["@OtpBackupCode"] }
                            });
                            await _appState.Log("[Blazor-SqLite] resultScalar (CheckBackupCode>>AppParameter):", data: backupCheck);
                            if (!string.IsNullOrEmpty(backupCheck.out_err))
                            {
                                result.out_err = backupCheck.out_err;
                                return result;
                            }

                            if (backupCheck.out_value_bool)
                            {
                                sql = @"UPDATE AuthUsers SET otp = NULL, FailedLoginAttempts = 0 WHERE UnixTS = @AuthUsers_UnixTS;";
                                sqlRequest = new SqlRequest
                                {
                                    Sql = sql,
                                    Params = new Dictionary<string, object?>
                                    {
                                        ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                                    }
                                };

                                resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                resultVerify = await VerifyScalarModel(resultJsonToScalar, "UPDATE AuthUsers SET otp = NULL,...");
                                await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                                if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                {
                                    result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Set otp: result is null";
                                    return result;
                                }

                                sql = @"DELETE FROM AppParameter WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS AND ParameterName = 'OtpBackupCode' AND Scope = 'config';";
                                sqlRequest = new SqlRequest
                                {
                                    Sql = sql,
                                    Params = new Dictionary<string, object?>
                                    {
                                        ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                                    }
                                };
                                resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                resultVerify = await VerifyScalarModel(resultJsonToScalar, "DELETE FROM AppParameter WHERE AuthUsers_UnixTS...");
                                await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                                if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                {
                                    result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Delete AppParameter: result is null";
                                    return result;
                                }

                                result.out_value_str = $"updated:{unixTS}:{unixTS}";
                            }
                            else
                            {
                                sql = @"UPDATE AuthUsers SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE UnixTS = @AuthUsers_UnixTS;";
                                sqlRequest = new SqlRequest
                                {
                                    Sql = sql,
                                    Params = new Dictionary<string, object?>
                                    {
                                        ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                                    }
                                };

                                resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                resultVerify = await VerifyScalarModel(resultJsonToScalar, "DELETE FROM AppParameter WHERE AuthUsers_UnixTS...");
                                await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);

                                if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                {
                                    result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Delete AppParameter: result is null";
                                    return result;
                                }

                                result.out_value_str = "no_userotpbackupcode1";
                            }
                        }
                        else
                        {
                            result.out_value_str = "no_userotpbackupcode2";
                        }
                        break;

                    case "UpdateTermsAccepted>>AuthUsers":
                        sql = "UPDATE AuthUsers SET TermsAccepted = 1, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                ["@UnixTS"] = unixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "UPDATE AuthUsers SET TermsAccepted = 1,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);

                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "UPDATE AuthUsers: result is null";
                            return result;
                        }

                        result.out_value_str = $"updated:{unixTS}:0";
                        break;

                    case "ChangePassword>>AuthUsers":
                        var checkPass = await this.Scalar(new Dictionary<string, string> {
                            { "@Case_", "CheckPassword>>AuthUsers" },
                            { "@UnixTS", unixTS },
                            { "@PasswordHash", dbParams["@PasswordHash"] }
                        });
                        await _appState.Log("[Blazor-SqLite] resultScalar (CheckPassword>>AuthUsers)", data: checkPass);
                        if (!string.IsNullOrEmpty(checkPass.out_err))
                        {
                            result.out_err = checkPass.out_err;
                            return result;
                        }

                        if (checkPass.out_value_bool)
                        {
                            sql = "UPDATE AuthUsers SET PasswordHash = @PasswordHashNew, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @UnixTS;";
                            sqlRequest = new SqlRequest
                            {
                                Sql = sql,
                                Params = new Dictionary<string, object?>
                                {
                                    ["@PasswordHashNew"] = dbParams.GetValueOrDefault("@PasswordHashNew", ""),
                                    ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                    ["@UnixTS"] = unixTS
                                }
                            };

                            resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                            await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                            resultVerify = await VerifyScalarModel(resultJsonToScalar, "UPDATE AuthUsers SET TermsAccepted = 1,...");
                            await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);

                            if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                            {
                                result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "UPDATE AuthUsers: result is null";
                                return result;
                            }

                            result.out_value_str = $"updated:{unixTS}:{unixTS}";
                        }
                        else
                        {
                            result.out_err = "not_updated:0:0";
                        }
                        break;


                    // Todo
                    case "Save>>Todo":
                        int isNotifyActivated = dbParams.GetValueOrDefault("@IsNotifyActivated", "") == "true" || dbParams.GetValueOrDefault("@IsNotifyActivated", "") == "1" ? 1 : 0;
                        await _appState.Log($"[Blazor-SqLite] IsNotifyActivated: {isNotifyActivated}");
                                                
                        long recordDateTimeUnix = long.TryParse(dbParams.GetValueOrDefault("@RecordDateTimeUnix", "0"), out var lRecDT) ? lRecDT : 0;
                        await _appState.Log($"[Blazor-SqLite] RecordDateTimeUnix: {recordDateTimeUnix}");

                        string categoryColor = dbParams.GetValueOrDefault("@CategoryColor", "");
                        await _appState.Log($"[Blazor-SqLite] CategoryColor: {categoryColor}");

                        sql = @"INSERT OR REPLACE INTO Todo 
                            (UnixTS, AuthUsers_UnixTS, DisplayName, IsChecked, Tasks, IsNotifyActivated, RecordDateTimeUnix, CategoryColor, LastUpdateUnixTS) 
                            VALUES (@UnixTS, @AuthUsers_UnixTS, @DisplayName, @IsChecked, @Tasks, @IsNotifyActivated, @RecordDateTimeUnix, @CategoryColor, @LastUpdateUnixTS);";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = unixTS,
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                                ["@DisplayName"] = dbParams.GetValueOrDefault("@DisplayName", ""),
                                ["@IsChecked"] = isChecked,
                                ["@Tasks"] = dbParams.GetValueOrDefault("@Tasks", ""),
                                ["@IsNotifyActivated"] = isNotifyActivated,
                                ["@RecordDateTimeUnix"] = recordDateTimeUnix,
                                ["@CategoryColor"] = categoryColor,
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO Todo,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Insert Todo: result is null";
                            return result;
                        }

                        sql = "DELETE FROM AuthUsersTodo WHERE Todo_UnixTS = @UnixTS AND IFNULL(AuthUsers_ShareFrom_UnixTS, '') != '';";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = unixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "DELETE FROM AuthUsersTodo WHERE Todo_UnixTS = @UnixTS,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Delete AuthUsersTodo: result is null";
                            return result;
                        }

                        // Alias holen
                        resultVerify = await this.Scalar(new Dictionary<string, string> {
                            { "@Case_", "SelectAlias>>AuthUsersExtend" }, { "@AuthUsers_UnixTS", authUsers_UnixTS }
                        });
                        await _appState.Log("[Blazor-SqLite] resultScalar (SelectAlias>>AuthUsersExtend):", data: resultVerify);
                        if (resultVerify?.out_err == DB_STATUS.NOT_CONNECTED.ToString())
                            throw new Exception("Integrity Check failed: Database not ready.");

                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            throw new Exception($"Table AppParameter not found or access error: {resultVerify?.out_err}");
                        }
                        var alias = resultVerify.out_value_str ?? string.Empty;

                        sql = @"INSERT OR REPLACE INTO AuthUsersTodo 
                            (UnixTS, AuthUsers_UnixTS, AuthUsers_ShareFrom_UnixTS, DisplayName, Todo_UnixTS, LastUpdateUnixTS) 
                            VALUES (@UnixTS2, @AuthUsers_UnixTS, @AuthUsers_ShareFrom_UnixTS, @Alias, @UnixTS, @LastUpdateUnixTS);";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS2"] = dbParams.GetValueOrDefault("@UnixTS2", ""),
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                                ["@AuthUsers_ShareFrom_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_ShareFrom_UnixTS", ""),
                                ["@Alias"] = alias,
                                ["@UnixTS"] = unixTS,
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                            }
                        };
                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO AuthUsersTodo,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Insert AuthUsersTodo: result is null";
                            return result;
                        }

                        result.out_value_str = $"saved:{unixTS}:{authUsers_UnixTS}";
                        break;
                    case "UpdateIsChecked>>Todo":
                        sql = "UPDATE Todo SET IsChecked = @IsChecked, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@IsChecked"] = isChecked,
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                ["@UnixTS"] = unixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "UPDATE AuthUsers SET TermsAccepted = 1,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);

                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "UPDATE AuthUsers: result is null";
                            return result;
                        }

                        result.out_value_str = $"updated:{unixTS}:0";
                        break;


                    // SharingUsers
                    case "ChangeSharingStatus>>SharingUsers":
                        sql = @"UPDATE SharingUsers SET SharingStatus = @SharingStatus, LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@SharingStatus"] = dbParams.GetValueOrDefault("@SharingStatus", ""),
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                ["@UnixTS"] = unixTS
                            }
                        };
                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO AuthUsersTodo,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Change sharing status: result is null";
                            return result;
                        }

                        result.out_value_str = $"updated:{unixTS}:0";
                        break;


                    // AuthUsersTodo
                    case "Save>>AuthUsersTodo":
                        string SharingUsersUnixTS = dbParams.GetValueOrDefault("@SharingUsersUnixTS", ""); // Hier befinden sich alle aktivierte User, zum Sharen
                        if (!string.IsNullOrEmpty(SharingUsersUnixTS))
                        {
                            var sharingInfoJson = System.Text.Json.JsonSerializer.Deserialize(
                                SharingUsersUnixTS,
                                BlazorCore.JsonContext.Default.ListSharingInfoJsonModel
                            );
                            if (sharingInfoJson != null)
                            {
                                foreach (var item in sharingInfoJson)
                                {
                                    if (item.IsChecked == "1") // Insert shared User
                                    {
                                        sql = @"
                                            INSERT INTO AuthUsersTodo 
                                                (UnixTS, AuthUsers_UnixTS, AuthUsers_ShareFrom_UnixTS, DisplayName, Todo_UnixTS, LastUpdateUnixTS)
                                            SELECT 
                                                @UnixTS, @AuthUsers_UnixTS, @AuthUsers_ShareFrom_UnixTS, @DisplayName, @Todo_UnixTS, @LastUpdateUnixTS
                                            WHERE NOT EXISTS (
                                                SELECT 1 
                                                FROM AuthUsersTodo  
                                                WHERE
                                                    AuthUsers_UnixTS = @AuthUsers_UnixTS
                                                    AND AuthUsers_ShareFrom_UnixTS = @AuthUsers_ShareFrom_UnixTS
                                                    AND Todo_UnixTS = @Todo_UnixTS
                                            );
                                        ";

                                        sqlRequest = new SqlRequest
                                        {
                                            Sql = sql,
                                            Params = new Dictionary<string, object?>
                                            {
                                                ["@UnixTS"] = unixTS,
                                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                                                ["@AuthUsers_ShareFrom_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_ShareFrom_UnixTS", ""),
                                                ["@DisplayName"] = dbParams.GetValueOrDefault("@DisplayName", ""),
                                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", ""),
                                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                                            }
                                        };

                                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO AuthUsersTodo,...");
                                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                        {
                                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Insert json AppParameterMaster: result is null";
                                            return result;
                                        }
                                    }
                                    else // Delete shared User
                                    {
                                        //dbParams["@AuthUsers_UnixTS"] = item.AuthUsers_UnixTS;
                                        await _appState.Log($"[Blazor-SqLite] AuthUsers_UnixTS: {item.AuthUsers_UnixTS}");

                                        //dbParams["@AuthUsers_ShareFrom_UnixTS"] = item.AuthUsers_ShareFrom_UnixTS;
                                        await _appState.Log($"[Blazor-SqLite] AuthUsers_ShareFrom_UnixTS: {item.AuthUsers_ShareFrom_UnixTS}");

                                        sql = @"DELETE 
                                            FROM AuthUsersTodo 
                                            WHERE 
                                                Todo_UnixTS = @Todo_UnixTS
                                                AND AuthUsers_UnixTS = @AuthUsers_UnixTS
                                                AND AuthUsers_ShareFrom_UnixTS = @AuthUsers_ShareFrom_UnixTS;";

                                        sqlRequest = new SqlRequest
                                        {
                                            Sql = sql,
                                            Params = new Dictionary<string, object?>
                                            {
                                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", ""),
                                                ["@AuthUsers_UnixTS"] = item.AuthUsers_UnixTS,
                                                ["@AuthUsers_ShareFrom_UnixTS"] = item.AuthUsers_ShareFrom_UnixTS
                                            }
                                        };
                                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "DELETE FROM AuthUsersTodo,...");
                                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                        {
                                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Delete AuthUsersTodo: result is null";
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                        result.out_value_str = $"saved:{todo_UnixTS}:0";
                        break;


                    // AppParameter
                    case "Save>>AppParameter":
                        sql = @"INSERT OR REPLACE INTO AppParameter 
                            (UnixTS, ParameterName, ParameterValue, Details, Scope, AuthUsers_UnixTS, LastUpdateUnixTS) 
                            VALUES (@UnixTS, @ParameterName, @ParameterValue, @Details, @Scope, @AuthUsers_UnixTS, @LastUpdateUnixTS);";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = unixTS,
                                ["@ParameterName"] = dbParams.GetValueOrDefault("@ParameterName", ""),
                                ["@ParameterValue"] = dbParams.GetValueOrDefault("@ParameterValue", ""),
                                ["@Details"] = dbParams.GetValueOrDefault("@Details", ""),
                                ["@Scope"] = dbParams.GetValueOrDefault("@Scope", ""),
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                            }
                        };
                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO AppParameter,...");
                        await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                        if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Insert AppParameter: result is null";
                            return result;
                        }

                        result.out_value_str = $"updated:{unixTS}:{authUsers_UnixTS}";
                        break;

                    case "SaveJson>>AppParameter":
                        string json = dbParams.GetValueOrDefault("@Json", "");
                        var items = System.Text.Json.JsonSerializer.Deserialize(json, BlazorCore.JsonContext.Default.ListAppParameterModel);
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                sql = @"INSERT OR REPLACE INTO AppParameter 
                                    (UnixTS, ParameterName, ParameterValue, Details, Scope, AuthUsers_UnixTS, LastUpdateUnixTS) 
                                    VALUES (@UnixTS, @ParameterName, @ParameterValue, @Details, @Scope, @AuthUsers_UnixTS, @LastUpdateUnixTS);";
                                sqlRequest = new SqlRequest
                                {
                                    Sql = sql,
                                    Params = new Dictionary<string, object?>
                                    {
                                        ["@UnixTS"] = item.UnixTS,
                                        ["@ParameterName"] = item.ParameterName,
                                        ["@ParameterValue"] = item.ParameterValue,
                                        ["@Details"] = item.Details,
                                        ["@Scope"] = item.Scope,
                                        ["@AuthUsers_UnixTS"] = item.AuthUsers_UnixTS,
                                        ["@LastUpdateUnixTS"] = item.LastUpdateUnixTS
                                    }
                                };
                                resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                                await _appState.Log("[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                                resultVerify = await VerifyScalarModel(resultJsonToScalar, "INSERT OR REPLACE INTO AppParameter,...");
                                await _appState.Log("[Blazor-SqLite] resultVerify:", data: resultVerify);
                                if (resultVerify == null || !string.IsNullOrEmpty(resultVerify.out_err))
                                {
                                    result.out_err = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err) ? resultVerify.out_err : "Insert json AppParameter: result is null";
                                    return result;
                                }
                            }
                            result.out_value_str = "updated:-1:json_bulk";
                        }
                        break;


                    // Serverseitige Abfragen
                    case "UpdateIdPToken>>AuthUsers":
                    case "DeleteCloudData":
                    case "DeleteAccount":
                    case "DeleteOtpByAuthUsers_UnixTS>>AuthUsers":
                    case "SaveFeedback>>AppMessages":
                    case "Save>>SharingUsers":
                        // Diese Abfragen werden nur serverseitig ausgeführt!
                        break;


                    // Methoden statt Abfragen
                    case "DeleteLocalData":
                        // Siehe SqLite.cs -> ClearAllData()
                        break;

                    case "DeleteLocalAccount":
                        // Siehe SqLite.cs -> DeleteDB()
                        break;

                    case "Register>>AuthUsers":
                        // Siehe SqLite.cs -> GetTokenDataTPC() / CreateLocalAccount()
                        break;

                }
            }
            catch (Exception ex)
            {
                result.out_err = $"SQLite Save Error: {ex.Message}";
            }

            await _appState.Log("[Blazor-SqLite] END Save");

            return result;
        }

        protected async Task<ScalarModel?> VerifyScalarModel(ScalarModel? scalarModel, string description)
        {
            ScalarModel? result = new();
            try
            {
                if (scalarModel == null)
                {
                    result.out_err = "Result is null";
                    await _appState.Log($"[Blazor-SqLite] result.out_err: {result.out_err}");
                    return result;
                }
                if (!string.IsNullOrEmpty(scalarModel.out_err))
                {
                    await _appState.Log($"[Blazor-SqLite] {description}: {result.out_err}");
                    result.out_value_str = scalarModel.out_err; // Anderes als bei MSSQL wo wir den wahren Grund kennen (weil T-SQL)
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (result == null)
                {
                    result = new();
                    result.out_err = ex.Message;
                }
            }
            return result;
        }

        public async Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[Blazor-SqLite] START ExecQuery");

            ScalarModel result = new();
            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                await _appState.Log($"[Blazor-SqLite] case_: {case_}");
                if (string.IsNullOrEmpty(case_)) return result;

                string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
                string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");
                string todo_UnixTS = dbParams.GetValueOrDefault("@Todo_UnixTS", "");
                long lastUpdateUnixTS = long.TryParse(dbParams.GetValueOrDefault("@LastUpdateUnixTS", ""), out var res) ? res : 0;
                ScalarModel? resultJsonToScalar = new();
                ScalarModel? resultVerify = new();
                string status = string.Empty;

                string? sql = string.Empty;
                //object[]? typedParams = Array.Empty<object>();
                SqlRequest sqlRequest = new();

                switch (case_)
                {
                    case "DeleteAuthUsers_UnixTS>>AppParameter":
                        sql = "DELETE FROM AppParameter WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM AppParameter");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }
                        status = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_value_str) ? "not_deleted" : "deleted";
                        result.out_value_str = $"{status}:0:{authUsers_UnixTS}";
                        break;

                    case "Delete>>AuthUsersExtend":
                        // --- Schritt 1: AuthUsersExtend ---
                        sql = "DELETE FROM AuthUsersExtend WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM AuthUsersExtend");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }

                        // AuthUsersTodo Berechtigungen/Verknüpfungen löschen
                        sql = @"DELETE FROM AuthUsersTodo WHERE 
                            (AuthUsers_UnixTS = @AuthUsers_UnixTS AND IFNULL(AuthUsers_ShareFrom_UnixTS, '') != '') OR 
                            (IFNULL(AuthUsers_UnixTS, '') != '' AND AuthUsers_ShareFrom_UnixTS = @AuthUsers_UnixTS);";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM AuthUsersTodo");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }

                        // Sharing-Einträge löschen
                        sql = "DELETE FROM SharingUsers WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM SharingUsers");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }
                        status = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_value_str) ? "not_deleted" : "deleted";
                        result.out_value_str = $"{status}:0:{authUsers_UnixTS}";
                        break;

                    case "Delete>>SharingUsers":
                        sql = "DELETE FROM SharingUsers WHERE UnixTS = @UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = unixTS
                            }
                        };
                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM SharingUsers");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }

                        sql = @"DELETE FROM AuthUsersTodo WHERE 
                            (AuthUsers_UnixTS = @AuthUsers_UnixTS AND IFNULL(AuthUsers_ShareFrom_UnixTS, '') != '') OR 
                            (IFNULL(AuthUsers_UnixTS, '') != '' AND AuthUsers_ShareFrom_UnixTS = @AuthUsers_UnixTS);";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = authUsers_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM AuthUsersTodo");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }
                        status = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_value_str) ? "not_deleted" : "deleted";
                        result.out_value_str = $"{status}:0:{authUsers_UnixTS}";
                        break;

                    case "Delete>>Todo":
                        var checkTasks = await this.Scalar(new Dictionary<string, string> {
                            {"@Case_", "ExistsTodoTasks>>Tasks"}, {"@Todo_UnixTS", unixTS}
                        });

                        // Fehlerprüfung für den Scalar-Aufruf
                        if (!string.IsNullOrEmpty(checkTasks.out_err))
                        {
                            result.out_err = checkTasks.out_err;
                            return result;
                        }

                        if (checkTasks.out_value_bool)
                        {
                            result.out_err = "record_exists_no_delete";
                        }
                        else
                        {
                            sql = "DELETE FROM AuthUsersTodo WHERE Todo_UnixTS = @UnixTS;";
                            sqlRequest = new SqlRequest
                            {
                                Sql = sql,
                                Params = new Dictionary<string, object?>
                                {
                                    ["@UnixTS"] = unixTS
                                }
                            };

                            resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                            await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                            resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM AuthUsersTodo");
                            if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                            {
                                result.out_err = resultVerify.out_err;
                                return result;
                            }

                            sql = "DELETE FROM Todo WHERE UnixTS = @UnixTS;";
                            sqlRequest = new SqlRequest
                            {
                                Sql = sql,
                                Params = new Dictionary<string, object?>
                                {
                                    ["@UnixTS"] = unixTS
                                }
                            };

                            resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                            await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                            resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM Todo");
                            if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                            {
                                result.out_err = resultVerify.out_err;
                                return result;
                            }
                            status = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_value_str) ? "not_deleted" : "deleted";
                            result.out_value_str = $"{status}:0:{authUsers_UnixTS}";
                        }
                        break;

                    case "Delete>>Tasks":
                        sql = "DELETE FROM Tasks WHERE UnixTS = @UnixTS;";
                        //sqlRequest = new SqlRequest
                        //{
                        //    Sql = sql,
                        //    Params = new object[] { unixTS }
                        //};
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = unixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM Tasks");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }

                        sql = @"UPDATE Todo SET Tasks = (
                                SELECT IFNULL(group_concat(DisplayName, ', '), '') 
                                FROM Tasks WHERE Todo_UnixTS = @Todo_UnixTS
                            ), LastUpdateUnixTS = @LastUpdateUnixTS 
                            WHERE UnixTS = @Todo_UnixTS;";
                        //sqlRequest = new SqlRequest
                        //{
                        //    Sql = sql,
                        //    Params = new object[] { todo_UnixTS, lastUpdateUnixTS, todo_UnixTS }
                        //};
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = todo_UnixTS,
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM Tasks");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }
                        status = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_value_str) ? "not_deleted" : "deleted";
                        result.out_value_str = $"{status}:0:{authUsers_UnixTS}";
                        break;

                    case "DeleteTodoTasks>>Tasks":
                        sql = "DELETE FROM Tasks WHERE Todo_UnixTS = @Todo_UnixTS;";
                        //sqlRequest = new SqlRequest
                        //{
                        //    Sql = sql,
                        //    Params = new object[] { todo_UnixTS }
                        //};
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = todo_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by DELETE FROM Todo Tasks");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }

                        sql = "UPDATE Todo SET Tasks = '', LastUpdateUnixTS = @LastUpdateUnixTS WHERE UnixTS = @Todo_UnixTS;";
                        //sqlRequest = new SqlRequest
                        //{
                        //    Sql = sql,
                        //    Params = new object[] { lastUpdateUnixTS, todo_UnixTS }
                        //};
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@LastUpdateUnixTS"] = lastUpdateUnixTS,
                                ["@Todo_UnixTS"] = todo_UnixTS
                            }
                        };

                        resultJsonToScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.execute);
                        await _appState.Log($"[Blazor-SqLite] resultJsonToScalar:", data: resultJsonToScalar);
                        resultVerify = await VerifyScalarModel(resultJsonToScalar, "[Blazor-SqLite] Error by SET Todo Tasks");
                        if (resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_err))
                        {
                            result.out_err = resultVerify.out_err;
                            return result;
                        }
                        status = resultVerify != null && !string.IsNullOrEmpty(resultVerify.out_value_str) ? "not_deleted" : "deleted";
                        result.out_value_str = $"{status}:0:{authUsers_UnixTS}";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"SQLite ExecQuery Error: {ex.Message} (Service)";
            }

            await _appState.Log("[Blazor-SqLite] END ExecQuery");

            return result;
        }

        public async Task<ScalarModel> Scalar(Dictionary<string, string> dbParams)
        {
            await _appState.Log("[Blazor-SqLite] START Scalar");

            ScalarModel result = new();
            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrEmpty(case_))
                {
                    await _appState.Warn("[Blazor-SqLite] SCALAR abgebrochen: @Case_ fehlt.");
                    return result;
                }

                string? sql = "";
                SqlRequest sqlRequest = new();

                // Festlegen des SQL-Statements basierend auf dem Case
                switch (case_)
                {
                    // AuthUsersExtend
                    case "SelectAlias>>AuthUsersExtend":
                        sql = "SELECT IFNULL(DisplayName, '') AS RES FROM AuthUsersExtend WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "ExistsDisplayName>>AuthUsersExtend":
                        sql = "SELECT EXISTS(SELECT 1 FROM AuthUsersExtend WHERE AuthUsers_UnixTS <> @AuthUsers_UnixTS AND DisplayName = @DisplayName) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                                ["@DisplayName"] = dbParams.GetValueOrDefault("@DisplayName", "")
                            }
                        };
                        break;
                    case "ExistsByAuthUsers_UnixTS>>AuthUsersExtend":
                        sql = "SELECT EXISTS(SELECT 1 FROM AuthUsersExtend WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;

                    // Todo
                    case "SharingInfoByUnixTS>>Todo":
                        // IFNULL stellt sicher, dass die Verkettung funktioniert.
                        sql = @"SELECT (DisplayName || IFNULL(': ' || Tasks, '')) AS RES
                            FROM Todo 
                            WHERE UnixTS = @UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", "")
                            }
                        };
                        break;
                    case "ExistsTodo>>Todo":
                        sql = "SELECT EXISTS(SELECT 1 FROM Todo WHERE UnixTS = @UnixTS) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", "")
                            }
                        };
                        break;
                    case "SelectIsChecked>>Todo":
                        sql = "SELECT IsChecked FROM Todo WHERE UnixTS = @Todo_UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                            }
                        };
                        break;
                    //case "IsNotSharingTodo>>Todo": // Konnte auf eine andere Weise gelöst werden
                    //    sql = @"SELECT 
                    //        CASE 
                    //            WHEN EXISTS (
                    //                SELECT 1
                    //                FROM Todo t
                    //                INNER JOIN AuthUsersTodo aut 
                    //                    ON t.UnixTS = aut.Todo_UnixTS
                    //                WHERE 
                    //                    aut.AuthUsers_UnixTS = @AuthUsers_UnixTS
                    //                    AND aut.Todo_UnixTS = @Todo_UnixTS
                    //                    AND IFNULL(aut.AuthUsers_ShareFrom_UnixTS, '') = ''
                    //            )
                    //            THEN 1
                    //            ELSE 0
                    //        END AS ExistsTodo;";
                    //    sqlRequest = new SqlRequest
                    //    {
                    //        Sql = sql,
                    //        Params = new Dictionary<string, object?>
                    //        {
                    //            ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                    //            ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                    //        }
                    //    };
                    //    break;

                    // Tasks
                    case "SelectImgJpeg>>Tasks":
                        sql = "SELECT imgJpeg FROM Tasks WHERE UnixTS = @UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", "")
                            }
                        };
                        break;
                    case "SelectTodo_UnixTS>>Tasks":
                        sql = "SELECT Todo_UnixTS FROM Tasks WHERE UnixTS = @UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", "")
                            }
                        };
                        break;
                    case "ExistsTodoTasks>>Tasks":
                        sql = "SELECT EXISTS(SELECT 1 FROM Tasks WHERE Todo_UnixTS = @Todo_UnixTS) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                            }
                        };
                        break;
                    case "ExistsOpenTasks>>Tasks":
                        sql = "SELECT EXISTS(SELECT 1 FROM Tasks WHERE Todo_UnixTS = @Todo_UnixTS AND IsChecked = 0) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                            }
                        };
                        break;

                    // AppParameter
                    case "SelectAppSettings>>AppParameter":
                        sql = @"SELECT IFNULL((
                                SELECT ParameterValue FROM AppParameter 
                                WHERE AuthUsers_UnixTS = IFNULL(@AuthUsers_UnixTS, (SELECT UnixTS FROM AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash))
                                AND ParameterName = @ParameterName 
                                AND Scope = @Scope
                            ), '') AS RES LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                                ["@EmailHash"] = dbParams.GetValueOrDefault("@EmailHash", ""),
                                ["@PasswordHash"] = dbParams.GetValueOrDefault("@PasswordHash", ""),
                                ["@ParameterName"] = dbParams.GetValueOrDefault("@ParameterName", ""),
                                ["@Scope"] = dbParams.GetValueOrDefault("@Scope", "")
                            }
                        };
                        break;
                    case "ExistsStoreUrl>>AppParameter":
                        sql = @"SELECT EXISTS(
                                    SELECT 1 FROM AppParameter 
                                    WHERE Scope = 'app' 
                                    AND ParameterName LIKE 'StoreUrl_%' 
                                    AND LENGTH(AuthUsers_UnixTS) < 35
                            ) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = null // Explizit keine Parameter
                        };
                        break;
                    case "CheckBackupCode>>AppParameter":
                        sql = "SELECT EXISTS(SELECT 1 FROM AppParameter WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS AND ParameterName = 'OtpBackupCode' AND ParameterValue = @OtpBackupCode AND Scope = 'config') AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                                ["@OtpBackupCode"] = dbParams.GetValueOrDefault("@OtpBackupCode", "")
                            }
                        };
                        break;

                    // AuthUsers
                    case "SelectTermsAccepted>>AuthUsers":
                        sql = "SELECT IFNULL(TermsAccepted, 0) AS RES FROM AuthUsers WHERE UnixTS = @AuthUsers_UnixTS AND active = 1 LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectAuthUsersEmail":
                        sql = "SELECT UnixTS AS RES FROM AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = 1 LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@EmailHash"] = dbParams.GetValueOrDefault("@EmailHash", ""),
                                ["@PasswordHash"] = dbParams.GetValueOrDefault("@PasswordHash", "")
                            }
                        };
                        break;
                    case "ExistsUnixTS>>AuthUsers":
                        sql = "SELECT EXISTS(SELECT 1 FROM AuthUsers WHERE UnixTS = @UnixTS AND active = 1) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", "")
                            }
                        };
                        break;
                    case "CheckPassword>>AuthUsers":
                        sql = "SELECT EXISTS(SELECT 1 FROM AuthUsers WHERE UnixTS = @UnixTS AND PasswordHash = @PasswordHash AND active = 1) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", ""),
                                ["@PasswordHash"] = dbParams.GetValueOrDefault("@PasswordHash", "")
                            }
                        };
                        break;
                    case "CheckEmail>>AuthUsers":
                        sql = "SELECT EXISTS(SELECT 1 FROM AuthUsers WHERE UnixTS = @UnixTS AND EmailHash = @EmailHash AND active = 1) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", ""),
                                ["@EmailHash"] = dbParams.GetValueOrDefault("@EmailHash", "")
                            }
                        };
                        break;
                    case "ExistsEmailHashPasswordHash>>AuthUsers":
                        sql = "SELECT EXISTS(SELECT 1 FROM AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = 1) AS RES;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@EmailHash"] = dbParams.GetValueOrDefault("@EmailHash", ""),
                                ["@PasswordHash"] = dbParams.GetValueOrDefault("@PasswordHash", "")
                            }
                        };
                        break;
                    case "SelectIdP>>AuthUsers":
                        sql = "SELECT IdP FROM AuthUsers WHERE UnixTS = @AuthUsers_UnixTS AND active = 1 LIMIT 1;";
                        //sqlRequest = new SqlRequest
                        //{
                        //    Sql = sql,
                        //    Params = new object[] {
                        //        dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                        //    }
                        //};
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "ExistsOtp>>AuthUsers":
                        sql = "-1"; // Diese abfrage wird nur serverseitig ausgeführt!
                        break;
                    case "ResetLoginAttempts>>AuthUsers":
                    case "DeleteOtp>>AuthUsers":
                    case "CheckAccount>>AuthUsers":
                    case "SaveOtp>>AuthUsers":
                    case "ResetFailedAttempts>>AuthUsers":
                    case "SelectOtp>>AuthUsers":
                        sql = "-1"; // Diese Abfragen werden nur serverseitig ausgeführt!
                        break;

                    // Allgemein
                    case "TableExists":
                        sql = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@TableName;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@TableName"] = dbParams.GetValueOrDefault("@TableName", "")
                            }
                        };
                        break;

                    case "CheckMultipleTablesExist":
                        string tableListRaw = dbParams.GetValueOrDefault("@TableList", "")?.ToString() ?? "";

                        // Split
                        var tables = tableListRaw
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .ToList();

                        // Named placeholders erzeugen: @Table0,@Table1,...
                        var paramNames = tables
                            .Select((_, i) => $"@Table{i}")
                            .ToList();

                        sql = $@"
                            SELECT COUNT(*) AS RES
                            FROM sqlite_master
                            WHERE type = 'table'
                              AND name IN ({string.Join(", ", paramNames)});
                            ";

                        // Parameter-Dictionary bauen
                        var paramDict = new Dictionary<string, object?>();

                        for (int i = 0; i < tables.Count; i++)
                        {
                            paramDict[paramNames[i]] = tables[i];
                        }

                        // SqlRequest bauen
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = paramDict
                        };
                        break;


                    default:
                        result.out_err = $"Case '{case_}' not implemented in SQLite Scalar.";
                        await _appState.Error(result.out_err);
                        return result;
                }

                // Typsichere Aufbereitung der Parameter für JS
                //var typedParams = PrepareTypedParams(sql, dbParams);

                // --- BRIDGE-AUFRUF ---
                await _appState.Log($"[Blazor-SqLite] case_: {case_}");

                await _appState.Log($"[Blazor-SqLite] sql: {sql}");

                // An Capacitor senden
                if (sql != "-1")
                {
                    await _appState.Log($"[Blazor-SqLite] SCALAR SEND TO JS -> SQL: {sqlRequest.Sql}", data: sqlRequest.Params);

                    var resultScalar = await ExecuteRawSqlAsync(sqlRequest.Sql, sqlRequest.Params, QUERY_TYPE.scalar);

                    await _appState.Log($"[Blazor-SqLite] ScalarModel:", data: resultScalar);
                    if (resultScalar == null || !string.IsNullOrEmpty(resultScalar.out_err))
                    {
                        result.out_err = resultScalar != null && !string.IsNullOrEmpty(resultScalar.out_err) ? resultScalar.out_err : "Scalar: result is null";
                        return result;
                    }

                    await _appState.Log($"[Blazor-SqLite] Scalar Erfolg für {case_}: Result={resultScalar.out_value_str}, Bool={resultScalar.out_value_bool}");

                    result = resultScalar;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"SQLite Scalar Exception: {ex.Message}";
                await _appState.Error($"[Blazor-SqLite] {result.out_err}");
            }

            await _appState.Log("[Blazor-SqLite] END Scalar");

            return result;
        }

        public async Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new()
        {
            await _appState!.Log("[BLAZOR SqLiteBase Read()] START");

            ReaderModel<T> result = new();
            try
            {
                string case_ = dbParams.GetValueOrDefault("@Case_", "");
                long recordDateTimeUnix = long.TryParse(dbParams.GetValueOrDefault("@RecordDateTimeUnix", "0"), out var lRecDT) ? lRecDT : 0;

                await _appState.Log($"[BLAZOR SqLiteBase Read()] case_: {case_}");
                await _appState.Log($"[BLAZOR SqLiteBase Read()] dbParams:", data: dbParams);

                if (string.IsNullOrEmpty(case_)) return result;

                string? sql = "";
                SqlRequest sqlRequest = new();

                // Switch Case für SQL-Logik (unverändert, da die @Parameter-Namen stabil bleiben)
                switch (case_)
                {
                    // AuthUsers
                    case "SelectByUnixTS>>AuthUsers":
                        sql = @"SELECT * 
                            FROM AuthUsers 
                            WHERE 
                                UnixTS = @UnixTS
                                AND active = 1";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@UnixTS"] = dbParams.GetValueOrDefault("@UnixTS", "")
                            }
                        };
                        break;
                    case "SelectByIdPClientIdent>>AuthUsers":
                        sql = "-1"; // Diese Abfrage wird nur serverseitig ausgeführt!
                        break;

                    // AuthUsersExtend
                    case "Select>>AuthUsersExtend":
                        sql = @"SELECT ID, UnixTS, AuthUsers_UnixTS, DisplayName, 
                            imgJpegThumbnail, 
                            LastUpdateUnixTS
                            FROM AuthUsersExtend 
                            WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectByDisplayName>>AuthUsersExtend":
                        sql = @"SELECT ae.ID, ae.UnixTS, ae.AuthUsers_UnixTS, ae.DisplayName, 
                               ae.imgJpegThumbnail, 
                               ae.LastUpdateUnixTS, IFNULL(au.UnixTS, '') AS Int__AuthUsers_UnixTS
                        FROM AuthUsersExtend ae
                        LEFT JOIN AuthUsers au ON au.UnixTS = ae.AuthUsers_UnixTS
                        WHERE ae.DisplayName = @DisplayName LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@DisplayName"] = dbParams.GetValueOrDefault("@DisplayName", "")
                            }
                        };
                        break;
                    case "SelectAuthUsersData>>AuthUsersExtend":
                        sql = @"SELECT 'empty for security reasons' AS EmailHash, 
                            'empty for security reasons' AS PasswordHash, 
                            au.active, au.TermsAccepted, au.IdP, ae.DisplayName, 
                            ae.imgJpegThumbnail
                            FROM AuthUsers au
                            INNER JOIN AuthUsersExtend ae ON au.UnixTS = ae.AuthUsers_UnixTS
                            WHERE ae.AuthUsers_UnixTS = @AuthUsers_UnixTS LIMIT 1;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;

                    // SharingUsers
                    case "Select>>SharingUsers":
                        sql = @"SELECT *, 0 AS Int__IsChecked,
                            CASE WHEN s.AuthUsers_UnixTS != @AuthUsers_UnixTS THEN s.AuthUsers_UnixTS ELSE s.AuthUsers_ShareTo_UnixTS END AS Int__AuthUsers_UnixTS,
                            IFNULL((SELECT ae.DisplayName FROM AuthUsersExtend ae WHERE ae.AuthUsers_UnixTS = (CASE WHEN s.AuthUsers_UnixTS != @AuthUsers_UnixTS THEN s.AuthUsers_UnixTS ELSE s.AuthUsers_ShareTo_UnixTS END) LIMIT 1), '') AS Int__Alias,
                            (SELECT ae.imgJpegThumbnail FROM AuthUsersExtend ae WHERE ae.AuthUsers_UnixTS = (CASE WHEN s.AuthUsers_UnixTS != @AuthUsers_UnixTS THEN s.AuthUsers_UnixTS ELSE s.AuthUsers_ShareTo_UnixTS END) LIMIT 1) AS Int__AliasImgJpegThumbnail
                            FROM SharingUsers
                            WHERE s.AuthUsers_UnixTS = @AuthUsers_UnixTS OR s.AuthUsers_ShareTo_UnixTS = @AuthUsers_UnixTS
                            ORDER BY s.ID DESC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectRequest>>SharingUsers":
                        sql = "SELECT * FROM SharingUsers WHERE SharingStatus = 0 ORDER BY ID DESC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = null
                        };
                        break;
                    case "SelectByAuthUsers_UnixTS>>SharingUsers":
                        sql = @"SELECT * FROM SharingUsers WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;

                    // Todo
                    case "Select>>Todo":
                        sql = @"SELECT t.ID, t.UnixTS, t.AuthUsers_UnixTS, t.DisplayName, t.IsChecked, t.Tasks, 
                            t.IsNotifyActivated, t.CategoryColor, t.LastUpdateUnixTS,
                            t.RecordDateTimeUnix,
                            CASE WHEN (t.Tasks IS NULL OR t.Tasks = '') THEN 1 ELSE 0 END AS Int__IsEmpty,
                            CASE WHEN (aut.AuthUsers_UnixTS = @AuthUsers_UnixTS AND IFNULL(aut.AuthUsers_ShareFrom_UnixTS, '') != '') THEN 1 ELSE 0 END AS Int__IsSharingFrom
                            FROM Todo AS t
                            INNER JOIN AuthUsersTodo AS aut ON t.UnixTS = aut.Todo_UnixTS
                            WHERE aut.AuthUsers_UnixTS = @AuthUsers_UnixTS
                            ORDER BY t.UnixTS DESC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    //case "SelectByFilter>>Todo":
                    //    string searchFields = dbParams.GetValueOrDefault("@SearchFields", "");
                    //    var searchParams = System.Text.Json.JsonSerializer.Deserialize(searchFields, BlazorCore.JsonContext.Default.SearchForCategoryColorModel);

                    //    // Vorbereitung der Filter-Parameter im Dictionary für PrepareTypedParams
                    //    var searchStr = $"%{searchParams?.SearchFor?.Trim() ?? ""}%";
                    //    var colorStr = $"%{searchParams?.CategoryColor?.Trim() ?? ""}%";
                    //    int top = int.TryParse(dbParams.GetValueOrDefault("@TOP", "100"), out var t) ? t : 100;
                    //    int isChecked = (dbParams.GetValueOrDefault("@IsChecked", "0").ToLower() == "true" ||
                    //                        dbParams.GetValueOrDefault("@IsChecked", "0") == "1") ? 1 : 0;
                    //    var authUsers_UnixTS = dbParams["@AuthUsers_UnixTS"];
                    //    sql = @"WITH TblTodoUnixTS AS (
                    //            SELECT DISTINCT
                    //                aut.Todo_UnixTS AS UnixTS,
                    //                CASE 
                    //                    WHEN IFNULL(aut.AuthUsers_ShareFrom_UnixTS, '') != '' 
                    //                    THEN 1 ELSE 0 
                    //                END AS IsSharingFrom
                    //            FROM AuthUsersTodo aut
                    //            WHERE aut.AuthUsers_UnixTS = @AuthUsers_UnixTS
                    //        ),

                    //        tblTodos AS (
                    //            SELECT
                    //                t.ID,
                    //                t.UnixTS,
                    //                t.AuthUsers_UnixTS,
                    //                t.DisplayName,
                    //                t.IsChecked,
                    //                t.Tasks,
                    //                t.IsNotifyActivated,
                    //                t.CategoryColor,
                    //                t.LastUpdateUnixTS,
                    //                t.RecordDateTimeUnix,

                    //                CASE 
                    //                    WHEN IFNULL(t.Tasks, '') = '' THEN 1 
                    //                    ELSE 0 
                    //                END AS Int__IsEmpty,

                    //                (SELECT IsSharingFrom 
                    //                 FROM TblTodoUnixTS tmp 
                    //                 WHERE tmp.UnixTS = t.UnixTS 
                    //                 LIMIT 1) AS Int__IsSharingFrom

                    //            FROM Todo t
                    //            WHERE t.UnixTS IN (SELECT UnixTS FROM TblTodoUnixTS)
                    //                AND t.CategoryColor LIKE @ColorStr
                    //                AND t.IsChecked = @IsChecked
                    //                AND (
                    //                    t.DisplayName LIKE @SearchStr
                    //                    OR EXISTS (
                    //                        SELECT 1 
                    //                        FROM Tasks ts 
                    //                        WHERE ts.Todo_UnixTS = t.UnixTS
                    //                          AND ts.DisplayName LIKE @SearchStr
                    //                    )
                    //                )
                    //        )

                    //        SELECT *,
                    //            (SELECT COUNT(DISTINCT UnixTS) FROM TblTodoUnixTS) AS Int__CountAll,
                    //            (SELECT COUNT(*) FROM tblTodos) AS Int__CountFilter
                    //        FROM tblTodos
                    //        ORDER BY UnixTS DESC
                    //        LIMIT @Top;";
                    //    sqlRequest = new SqlRequest
                    //    {
                    //        Sql = sql,
                    //        Params = new Dictionary<string, object?>
                    //        {
                    //            ["@AuthUsers_UnixTS"] = authUsers_UnixTS,
                    //            ["@IsChecked"] = isChecked,
                    //            ["@SearchStr"] = searchStr,
                    //            ["@ColorStr"] = colorStr,
                    //            ["@Top"] = top
                    //        }
                    //    };
                    //    break;
                    case "SelectByAuthUsers_UnixTS>>Todo":
                        sql = @"SELECT 
                                ID, 
                                UnixTS, 
                                AuthUsers_UnixTS, 
                                DisplayName, 
                                IsChecked, 
                                Tasks, 
                                IsNotifyActivated, 
                                RecordDateTimeUnix, 
                                CategoryColor, 
                                LastUpdateUnixTS
                            FROM Todo WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectByTodo_UnixTS>>Todo":
                        sql = @"SELECT
                                ID, 
                                UnixTS, 
                                AuthUsers_UnixTS, 
                                DisplayName, 
                                IsChecked, 
                                Tasks, 
                                IsNotifyActivated, 
                                RecordDateTimeUnix, 
                                CategoryColor, 
                                LastUpdateUnixTS
                            FROM Todo WHERE Todo_UnixTS = @Todo_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectNotify>>Todo":
                        sql = @"SELECT 
                                UnixTS,
                                DisplayName,
                                IsNotifyActivated,
                                RecordDateTimeUnix,
                                Tasks
                            FROM Todo
                            WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS
                              AND IsNotifyActivated = 1
                              AND RecordDateTimeUnix > @RecordDateTimeUnix
                            ORDER BY RecordDateTimeUnix ASC;";

                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                                ["@RecordDateTimeUnix"] = recordDateTimeUnix  // Direkter Vergleich von Unix-Timestamps
                            }
                        };
                        break;
                    case "SelectNotifyPending>>Todo":
                        // 300 Sekunden entsprechen 5 Minuten
                        sql = @"SELECT 
                                UnixTS,
                                DisplayName,
                                IsNotifyActivated,
                                RecordDateTimeUnix,
                                Tasks
                            FROM Todo
                            WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS
                              AND IsNotifyActivated = 1
                              AND RecordDateTimeUnix >= (@RecordDateTimeUnix - 300)
                              AND RecordDateTimeUnix <= (@RecordDateTimeUnix + 300)
                            ORDER BY RecordDateTimeUnix ASC;";

                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                                ["@RecordDateTimeUnix"] = recordDateTimeUnix // Der aktuelle Zeitstempel vom Client
                            }
                        };
                        break;

                    // Tasks
                    case "SelectByAuthUsers_UnixTS>>Tasks":
                        sql = @"SELECT ID, AuthUsers_UnixTS, UnixTS, DisplayName, IsChecked, NULL AS imgJpeg, 
                            imgJpegThumbnail, Todo_UnixTS, LastUpdateUnixTS
                            FROM Tasks WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS ORDER BY ID DESC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectByTodo_UnixTS>>Tasks":
                        sql = @"SELECT ID, AuthUsers_UnixTS, UnixTS, DisplayName, IsChecked, NULL AS imgJpeg, 
                            imgJpegThumbnail, Todo_UnixTS, LastUpdateUnixTS
                            FROM Tasks WHERE Todo_UnixTS = @Todo_UnixTS ORDER BY ID DESC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectContainerByAuthUsers_UnixTS>>Tasks":
                        sql = @"SELECT
                                ID,
                                AuthUsers_UnixTS,
                                UnixTS,
                                (COALESCE((
                                    SELECT DisplayName 
                                    FROM Todo 
                                    WHERE Todo.UnixTS = Tasks.Todo_UnixTS
                                    LIMIT 1
                                    ), 'no todo name') || ': ' || DisplayName) AS DisplayName,
                                IsChecked,
                                NULL AS imgJpeg,
                                imgJpegThumbnail,
                                Todo_UnixTS,
                                LastUpdateUnixTS
                            FROM Tasks 
                            WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;

                    // AppParameter
                    case "Select>>AppParameter":
                        sql = @"SELECT * FROM AppParameter WHERE (AuthUsers_UnixTS = @AuthUsers_UnixTS OR AuthUsers_UnixTS = 0) AND Scope = @Scope ORDER BY ID ASC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", ""),
                                ["@Scope"] = dbParams.GetValueOrDefault("@Scope", "")
                            }
                        };
                        break;
                    case "SelectStoreUrl>>AppParameter":
                        sql = @"SELECT * FROM AppParameter WHERE (AuthUsers_UnixTS = '' OR LENGTH(AuthUsers_UnixTS) < 35 OR AuthUsers_UnixTS IS NULL)
                            AND Scope = 'app' AND ParameterName LIKE 'StoreUrl_%' ORDER BY ID ASC;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = null
                        };
                        break;

                    // AuthUsersTodo
                    case "Select>>AuthUsersTodo":
                        sql = @"SELECT * FROM AuthUsersTodo WHERE Todo_UnixTS = @Todo_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@Todo_UnixTS"] = dbParams.GetValueOrDefault("@Todo_UnixTS", "")
                            }
                        };
                        break;
                    case "SelectByAuthUsers_UnixTS>>AuthUsersTodo":
                        sql = @"SELECT * FROM AuthUsersTodo WHERE AuthUsers_UnixTS = @AuthUsers_UnixTS;";
                        sqlRequest = new SqlRequest
                        {
                            Sql = sql,
                            Params = new Dictionary<string, object?>
                            {
                                ["@AuthUsers_UnixTS"] = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "")
                            }
                        };
                        break;

                }

                await _appState.Log($"[BLAZOR SqLiteBase Read()] sql: {sql}");

                // An Capacitor senden
                if (sql != "-1")
                {
                    await _appState.Log($"[BLAZOR SqLiteBase Read()] READ SEND TO JS -> SQL: {sqlRequest.Sql}", data: sqlRequest.Params);

                    ReadModel<T> resultQuery = await ExecuteReadAsync<T>(sqlRequest.Sql!, sqlRequest.Params);

                    await _appState.Log($"[BLAZOR SqLiteBase Read()] resultQuery:", data: resultQuery);

                    if (resultQuery == null || !string.IsNullOrEmpty(resultQuery.out_err))
                    {
                        result.out_err = resultQuery != null && !string.IsNullOrEmpty(resultQuery.out_err) ? resultQuery.out_err : "Query: result is null";
                        return result;
                    }

                    if (resultQuery != null && resultQuery.out_list != null)
                    {
                        result.out_list = resultQuery.out_list;
                        await _appState.Log($"[BLAZOR SqLiteBase Read()] result.out_list:", data: result.out_list);

                        if (result.out_list?.Count > 0)
                            result.out_data = result.out_list[0];

                        await _appState.Log($"[BLAZOR SqLiteBase Read()] result.out_data:", data: result.out_data);
                    }

                }
            }
            catch (Exception ex)
            {
                await _appState!.Log($"[BLAZOR SqLiteBase Read()] ERROR={ex.Message}");
                result.out_err = $"SQLite Read Error: {ex.Message}";
            }

            await _appState!.Log("[BLAZOR SqLiteBase Read()] END");
            return result;
        }

        //private PreparedData Prepare(string sql, params object[] values)
        //{
        //    // Ersetzt @Name durch ?
        //    string cleanSql = System.Text.RegularExpressions.Regex.Replace(sql, @"@\w+\b", "?");
        //    return new PreparedData { Sql = cleanSql, Params = values };
        //}

        /// <summary>
        /// LEVEL 1: Clear data via JS Bridge.
        /// Returns detailed error info via ScalarModel even for simple bool tasks.
        /// </summary>
        public async Task<ScalarModel> ClearAllData()
        {
            ScalarModel result = new();
            try
            {
                await _appState.Log($"[Blazor-SqLite] SEND TO JS -> Action: {QUERY_TYPE.clearAllData.ToString()}");

                //var resultClearAllData = await VerifyJsonToScalarModel(
                //    await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.clearAllData.ToString()}"),
                //    QUERY_TYPE.clearAllData);
                var resultClearAllData = await ExecuteRawSqlAsync(null, null, QUERY_TYPE.clearAllData);
                if (resultClearAllData == null || !string.IsNullOrEmpty(resultClearAllData.out_err))
                {
                    result.out_err = resultClearAllData != null && !string.IsNullOrEmpty(resultClearAllData.out_err) ? resultClearAllData.out_err : "Clear all data: result is null";
                    return result;
                }

                // Ergebnis übernehmen
                if (resultClearAllData != null && resultClearAllData.out_value_bool)
                {
                    await _appState.Log("[Blazor-SqLite] ClearAllData successful.");
                    IsInitialized = false;
                    result.out_value_str = "deleted:0:0";
                    result.out_value_bool = resultClearAllData.out_value_bool;
                }
                else
                {
                    // Falls out_value_bool false ist, aber kein out_err vorlag
                    await _appState.Log($"[Blazor-SqLite] ClearAllData FAIL: {result.out_value_bool})", AppLogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"ClearAllData Error: {ex.Message}";
                await _appState.Log($"[Blazor-SqLite] result.out_err: {result.out_err}", AppLogLevel.Error);
            }

            return result;
        }

        /// <summary>
        /// LEVEL 2: Drop tables via JS Bridge.
        /// </summary>
        public async Task<ScalarModel> DropAllTables()
        {
            ScalarModel result = new();
            try
            {
                await _appState.Log($"[Blazor-SqLite] SEND TO JS -> Action: {QUERY_TYPE.dropAllTables.ToString()}");

                //var resultDropAllTables = await VerifyJsonToScalarModel(
                //    await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.dropAllTables.ToString()}"),
                //    QUERY_TYPE.dropAllTables);
                var resultDropAllTables = await ExecuteRawSqlAsync(null, null, QUERY_TYPE.dropAllTables);
                if (resultDropAllTables == null || !string.IsNullOrEmpty(resultDropAllTables.out_err))
                {
                    result.out_err = resultDropAllTables != null && !string.IsNullOrEmpty(resultDropAllTables.out_err) ? resultDropAllTables.out_err : "Drop all tables: result is null";
                    return result;
                }

                // Ergebnis übernehmen
                if (resultDropAllTables.out_value_bool)
                {
                    await _appState.Log("[Blazor-SqLite] DropAllTables successful.");
                    IsInitialized = false;
                    result.out_value_str = "deleted:0:0";
                }
                else
                {
                    // Falls out_value_bool false ist, aber kein out_err vorlag
                    await _appState.Log($"[Blazor-SqLite] DropAllTables FAIL: {result.out_value_bool})", AppLogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"DropAllTables Error: {ex.Message}";
                await _appState.Log($"[Blazor-SqLite] result.out_err: {result.out_err}", AppLogLevel.Error);
            }
            return result;
        }

        /// <summary>
        /// LEVEL 3: Full Purge (Data -> Schema -> File).
        /// This is the most robust way to ensure GDPR compliance on mobile devices.
        /// </summary>
        public async Task<ScalarModel> DeleteDB()
        {
            ScalarModel result = new();
            try
            {
                await _appState.Log($"[Blazor-SqLite] SEND TO JS -> Action: {QUERY_TYPE.deleteDatabase.ToString()}");

                //var resultDeleteDB = await VerifyJsonToScalarModel(
                //    await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.deleteDatabase.ToString()}"),
                //    QUERY_TYPE.deleteDatabase);
                var resultDeleteDB = await ExecuteRawSqlAsync(null, null, QUERY_TYPE.deleteDatabase);
                if (resultDeleteDB == null || !string.IsNullOrEmpty(resultDeleteDB.out_err))
                {
                    result.out_err = resultDeleteDB != null && !string.IsNullOrEmpty(resultDeleteDB.out_err) ? resultDeleteDB.out_err : "Delete DB: result is null";
                    return result;
                }

                // Ergebnis übernehmen
                if (resultDeleteDB.out_value_bool)
                {
                    await _appState.Log("[Blazor-SqLite] DeleteDB erfolgreich. State wurde zurückgesetzt.");
                    IsInitialized = false;
                    result.out_value_str = "deleted:0:0";
                }
                else
                {
                    // Falls out_value_bool false ist, aber kein out_err vorlag
                    await _appState.Log($"[Blazor-SqLite] DropDB FAIL: {result.out_value_bool})", AppLogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"Critical-SQLite-Error: {ex.Message}";
                await _appState.Error($"[Blazor-SqLite] Fataler Fehler beim Löschen der Datenbank: {ex.Message}", data: ex);
            }

            return result;
        }

        /// <summary>
        /// Konvertiert einen einzelnen DB-Rückgabewert in ein vollständiges ScalarModel.
        /// Entspricht der JS-Funktion 'toScalar' für 100% Parität zwischen WPF und Capacitor.
        /// </summary>
        protected ScalarModel ToScalarModel(object? rawVal)
        {
            // Fall 1: Wert ist NULL oder DBNull
            if (rawVal == null || rawVal == DBNull.Value)
            {
                return new ScalarModel { out_value_bool = true, out_value_str = null };
            }

            // Grundwert als String für die Konvertierung
            string valStr = rawVal.ToString() ?? "";

            // 1. Numerische Prüfung (Wichtig: InvariantCulture für Punkt-Dezimalstellen)
            bool isNumber = double.TryParse(valStr,
                                           System.Globalization.NumberStyles.Any,
                                           System.Globalization.CultureInfo.InvariantCulture,
                                           out double dblVal);

            // 2. Boolean Logik (Erkennt 1, "1", true, "true")
            bool boolVal = rawVal is bool b ? b :
                          (valStr == "1" || valStr.ToLower() == "true");

            // 3. Ergebnis-Mapping
            return new ScalarModel
            {
                out_value_str = valStr,
                out_value_int = isNumber ? (int)Math.Floor(dblVal) : 0,
                out_value_long = isNumber ? (long)Math.Floor(dblVal) : 0,
                out_value_dbl = isNumber ? dblVal : 0.0,
                out_value_bool = boolVal,
                out_bytes = rawVal as byte[], // Falls es ein BLOB/Bild ist
                out_err = ""
            };
        }

        /// <summary>
        /// Die Brücken-Methode zum Lesen von Daten.
        /// </summary>
        protected virtual async Task<ReadModel<T>> ExecuteReadAsync<T>(
            string sql,
            IReadOnlyDictionary<string, object?>? parameters) where T : new()
        {
            // Standard-Fallback
            return await Task.FromResult(new ReadModel<T>
            {
                out_list = new List<T?>(),
                out_err = "Not implemented on this platform"
            });
        }

        private object? PrepareBinaryParam(object? value)
        {
            if (value is string base64Str && base64Str.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Extrahiert den Teil nach dem Komma (den reinen Base64-Content)
                    var base64Data = base64Str.Substring(base64Str.IndexOf(",") + 1);
                    return Convert.FromBase64String(base64Data);
                }
                catch
                {
                    // Falls Konvertierung fehlschlägt, geben wir den Originalwert zurück 
                    // oder loggen den Fehler. Robustheit geht vor!
                    return value;
                }
            }
            return value; // Kein Bild-String? Dann einfach so lassen (z.B. null oder normaler Text)
        }

    }
}
