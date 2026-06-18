using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorCore.Services.LocalStorage
{
    /// <summary>
    /// Interface for the local storage engine (RAM-based).
    /// Formerly IMemoryStorageBase - renamed for clarity.
    /// Serves as the single source of truth for reads in MEMORY and JSON_HYBRID modes.
    /// </summary>
    public interface ILocalStorage
    {
        /// <summary>
        /// Indicates whether the RAM lists are initialized and ready.
        /// </summary>
        bool IsInitialized { get; set; }

        /// <summary>
        /// Hashed user identifier for json-path on local machine (e.g., "user12345hash") - used for JSON_HYBRID file organization.
        /// </summary>
        string UserAccountHashed { get; set; }

        /// <summary>
        /// Data in RAM cache - Single Source of Truth for reads.
        /// Key = TableName (e.g., "Cycles", "AuthUsers")
        /// Value = List of data records stored as objects.
        /// </summary>
        Dictionary<string, List<object>> RamCache { get; }

        /// <summary>
        /// Initializes the local storage by preparing the internal RAM lists.
        /// </summary>
         ///// <param name="userAccount">The identifier for the user's session container.</param>
        /// <returns>A ScalarModel indicating initialization success.</returns>
        Task<ScalarModel> InitializeAsync(string userAccount);

        /// <summary>
        /// Performs authentication-related operations or credential synchronization.
        /// Delegates to LocalQueryExecutor.
        /// </summary>
        /// <param name="dbParams">Database parameters containing auth/sync data.</param>
        /// <returns>A ClientStorageModel indicating the result of the auth operation.</returns>
        Task<ClientStorageModel> GetTokenTPC(Dictionary<string, string> dbParams);

        /// <summary>
        /// Saves a record directly to the RAM cache (and JSON if configured).
        /// Delegates to LocalQueryExecutor.
        /// </summary>
        /// <param name="dbParams">Database parameters containing the record data.</param>
        /// <returns>A ScalarModel indicating success or error.</returns>
        Task<ScalarModel> Save(Dictionary<string, string> dbParams);

        /// <summary>
        /// Executes logic based on command cases (e.g., Update or Delete).
        /// Delegates to LocalQueryExecutor.
        /// </summary>
        /// <param name="dbParams">Database parameters defining the operation.</param>
        /// <returns>A ScalarModel indicating success or error.</returns>
        Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams);

        /// <summary>
        /// Retrieves a single value from the RAM cache based on parameters.
        /// Delegates to LocalQueryExecutor.
        /// </summary>
        /// <param name="dbParams">Database parameters for the query.</param>
        /// <returns>A ScalarModel containing the scalar value or error.</returns>
        Task<ScalarModel> Scalar(Dictionary<string, string> dbParams);

        /// <summary>
        /// Queries the RAM cache using LINQ based on dbParams and returns a typed list.
        /// Delegates to LocalQueryExecutor.
        /// </summary>
        /// <typeparam name="T">The model type to map the memory records to.</typeparam>
        /// <param name="dbParams">Database parameters for the query.</param>
        /// <returns>A ReaderModel containing the typed list or error.</returns>
        Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new();

        /// <summary>
        /// LEVEL 1: Clears all records from the RAM cache.
        /// </summary>
        /// <returns>A ScalarModel indicating success or error.</returns>
        Task<ScalarModel> ClearAllData();

        /// <summary>
        /// LEVEL 2: Resets the RAM cache (same as ClearAllData for LocalStorage).
        /// </summary>
        /// <returns>A ScalarModel indicating success or error.</returns>
        Task<ScalarModel> DropAllTables();

        /// <summary>
        /// LEVEL 3: Full reset of the local storage service.
        /// </summary>
        /// <returns>A ScalarModel indicating success or error.</returns>
        Task<ScalarModel> DeleteDB();
    }
}