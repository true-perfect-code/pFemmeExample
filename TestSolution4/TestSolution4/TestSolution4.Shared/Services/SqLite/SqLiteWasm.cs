using Microsoft.JSInterop;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.SqLite;

namespace TestSolution4.Shared.Services.SqLite
{
    /// <summary>
    /// Capacitor/WASM implementation of SQLite.
    /// Following the naming convention to match other platform services.
    /// </summary>
    public class SqLiteWasm : SqLiteBase
    {
        public SqLiteWasm(IServiceProvider serviceProvider, IJSRuntime jsRuntime)
        : base(serviceProvider, jsRuntime) // Reicht die Services an SqLiteBase weiter
        {
            // Zusätzliche WASM-spezifische Init-Logik falls nötig
        }

        public override async Task<ScalarModel> InitializeAsync(bool register, string userAccount)
        {
            await _initLock.WaitAsync();

            try
            {
                if (IsInitialized)
                    return new ScalarModel { out_value_bool = true };

                // JS-LOGS
                await _appState.Log("[Blazor WASM-SqLite] START InitializeAsync");

                var platform = _platform.GetCurrPlatform();

                if (platform == PLATFORMS.WASM)
                {
                    // --- FALL A: LOKALER BROWSER (Debug im VS) ---
                    // Hier nutzen wir oft ein JS-Fallback oder 
                    // das spezifische Web-Store Setup von Capacitor-SQLite
                    //await InitWebSqliteAsync();
                    //IsInitialized = true;
                    await Task.CompletedTask;
                    IsInitialized = true;
                    return new ScalarModel { out_value_bool = false };
                }
                else
                {
                    // --- FALL B: NATIV (Capacitor auf Handy/Tablet) ---
                    // Direkter Zugriff auf die native SQLite Bridge

                    await _appState.Log($"[Blazor WASM-SqLite] Init native SqLite DB: register = {register}", data: register);
                    var result = await InitNativeSqliteAsync(register, userAccount);
                    IsInitialized = result.out_value_bool;
                    return result;
                }
            }
            catch (System.Exception ex)
            {
                await _appState.Log($"[Blazor WASM-SqLite] Exception await InitNativeSqliteAsync(...): error = {ex.Message}", data: ex);
                IsInitialized = false;
                return new ScalarModel { out_value_bool = false, out_err = ex.Message };
                ////System.Console.WriteLine($"SQLite Initialization failed: {ex.Message}");
                //IsInitialized = false;
                //throw;

            }
            finally
            {
                await _appState.Log("[Blazor WASM-SqLite] END InitializeAsync");
                _initLock.Release();
            }
        }

