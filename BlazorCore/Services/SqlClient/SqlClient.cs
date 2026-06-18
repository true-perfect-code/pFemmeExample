using Microsoft.Data.SqlClient;
using BlazorCore.Services.AppState;
using BlazorCore.Services.GlobalState;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace BlazorCore.Services.SqlClient
{
    public class SqlClient : ISqlClientBase
    {
        private IGlobalStateBase _globalState = null!;
        //private readonly IAppStateBase _appState;

        /// <summary>
        /// Verbindungsinformationen zum MSSQL Server
        /// </summary>
        //public string connectionString { get; } = (PlatformHelper.GetConnectionString().Success
        //    && PlatformHelper.GetConnectionString().ValString != null
        //    ? PlatformHelper.GetConnectionString().ValString! : "");
        //public string connectionString { get; set; } = string.Empty;

        /// <summary>
        /// Information darüber, ob eine Verbindung zum MSSQL Server existiert
        /// </summary>
        public bool isConnected { get; set; } = false;

        //public bool isMapApplTbls = false;

        //public SqlClient(string connString)
        //{
        //    connectionString = connString;

        //    if (!string.IsNullOrEmpty(connectionString))
        //    {
        //        SqlConnection con = new SqlConnection(connectionString);

        //        if (con.State == ConnectionState.Closed)
        //        {
        //            con.Open();
        //            isConnected = true;
        //            con.Close();
        //        }
        //    }
        //}

        //        public SqlClient(IGlobalStateBase globalState)
        //        {
        //            _globalState = globalState;

        //            connectionString = string.Empty;

        //            // SqlClient ConnectionString ermitteln
        //            string basedir = AppContext.BaseDirectory;
        //            if (!string.IsNullOrEmpty(basedir))
        //            {
        //                DirectoryInfo? parentbasedir = Directory.GetParent(basedir);
        //                if (parentbasedir != null && parentbasedir.Parent != null)
        //                {
        //                    string connectionstringfolder = Path.Combine(parentbasedir.Parent.FullName, _globalState.ConfigGeneral.ConnectionsServerFolder);
        //                    string connectionstringpath = Path.Combine(connectionstringfolder, $"{_globalState.ConfigGeneral.ApplicationName}{_globalState.ConfigGeneral.FileExtensionJson}");
        //                    if (File.Exists(connectionstringpath))
        //                    {
        //                        // Json Connection Datei auslesen
        //                        var ConnectionString = System.Text.Json.JsonSerializer.Deserialize(
        //                            File.ReadAllText(connectionstringpath),
        //                            JsonContext.Default.ConnectionStringModel // <-- Typ-Resolver
        //                        )!;
        //                        if (ConnectionString != null)
        //                        {

        //#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
        //                            using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
        //                            {
        //                                // Beim ersten Start soll Schlüssel zuerst ermittelt werden, um ihn dann in den json-File zu speichern (perfect[PROJECT].json)
        //                                // Die nächste Zeile aktivieren und App erneut starten:
        //                                //string key = aes.Encrypt([HIER PASSWORT AUS dbs.md FIRESTORMCLOUD]);
        //                                // Ermittelten 'key' dan in das json-file perfect[PROJECT].json unter '"Password":' speichern.
        //                                // Die Zeile 'string key = aes.Encrypt(...' wieder kommentieren und App erneut starten. Prüfen, ob Passwort entschlüsselt wird.
        //                                ConnectionString.Password = aes.Decrypt(ConnectionString.Password!);
        //                            }
        //#pragma warning restore CA1416

        //                            connectionString = (_globalState.GenerateConnectionString(ConnectionString).Success
        //                               && _globalState.GenerateConnectionString(ConnectionString).ValString != null
        //                               ? _globalState.GenerateConnectionString(ConnectionString).ValString!
        //                               : $@"C:\inetpub\vhosts\true-perfect-code.ch\_Connections\{_globalState.ConfigGeneral.ApplicationName}.json");
        //                        }
        //                    }
        //                }
        //            }

        //            if (!string.IsNullOrEmpty(connectionString))
        //            {
        //                SqlConnection con = new SqlConnection(connectionString);

        //                if (con.State == ConnectionState.Closed)
        //                {
        //                    con.Open();
        //                    isConnected = true;
        //                    con.Close();
        //                }
        //            }
        //        }

        public SqlClient(IGlobalStateBase globalState)
        {
            _globalState = globalState;

            //if (!string.IsNullOrEmpty(_globalState.ConfigGeneral.ApplicationName))
            //    InitializeAsync(globalState);
        }

        public async Task<BlazorCore.Services.SqlClient.ScalarModel> InitializeAsync(GlobalState.IGlobalStateBase globalState)
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            _globalState = globalState;

            //connectionString = string.Empty;

            // SqlClient ConnectionString ermitteln
            string basedir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(basedir))
            {
                DirectoryInfo? parentbasedir = Directory.GetParent(basedir);
                if (parentbasedir != null && parentbasedir.Parent != null)
                {
                    string connectionstringfolder = Path.Combine(parentbasedir.Parent.FullName, _globalState.ConfigGeneral.ConnectionsServerFolder);
                    string connectionstringpath = Path.Combine(connectionstringfolder, $"{_globalState.ConfigGeneral.ApplicationName}{_globalState.ConfigGeneral.FileExtensionJson}");
                    if (File.Exists(connectionstringpath))
                    {
                        // Json Connection Datei auslesen
                        var ConnectionString = System.Text.Json.JsonSerializer.Deserialize(
                            File.ReadAllText(connectionstringpath),
                            JsonContext.Default.ConnectionStringModel // <-- Typ-Resolver
                        )!;
                        if (ConnectionString != null)
                        {

#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
                            using (BlazorCore.Services.ServerShared.Security aes = new(_globalState.ConfigGeneral.ApplicationName, _globalState.ConfigGeneral.TableSchema))
                            {
                                // Beim ersten Start soll Schlüssel zuerst ermittelt werden, um ihn dann in den json-File zu speichern (perfect[PROJECT].json)
                                // Die nächste Zeile aktivieren und App erneut starten:
                                //string key = aes.Encrypt([HIER PASSWORT AUS dbs.md FIRESTORMCLOUD]);
                                // Ermittelten 'key' dan in das json-file perfect[PROJECT].json unter '"Password":' speichern.
                                // Die Zeile 'string key = aes.Encrypt(...' wieder kommentieren und App erneut starten. Prüfen, ob Passwort entschlüsselt wird.
                                ConnectionString.Password = aes.Decrypt(ConnectionString.Password!);
                            }
#pragma warning restore CA1416

                            _globalState.ConfigGeneral.ConnectionString = (_globalState.GenerateConnectionString(ConnectionString).Success
                               && _globalState.GenerateConnectionString(ConnectionString).ValString != null
                               ? _globalState.GenerateConnectionString(ConnectionString).ValString!
                               : $@"C:\inetpub\vhosts\true-perfect-code.ch\_Connections\{_globalState.ConfigGeneral.ApplicationName}.json");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(_globalState.ConfigGeneral.ConnectionString))
            {
                SqlConnection con = new SqlConnection(_globalState.ConfigGeneral.ConnectionString);

                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                    isConnected = true;
                    con.Close();
                }
            }

            return result;
        }

        public async Task MapApplTbls()
        {
            if (_globalState.Catalog.TablesMSSQL == null || _globalState.Catalog.ParaMSSQL == null) return;

            //string err = "";
            try
            {
                ReaderModel<TableDefinition> RES = new();
                foreach (var table in _globalState.Catalog.TablesMSSQL)
                {
                    Dictionary<string, string> db_para = new()
                    {
                        { "@Case_", "SelectTableninformationen" },
                        { "@TableName", table.Trim() },
                    };
                    RES = await Reader<TableDefinition>(db_para);

                    //_globalState.Tables.Add(table.Trim());

                    if (RES.out_list != null && RES.out_list.Count > 0)
                    {
                        foreach (var item in RES.out_list)
                        {
                            string normalizedColumnName = $"@{item!.COLUMN_NAME}";
                            try
                            {
                                if (item != null && !String.IsNullOrEmpty(item.COLUMN_NAME))
                                {
                                    if (!_globalState.Catalog.ParaMSSQL.Where(x => x.COLUMN_NAME == normalizedColumnName).Any())
                                    {
                                        if (item.DATA_TYPE.ToLower() == "varbinary")
                                        {
                                            item.DATA_TYPE = "nvarchar";
                                            item.COL_SIZE = -1;
                                        }

                                        _globalState.Catalog.ParaMSSQL.Add(new TableDefinition()
                                        {
                                            COLUMN_NAME = normalizedColumnName,
                                            SP_PARAMETER_NAME = normalizedColumnName,
                                            DATA_TYPE = item.DATA_TYPE,
                                            COL_SIZE = item.COL_SIZE
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //err = ex.Message;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //err += (String.IsNullOrEmpty(err) ? "" : "\r\n") + "Error: " + ex.Message + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            //isMapApplTbls = true;

            //return err;
        }


        /// <summary>
        /// Methode, um einen Scalar-Wert aus einer gespeicherten Prozedur abzufragen
        /// </summary>
        /// <param name="_db_para">SQL Parameter für die Abfrage (EXEC)</param>
        /// <returns>Rückgabewert ist ein Scalar Objekt</returns>
        //public async Task<ScalarModel> Scalar(Dictionary<string, (string mssql, string realm)> _db_para)
        public async Task<ScalarModel> Scalar(Dictionary<string, string> _db_para)
        {
            ScalarModel RES = new();

            try
            {
                ReaderModel<DummyModel> READ = await spExecute<DummyModel>(_db_para, ExecuteMode.ExecuteScalar);

                RES.out_err = READ.out_err;
                RES.out_value_str = READ.out_value_str;
                RES.out_value_int = READ.out_value_int;
                RES.out_value_dbl = READ.out_value_dbl;
                RES.out_value_long = READ.out_value_long;
                RES.out_value_bool = READ.out_value_bool;
            }
            catch (Exception ex)
            {
                string exec = (_globalState.ConfigGeneral.ShowConnectionStringByError ? CreateExec(_db_para) : "");
                RES.out_err = (String.IsNullOrEmpty(RES.out_err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message) + ", Info: " + exec + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            return RES;
        }

        /// <summary>
        /// Methode, um eine Reader gespeicherte Prozedur auszuführen, die Json als Ergebnis liefert
        /// </summary>
        /// <param name="_execCode">SQL Code für die Ausführung einer gespeicherten Prozedur (EXEC)</param>
        /// <returns>Rückgabewert ist ein Json Reader Objekt</returns>
        //public async Task<ReaderDynamicModel> Reader(Dictionary<string, (string mssql, string realm)> _db_para)
        public async Task<ReaderDynamicModel> Reader(Dictionary<string, string> _db_para)
        {
            ReaderDynamicModel RES = new();

            try
            {
                ReaderModel<DummyModel> READ = await spExecute<DummyModel>(_db_para, ExecuteMode.ExecuteReaderJson);
                RES.out_err = READ.out_err;
                RES.out_json = READ.out_json;
            }
            catch (Exception ex)
            {
                string exec = (_globalState.ConfigGeneral.ShowConnectionStringByError ? CreateExec(_db_para) : "");
                RES.out_err = (String.IsNullOrEmpty(RES.out_err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message) + ", Info: " + exec + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            return RES;
        }

        /// <summary>
        /// Methode, um eine Reader gespeicherte Prozedur auszuführen
        /// </summary>
        /// <param name="_db_para">SQL Parameter für die Abfrage (EXEC)</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
        //public async Task<ReaderModel<T>> Reader<T>(Dictionary<string, (string mssql, string realm)> _db_para)
        public async Task<ReaderModel<T>> Reader<T>(Dictionary<string, string> _db_para) where T : new()
        {
            ReaderModel<T> RES = new();

            try
            {
                RES = await spExecute<T>(_db_para, ExecuteMode.ExecuteReader);
            }
            catch (Exception ex)
            {
                string exec = (_globalState.ConfigGeneral.ShowConnectionStringByError ? CreateExec(_db_para) : "");
                RES.out_err = (String.IsNullOrEmpty(RES.out_err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message) + ", Info: " + exec + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            return RES;
        }

        /// <summary>
        /// Methode, um einen Scalar-Wert aus einer gespeicherten Prozedur abzufragen
        /// </summary>
        /// <param name="_db_para">SQL Code für die Ausführung einer gespeicherten Prozedur (EXEC)</param>
        /// <returns>Rückgabewert ist ein Scalar Objekt</returns>
        //public async Task<ScalarModel> Bytes(Dictionary<string, (string mssql, string realm)> _db_para)
        public async Task<ScalarModel> Bytes(Dictionary<string, string> _db_para)
        {
            ScalarModel RES = new();

            try
            {
                ReaderModel<DummyModel> READ = await spExecute<DummyModel>(_db_para, ExecuteMode.ExecuteByte);

                RES.out_err = READ.out_err;
                RES.out_bytes = READ.out_bytes;
            }
            catch (Exception ex)
            {
                string exec = (_globalState.ConfigGeneral.ShowConnectionStringByError ? CreateExec(_db_para) : "");
                RES.out_err = (String.IsNullOrEmpty(RES.out_err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message) + ", Info: " + exec + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            return RES;
        }

        /// <summary>
        /// Methode, um eine Aktualisierungsabfrage in der gespeicherten Prozedur auszuführen
        /// </summary>
        /// <param name="_db_para">SQL Parameter für die Abfrage (EXEC)</param>
        /// <returns>Rückgabewert ist ein Scalar Objekt</returns>
        //public async Task<ScalarModel> NonQuery(Dictionary<string, (string mssql, string realm)> _db_para)
        public async Task<ScalarModel> NonQuery(Dictionary<string, string> _db_para)
        {
            ScalarModel RES = new();

            try
            {
                ReaderModel<DummyModel> NONQUERY = await spExecute<DummyModel>(_db_para, ExecuteMode.ExecuteNonQuery);

                RES.out_err = NONQUERY.out_err;
                RES.out_value_str = NONQUERY.out_value_str;
                RES.out_value_int = NONQUERY.out_value_int;
                RES.out_value_dbl = NONQUERY.out_value_dbl;
                RES.out_value_bool = NONQUERY.out_value_bool;
            }
            catch (Exception ex)
            {
                string exec = (_globalState.ConfigGeneral.ShowConnectionStringByError ? CreateExec(_db_para) : "");
                RES.out_err = (String.IsNullOrEmpty(RES.out_err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message) + ", Info: " + exec + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            return RES;
        }

        /// <summary>
        /// Methode, um Listendaten aus einer gespeicherten Prozedur abzufragen
        /// </summary>
        /// <param name="_db_para">SQL Parameter für die Abfrage (EXEC)</param>
        /// <param name="_mode">Mode für die Ausführung (Reader, Scalar oder NonQuery)</param>
        /// <returns>Rückgabewert ist ein Reader Objekt</returns>
        //public async Task<ReaderModel<T>> spExecute<T>(Dictionary<string, (string mssql, string realm)> _db_para, ExecuteMode _mode, string _sp = ".Crud")
        public async Task<ReaderModel<T>> spExecute<T>(Dictionary<string, string> _db_para, ExecuteMode _mode, string _sp = ".Crud") where T : new()
        {
            ReaderModel<T> RES = new();
            RES.out_list = new();
            try
            {
                SqlCommand cmd;

                //string exec = Appl.CreateExec(_db_para);

                using (SqlConnection con = new SqlConnection(_globalState.ConfigGeneral.ConnectionString))
                {
                    if (con.State == ConnectionState.Closed)
                        con.Open();

                    //// MSSQL Tabellen mappen
                    //if(!isMapApplTbls)
                    //    await MapApplTbls();

                    // Gespeicherte Prozedur hiunzufügen
                    string spname = _globalState.ConfigGeneral.TableSchema.Trim() + _sp;
                    cmd = new SqlCommand(spname, con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    // Input Parameter hinzufügen
                    RES.out_err = GetPara(_db_para, ref cmd);

                    // Output parameter hinzufügen
                    SqlParameter prm_output = new SqlParameter();
                    prm_output.ParameterName = "@OUTPUT_RES";
                    prm_output.SqlDbType = SqlDbType.VarChar;
                    prm_output.Size = 1000;
                    prm_output.Direction = ParameterDirection.Output;
                    //cmd.Parameters.Add(prm_output);
                    if (!cmd.Parameters.Contains(prm_output.ParameterName))
                        cmd.Parameters.Add(prm_output);

                    //string? output = "";

                    switch (_mode)
                    {
                        //case ExecuteMode.ExecuteReader:
                        //    SqlDataAdapter da = await Task.FromResult(new SqlDataAdapter(cmd));
                        //    DataTable tbl = new DataTable("tblList");
                        //    da.Fill(tbl);

                        //    if (con.State == ConnectionState.Open)
                        //        con.Close();

                        //    // ...und in die Model-Klasse überführen
                        //    foreach (DataRow row in tbl.Rows)
                        //    {
                        //        T? item = GetItem<T>(row);
                        //        if (item != null)
                        //            RES.out_list.Add(item);
                        //    }
                        //    if (RES.out_list.Count > 0)
                        //        RES.out_data = RES.out_list.FirstOrDefault();

                        //    break;
                        case ExecuteMode.ExecuteReader:
                            // HINWEIS: Es ist eine gute Praxis, SqlCommand und SqlConnection 
                            // immer in 'using' oder 'using declaration' Blöcken zu kapseln.

                            // 1. Daten in DataTable laden
                            SqlDataAdapter da = await Task.FromResult(new SqlDataAdapter(cmd));
                            DataTable tbl = new DataTable("tblList");
                            da.Fill(tbl);

                            // 2. Verbindungshandhabung (Optional: Kann entfernt werden, wenn 'using' verwendet wird)
                            // Wenn con.State != ConnectionState.Closed, war die Verbindung bereits offen.
                            // In diesem Fall sollte sie später durch den Aufrufer geschlossen werden. 
                            // Der SqlDataAdapter schließt sie nur, wenn er sie geöffnet hat.
                            if (con.State == ConnectionState.Open)
                                con.Close();

                            // 3. Konvertierung in Liste (über die neue, optimierte Methode)
                            // Angenommen, diese Methode ruft GetItemOptimized<T> auf
                            RES.out_list = ConvertDataTable<T>(tbl)!;

                            // 4. Erstes Element als out_data zuweisen
                            if (RES.out_list.Count > 0)
                                RES.out_data = RES.out_list.FirstOrDefault();

                            break;

                        case ExecuteMode.ExecuteReaderJson:
                            SqlDataAdapter dajson = await Task.FromResult(new SqlDataAdapter(cmd));
                            DataTable tbljson = new DataTable("tblJson");
                            dajson.Fill(tbljson);

                            RES.out_json = DataTableToJson(tbljson);

                            break;

                        //case ExecuteMode.ExecuteScalar:
                        //    var res = await cmd.ExecuteScalarAsync();

                        //    output = cmd.Parameters["@OUTPUT_RES"].Value.ToString();
                        //    if (output != null)
                        //        RES.out_err = output;

                        //    if (res != null)
                        //    {
                        //        RES.out_value_str = res.ToString();
                        //        int tmpInt = 0;
                        //        int.TryParse(RES.out_value_str, out tmpInt);
                        //        RES.out_value_int = tmpInt;
                        //        double tmpDbl = 0.0;
                        //        double.TryParse(RES.out_value_str, out tmpDbl);
                        //        RES.out_value_dbl = tmpDbl;
                        //        long tmpLong = 0;
                        //        long.TryParse(RES.out_value_str, out tmpLong);
                        //        RES.out_value_long = tmpLong;
                        //        bool tmpBool = false;
                        //        bool.TryParse(RES.out_value_str, out tmpBool);
                        //        if (!tmpBool)
                        //            tmpBool = (RES.out_value_str == "1" ? true : false);
                        //        RES.out_value_bool = tmpBool;
                        //    }

                        //    break;
                        case ExecuteMode.ExecuteScalar:

                            var scalar = await cmd.ExecuteScalarAsync();

                            // OUTPUT-Parameter sicher lesen
                            var outputSql = cmd.Parameters["@OUTPUT_RES"].Value;
                            RES.out_err = Convert.ToString(outputSql) ?? string.Empty;

                            if (scalar != null)
                            {
                                // String-Repräsentation trimming- & AOT-sicher erzeugen
                                var valueStr = Convert.ToString(scalar) ?? string.Empty;
                                RES.out_value_str = valueStr;

                                // int
                                if (int.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
                                    RES.out_value_int = intValue;

                                // double
                                if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dblValue))
                                    RES.out_value_dbl = dblValue;

                                // long
                                if (long.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
                                    RES.out_value_long = longValue;

                                // bool (robust)
                                RES.out_value_bool = ParseBool(valueStr);
                            }

                            break;

                        //case ExecuteMode.ExecuteByte:
                        //    byte[]? bytes = null;
                        //    var dbbytes = await cmd.ExecuteScalarAsync();
                        //    var output = cmd.Parameters["@OUTPUT_RES"].Value.ToString();
                        //    if (dbbytes != null && dbbytes != DBNull.Value)
                        //    {
                        //        try
                        //        {
                        //            // Ergebnis in ein Byte-Array umwandeln
                        //            bytes = (byte[])dbbytes;
                        //        }
                        //        catch (InvalidCastException ex)
                        //        {
                        //            // Das Ergebnis konnte nicht in ein Byte-Array umgewandelt werden
                        //            // Behandeln Sie die Ausnahme hier
                        //            RES.out_err = ex.Message;
                        //        }
                        //    }

                        //    if (output != null)
                        //        RES.out_err = output;

                        //    RES.out_bytes = bytes;
                        //    //if (bytes != null)
                        //    //{
                        //    //    if(bytes.Length != 0)
                        //    //        RES.out_bytes = bytes;
                        //    //}

                        //    break;
                        case ExecuteMode.ExecuteByte:
                            byte[]? bytes = null;
                            var dbbytes = await cmd.ExecuteScalarAsync();

                            // OUTPUT-Parameter sicher lesen (Korrektur: Null-Handling)
                            var outputValueByte = cmd.Parameters["@OUTPUT_RES"].Value;
                            string outputByte;

                            if (outputValueByte == null || outputValueByte == DBNull.Value)
                            {
                                outputByte = string.Empty;
                            }
                            else
                            {
                                outputByte = Convert.ToString(outputValueByte) ?? string.Empty;
                            }

                            if (dbbytes != null && dbbytes != DBNull.Value)
                            {
                                try
                                {
                                    // Direkter Cast, AOT-sicher
                                    bytes = (byte[])dbbytes;
                                }
                                catch (InvalidCastException ex)
                                {
                                    // Fehlerbehandlung
                                    RES.out_err = ex.Message;
                                }
                            }

                            // Zuweisen des Output-Parameters als Fehler/Status
                            if (!string.IsNullOrEmpty(outputByte))
                                RES.out_err = outputByte;

                            RES.out_bytes = bytes;
                            break;

                        //case ExecuteMode.ExecuteNonQuery:
                        //    await cmd.ExecuteNonQueryAsync();
                        //    output = cmd.Parameters["@OUTPUT_RES"].Value.ToString();

                        //    if (output != null)
                        //    {
                        //        RES.out_value_str = output;
                        //        //int tmpInt = 0;
                        //        //int.TryParse(RES.out_value_str, out tmpInt);
                        //        //RES.out_value_int = tmpInt;
                        //        //double tmpDbl = 0;
                        //        //double.TryParse(RES.out_value_str, out tmpDbl);
                        //        //RES.out_value_dbl = tmpDbl;
                        //        //long tmpLong = 0;
                        //        //long.TryParse(RES.out_value_str, out tmpLong);
                        //        //RES.out_value_long = tmpLong;
                        //    }

                        //    break;
                        case ExecuteMode.ExecuteNonQuery:
                            await cmd.ExecuteNonQueryAsync();

                            // 1. Hole den Wert des Output-Parameters
                            var outputValue = cmd.Parameters["@OUTPUT_RES"].Value;

                            // 2. Prüfe sicher auf DBNull.Value und konvertiere den Wert
                            if (outputValue == null || outputValue == DBNull.Value)
                            {
                                RES.out_value_str = string.Empty;
                            }
                            else
                            {
                                // Sicherer Konvertierungsversuch (wie im ExecuteScalar-Abschnitt)
                                RES.out_value_str = Convert.ToString(outputValue) ?? string.Empty;
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                string exec = (_globalState.ConfigGeneral.ShowConnectionStringByError ? CreateExec(_db_para) : "");
                RES.out_err = (String.IsNullOrEmpty(RES.out_err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message) + ", Info: " + exec + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
            }

            return RES;
        }

        private bool ParseBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim().ToLowerInvariant();

            if (bool.TryParse(value, out var b))
                return b;

            return value switch
            {
                "1" => true,
                "0" => false,
                "yes" => true,
                "y" => true,
                "on" => true,
                "no" => false,
                "n" => false,
                "off" => false,
                _ => false
            };
        }

        public string DataTableToJson(DataTable dataTable)
        {
            var rows = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();

            foreach (DataRow dr in dataTable.Rows)
            {
                var row = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    var value = dr[col];

                    // Überprüfen, ob der Wert DBNull ist und in null umwandeln
                    if (value == DBNull.Value)
                    {
                        value = null;
                    }

                    row.Add(col.ColumnName, value!);
                }
                rows.Add(row);
            }

            var jsonSerializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                //IgnoreNullValues = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters = { new CustomNullConverter() }
            };

            return System.Text.Json.JsonSerializer.Serialize(rows, jsonSerializerOptions);

            //var typeInfo = DynamicReaderJsonContext.Default.ListDictionaryStringObject;

            //return System.Text.Json.JsonSerializer.Serialize(rows, typeInfo);
        }

        //private string GetPara(Dictionary<string, (string mssql, string realm)> _db_para, ref SqlCommand _cmd)
        //private string GetPara(Dictionary<string, string> _db_para, ref SqlCommand _cmd)
        //{
        //    string err = "";

        //    try
        //    {
        //        // Dictionary durchlaufen
        //        foreach (var kvp in _db_para)
        //        {
        //            // Case_ hinzufügen
        //            if (kvp.Key.ToLower() == "@case_")
        //            {
        //                try
        //                {
        //                    SqlParameter sppara = new SqlParameter();
        //                    sppara.ParameterName = "@Case_";
        //                    sppara.SqlDbType = SqlDbTyBlazorCore.VarChar;
        //                    sppara.Size = 2000;
        //                    sppara.Direction = ParameterDirection.Input;
        //                    //sppara.Value = Appl.ConvertStrPara<string>(kvp.Value.mssql!).Trim();
        //                    sppara.Value = _AotConverter.ConvertTo<string>(kvp.Value!).Trim();

        //                    //_cmd.Parameters.Add(sppara);
        //                    if (!_cmd.Parameters.Contains(sppara.ParameterName))
        //                        _cmd.Parameters.Add(sppara);

        //                    // Tabelle mappen (falls nötig)
        //                    //if (kvp.Value.mssql!.Trim().Contains(">>"))
        //                    if (kvp.Value!.Trim().Contains(">>"))
        //                    {
        //                        //string[] arrTbl = Appl.ConvertStrPara<string>(kvp.Value.mssql).Trim().Split(">>");
        //                        string[] arrTbl = _AotConverter.ConvertTo<string>(kvp.Value).Trim().Split(">>");
        //                        if (arrTbl.Length > 1)
        //                            err = MapTbls(arrTbl[1]).Result;
        //                        else
        //                            err = "cammot_parse exec case: " + kvp.Value.Trim(); ; //.mssql.Trim();
        //                    }
        //                }
        //                catch { }
        //            }
        //            else
        //            {
        //                // Prüfen, ob Parameter-Definition vorhanden
        //                if (_globalState.Parameters.Where(x => x.SP_PARAMETER_NAME.ToLower() == kvp.Key.Trim().ToLower()).Any())
        //                {
        //                    try
        //                    {
        //                        TableDefinition tbldef = _globalState.Parameters.Where(x => x.SP_PARAMETER_NAME.ToLower() == kvp.Key.Trim().ToLower()).FirstOrDefault()!;

        //                        if (tbldef != null)
        //                        {
        //                            SqlParameter sppara = new SqlParameter();
        //                            sppara.ParameterName = kvp.Key.Trim();

        //                            //if (kvp.Value.mssql != null)
        //                            if (kvp.Value != null)
        //                            {
        //                                sppara.SqlDbType = (SqlDbType)tbldef.DATA_ADOTYPE;
        //                                if ((SqlDbType)tbldef.DATA_ADOTYPE == SqlDbTyBlazorCore.VarBinary)
        //                                {
        //                                    //sppara.Size = Appl.ConvertStrPara<string>(kvp.Value.mssql).Trim().Length;
        //                                    //sppara.Value = Appl.StringToByteArray(Appl.ConvertStrPara<string>(kvp.Value.mssql.Trim()));
        //                                    sppara.Size = _AotConverter.ConvertTo<string>(kvp.Value).Trim().Length;
        //                                    sppara.Value = _globalState.StringToByteArray(_AotConverter.ConvertTo<string>(kvp.Value.Trim()));
        //                                }
        //                                else
        //                                {
        //                                    sppara.Size = tbldef.COL_SIZE;
        //                                    //sppara.Value = ConvertNETtoSQL(tbldef.NET_TYPE, Appl.ConvertStrPara<string>(kvp.Value.mssql).Trim());
        //                                    sppara.Value = ConvertNETtoSQL(tbldef.NET_TYPE, _AotConverter.ConvertTo<string>(kvp.Value).Trim());
        //                                }
        //                            }

        //                            sppara.Direction = ParameterDirection.Input;

        //                            //_cmd.Parameters.Add(sppara);
        //                            if (!_cmd.Parameters.Contains(sppara.ParameterName))
        //                                _cmd.Parameters.Add(sppara);
        //                            //cmd.Parameters[sp_para[0].Trim()].Value = sp_para[0].Trim().Replace("'", "");
        //                        }
        //                        else
        //                        {
        //                            err += kvp.Key + " is null in Sql.GetPara()";
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        //err = "Error[1] = " + ex.Message + ", ConnStr = " + ConnectionString.Replace(";Password=", ";Password=Fg940*!/6_GH") + ", StackTrace = " + ex.StackTrace;
        //                        err += GetConnectionStringForError(ex.Message);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        err = (String.IsNullOrEmpty(err) ? "" : "\r\n") + "Error: " + ex.Message + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
        //    }

        //    return err;
        //}
        //private string GetPara(Dictionary<string, string> _db_para, ref SqlCommand _cmd)
        //{
        //    string err = "";

        //    // OPTIMIERUNG 1: Erstelle eine schnelle Lookup-Tabelle für Parameterdefinitionen
        //    // Einmalig konvertieren, um O(N) Schleifen in O(1) Lookups zu verwandeln.
        //    var paramDefinitions = _globalState.Parameters.ToDictionary(
        //        p => p.SP_PARAMETER_NAME.Trim(),
        //        p => p
        //    );

        //    try
        //    {
        //        foreach (var kvp in _db_para)
        //        {
        //            string key = kvp.Key.Trim();
        //            //string value = kvp.Value.Trim();
        //            string value = (kvp.Value ?? string.Empty).Trim();

        //            // OPTIMIERUNG 2: Trenne Spezialfälle klar ab
        //            if (key == "@Case_")
        //            {
        //                // **Spezialbehandlung für @Case_ (könnte in eigene Methode ausgelagert werden)**
        //                // ... (Ihre Logik für @Case_ bleibt hier, aber vereinfacht)
        //                // HIER: Fügen Sie den Parameter hinzu und rufen MapTbls auf

        //                // OPTIMIERUNG 3: Vermeide unnötiges Contains
        //                if (_cmd.Parameters.Contains("@Case_"))
        //                    _cmd.Parameters.Remove("@Case_");

        //                SqlParameter sppara = new SqlParameter("@Case_", SqlDbTyBlazorCore.VarChar, 2000);
        //                sppara.Value = _AotConverter.ConvertTo<string>(value);
        //                _cmd.Parameters.Add(sppara);

        //                //if (value.Contains(">>"))
        //                //{
        //                //    // MapTbls synchron aufzurufen, ist riskant. Besser: Asynchron behandeln oder sicherstellen,
        //                //    // dass MapTbls blockierungsfrei implementiert ist (was bei .Result schwierig ist).
        //                //    string[] arrTbl = _AotConverter.ConvertTo<string>(value).Split(">>");
        //                //    err = MapTbls(arrTbl.Length > 1 ? arrTbl[1] : "").Result;
        //                //}
        //            }
        //            // Behandle alle anderen definierten Parameter
        //            else if (paramDefinitions.TryGetValue(key, out TableDefinition? tbldef))
        //            {
        //                // Hier brauchen wir KEINE LINQ-Abfrage mehr! Schneller O(1) Lookup.
        //                // OPTIMIERUNG 4: Null-Check und Parametrisierung vereinfachen
        //                if (tbldef != null) // Sollte dank TryGetValue nicht null sein, aber zur Sicherheit
        //                {
        //                    // OPTIMIERUNG 5: Wiederholte Überprüfung vermeiden
        //                    if (_cmd.Parameters.Contains(kvp.Key.Trim()))
        //                        _cmd.Parameters.Remove(kvp.Key.Trim());

        //                    SqlParameter sppara = new SqlParameter();
        //                    sppara.ParameterName = kvp.Key.Trim();
        //                    sppara.Direction = ParameterDirection.Input;

        //                    // Typzuweisung (wie in Ihrem Code)
        //                    sppara.SqlDbType = (SqlDbType)tbldef.DATA_ADOTYPE;

        //                    if ((SqlDbType)tbldef.DATA_ADOTYPE == SqlDbTyBlazorCore.VarBinary)
        //                    {
        //                        // ... Ihre Byte-Konvertierungslogik ...
        //                        // Hier muss die Größe VOR der Zuweisung des Werts zugewiesen werden.
        //                        byte[] byteValue = _globalState.StringToByteArray(_AotConverter.ConvertTo<string>(value));
        //                        sppara.Size = byteValue.Length;
        //                        sppara.Value = byteValue;
        //                    }
        //                    else
        //                    {
        //                        sppara.Size = tbldef.COL_SIZE;
        //                        //sppara.Value = ConvertNETtoSQL(tbldef.NET_TYPE, _AotConverter.ConvertTo<string>(value));
        //                        sppara.Value = ConvertNETtoSQL(tbldef.NET_TYPE, value);
        //                    }

        //                    _cmd.Parameters.Add(sppara);
        //                }
        //            }
        //            // else: Nicht definierte Parameter werden ignoriert (korrektes Verhalten)
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Fehlerbehandlung
        //        err = (String.IsNullOrEmpty(err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message);
        //    }

        //    return err;
        //}
        private string GetPara(Dictionary<string, string> _db_para, ref SqlCommand _cmd)
        {
            string err = "";

            if (_globalState.Catalog.ParaMSSQL == null) return "_appState.Catalog.ParaMSSQL == null";


            // OPTIMIERUNG 1: Erstelle eine schnelle Lookup-Tabelle für Parameterdefinitionen
            var paramDefinitions = _globalState.Catalog.ParaMSSQL.ToDictionary(
                p => p.SP_PARAMETER_NAME.Trim(),
                p => p
            );

            try
            {
                foreach (var kvp in _db_para)
                {
                    string key = kvp.Key.Trim();

                    // --- FIX START: Null-Safe Handling ---
                    // Wir prüfen, ob der Wert im Dictionary null ist
                    bool isActuallyNull = (kvp.Value == null);
                    // Falls null, nehmen wir Empty für Trim(), merken uns aber den Status
                    string value = (kvp.Value ?? string.Empty).Trim();
                    // --- FIX END ---

                    // OPTIMIERUNG 2: Trenne Spezialfälle klar ab
                    if (key == "@Case_")
                    {
                        if (_cmd.Parameters.Contains("@Case_"))
                            _cmd.Parameters.Remove("@Case_");

                        SqlParameter sppara = new SqlParameter("@Case_", SqlDbType.VarChar, 100);
                        sppara.Value = AotConverter.ConvertTo<string>(value);
                        _cmd.Parameters.Add(sppara);
                    }
                    // Behandle alle anderen definierten Parameter
                    else if (paramDefinitions.TryGetValue(key, out TableDefinition? tbldef))
                    {
                        if (tbldef != null)
                        {
                            // OPTIMIERUNG 5: Wiederholte Überprüfung vermeiden
                            if (_cmd.Parameters.Contains(key))
                                _cmd.Parameters.Remove(key);

                            SqlParameter sppara = new SqlParameter();
                            sppara.ParameterName = key;
                            sppara.Direction = ParameterDirection.Input;
                            sppara.SqlDbType = (SqlDbType)tbldef.DATA_ADOTYPE;

                            // --- ERWEITERUNG: Echter NULL-Support für die DB ---
                            if (isActuallyNull)
                            {
                                sppara.Value = DBNull.Value;
                            }
                            else if (sppara.SqlDbType == SqlDbType.VarBinary)
                            {
                                // Hier muss die Größe VOR der Zuweisung des Werts zugewiesen werden.
                                byte[] byteValue = AotConverter.StringToByteArray(AotConverter.ConvertTo<string>(value));
                                sppara.Size = byteValue.Length;
                                sppara.Value = byteValue;
                            }
                            else
                            {
                                sppara.Size = tbldef.COL_SIZE;
                                sppara.Value = ConvertNETtoSQL(tbldef.NET_TYPE, value);
                            }

                            _cmd.Parameters.Add(sppara);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                err = (String.IsNullOrEmpty(err) ? "" : "\r\n") + GetConnectionStringForError(ex.Message);
            }

            return err;
        }


        /// <summary>
        /// Liefert alle ConnectionString Parameter ausser Passwort 
        /// </summary>
        /// <param name="_err">Bereits ausgelöster Fehler als string</param>
        /// <returns>Rückgabewert ist die Zusammensetzung aus Fehlermeldung und allen ConnectionString Parameter ausser Passowrt</returns>
        public string GetConnectionStringForError(string _err)
        {
            string RES = "Error: " + _err;

            string paras = "";
            if (_globalState.ConfigGeneral.ShowConnectionStringByError)
            {
                List<DictionaryModel> connectionParameters = new();

                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_globalState.ConfigGeneral.ConnectionString);

                    foreach (var key in builder.Keys)
                    {
                        if (key != null)
                        {
                            if (key!.ToString()!.ToLower() != "password")
                            {
                                if (!String.IsNullOrEmpty(builder[key.ToString()].ToString()))
                                {
                                    connectionParameters.Add(new DictionaryModel
                                    {
                                        key = key.ToString(),
                                        value = builder[key.ToString()].ToString()
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RES += (String.IsNullOrEmpty(RES) ? "" : "\r\n") + "Error: " + ex.Message + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
                }

                foreach (var param in connectionParameters)
                {
                    paras += $"{param.key} = {param.value} , ";
                }
            }

            return RES + (String.IsNullOrEmpty(paras) ? "" : " , ConnectionString: " + paras);
        }

        ///// <summary>
        ///// Diese Methode konvertiert die geparste String-Werte aus einer EXEC-Abfrage in die reale MSSQL Parameterwerte.
        ///// </summary>
        ///// <param name="_NETtype">NET Datentyp</param>
        ///// <param name="_value">Der Datentyp konvertierter Wert</param>
        ///// <returns>Rückgabewert ist die Zusammensetzung aus</returns>
        //private object ConvertNETtoSQL(string _NETtype, string _value)
        //{
        //    try
        //    {
        //        switch (_NETtype)
        //        {
        //            case "string":
        //                return _value;
        //            case "int":
        //                int tmpInt = 0;
        //                int.TryParse(_value, out tmpInt);
        //                return tmpInt;
        //            case "long":
        //                long tmpLng = 0;
        //                long.TryParse(_value, out tmpLng);
        //                return tmpLng;
        //            case "double":
        //                double tmpDbl = 0.0;
        //                tmpDbl = Convert.ToDouble(_value, System.Globalization.CultureInfo.GetCultureInfo("en-EN"));
        //                return tmpDbl;
        //            case "bool":
        //                bool tmpBool = false;
        //                if (_value == "1")
        //                    _value = "true";
        //                bool.TryParse(_value, out tmpBool);
        //                return tmpBool;
        //            case "DateTime":
        //                DateTime tmpDT = new DateTime(1111, 1, 1);
        //                DateTime.TryParse(_value, out tmpDT);
        //                return tmpDT;
        //            default:
        //                // code block
        //                return "";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Error: " + ex.Message + " , Classname : " + (System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
        //    }
        //}
        ///// <summary>
        ///// Diese Methode konvertiert die geparsten String-Werte aus einer EXEC-Abfrage
        ///// in reale MSSQL-Parameterwerte.
        ///// </summary>
        ///// <param name="_NETtype">.NET-Datentyp</param>
        ///// <param name="_value">String-Wert, der konvertiert werden soll</param>
        ///// <returns>Der konvertierte Wert im passenden .NET-Typ</returns>
        //private object ConvertNETtoSQL(string _NETtype, string _value)
        //{
        //    try
        //    {
        //        switch (_NETtype)
        //        {
        //            case "string":
        //                return _value;

        //            case "int":
        //                return int.TryParse(_value, out var tmpInt) ? tmpInt : 0;

        //            case "long":
        //                return long.TryParse(_value, out var tmpLng) ? tmpLng : 0L;

        //            case "double":
        //                return double.TryParse(_value, System.Globalization.NumberStyles.Any,
        //                    System.Globalization.CultureInfo.GetCultureInfo("en-EN"), out var tmpDbl)
        //                    ? tmpDbl : 0.0;

        //            case "bool":
        //                if (_value == "1") _value = "true";
        //                return bool.TryParse(_value, out var tmpBool) && tmpBool;

        //            case "DateTime":
        //                if (string.IsNullOrWhiteSpace(_value))
        //                    return DBNull.Value;

        //                if (DateTime.TryParse(_value, out var tmpDT))
        //                {
        //                    // MSSQL akzeptiert erst ab 1753-01-01
        //                    if (tmpDT < new DateTime(1753, 1, 1))
        //                        return DBNull.Value;

        //                    return tmpDT;
        //                }

        //                return DBNull.Value;

        //            default:
        //                return "";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Error: " + ex.Message + " , Classname : " +
        //               (System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.AssemblyQualifiedName ?? "Unknown");
        //    }
        //}
        /// <summary>
        /// Diese Methode konvertiert die geparsten String-Werte aus einer EXEC-Abfrage
        /// in reale MSSQL-Parameterwerte.
        /// </summary>
        /// <param name="_NETtype">.NET-Datentyp</param>
        /// <param name="_value">String-Wert, der konvertiert werden soll</param>
        /// <returns>Der konvertierte Wert im passenden .NET-Typ (oder DBNull.Value)</returns>
        private object ConvertNETtoSQL(string _NETtype, string _value)
        {
            // C# 8/9 Switch-Ausdruck für eine kompaktere und lesbarere Struktur
            try
            {
                return _NETtype.ToLower() switch
                {
                    "string" => _value,

                    // int
                    "int" => int.TryParse(_value, out var i) ? i : 0,

                    // long
                    "long" => long.TryParse(_value, out var l) ? l : 0L,

                    // double (optional: System.Globalization.NumberStyles.Float zur Präzisierung)
                    "double" => double.TryParse(_value, System.Globalization.NumberStyles.Any,
                                                System.Globalization.CultureInfo.GetCultureInfo("en-EN"), out var d)
                                                ? d : (object)0.0,
                    // bool
                    "bool" => _value.Trim().ToLowerInvariant() switch
                    {
                        "1" or "true" or "yes" or "on" => true,
                        "0" or "false" or "no" or "off" => false,
                        _ => false // oder throw Exception
                    },

                    // DateTimeOffset
                    "datetimeoffset" => ParseDateTimeOffset(_value),

                    // DateTime
                    "datetime" or "date" => ConvertDateTimeToSql(_value),

                    // Standardfall (oder anderer Typ, der nicht behandelt wird)
                    _ => _value // oder DBNull.Value, je nachdem, was in MSSQL erwartet wird
                };
            }
            catch (Exception ex)
            {
                // WICHTIG: Fehler protokollieren, aber DBNull.Value zurückgeben.
                // Ein Fehler-String als Rückgabewert würde SqlCommand.Parameters.Add() fehlschlagen lassen.
                // HIER: Fügen Sie Ihre Fehlerprotokollierungslogik ein (z.B. Log.Error(...) )
                System.Diagnostics.Debug.WriteLine($"Error in ConvertNETtoSQL for type {_NETtype} and value '{_value}': {ex.Message}");

                return DBNull.Value;
            }
        }

        private static object ParseDateTimeOffset(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DBNull.Value;

            // ISO-8601 bevorzugt (z.B. 2024-01-10T13:45:00+01:00)
            if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var dto))
            {
                return dto;
            }

            // Fallback: ohne Offset → Local oder UTC (ENTSCHEIDEN!)
            if (DateTime.TryParse(value, out var dt))
            {
                // WICHTIG: Entscheidung treffen
                // Empfehlung: UTC erzwingen
                return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
            }

            throw new FormatException($"Ungültiges DateTimeOffset-Format: '{value}'");
        }

        // Hilfsmethode zur besseren Lesbarkeit und Wiederverwendbarkeit der komplexen Datumslogik
        private object ConvertDateTimeToSql(string _value)
        {
            if (string.IsNullOrWhiteSpace(_value))
                return DBNull.Value;

            if (DateTime.TryParse(_value, out var tmpDT))
            {
                // MSSQL akzeptiert standardmäßig Datumsangaben erst ab 1753-01-01 (smalldatetime beginnt später)
                if (tmpDT < new DateTime(1753, 1, 1))
                    return DBNull.Value;

                return tmpDT;
            }

            return DBNull.Value;
        }

        ///// <summary>
        ///// Diese Methode konvertiert die ausgelesene DataRow-Zeile aus einer Tabelle in die Modellisten-Zeile.
        ///// </summary>
        ///// <param name="_dr">DataRow Objekt einer Tabelle</param>
        ///// <param name="_namespace_classname">Name des Tabellen-Models</param>
        ///// <returns>Rückgabewert ist ein generisch erstelltes Objekt-Row, das in einer Modellistenzeile eingelesen werden kann</returns>
        //public object? GetItem(DataRow _dr, string _namespace_classname)
        //{
        //    Type? temp = TyBlazorCore.GetType(_namespace_classname);

        //    if (temp != null)
        //    {
        //        var obj = Activator.CreateInstance(temp);

        //        foreach (DataColumn column in _dr.Table.Columns)
        //        {
        //            foreach (System.Reflection.PropertyInfo pro in temp.GetProperties())
        //            {
        //                if (pro.Name == column.ColumnName)
        //                {
        //                    try
        //                    {
        //                        var value = _dr[column.ColumnName];
        //                        if (value != System.DBNull.Value)
        //                            pro.SetValue(obj, value, null);
        //                        else
        //                            pro.SetValue(obj, null, null);
        //                    }
        //                    catch
        //                    {
        //                        pro.SetValue(obj, null, null);
        //                    }
        //                }
        //                else
        //                {
        //                    continue;
        //                }
        //            }
        //        }

        //        return obj;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        ///// <summary>
        ///// Diese Methode konvertiert die ausgelesene DataRow-Zeile aus einer Tabelle in die Modellisten-Zeile.
        ///// </summary>
        ///// <param name="_dr">DataRow Objekt einer Tabelle</param>
        ///// <returns>Rückgabewert ist ein generisch erstelltes Objekt-Row, das in einer Modellistenzeile eingelesen werden kann</returns>
        //public T? GetItem<T>(DataRow dr)
        //{
        //    try
        //    {
        //        Type temp = typeof(T);
        //        T obj = Activator.CreateInstance<T>();
        //        string? tmpValue = "";

        //        foreach (DataColumn column in dr.Table.Columns)
        //        {
        //            foreach (System.Reflection.PropertyInfo pro in temp.GetProperties())
        //            {
        //                if (pro.Name == column.ColumnName)
        //                    try
        //                    {
        //                        Type type = Nullable.GetUnderlyingType(pro.PropertyType) ?? pro.PropertyType;
        //                        string typeName = tyBlazorCore.Name;
        //                        if (typeName == "String")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                    pro.SetValue(obj, tmpValue, null);
        //                                else
        //                                    pro.SetValue(obj, "", null);
        //                                break;
        //                            }
        //                        }
        //                        if (typeName == "Double")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                    pro.SetValue(obj, Convert.ToDouble(tmpValue), null);
        //                                else
        //                                    pro.SetValue(obj, 0.0, null);
        //                                break;
        //                            }
        //                        }
        //                        if (typeName == "Int" || typeName == "SHARING_STATUS")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                    pro.SetValue(obj, int.Parse(tmpValue), null);
        //                                else
        //                                    pro.SetValue(obj, 0, null);
        //                                break;
        //                            }
        //                        }
        //                        if (typeName == "Int32")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                    pro.SetValue(obj, int.Parse(tmpValue), null);
        //                                else
        //                                    pro.SetValue(obj, 0, null);
        //                                break;
        //                            }
        //                        }
        //                        if (typeName == "Int64")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                    pro.SetValue(obj, long.Parse(tmpValue), null);
        //                                else
        //                                    pro.SetValue(obj, 0, null);
        //                                break;
        //                            }
        //                        }
        //                        if (typeName == "Boolean")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                {
        //                                    bool flag;
        //                                    if (Boolean.TryParse(tmpValue, out flag))
        //                                        pro.SetValue(obj, flag, null);
        //                                    else
        //                                        pro.SetValue(obj, false, null);
        //                                }
        //                            }
        //                        }
        //                        if (typeName == "DateTime")
        //                        {
        //                            if (dr[column.ColumnName] != null)
        //                            {
        //                                tmpValue = dr[column.ColumnName].ToString();
        //                                if (!(String.IsNullOrEmpty(tmpValue)))
        //                                    pro.SetValue(obj, Convert.ToDateTime(tmpValue), null);
        //                                else
        //                                    pro.SetValue(obj, null, null);
        //                                break;
        //                            }
        //                        }
        //                    }
        //                    catch
        //                    {
        //                        throw;
        //                    }
        //                else
        //                    continue;
        //            }
        //        }
        //        return obj;
        //    }
        //    catch (Exception)
        //    {
        //        return default(T);
        //    }
        //}
        public T? GetItemOptimized<T>(DataRow dr) where T : new()
        {
            // Statt Activator.CreateInstance<T>(), was Reflection verwendet:
            // Der where T : new() Constraint erlaubt eine sichere und schnellere Instanziierung.
            if (dr == null) return default;
            T obj = new T();

            // Reflection wird nur EINMAL ausgeführt
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // 1. Spaltenprüfung
                if (!dr.Table.Columns.Contains(prop.Name)) continue;

                var rawValue = dr[prop.Name];

                // 2. KORREKTE DB NULL PRÜFUNG
                if (rawValue == DBNull.Value)
                {
                    // Nullable-Typen werden mit null gesetzt, Non-Nullable-Typen (int, double) behalten ihren
                    // Standardwert (0, 0.0) oder die Zuweisung wird ignoriert, da der Wert null ist.
                    prop.SetValue(obj, null);
                    continue;
                }

                try
                {
                    // 3. ROBUSTE TYP-KONVERTIERUNG
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    // Convert.ChangeType übernimmt die gesamte manuelle Typ-Casting-Logik 
                    // der ursprünglichen Methode (String, Double, Int32, Int64, Boolean, DateTime, etc.)
                    var convertedValue = Convert.ChangeType(rawValue, targetType);

                    prop.SetValue(obj, convertedValue);
                }
                catch (Exception ex)
                {
                    // Fügen Sie hier Ihre Logik für Fehlerprotokollierung hinzu.
                    throw new InvalidOperationException($"Mapping Fehler bei Spalte '{prop.Name}'.", ex);
                }
            }
            return obj;
        }

        /// <summary>
        /// Konvertierung des Tabellenergebnisses
        /// </summary>
        /// <remarks>
        /// Mit dieser Methode werden die Tabellenergebnisses in die angegebene Modelklasse des Listenergebnisses konvertiert und zurückgeliefert.
        /// </remarks>
        //public List<T> ConvertDataTable<T>(System.Data.DataTable? dt)
        //{
        //    List<T> data = new List<T>();
        //    if (dt != null)
        //    {
        //        foreach (DataRow row in dt.Rows)
        //        {
        //            T? item = GetItem<T>(row);
        //            data.Add(item!);
        //        }
        //    }
        //    //System.Threading.Thread.Sleep(200);
        //    return data;
        //}
        /// Konvertiert eine DataTable zeilenweise in eine typsichere Liste von Modellen.
        /// </summary>
        /// <typeparam name="T">Der Zieltyp des Modells, muss einen parameterlosen Konstruktor haben.</typeparam>
        /// <param name="dt">Die zu konvertierende DataTable.</param>
        /// <returns>Eine Liste von Objekten des Typs T. Gibt immer eine leere, nicht-null-Liste zurück.</returns>
        public List<T> ConvertDataTable<T>(System.Data.DataTable? dt) where T : new()
        {
            // Die Liste wird IMMER initialisiert und zurückgegeben, um null-Prüfungen zu vermeiden (CS8619).
            List<T> data = new List<T>();

            // Die Null-Prüfung ist notwendig, da der Eingangsparameter DataTable? nullable ist.
            if (dt != null)
            {
                // 1. Iteration durch alle DataRows.
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    // 2. Aufruf der optimierten Mapping-Methode, die Reflection performant nutzt.
                    // (GetItemOptimized<T> muss den where T : new() Constraint ebenfalls haben!)
                    T? item = GetItemOptimized<T>(row);

                    // 3. Hinzufügen des Items. 
                    // Das "!" (Null-forgiving Operator) wird hier verwendet, weil wir erwarten, 
                    // dass GetItemOptimized<T> nur dann null zurückgibt, wenn ein schwerwiegender Fehler 
                    // aufgetreten ist (was die try-catch-Logik abfängt).
                    if (item != null)
                    {
                        data.Add(item);
                    }
                }
            }

            // Die Methode gibt garantiert List<T> und nicht List<T>? zurück.
            return data;
        }



        /// <summary>
        /// Methode, die Storage Procedur anhand gelieferte Parameter-Paare zusammenzusetzt 
        /// </summary>
        /// <param name="_db_para">Dictionary Parameter</param>
        /// <returns>Rückgabewert ist T-SQL konforme Storage Procedur als String</returns>
        //public static string CreateExec(Dictionary<string, (string mssql, string realm)> _db_para, string _sp_name = "Crud")
        public string CreateExec(Dictionary<string, string> _db_para, string _sp_name = "Crud")
        {
            string RES = "";

            try
            {
                string pairs = "";
                foreach (var item in _db_para)
                {
                    if (!item.Key.ToLower().StartsWith("@int__"))
                        pairs += (string.IsNullOrEmpty(pairs) ? " " : " , ") + item.Key + " = " + item.Value;  //+ item.Value.mssql;
                }

                RES = "EXEC " + _globalState.ConfigGeneral.TableSchema + "." + _sp_name.Trim() + pairs;
            }
            catch { }

            return RES;
        }



    }
}
