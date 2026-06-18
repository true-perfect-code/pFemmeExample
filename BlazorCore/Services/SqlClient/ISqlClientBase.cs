using System.Data;

namespace BlazorCore.Services.SqlClient
{
    /// <summary>
    /// Provides direct SQL Server database access for server-side operations.
    /// Used by DAM on WINDOWS_SERVER and WINDOWS_API platforms.
    /// </summary>
    public interface ISqlClientBase
    {
        /// <summary>
        /// Gets the database connection string.
        /// The connection string is loaded from configuration and may be encrypted.
        /// </summary>
        //string connectionString { get; }

        /// <summary>
        /// Indicates whether the database connection is established and ready.
        /// </summary>
        bool isConnected { get; }

        /// <summary>
        /// Initializes the database, handles encryption keys, and creates connectionstring.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<BlazorCore.Services.SqlClient.ScalarModel> InitializeAsync(GlobalState.IGlobalStateBase globalState);

        /// <summary>
        /// Maps all application tables to the database.
        /// Called once during application startup to ensure the database schema is ready.
        /// </summary>
        Task MapApplTbls();

        /// <summary>
        /// Executes a scalar query and returns a single value.
        /// </summary>
        /// <param name="_db_para">Dictionary containing case_ and query parameters.</param>
        /// <returns>ScalarModel with out_value_str containing the result.</returns>
        Task<ScalarModel> Scalar(Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes a query and returns dynamic reader results as JSON.
        /// </summary>
        /// <param name="_db_para">Dictionary containing case_ and query parameters.</param>
        /// <returns>ReaderDynamicModel with out_json containing the result as JSON.</returns>
        Task<ReaderDynamicModel> Reader(Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes a query and returns strongly-typed reader results.
        /// </summary>
        /// <typeparam name="T">The target type for deserialization (must have parameterless constructor).</typeparam>
        /// <param name="_db_para">Dictionary containing case_ and query parameters.</param>
        /// <returns>ReaderModel&lt;T&gt; with Items list of type T.</returns>
        Task<ReaderModel<T>> Reader<T>(Dictionary<string, string> _db_para) where T : new();

        /// <summary>
        /// Executes a query that returns binary data (e.g., images, files).
        /// </summary>
        /// <param name="_db_para">Dictionary containing case_ and query parameters.</param>
        /// <returns>ScalarModel with out_bytes containing the binary data.</returns>
        Task<ScalarModel> Bytes(Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes a non-query command (INSERT, UPDATE, DELETE, DDL).
        /// </summary>
        /// <param name="_db_para">Dictionary containing case_ and query parameters.</param>
        /// <returns>ScalarModel with out_value_bool indicating success.</returns>
        Task<ScalarModel> NonQuery(Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes a stored procedure with flexible execution mode.
        /// </summary>
        /// <typeparam name="T">The target type for deserialization (must have parameterless constructor).</typeparam>
        /// <param name="_db_para">Dictionary containing parameters for the stored procedure.</param>
        /// <param name="_mode">Execution mode (Reader, Scalar, NonQuery).</param>
        /// <param name="_sp">Stored procedure name suffix (default: ".Crud").</param>
        /// <returns>ReaderModel&lt;T&gt; with Items list of type T.</returns>
        Task<ReaderModel<T>> spExecute<T>(Dictionary<string, string> _db_para, ExecuteMode _mode, string _sp = ".Crud") where T : new();

        /// <summary>
        /// Converts a DataTable to a JSON string.
        /// Used for API responses and serialization.
        /// </summary>
        /// <param name="dataTable">The DataTable to convert.</param>
        /// <returns>JSON string representation of the DataTable.</returns>
        string DataTableToJson(DataTable dataTable);

        /// <summary>
        /// Returns a safe version of the connection string for error logging.
        /// Sensitive information (credentials) are removed.
        /// </summary>
        /// <param name="_err">Optional error context.</param>
        /// <returns>A sanitized connection string or error message.</returns>
        string GetConnectionStringForError(string _err);

        /// <summary>
        /// Converts a DataTable to a strongly-typed list.
        /// AOT-safe because the target type must have a parameterless constructor.
        /// </summary>
        /// <typeparam name="T">The target type (must have parameterless constructor).</typeparam>
        /// <param name="dt">The DataTable to convert.</param>
        /// <returns>List of type T with the converted data.</returns>
        List<T> ConvertDataTable<T>(DataTable? dt) where T : new();

        /// <summary>
        /// Generates an SQL command string and parameter list from a dictionary.
        /// Used internally to build dynamic queries.
        /// </summary>
        /// <param name="_db_para">Dictionary containing case_ and query parameters.</param>
        /// <param name="_sp_name">Stored procedure name suffix (default: "Crud").</param>
        /// <returns>SQL command string with embedded parameters.</returns>
        string CreateExec(Dictionary<string, string> _db_para, string _sp_name = "Crud");
    }
}

//using System.Data;

//namespace BlazorCore.Services.SqlClient
//{
//    public interface ISqlClientBase
//    {
//        string connectionString { get; }
//        bool isConnected { get; }

//        //Task<string> MapSaveExecTbl(string _exec);
//        //Task<string> MapTbls(string _tables);
//        Task MapApplTbls();

//        Task<ScalarModel> Scalar(Dictionary<string, string> _db_para);
//        Task<ReaderDynamicModel> Reader(Dictionary<string, string> _db_para);

//        // KORREKTUR: Constraint hinzugefügt 
//        Task<ReaderModel<T>> Reader<T>(Dictionary<string, string> _db_para) where T : new();

//        Task<ScalarModel> Bytes(Dictionary<string, string> _db_para);
//        Task<ScalarModel> NonQuery(Dictionary<string, string> _db_para);

//        // Dieser Constraint war schon korrekt
//        Task<ReaderModel<T>> spExecute<T>(Dictionary<string, string> _db_para, ExecuteMode _mode, string _sp = ".Crud") where T : new();

//        string DataTableToJson(DataTable dataTable);
//        string GetConnectionStringForError(string _err);
//        //object? GetItem(DataRow _dr, string _namespace_classname);
//        //T? GetItem<T>(DataRow dr) where T : new(); // Hinzugefügt, falls es die alte Reflection-Methode ist

//        // KORREKTUR: Constraint hinzugefügt (Lösung für CS0425/CS0310)
//        List<T> ConvertDataTable<T>(DataTable? dt) where T : new();

//        string CreateExec(Dictionary<string, string> _db_para, string _sp_name = "Crud");

//    }
//}