        protected override async Task<ScalarModel> InitNativeSqliteAsync(bool register, string userAccount)
        {
            // Kleiner Puffer für die Bridge (Android WebView)
            await Task.Delay(300);

            // JS-LOGS
            await _appState.Log("[Blazor WASM-SqLite] START InitNativeSqliteAsync");

            string dbName = DbName;
            const int targetVersion = SqLiteModel.CurrentSchemaVersion;

            await _appState.Log($"[Blazor WASM-SqLite] dbName / targetVersion: register = {register} , dbName = {dbName} , userAccount = {userAccount} , targetVersion = {targetVersion}");

            try
            {
                // 1. Verbindung initialisieren (Wir erwarten einen JSON-String)
                //--------------------------------------------------------------
                var resultInitConnection = await VerifyJsonToScalarModel(
                    await _jsRuntime.InvokeAsync<string>(
                        $"{JsPrefix}.{QUERY_TYPE.initConnection.ToString()}",
                        dbName,
                        userAccount,
                        register
                    ));
                await _appState.Log($"[Blazor WASM-SqLite] initConnection Ergebnis: out_value_bool = {resultInitConnection?.out_value_bool}");
                if (resultInitConnection == null || !string.IsNullOrEmpty(resultInitConnection.out_err) || !resultInitConnection.out_value_bool)
                {
                    //string err = "Datenbank ist nicht initialisiert";
                    //await _appState.Error($"SQL-INIT-FAIL: {err}");
                    //throw new Exception($"SQL-INIT-FAIL: {err}");

                    // Hier greifen wir die spezifische Meldung aus JS ab!
                    string err = resultInitConnection?.out_err ?? "Init failed";

                    return new ScalarModel { out_value_bool = false, out_err = err };
                }

                // 2. Bridge Bereitschaft (DB Status)
                //--------------------------------------------------------------
                DB_STATUS currentStatus = DB_STATUS.INITIALIZING;
                int retryCount = 0;

                while (currentStatus == DB_STATUS.INITIALIZING && retryCount < 15) // Max 3 Sekunden
                {
                    var resultDatabaseStatus = await VerifyJsonToScalarModel(
                        await _jsRuntime.InvokeAsync<string>(
                            $"{JsPrefix}.{QUERY_TYPE.getDatabaseStatus.ToString()}",
                            dbName,
                            _appState.HasUserAccount() ? userAccount : string.Empty,
                            register
                        ));

                    // Json zu ScalarModel konvertieren
                    if (resultDatabaseStatus != null && string.IsNullOrEmpty(resultDatabaseStatus.out_err))
                        currentStatus = (DB_STATUS)resultDatabaseStatus.out_value_int;
                    else
                        currentStatus = DB_STATUS.ERROR;

                    await _appState.Log($"[Blazor WASM-SqLite] DB_STATUS: {currentStatus}");

                    await Task.Delay(200);
                    retryCount++;
                }

                // Loggen des finalen Status (C# nutzt hier automatisch den Namen des Enums, z.B. "READY")
                await _appState.Log($"[Blazor WASM-SqLite] Finaler DB_STATUS: {currentStatus}");


                // Prüfen, ob Tabellen existieren, falls nicht, dann erstellen
                // (vielleicht konnte DB nicht gelöscht werden weil sie von OS gesperrt war, dann ist sie aber hier leer)
                string tableList = string.Join(",", AllTables);
                ScalarModel CheckIfAllTablesExist = await CheckAllTablesExist(tableList);
                if (!CheckIfAllTablesExist.out_value_bool)
                    currentStatus = DB_STATUS.NEW;

                // Validierung
                if (currentStatus == DB_STATUS.INITIALIZING || currentStatus == DB_STATUS.ERROR)
                {
                    //await _appState.Error("Timeout oder Fehler: SQLite Bridge konnte nicht initialisiert werden.");
                    //throw new Exception($"SQLite Bridge Error. Status: {currentStatus}");

                    string err = "CAPACITOR LOCAL SQLITE INITIALIZING OR ERROR";

                    await _appState.Error(err);
                    return new ScalarModel { out_value_bool = false, out_err = err };
                }

                // 3. Schema-Management (Tabellen erstellen, Defaultwerte einfügenm Integritätcheck)
                //--------------------------------------------------------------
                if (currentStatus == DB_STATUS.NEW)
                {
                    //SCHEMA ERSTELLEN
                    var schemaStatements = SqLiteModel.CreateTablesScript
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s + ";")
                        .ToList();

                    await _appState.Log($"[Blazor WASM-SqLite] schemaStatements", data: schemaStatements);

                    var resultSchemaStatements = await VerifyJsonToScalarModel(
                        await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.executeBatch.ToString()}", schemaStatements));
                    if (resultSchemaStatements == null || !string.IsNullOrEmpty(resultSchemaStatements.out_err) || !resultSchemaStatements.out_value_bool)
                    {
                        //var err = "executeBatch response from SQLite Bridge failed (SchemaStatements).";
                        //await _appState.Log($"[Blazor WASM-SqLite] {err}", data: schemaStatements);
                        //throw new Exception(err);

                        string err = "CAPACITOR LOCAL SQLITE SCHEMA STATEMENTS FAILED";

                        await _appState.Error(err);
                        return new ScalarModel { out_value_bool = false, out_err = err };
                    }

                    await _appState.Log($"[Blazor WASM-SqLite] schemaResult erhalten: out_value_bool = {resultSchemaStatements?.out_value_bool}");

                    await Task.Delay(30);

                    // DEFAULTS EINFÜGEN
                    var defaultStatements = SqLiteModel.InsertDefaultParametersScript
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s + ";")
                        .ToList();

                    var resultDefaultStatements = await VerifyJsonToScalarModel(
                        await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.executeBatch.ToString()}", defaultStatements));
                    if (resultDefaultStatements == null || !string.IsNullOrEmpty(resultDefaultStatements.out_err) || !resultDefaultStatements.out_value_bool)
                    {
                        //var err = "executeBatch response from SQLite Bridge failed (defaultStatements).";
                        //await _appState.Log($"[Blazor WASM-SqLite] {err}", data: defaultStatements);
                        //throw new Exception(err);

                        string err = "CAPACITOR LOCAL SQLITE DEFAULT STATEMENTS FAILED";

                        await _appState.Error(err);
                        return new ScalarModel { out_value_bool = false, out_err = err };
                    }

                    // Version setzen (JS nutzt internen State)
                    var resultSetVersion = await VerifyJsonToScalarModel(
                        await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.setVersion.ToString()}", targetVersion));
                    if (resultSetVersion == null || !string.IsNullOrEmpty(resultSetVersion.out_err) || !resultSetVersion.out_value_bool)
                    {
                        var err = resultSetVersion != null && !string.IsNullOrEmpty(resultSetVersion.out_err) ? resultSetVersion.out_err : "Set version: result is null";
                        //await _appState.Error($"[Blazor WASM-SqLite] Version-Update fehlgeschlagen: {err}");
                        //throw new Exception($"Migration Error: DB-Version konnte nicht auf {targetVersion} gesetzt werden. Fehler: {err}");

                        await _appState.Error(err);
                        return new ScalarModel { out_value_bool = false, out_err = err };
                    }

                }
                else if (currentStatus == DB_STATUS.READY)
                {
                    // VERSION PRÜFEN MIT RETRY-LOGIK
                    int currentDbVersion = -1;
                    int vRetry = 0;
                    while (currentDbVersion == -1 && vRetry < 5)
                    {
                        var resultGetVersion = await VerifyJsonToScalarModel(
                            await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.getVersion.ToString()}"));
                        if (resultGetVersion == null || !string.IsNullOrEmpty(resultGetVersion.out_err))
                        {
                            var err = resultGetVersion != null && !string.IsNullOrEmpty(resultGetVersion.out_err) ? resultGetVersion.out_err : "Get version: result is null";
                            //await _appState.Error($"[Blazor WASM-SqLite] Version-Get fehlgeschlagen: {err}");
                            //throw new Exception($"Migration Error: DB-Version konnte nicht ausgelesen werden. Fehler: {err}");
                            await _appState.Error(err);
                            return new ScalarModel { out_value_bool = false, out_err = err };
                        }
                        if (resultGetVersion != null)
                            currentDbVersion = resultGetVersion.out_value_int;

                        if (currentDbVersion == -1) { await Task.Delay(200); vRetry++; }
                    }

                    if (currentDbVersion < targetVersion)
                    {
                        for (int v = currentDbVersion + 1; v <= targetVersion; v++)
                        {
                            if (SqLiteModel.Migrations.TryGetValue(v, out var migrationScript))
                            {
                                var migScript = migrationScript.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => s.Trim())
                                               .Where(s => !string.IsNullOrWhiteSpace(s))
                                               .Select(s => s + ";")
                                               .ToList();

                                var resultMStmts = await VerifyJsonToScalarModel(
                                    await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.executeBatch.ToString()}", migScript));
                                await _appState.Log($"[Blazor WASM-SqLite] Migration v{v} Result: {resultMStmts?.out_value_bool}");
                                if (resultMStmts == null || !string.IsNullOrEmpty(resultMStmts.out_err) || !resultMStmts.out_value_bool)
                                {
                                    var err = "CAPACITOR LOCAL SQLITE MIGRATION SCRIPT FAILED";
                                    //await _appState.Log($"[Blazor WASM-SqLite] err: {err}", data: migScript);
                                    //throw new Exception(err);
                                    await _appState.Error(err);
                                    return new ScalarModel { out_value_bool = false, out_err = err };
                                }
                            }
                        }

                        var resultSetVersion = await VerifyJsonToScalarModel(
                            await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{QUERY_TYPE.setVersion.ToString()}", targetVersion));
                        if (resultSetVersion == null || !string.IsNullOrEmpty(resultSetVersion.out_err) || !resultSetVersion.out_value_bool)
                        {
                            var err = resultSetVersion != null && !string.IsNullOrEmpty(resultSetVersion.out_err) ? resultSetVersion.out_err : "Set version: result is null";
                            //await _appState.Error($"[Blazor WASM-SqLite] Version-Update fehlgeschlagen: {err}");
                            //throw new Exception($"Migration Error: DB-Version konnte nicht auf {targetVersion} gesetzt werden. Fehler: {err}");
                            await _appState.Error(err);
                            return new ScalarModel { out_value_bool = false, out_err = err };
                        }

                        await _appState.Log($"[Blazor WASM-SqLite] Datenbank-Version erfolgreich auf v{targetVersion} gesetzt.");
                    }
                }

