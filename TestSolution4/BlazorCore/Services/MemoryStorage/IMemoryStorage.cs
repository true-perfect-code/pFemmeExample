using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.Dam;

namespace BlazorCore.Services.MemoryStorage
{
    /// <summary>
    /// Interface for the Volatile Memory storage implementation.
    /// This follows the same signature as ISqLiteBase to ensure 
    /// seamless integration with DamBase, but operates strictly in RAM.
    /// Implementation principle: "Transient data only - lost on application restart."
    /// </summary>
    public interface IMemoryStorageBase
    {
        /// <summary>
        /// Indicates whether the RAM lists are initialized and ready.
        /// </summary>
        bool IsInitialized { get; set; }

        /// <summary>
        /// Initializes the volatile storage by preparing the internal RAM lists.
        /// </summary>
        /// <param name="register">Not used for Memory, kept for signature compatibility.</param>
        /// <param name="userAccount">The identifier for the user's session container.</param>
        /// <returns>A ScalarModel indicating initialization success.</returns>
        Task<ScalarModel> InitializeAsync(bool register, string userAccount);
        Task<ScalarModel> InitializeAsync(); // Für reines Memory (parameterlos)

        /// <summary>
        /// Retrieves token or metadata information specifically for TPC logic.
        /// </summary>
        Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams);

        /// <summary>
        /// Saves a record directly to the RAM cache.
        /// Maps to Dam.Save.
        /// </summary>
        Task<ScalarModel> Save(Dictionary<string, string> dbParams);

        /// <summary>
        /// Executes logic based on command cases (e.g., Update or Delete from RAM).
        /// Maps to Dam.ExecQuery.
        /// </summary>
        Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams);

        /// <summary>
        /// Retrieves a single value from the RAM cache based on parameters.
        /// Maps to Dam.Scalar.
        /// </summary>
        Task<ScalarModel> Scalar(Dictionary<string, string> dbParams);

        /// <summary>
        /// Queries the RAM cache using LINQ based on dbParams and returns a typed list.
        /// Maps to Dam.Read.
        /// </summary>
        /// <typeparam name="T">The model type to map the memory records to.</typeparam>
        Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new();

        /// <summary>
        /// LEVEL 1: Clears all records from the RAM cache.
        /// </summary>
        Task<ScalarModel> ClearAllData();

        /// <summary>
        /// LEVEL 2: Resets the RAM cache (same as ClearAllData for Memory).
        /// </summary>
        Task<ScalarModel> DropAllTables();

        /// <summary>
        /// LEVEL 3: Full reset of the memory storage service.
        /// </summary>
        Task<ScalarModel> DeleteDB();
    }
}
