//namespace BlazorCore.Services.SqLite
//{
//    /// <summary>
//    /// Interface for SQLite platform-specific implementations.
//    /// This follows the "Polling-Bridge" and "Pass-Through" architecture principles.
//    /// </summary>
//    public interface ISqLiteBase
//    {
//        ///// <summary>
//        ///// Indicates whether the database connection and schema are initialized.
//        ///// </summary>
//        bool IsInitialized { get; set; }

//        /// <summary>
//        /// Initializes the database, handles encryption keys, and creates tables.
//        /// </summary>
//        /// <returns>A task representing the asynchronous operation.</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> InitializeAsync(bool register, string userAccount);

//        Task<BlazorCore.Services.Dam.ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams);

//        /// <summary>
//        /// Executes an insert or update operation and returns a ScalarModel.
//        /// Used by the Dam.Save method.
//        /// </summary>
//        /// <param name="dbParams">Dictionary containing command cases and parameters.</param>
//        /// <returns>Result containing status and metadata.</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> Save(Dictionary<string, string> dbParams);

//        /// <summary>
//        /// Executes a non-query SQL command (Update, Delete, etc.).
//        /// Used by the Dam.ExecQuery method.
//        /// </summary>
//        /// <param name="dbParams">Dictionary containing command cases and parameters.</param>
//        /// <returns>Result containing status and metadata.</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> ExecQuery(Dictionary<string, string> dbParams);

//        /// <summary>
//        /// Executes a query that returns a single value.
//        /// Used by the Dam.Scalar method.
//        /// </summary>
//        /// <param name="dbParams">Dictionary containing command cases and parameters.</param>
//        /// <returns>Result containing the scalar value.</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> Scalar(Dictionary<string, string> dbParams);

//        /// <summary>
//        /// Reads a set of data and maps it to a specific model tyBlazorCore.
//        /// </summary>
//        /// <typeparam name="T">The model type to map the results to.</typeparam>
//        /// <param name="dbParams">Dictionary containing command cases and parameters.</param>
//        /// <returns>A ReaderModel containing a list of type T.</returns>
//        Task<BlazorCore.Services.SqlClient.ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new();

//        ///// <summary>
//        ///// Deletes the local database file.
//        ///// </summary>
//        ///// <returns>A string message indicating the result of the deletion.</returns>
//        //Task<string> DeleteDB();

//        /// <summary>
//        /// LEVEL 1: Deletes all record entries from all user tables but keeps the schema.
//        /// </summary>
//        /// <returns>True if data was cleared successfully.</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> ClearAllData();

//        /// <summary>
//        /// LEVEL 2: Drops all user tables from the database. Internally calls ClearAllData first.
//        /// </summary>
//        /// <returns>True if all tables were dropped successfully.</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> DropAllTables();

//        /// <summary>
//        /// LEVEL 3: Deletes the physical database file. Internally calls DropAllTables first.
//        /// </summary>
//        /// <returns>A string message indicating the result (e.g., "OK").</returns>
//        Task<BlazorCore.Services.SqlClient.ScalarModel> DeleteDB();

//    }
//}