                await Task.Delay(30);

                // 4. PRUEFEN OB TABELLEN ERSTELLT/VORHANDEN
                //foreach (var tableName in AllTables)
                //{
                //    // Nutze deine Scalar-Methode für einen schnellen Check
                //    var checkResult = await this.Scalar(new Dictionary<string, string> {
                //            { "@Case_", "CheckMultipleTablesExist" },
                //            { "@TableName", tableName }
                //        });

                //    if (checkResult == null || !checkResult.out_value_bool)
                //    {
                //        string err = $"[Blazor WASM-SqLite] KRITISCH: Tabelle {tableName} fehlt nach Setup!";
                //        await _appState.Error($"SQL-INIT-FAIL: {err}");
                //        throw new Exception($"SQL-INIT-FAIL: {err}");
                //    }
                //}


                //// 1. Alle Tabellennamen zu einem Komma-String zusammenführen
                //string tableList = string.Join(",", AllTables);

                //// 2. Den Batch-Check einmalig ausführen
                //var checkResult = await this.Scalar(new Dictionary<string, string> {
                //    { "@Case_", "CheckMultipleTablesExist" },
                //    { "@TableList", tableList }
                //});
                ////  Wir prüfen nicht auf .out_value_bool, 
                //// sondern ob out_value_int exakt der Anzahl Tabellen entspricht.
                //bool allTablesExist = checkResult != null && checkResult.out_value_int == AllTables.Length;

