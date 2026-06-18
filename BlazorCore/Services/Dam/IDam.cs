using BlazorCore.Services.AppState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Dam
{
    /// <summary>
    /// Data Access Management - Abstraction layer for cloud/local data access
    /// </summary>
    public interface IDamBase
    {
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
        /// Retrieves authentication token for TPC (Two-Phase Commit / internal) operations
        /// </summary>
        /// <param name="dbPara">Dictionary containing user information parameters</param>
        /// <returns>Client storage model containing token data or error</returns>
        Task<ClientStorageModel> GetTokenTPC(Dictionary<string, string> dbPara);

        /// <summary>
        /// Retrieves authentication token for IDP (Identity Provider / external) operations
        /// </summary>
        /// <param name="dbPara">Dictionary containing identity provider parameters</param>
        /// <returns>Client storage model containing token data or error</returns>
        Task<ClientStorageModel> GetTokenIDP(Dictionary<string, string> dbPara);

        /// <summary>
        /// Changes user password
        /// </summary>
        /// <param name="dbPara">Dictionary containing password change parameters</param>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> ChangePassword(Dictionary<string, string> dbPara);

        /// <summary>
        /// Executes a scalar query (single value)
        /// </summary>
        /// <param name="dbPara">Dictionary containing query parameters</param>
        /// <returns>Scalar result containing query result or error</returns>
        Task<ScalarModel> Scalar(Dictionary<string, string> dbPara);

        /// <summary>
        /// Reads data from storage with comparison support for migration
        /// </summary>
        /// <typeparam name="T">Type implementing IBasisModel and IMigrationState</typeparam>
        /// <param name="dbPara">Dictionary containing read parameters</param>
        /// <returns>Read model containing result list or error</returns>
        Task<ReadModel<T?>>? ReadCompare<T>(Dictionary<string, string> dbPara) where T : class, IBasisModel, IMigrationState, new();

        /// <summary>
        /// Reads data from storage
        /// </summary>
        /// <typeparam name="T">Type to deserialize the data into</typeparam>
        /// <param name="dbPara">Dictionary containing read parameters</param>
        /// <returns>Read model containing result list or error</returns>
        Task<ReadModel<T?>>? ReadData<T>(Dictionary<string, string> dbPara) where T : class, new();

        /// <summary>
        /// Saves data to storage (INSERT/UPDATE)
        /// </summary>
        /// <param name="dbPara">Dictionary containing save parameters</param>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> Save(Dictionary<string, string> dbPara);

        /// <summary>
        /// Executes a non-query operation (INSERT/UPDATE/DELETE without return value)
        /// </summary>
        /// <param name="dbPara">Dictionary containing query parameters</param>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> ExecQuery(Dictionary<string, string> dbPara);

        /// <summary>
        /// Executes an anonymous query without authentication
        /// </summary>
        /// <param name="dbPara">Dictionary containing query parameters</param>
        /// <returns>Scalar result containing query result or error</returns>
        Task<ScalarModel> AnonymousQuery(Dictionary<string, string> dbPara);

        /// <summary>
        /// Executes an AI operation (Chat, Embedding, etc.) through the configured AI service.
        /// AI operations are CLOUD-ONLY. No local fallback exists.
        /// </summary>
        /// <param name="dbPara">Dictionary containing AI parameters (must include @Case_)</param>
        /// <returns>ScalarModel with out_value_str = AI response, out_err = error message</returns>
        Task<ScalarModel> Ai(Dictionary<string, string> dbPara);

        /// <summary>
        /// Compares cloud and local data for synchronization
        /// </summary>
        /// <typeparam name="T">Type implementing IBasisModel and IMigrationState</typeparam>
        /// <param name="mssqlItems">Data from MSSQL (cloud)</param>
        /// <param name="realmItems">Data from local storage</param>
        /// <returns>Merged list with migration flags</returns>
        Task<List<T?>> CompareCloudLocalData<T>(
            IReadOnlyCollection<T?>? mssqlItems,
            IReadOnlyCollection<T?>? realmItems
        ) where T : class, IBasisModel, IMigrationState;

        /// <summary>
        /// Deletes all data from local storage
        /// </summary>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> DeleteData();

        /// <summary>
        /// Deletes the entire local storage database/file
        /// </summary>
        /// <returns>Scalar result containing success status or error message</returns>
        Task<ScalarModel> DeleteDB();

        ///// <summary>
        ///// Loads JSON storage (initializes JSON-based local cache)
        ///// </summary>
        ///// <returns>Scalar result containing success status or error message</returns>
        //Task<ScalarModel> LoadJsonStorage();

        /// <summary>
        /// Validates if the provided token is still valid on the server
        /// </summary>
        /// <param name="user">User object containing token</param>
        /// <returns>True if token is valid, otherwise false</returns>
        Task<bool> CheckToken(Apis.UserWebApi user);

        /// <summary>
        /// Sets migration flags (cmd_nosqlite, cmd_nomssql, @IsMigration) based on storage location
        /// </summary>
        /// <param name="dbPara">Dictionary to modify with migration flags</param>
        /// <param name="storagelocation">Current storage location setting</param>
        /// <returns>Error message if operation failed, otherwise empty string</returns>
        string SetMigrationFlags(ref Dictionary<string, string> dbPara, STORAGE_LOCATION storagelocation);

        /// <summary>
        /// Sets/overrides the current platform for testing purposes.
        /// </summary>
        /// <param name="platform">The platform to set</param>
        void SetCurrPlatform(PLATFORMS platform);
    }

    /// <summary>
    /// Base model interface for all data entities
    /// </summary>
    public interface IBasisModel
    {
        /// <summary>
        /// Primary key (database identity)
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Last update timestamp (Unix format, comparable between cloud and local)
        /// </summary>
        public long LastUpdateUnixTS { get; set; }

        /// <summary>
        /// Unique identifier across all platforms (35 chars with "T" prefix)
        /// </summary>
        public string? UnixTS { get; set; }

        /// <summary>
        /// Creates a deep copy of the object
        /// </summary>
        public object Clone();
    }

    /// <summary>
    /// Migration state interface for synchronization between cloud and local storage
    /// </summary>
    public interface IMigrationState
    {
        /// <summary>
        /// Flag indicating whether this entity needs to be migrated TO cloud
        /// </summary>
        public bool Int__MigrationToCloud { get; set; }

        /// <summary>
        /// Flag indicating whether this entity needs to be migrated TO local storage
        /// </summary>
        public bool Int__MigrationToLocal { get; set; }
    }
}