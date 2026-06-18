using BlazorCore.Services.AppState;
using BlazorCore.Services.SqlClient;          // Pfad zu ScalarModel etc.
using BlazorCore.Services.SqLite;
using TestSolution4.Shared.Services.SqLite;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.JSInterop;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TestSolution4.Wpf.Services
{
    /// <summary>
    /// WPF Implementation des SQLite Service.
    /// Nutzt Microsoft.Data.Sqlite und Dapper für native Windows-Performance.
    /// </summary>
    public class SqLite : SqLiteBase
    {
        private string _connectionString = string.Empty;

        public SqLite(IServiceProvider serviceProvider, IJSRuntime jsRuntime)
            : base(serviceProvider, jsRuntime)
        {
            // Basisklasse wird über den Konstruktor bedient
        }

        /// <summary>
        /// Haupt-Initialisierung für Windows (WPF). 
        /// Steuert den Ablauf über den Semaphore-Lock der Basisklasse.
        /// </summary>
        public override async Task<ScalarModel> InitializeAsync(bool register, string userAccount)
        {
            await _initLock.WaitAsync();

            try
            {
                if (IsInitialized)
                {
                    await _appState.Log("[Blazor WPF-SqLite] Bereits initialisiert, überspringe...");
                    return new ScalarModel { out_value_bool = true };
                }

                await _appState.Log("[Blazor WPF-SqLite] START InitializeAsync");

                // Aufruf der nativen WPF-Logik
                var result = await InitNativeSqliteAsync(register, userAccount);

                IsInitialized = true;
                await _appState.Log("[Blazor WPF-SqLite] InitializeAsync erfolgreich beendet");

                return result;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Blazor WPF-SqLite] KRITISCHER FEHLER während InitializeAsync: {ex.Message}", data: ex);
                IsInitialized = false;
                return new ScalarModel { out_value_bool = false, out_err = ex.Message };
            }
            finally
            {
                _initLock.Release();
                await _appState.Log("[Blazor WPF-SqLite] END InitializeAsync (Lock released)");
            }
        }

        /// <summary>
        /// Technische DB-Vorbereitung: Pfade, Dateierstellung, Schema & Migrationen.
        /// </summary>
        protected override async Task<ScalarModel> InitNativeSqliteAsync(bool register, string userAccount)
        {
            SqlMapper.AddTypeHandler(new SqliteBlobToStringHandler());

            string dbName = DbName;
            const int targetVersion = SqLiteModel.CurrentSchemaVersion;

            await _appState.Log($"[Blazor WPF-SqLite] START InitNativeSqliteAsync: dbName={dbName}");

            try
            {
                // 1. Verbindung initialisieren (Pfad & Hash-Logik)
                // --------------------------------------------------------------
                string normalizedAccount = userAccount.Trim().ToLower();
                string accountHash = GenerateShortHash(normalizedAccount);
                string effectiveDbFileName = $"{dbName}_{accountHash}.db";

                //// Roaming
                //string appDataPath = Path.Combine(
                //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                //    dbName);

                // Von Roaming auf Local wechseln:
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    dbName);

                if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
                string dbPath = Path.Combine(appDataPath, effectiveDbFileName);

                bool dbExists = File.Exists(dbPath); // z.B. C:\Users\perfe\AppData\Roaming\pMunus\

                // Falls DB nicht da und kein Register-Prozess -> Fehler wie in WASM
                if (!dbExists && !register)
                {
                    //throw new Exception("LOCAL_DB_NOT_FOUND");
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = "LOCAL_DB_NOT_FOUND" // Wichtig für die UI-Übersetzung!
                    };
                }
                else
                {
                    _appState.UpdateLocalSqLiteDbPath(dbPath);
                }

                    _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
                await _appState.Log($"[Blazor WPF-SqLite] initConnection: Path={dbPath}");

                // 2. Status ermitteln (Parität zu Capacitor-Logik)
                // --------------------------------------------------------------
                DB_STATUS currentStatus = dbExists ? DB_STATUS.READY : DB_STATUS.NEW;
                await _appState.Log($"[Blazor WPF-SqLite] DB_STATUS erkannt als: {currentStatus}");

                // Prüfen, ob Tabellen existieren, falls nicht, dann erstellen
                // (vielleicht konnte DB nicht gelöscht werden weil sie von OS gesperrt war, dann ist sie aber hier leer)
                string tableList = string.Join(",", AllTables);
                ScalarModel CheckIfAllTablesExist = await CheckAllTablesExist(tableList);
                if (!CheckIfAllTablesExist.out_value_bool)
                    currentStatus = DB_STATUS.NEW;

                // 3. Schema-Management (Neuinstallation oder Migration)
                // --------------------------------------------------------------
                if (currentStatus == DB_STATUS.NEW)
                {
                    await _appState.Log("[Blazor WPF-SqLite] Installiere neues Schema (executeBatch)...");

                    await ExecuteBatchAsync(SqLiteModel.CreateTablesScript);
                    await ExecuteBatchAsync(SqLiteModel.InsertDefaultParametersScript);
                    await SetVersionAsync(targetVersion);
                }
                else if (currentStatus == DB_STATUS.READY)
                {
                    int currentDbVersion = await GetVersionAsync();
                    if (currentDbVersion < targetVersion)
                    {
                        await _appState.Log($"[Blazor WPF-SqLite] Migration von v{currentDbVersion} zu v{targetVersion} gestartet.");

                        for (int v = currentDbVersion + 1; v <= targetVersion; v++)
                        {
                            if (SqLiteModel.Migrations.TryGetValue(v, out var migrationScript))
                            {
                                await ExecuteBatchAsync(migrationScript);
                            }
                        }
                        await SetVersionAsync(targetVersion);
                    }
                }

                ScalarModel allTablesExist = await CheckAllTablesExist(tableList);
                if (!allTablesExist.out_value_bool)
                {
                    // Wir nutzen string.Join, damit die Fehlermeldung die Namen anzeigt statt "System.String[]"
                    string err = $"[Blazor WASM-SqLite] KRITISCH: Erwartet: {AllTables.Length} Tabellen, Gefunden: {allTablesExist?.out_value_int ?? 0}. Tabellenliste: {tableList}";

                    await _appState.Error($"SQL-INIT-FAIL: {err}");
                    //throw new Exception($"SQL-INIT-FAIL: {err}");
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = err
                    };
                }

                // Parameter prüfen

                // Finaler Test auf AppParameter Tabelle
                var resultParams = await this.Scalar(new Dictionary<string, string> {
                    { "@Case_", "ExistsStoreUrl>>AppParameter" }
                });

                if (resultParams == null || !string.IsNullOrEmpty(resultParams.out_err))
                {
                    //throw new Exception("Table AppParameter not found or access error in Blazor WPF-SqLite.");
                    return new ScalarModel
                    {
                        out_value_bool = false,
                        out_err = "Table AppParameter not found or access error in Blazor WPF-SqLite."
                    };
                }

                await _appState.Log("[Blazor WPF-SqLite] Initialization and Integrity Check successful.");

                return new ScalarModel { out_value_bool = true };
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

                if(checkResult != null)
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

        /// <summary>
        /// Führt mehrere SQL-Statements in einer Transaktion aus.
        /// Optimiertes Splitting analog zur Capacitor-Implementierung.
        /// </summary>
        private async Task ExecuteBatchAsync(string script)
        {
            var statements = script.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s + ";");

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var sql in statements)
                {
                    await connection.ExecuteAsync(sql, transaction: transaction);
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await _appState.Error($"[Blazor WPF-SqLite] Batch-Fehler: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Liest die interne SQLite-Version (user_version).
        /// </summary>
        private async Task<int> GetVersionAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            return await conn.ExecuteScalarAsync<int>("PRAGMA user_version;");
        }

        /// <summary>
        /// Schreibt die interne SQLite-Version (user_version).
        /// </summary>
        private async Task SetVersionAsync(int version)
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync($"PRAGMA user_version = {version};");
        }

        /// <summary>
        /// Erzeugt einen 10-stelligen SHA-256 Hash für den Dateinamen.
        /// Parität zur JavaScript Krypto-Implementierung.
        /// </summary>
        private string GenerateShortHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower().Substring(0, 10);
        }

        /// <summary>
        /// Zentrale Methode für alle SQL-Anfragen. Mappt C# Objekte/Dictionaries auf Dapper-Parameter.
        /// Diese Version bietet 100% Parität zur Capacitor-JS-Bridge (Status, Version, ClearAllData-Logik & iterative Batches).
        /// </summary>
        //protected override async Task<ScalarModel> ExecuteRawSqlAsync(string? sql, object[]? parameters, QUERY_TYPE type)
        protected override async Task<ScalarModel> ExecuteRawSqlAsync(
            string? sql,
            IReadOnlyDictionary<string, object?>? parameters, // NEU: Dictionary statt object[]
            QUERY_TYPE type)
        {
            try
            {
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

                
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // --- 1. SONDERFALL: getDatabaseStatus ---
                if (type == QUERY_TYPE.getDatabaseStatus)
                {
                    var tableCheck = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name='AppParameter';");

                    bool tableExists = !string.IsNullOrEmpty(tableCheck);
                    int statusInt = (int)(tableExists ? DB_STATUS.READY : DB_STATUS.NEW);

                    await _appState.Log($"[WPF-SqLite] Status-Check: {(tableExists ? "READY" : "NEW")} (Tabelle 'AppParameter' {(tableExists ? "gefunden" : "fehlt")})");

                    return new ScalarModel
                    {
                        out_value_int = statusInt,
                        out_value_bool = true,
                        out_value_str = tableExists ? "READY" : "NEW"
                    };
                }

                // --- 2. SONDERFALL: getVersion ---
                if (type == QUERY_TYPE.getVersion)
                {
                    await _appState.Log($"[WPF-SqLite] Lese DB-Version für {DbName} (PRAGMA user_version)...");
                    int version = await connection.ExecuteScalarAsync<int>("PRAGMA user_version;");
                    await _appState.Log($"[WPF-SqLite] Aktuelle DB-Version von {DbName}: {version}");

                    return new ScalarModel { out_value_int = version, out_value_bool = true, out_value_str = version.ToString() };
                }
                
                // --- 3. SONDERFALL: setVersion ---
                if (type == QUERY_TYPE.setVersion)
                {
                    // Korrektur des Syntaxfehlers:
                    // Wir prüfen, ob das Dictionary Werte enthält und nehmen den ersten (Values.First())
                    int targetVersion = (parameters != null && parameters.Any())
                        ? Convert.ToInt32(parameters.Values.First())
                        : 0;

                    await _appState.Log($"[WPF-SqLite] Setze DB-Version auf: {targetVersion}");

                    // PRAGMA akzeptiert keine @Parameter, daher ist die String-Interpolation hier korrekt
                    await connection.ExecuteAsync($"PRAGMA user_version = {targetVersion};");

                    int currentVersion = await connection.ExecuteScalarAsync<int>("PRAGMA user_version;");

                    if (currentVersion == targetVersion)
                    {
                        await _appState.Log($"[WPF-SqLite] Version erfolgreich verifiziert: {currentVersion}");
                        return new ScalarModel { out_value_bool = true, out_value_int = currentVersion, out_value_str = "true" };
                    }
                    else
                    {
                        string msg = $"Version mismatch: Soll {targetVersion}, Ist {currentVersion}";
                        await _appState.Error($"[WPF-SqLite] {msg}");
                        return new ScalarModel { out_value_bool = false, out_err = msg };
                    }
                }

                // --- 4. SONDERFALL: clearAllData (Parität zur JS-Logik) ---
                if (type == QUERY_TYPE.clearAllData || type == QUERY_TYPE.deleteDatabase)
                {
                    await _appState.Log($"[WPF-SqLite] Starte vollständige Datenreinigung auf: {DbName}...");

                    // A: Foreign Keys OFF
                    await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

                    try
                    {
                        // B: Alle Tabellen abfragen
                        var tables = (await connection.QueryAsync<string>(
                            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';")).ToList();

                        foreach (var tableName in tables)
                        {
                            // SYSTEMTABELLEN STRIKT IGNORIEREN (analog zu JS)
                            if (tableName == "sync_table" || tableName == "_cap_sqlite_metadata_" || tableName == "sqlite_sequence")
                            {
                                await _appState.Log($"[WPF-SqLite] Überspringe Systemtabelle: {tableName}");
                                continue;
                            }

                            await _appState.Log($"[WPF-SqLite] Lösche Inhalt von Tabelle: {tableName}");
                            await connection.ExecuteAsync($"DELETE FROM {tableName};");
                        }

                        // C: Auto-Increment zurücksetzen
                        try { await connection.ExecuteAsync("DELETE FROM sqlite_sequence;"); } catch { /* ignore if not exists */ }

                        // D: Speicher optimieren
                        await connection.ExecuteAsync("VACUUM;");

                        await _appState.Log($"[WPF-SqLite] Datenbank {DbName} erfolgreich geleert und optimiert.");
                        if(type != QUERY_TYPE.deleteDatabase)
                            return new ScalarModel { out_value_bool = true, out_value_str = "true" };
                    }
                    finally
                    {
                        // E: Foreign Keys IMMER wieder ein (Safety)
                        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
                    }
                }

                // --- 5. SONDERFALL: dropAllTables (Erweiterung für Schema-Reset) ---
                if (type == QUERY_TYPE.dropAllTables || type == QUERY_TYPE.deleteDatabase)
                {
                    await _appState.Log($"[WPF-SqLite] Starte vollständiges Löschen aller Tabellen in {DbName} (DROP ALL TABLES)...");

                    // A: Foreign Keys OFF
                    await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

                    try
                    {
                        // B: Alle benutzerdefinierten Tabellennamen abfragen
                        var tables = (await connection.QueryAsync<string>(
                            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';")).ToList();

                        foreach (var tableName in tables)
                        {
                            // SYSTEMTABELLEN STRIKT SCHÜTZEN
                            if (tableName == "sync_table" || tableName == "_cap_sqlite_metadata_" || tableName == "sqlite_sequence")
                            {
                                await _appState.Log($"[WPF-SqLite] Überspringe geschützte Systemtabelle: {tableName}");
                                continue;
                            }

                            await _appState.Log($"[WPF-SqLite] Lösche Tabelle (DROP): {tableName}");
                            await connection.ExecuteAsync($"DROP TABLE IF EXISTS {tableName};");
                        }

                        // C: Version zurücksetzen auf 0
                        await _appState.Log($"[WPF-SqLite] Resetze DB-Version auf 0...");
                        await connection.ExecuteAsync("PRAGMA user_version = 0;");

                        // D: Speicher optimieren (VACUUM)
                        await connection.ExecuteAsync("VACUUM;");

                        await _appState.Log($"[WPF-SqLite] Alle Tabellen in {DbName} erfolgreich entfernt und DB optimiert.");
                        if (type != QUERY_TYPE.deleteDatabase)
                            return new ScalarModel { out_value_bool = true, out_value_str = "true" };
                    }
                    finally
                    {
                        // E: Foreign Keys wieder einschalten
                        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
                    }
                }

                // --- 6. SONDERFALL: deleteDatabase (Physisches Löschen) ---
                // Muss vor 'connection.OpenAsync()' stehen, damit die Datei nicht gesperrt ist.
                if (type == QUERY_TYPE.deleteDatabase)
                {
                    await _appState.Log($"[WPF-SqLite] START FULL PURGE: {DbName}", AppLogLevel.Warn);

                    try
                    {
                        // Connection schliessen
                        await connection.DisposeAsync();

                        // Connection Pools leeren, um alle Griffe auf die Datei zu lösen
                        SqliteConnection.ClearAllPools();

                        // Erzwinge die Freigabe der Handles
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        // Gib dem System einen Moment Zeit
                        await Task.Delay(500);

                        //string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DbName);
                        string dbPath = _appState.LocalSqLiteDbPath;
                        bool deleted = false;

                        if (File.Exists(dbPath))
                        {
                            await _appState.Log($"[WPF-SqLite] Deleting physical file and metadata for: {DbName}", AppLogLevel.Warn);
                            //File.Delete(dbPath);

                            // 3. Robustes Löschen mit Retry
                            for (int i = 0; i < 3; i++)
                            {
                                try
                                {
                                    File.Delete(dbPath);
                                    // Journal-Dateien ebenfalls entfernen
                                    if (File.Exists(dbPath + "-shm")) File.Delete(dbPath + "-shm");
                                    if (File.Exists(dbPath + "-wal")) File.Delete(dbPath + "-wal");
                                    deleted = true;
                                    break;
                                }
                                catch (IOException) { await Task.Delay(500); }
                            }

                            //// Auch Journal-Dateien entfernen falls vorhanden
                            //if (File.Exists(dbPath + "-shm")) File.Delete(dbPath + "-shm");
                            //if (File.Exists(dbPath + "-wal")) File.Delete(dbPath + "-wal");
                        }
                        else
                            deleted = true;

                        await _appState.Log("FULL PURGE SUCCESSFUL - State cleared.", AppLogLevel.Info);
                        return new ScalarModel { out_value_bool = deleted, out_value_str = (deleted ? "true" : "false") };
                    }
                    catch (Exception purgeEx)
                    {
                        await _appState.Error($"[WPF-SqLite] Kritischer Fehler beim Löschen der DB: {purgeEx.Message}");
                        return new ScalarModel { out_value_bool = false, out_err = "DELETE_FAILED: " + purgeEx.Message };
                    }
                }

                switch (type)
                {
                    case QUERY_TYPE.scalar:
                        // Ein einziger Wert wird erwartet (z.B. COUNT oder ein einzelnes Feld)
                        var rawScalar = await connection.ExecuteScalarAsync<object>(sql!, parameters);
                        return ToScalarModel(rawScalar);

                    case QUERY_TYPE.query:
                        // Gibt eine Liste von Objekten zurück (SELECT * FROM ...)
                        // Dapper mappt das Dictionary 'parameters' auf die @Platzhalter im SQL
                        var listResult = (await connection.QueryAsync<object>(sql!, parameters)).ToList();

                        await _appState.Log($"[WPF-SqLite] Query erfolgreich. Datensätze: {listResult.Count}");

                        return new ScalarModel
                        {
                            out_value_bool = true,
                            out_value_str = System.Text.Json.JsonSerializer.Serialize(listResult),
                            out_err = ""
                        };

                    case QUERY_TYPE.execute:
                        try
                        {
                            // UPDATE, INSERT, DELETE
                            int affectedRows = await connection.ExecuteAsync(sql!, parameters);

                            await _appState.Log($"[WPF-SqLite] EXECUTE erfolgreich. Betroffene Zeilen: {affectedRows}");

                            return new ScalarModel
                            {
                                out_value_bool = true,
                                out_value_str = "true",
                                out_value_int = affectedRows,
                                out_err = ""
                            };
                        }
                        catch (Exception ex)
                        {
                            await _appState.Error($"[WPF-SqLite] EXECUTE FAILED! SQL: {sql} | Error: {ex.Message}");
                            return new ScalarModel
                            {
                                out_value_bool = false,
                                out_value_str = "",
                                out_err = ex.Message ?? "SQL_EXEC_ERROR"
                            };
                        }

                    case QUERY_TYPE.executeBatch:
                        string[] statements = sql?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                        int totalAffected = 0;

                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                foreach (var stmt in statements)
                                {
                                    string cleanStmt = stmt.Trim();
                                    if (!string.IsNullOrEmpty(cleanStmt))
                                    {
                                        // Bei Batches (z.B. Tabellenerstellung) nutzen wir meist keine Parameter
                                        totalAffected += await connection.ExecuteAsync(cleanStmt, null, transaction);
                                    }
                                }

                                transaction.Commit();
                                await _appState.Log($"[WPF-SqLite] BATCH erfolgreich. Zeilen gesamt: {totalAffected}");

                                return new ScalarModel
                                {
                                    out_value_bool = true,
                                    out_value_int = totalAffected,
                                    out_value_str = "true",
                                    out_err = ""
                                };
                            }
                            catch (Exception batchEx)
                            {
                                try { transaction.Rollback(); } catch { }
                                string errorMsg = $"BATCH_ERROR: {batchEx.Message}";
                                await _appState.Error($"[WPF-SqLite] {errorMsg}");
                                return new ScalarModel { out_value_bool = false, out_err = errorMsg, out_value_str = "" };
                            }
                        }

                    default:
                        return new ScalarModel { out_err = $"QUERY_TYPE {type} nicht implementiert", out_value_bool = false };
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF-SqLite] ExecuteRawSqlAsync Fehler bei {type}: {ex.Message}");

                bool isStatusOrVersion = type == QUERY_TYPE.getDatabaseStatus || type == QUERY_TYPE.getVersion ||
                                         type == QUERY_TYPE.setVersion || type == QUERY_TYPE.clearAllData ||
                                         type == QUERY_TYPE.dropAllTables || type == QUERY_TYPE.deleteDatabase;

                return new ScalarModel
                {
                    out_value_int = isStatusOrVersion ? (int)DB_STATUS.ERROR : 0,
                    out_value_bool = false,
                    out_err = ex.Message
                };
            }
        }

        protected override async Task<ReadModel<T>> ExecuteReadAsync<T>(
            string sql,
            IReadOnlyDictionary<string, object?>? parameters) // Signatur angepasst
        {
            var result = new ReadModel<T>();
            try
            {
                // --- NEU: SICHERUNGSSCHRANKE ---
                // Wenn das SQL leer ist, dürfen wir auf keinen Fall die JS-Bridge aufrufen.
                if (string.IsNullOrWhiteSpace(sql))
                {
                    // Wir loggen den Fehler detailliert, damit die Suche nicht "ewig" dauert.
                    string errorContext = $"[WASM-SQLite] KRITISCHER FEHLER: SQL-String ist leer! Parameter-Count: {parameters?.Count ?? 0}";

                    await _appState.Error(errorContext);

                    result.out_err = "SQL_COMMAND_MISSING_IN_SHARED_CODE"; // Klarer Hinweis auf den fehlenden case
                }
                // -------------------------------

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // DIREKTES MAPPING: 
                // Dapper nimmt das IReadOnlyDictionary und ordnet die Keys den @Parametern im SQL zu.
                // Falls parameters null ist, führt Dapper das SQL ohne Parameter aus.
                var list = (await connection.QueryAsync<T>(sql, parameters)).ToList();

                // Resultat befüllen
                result.out_list = list!;
                result.out_list_local = list!; // Für Kompatibilität beibehalten

                await _appState.Log($"[WPF-SqLite] ExecuteReadAsync erfolgreich. Datensätze: {list.Count}");
            }
            catch (Exception ex)
            {
                string err = $"[WPF-SqLite] ExecuteReadAsync Fehler: {ex.Message}";
                await _appState.Error(err);
                result.out_err = err;
            }

            return result;
        }
    }

    public class SqliteBlobToStringHandler : SqlMapper.TypeHandler<string>
    {
        // Diese Methode wird gerufen, wenn Daten AUS der DB gelesen werden
        public override string? Parse(object value)
        {
            if (value is byte[] bytes)
            {
                return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
            }
            return value?.ToString();
        }

        // Diese Methode wird gerufen, wenn Daten IN die DB geschrieben werden
        // WICHTIG: Die Signatur muss exakt so aussehen (string? value)
        public override void SetValue(System.Data.IDbDataParameter parameter, string? value)
        {
            parameter.Value = (object?)value ?? DBNull.Value;
        }
    }
}