                ScalarModel allTablesExist = await CheckAllTablesExist(tableList);

                if (!allTablesExist.out_value_bool)
                {
                    // Wir nutzen string.Join, damit die Fehlermeldung die Namen anzeigt statt "System.String[]"
                    //string tableNames = string.Join(", ", AllTables);
                    string err = $"[Blazor WASM-SqLite] KRITISCH: Erwartet: {AllTables.Length} Tabellen, Gefunden: {allTablesExist.out_value_int}. Tabellenliste: {tableList}";

                    //await _appState.Error($"SQL-INIT-FAIL: {err}");
                    //throw new Exception($"SQL-INIT-FAIL: {err}");
                    await _appState.Error(err);
                    return new ScalarModel { out_value_bool = false, out_err = err };
                }

                // Parameter prüfen
                var resultOpenTasks = await this.Scalar(new Dictionary<string, string> {
                    { "@Case_", "ExistsStoreUrl>>AppParameter" }
                });
                if (resultOpenTasks?.out_err == DB_STATUS.NOT_CONNECTED.ToString())
                {
                    //throw new Exception("Integrity Check failed: Database not ready.");
                    var err = "CAPACITOR LOCAL SQLITE INTEGRITY CHECK FAILED: NOT CONNECTED";
                    await _appState.Error(err);
                    return new ScalarModel { out_value_bool = false, out_err = err };
                }

                if (resultOpenTasks == null || !string.IsNullOrEmpty(resultOpenTasks.out_err))
                {
                    //throw new Exception($"Table AppParameter not found or access error: {resultOpenTasks?.out_err}");
                    var err = "CAPACITOR LOCAL SQLITE INTEGRITY CHECK FAILED: APP PARAMETER TABLE ISSUE";
                    await _appState.Error(err);
                    return new ScalarModel { out_value_bool = false, out_err = err };
                }

                await _appState.Log("[Blazor WASM-SqLite] Initialization and Integrity Check successful.");
            }
            catch (Exception ex)
            {
                //Console.Error.WriteLine($"[Blazor WASM-SqLite] Critical Init Error: {ex.Message}");
                //throw;
                var err = ex.Message;
                await _appState.Error(err);
                return new ScalarModel { out_value_bool = false, out_err = err };
            }

            await _appState.Log("[Blazor WASM-SqLite] END InitNativeSqliteAsync");

