using BlazorCore.Services.AppState;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Dam
{
    public interface IDamBase
    {
        ///// <summary>
        ///// Initializes the DAM class and its dependencies based on the current platform
        ///// </summary>
        ///// <returns>Scalar result containing success status or error message</returns>
        //Task<ScalarModel> Initialize(IServiceProvider serviceProvider);

        /// <summary>
        /// Checks if SQL connection is initialized
        /// </summary>
        /// <returns>True if SQL is connected, otherwise false</returns>
        bool IsInitializeSql();

        /// <summary>
        /// Initializes the SQL connection
        /// </summary>
        /// <returns>Scalar result containing success status or error message</returns>
        ScalarModel InitializeSql();

        /// <summary>
        /// Retrieves authentication token for TPC (Two-Phase Commit) operations
        /// </summary>
        /// <param name="_db_para">Dictionary containing user information parameters</param>
        /// <returns>Client storage model containing token data or error</returns>
        Task<ClientStorageModel> GetTokenTPC(Dictionary<string, string> _db_para);

        /// <summary>
        /// Retrieves authentication token for IDP (Identity Provider) operations
        /// </summary>
        /// <param name="_db_para">Dictionary containing identity provider parameters</param>
        /// <returns>Client storage model containing token data or error</returns>
        Task<ClientStorageModel> GetTokenIDP(Dictionary<string, string> _db_para);

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="_db_para">Dictionary containing password change parameters</param>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> ChangePassword(Dictionary<string, string> _db_para);

        ///// <summary>
        ///// Manages OTP (One-Time Password) operations
        ///// </summary>
        ///// <param name="otpmana">OTP management operation type</param>
        ///// <param name="_db_para">Dictionary containing OTP parameters</param>
        ///// <returns>Scalar result containing success status or error message</returns>
        //Task<ScalarModel> ManageOtp(MANAGE_OTP otpmana, Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes a scalar query
        /// </summary>
        /// <param name="_db_para">Dictionary containing query parameters</param>
        /// <returns>Scalar result containing query result or error</returns>
        Task<ScalarModel> Scalar(Dictionary<string, string> _db_para);

        /// <summary>
        /// Reads data of type T from storage
        /// </summary>
        /// <typeparam name="T">Type implementing IBasisModel</typeparam>
        /// <param name="_db_para">Dictionary containing read parameters</param>
        /// <returns>Read model containing result list or error</returns>
        Task<ReadModel<T?>>? ReadCompare<T>(Dictionary<string, string> _db_para) where T : class, IBasisModel, IMigrationState, new(); //where T : IBasisModel, new();
        Task<ReadModel<T?>>? ReadData<T>(Dictionary<string, string> _db_para) where T : class, new(); //where T : new();

        /// <summary>
        /// Saves data to storage
        /// </summary>
        /// <param name="_db_para">Dictionary containing save parameters</param>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> Save(Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes a query (non-query operation)
        /// </summary>
        /// <param name="_db_para">Dictionary containing query parameters</param>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> ExecQuery(Dictionary<string, string> _db_para);

        /// <summary>
        /// Executes an anonymous query (without authentication)
        /// </summary>
        /// <param name="_db_para">Dictionary containing query parameters</param>
        /// <returns>Scalar result containing query result or error</returns>
        Task<ScalarModel> AnonymousQuery(Dictionary<string, string> _db_para);

        ///// <summary>
        ///// Compares cloud and local data for synchronization
        ///// </summary>
        ///// <typeparam name="T">Type implementing IBasisModel</typeparam>
        ///// <param name="_list_mssql">List of data from MSSQL (cloud)</param>
        ///// <param name="_list_realm">List of data from Realm (local)</param>
        ///// <returns>Merged list with migration flags</returns>
        //Task<List<T?>>? CompareCloudLocalData<T>(List<T?>? _list_mssql, List<T?>? _list_realm) where T : IBasisModel, new();
        /// <summary>
        /// Compares cloud and local data for synchronization
        /// </summary>
        /// <typeparam name="T">Type implementing IBasisModel</typeparam>
        /// <param name="mssqlItems">Data from MSSQL (cloud)</param>
        /// <param name="realmItems">Data from Realm (local)</param>
        /// <returns>Merged list with migration flags</returns>
        public interface IDamBase
        {
            Task<List<T?>> CompareCloudLocalData<T>(
                IReadOnlyCollection<T?>? mssqlItems,
                IReadOnlyCollection<T?>? realmItems
            ) where T : class, IBasisModel, IMigrationState;
        }

        /// <summary>
        /// Deletes all data from Realm database
        /// </summary>
        /// <returns>Error message if operation failed, otherwise empty string</returns>
        //Task<ScalarModel> DeleteSqLiteData();
        Task<ScalarModel> DeleteData();

        /// <summary>
        /// Deletes the entire Realm database file
        /// </summary>
        /// <returns>Error message if operation failed, otherwise empty string</returns>
        //Task<ScalarModel> DeleteSqLiteDB();
        Task<ScalarModel> DeleteDB();

        Task<ScalarModel> LoadJsonStorage();

        /// <summary>
        /// Deletes all data from Realm database
        /// </summary>
        /// <returns>Error message if operation failed, otherwise empty string</returns>
        Task<bool> CheckToken(Apis.UserWebApi user);

        /// <summary>
        /// Set migration flags 'cmd_nosqlite', 'cmd_nomssql' and '@IsMigration'
        /// </summary>
        /// <returns>Error message if operation failed, otherwise empty string</returns>
        string SetMigrationFlags(ref Dictionary<string, string> _db_para, STORAGE_LOCATION storagelocation);

        //Task<string> MapTablesMSSQL();
    }

    public interface IBasisModel //<T> where T : class
    {
        public int ID { get; set; }

        public long LastUpdateUnixTS { get; set; }

        public string? UnixTS { get; set; }

        //public bool Int__MigrationToMSSQL { get; set; }

        //public bool Int__MigrationToSqLite { get; set; }

        public object Clone();
    }

    public interface IMigrationState
    {
        //public bool Int__MigrationToMSSQL { get; set; }
        public bool Int__MigrationToCloud { get; set; }
        //public bool Int__MigrationToSqLite { get; set; }
        public bool Int__MigrationToLocal { get; set; }
    }


}