            return new ScalarModel { out_value_bool = true };
        }

        private async Task<ScalarModel> CheckAllTablesExist(string tableList)
        {
            try
            {
                var checkResult = await this.Scalar(new Dictionary<string, string> {
                    { "@Case_", "CheckMultipleTablesExist" },
                    { "@TableList", tableList }
                });
                //  Wir prüfen nicht auf .out_value_bool, 
                // sondern ob out_value_int exakt der Anzahl Tabellen entspricht.
                bool allTablesExist = checkResult != null && checkResult.out_value_int == AllTables.Length;

                if (checkResult != null)
                {
                    return new ScalarModel
                    {
                        out_value_bool = allTablesExist,
                        out_value_int = checkResult.out_value_int
                    };
                }
                else
                {
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = $"[Blazor WPF-SqLite] Critical Init Error: checkResult is null"
                    };
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Blazor WPF-SqLite] Critical Init Error: {ex.Message}");
                //throw;
                return new ScalarModel
                {
                    out_value_bool = false,
                    out_err = $"[Blazor WPF-SqLite] Critical Init Error: {ex.Message}"
                };
            }
        }

        protected override async Task<ScalarModel> ExecuteRawSqlAsync(
            string? sql,
            IReadOnlyDictionary<string, object?>? parameters, // NEU: Dictionary
            QUERY_TYPE type)
        {
            string json;

            // --- ANPASSUNG DER SICHERUNGSSCHRANKE ---
            bool isAdministrative = type == QUERY_TYPE.clearAllData ||
                                    type == QUERY_TYPE.dropAllTables ||
                                    type == QUERY_TYPE.deleteDatabase ||
                                    type == QUERY_TYPE.getDatabaseStatus ||
                                    type == QUERY_TYPE.getVersion;

            // --- NEU: SICHERUNGSSCHRANKE ---
            // Wenn das SQL leer ist, dürfen wir auf keinen Fall die JS-Bridge aufrufen.
            //if (string.IsNullOrWhiteSpace(sql))
            if (string.IsNullOrWhiteSpace(sql) && !isAdministrative)
            {
                // Wir loggen den Fehler detailliert, damit die Suche nicht "ewig" dauert.
                string errorContext = $"[WASM-SQLite] KRITISCHER FEHLER: SQL-String ist leer! " +
                                      $"Query-Type: {type}, Parameter-Count: {parameters?.Count ?? 0}";

                await _appState.Error(errorContext);

                return new ScalarModel
                {
                    out_err = "SQL_COMMAND_MISSING_IN_SHARED_CODE", // Klarer Hinweis auf den fehlenden case
                    out_value_bool = false
                };
            }
            // -------------------------------

            if (isAdministrative && string.IsNullOrEmpty(sql))
            {
                // WICHTIG: .ToLower() muss mit deiner JS-Bridge-Funktionsnamen übereinstimmen
                json = await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{type.ToString()}");
            }

            // 1. Plattformspezifische SQL-Konvertierung & Parameter-Mapping
            // Wir nutzen jetzt den Transformer, um aus dem Dictionary das Array zu machen
            var (processedSql, finalParams) = PrepareSqlAndParams(sql, parameters);

            // 2. JS-Aufruf Logik (Struktur identisch zu deiner Version)
            // Wir prüfen, ob wir administrative Befehle ohne SQL/Parameter haben
            if (string.IsNullOrEmpty(processedSql) && (parameters == null || parameters.Count == 0))
            {
                // Aufruf ohne zusätzliche Argumente (clearAllData, deleteDatabase etc.)
                json = await _jsRuntime.InvokeAsync<string>($"{JsPrefix}.{type.ToString().ToLower()}");
            }
            else
            {
                // Standard-Aufruf mit konvertiertem SQL und den nun synchronisierten Parametern
                json = await _jsRuntime.InvokeAsync<string>(
                    $"{JsPrefix}.{type.ToString().ToLower()}",
                    processedSql,
                    finalParams // Das vom Transformer erstellte Array
                );
            }

            // 3. Deserialisierung
            var result = await GetScalarFromJson(json);

            return result ?? new ScalarModel
            {
                out_err = "json_deserialization_failed",
                out_value_bool = false
            };
        }

        protected override async Task<ReadModel<T>> ExecuteReadAsync<T>(
            string sql,
            IReadOnlyDictionary<string, object?>? parameters) // NEU: Dictionary Signatur
        {
            await _appState!.Log("[BLAZOR WASM SqLite ExecuteReadAsync()] START");

            var result = new ReadModel<T>();

            try
            {
                // --- NEU: SICHERUNGSSCHRANKE ---
                // Wenn das SQL leer ist, dürfen wir auf keinen Fall die JS-Bridge aufrufen.
                if (string.IsNullOrWhiteSpace(sql))
                {
                    // Wir loggen den Fehler detailliert, damit die Suche nicht "ewig" dauert.
                    string errorContext = $"[WASM-SQLite] KRITISCHER FEHLER: SQL-String ist leer! Parameter-Count: {parameters?.Count ?? 0}";
                    await _appState.Error($"[BLAZOR WASM SqLite ExecuteReadAsync()] ERROR={errorContext}");

                    result.out_err = "SQL_COMMAND_MISSING_IN_SHARED_CODE"; // Klarer Hinweis auf den fehlenden case
                }
                // -------------------------------

                await _appState!.Log($"[BLAZOR WASM SqLite ExecuteReadAsync()] sql={sql}");
                await _appState!.Log("[BLAZOR WASM SqLite ExecuteReadAsync()] parameters:", data: parameters);

                // Wir nutzen die bereits umgestellte Methode ExecuteRawSqlAsync.
                // Diese ruft intern PrepareSqlAndParams auf, wandelt @ in ? um 
                // und synchronisiert das Dictionary mit dem JS-Array.
                var resultQuery = await ExecuteRawSqlAsync(sql, parameters, QUERY_TYPE.query);

                await _appState!.Log("[BLAZOR WASM SqLite ExecuteReadAsync()] resultQuery:", data: resultQuery);

                if (resultQuery == null || !string.IsNullOrEmpty(resultQuery.out_err))
                {
                    result.out_err = resultQuery?.out_err ?? "WASM-Query: result is null";
                    return result;
                }

                // AOT-sichere Deserialisierung (dein bewährter Weg über AotUtility)
                if (!string.IsNullOrEmpty(resultQuery.out_value_str) && resultQuery.out_value_str != "[]")
                {
                    var typeInfo = BlazorCore.AotUtility.GetListTypeInfo<T>();

                    // Deserialisierung in die typsichere Liste
                    var list = System.Text.Json.JsonSerializer.Deserialize(resultQuery.out_value_str, typeInfo);

                    result.out_list = list;
                    result.out_list_local = list; // Parität zur WPF-Logik

                    await _appState!.Log("[BLAZOR WASM SqLite ExecuteReadAsync()] result.out_list:", data: result.out_list);
                }
                else
                {
                    await _appState!.Log($"[BLAZOR WASM SqLite ExecuteReadAsync()] resultQuery.out_value_str={resultQuery.out_value_str}");
                    result.out_list = new List<T?>();
                }
            }
            catch (Exception ex)
            {
                var err = ex.Message;
                await _appState!.Error($"[BLAZOR WASM SqLite ExecuteReadAsync()] ERROR={err}");
            }

            await _appState!.Log("[BLAZOR WASM SqLite ExecuteReadAsync()] END");
            return result;
        }

        private (string? processedSql, object[] finalParameters) PrepareSqlAndParams(
            string? sql,
            IReadOnlyDictionary<string, object?>? parameters)
        {
            if (string.IsNullOrEmpty(sql))
                return (sql, Array.Empty<object>());

            var valuesList = new List<object?>();

            // Wir nutzen Regex.Replace, um die @Parameter zu finden, die Werte 
            // in die Liste zu schreiben (Reihenfolge!) und durch ? zu ersetzen.
            string processedSql = System.Text.RegularExpressions.Regex.Replace(sql, @"@\w+\b", match =>
            {
                string paramName = match.Value;
                if (parameters != null && parameters.TryGetValue(paramName, out var value))
                {
                    valuesList.Add(value);
                }
                else
                {
                    valuesList.Add(null); // Sicherheit für Capacitor-Synchronität
                }
                return "?";
            });

            // Die einfachste und sicherste Variante für Blazor WASM:
            return (processedSql, valuesList.Cast<object>().ToArray());
        }


    }
}

