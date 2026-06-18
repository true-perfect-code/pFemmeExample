using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.JsonHybridStorage
{
    /// <summary>
    /// Interface for the Persistent JSON Hybrid storage implementation.
    /// This follows the same signature as IMemoryStorageBase and ISqLiteBase to ensure 
    /// seamless integration with DamBase.
    /// Implementation principle: "Logic first, Storage second" – Operations occur in RAM 
    /// first and are then persisted as encrypted JSON files (File-per-Record).
    /// </summary>
    public interface IJsonHybridStorageBase
    {
        /// <summary>
        /// Indicates whether the RAM cache and the file-system bridge are initialized.
        /// </summary>
        bool IsInitialized { get; set; }

        /// <summary>
        /// Initializes the storage, prepares RAM lists, and performs the 'Initial Load' 
        /// by decrypting existing records from the persistent store.
        /// </summary>
        /// <param name="register">Indicates if this is a new registration context.</param>
        /// <param name="userAccount">The identifier (Identity-ID) used for key derivation and storage pathing.</param>
        /// <returns>A ScalarModel indicating initialization and decryption success.</returns>
        Task<ScalarModel> InitializeAsync(bool register, string userAccount);

        /// <summary>
        /// Retrieves token or metadata information specifically for TPC logic.
        /// </summary>
        Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams);

        /// <summary>
        /// Saves a record to the RAM cache and triggers the encrypted persistence (Atomic Write).
        /// Maps to Dam.Save.
        /// </summary>
        Task<ScalarModel> Save(Dictionary<string, string> dbParams);

        /// <summary>
        /// Executes logic based on command cases (e.g., Delete record from RAM and Disk).
        /// Maps to Dam.ExecQuery.
        /// </summary>
        Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams);

        /// <summary>
        /// Retrieves a single value from the RAM cache (Read-Factor 100x faster than Disk).
        /// Maps to Dam.Scalar.
        /// </summary>
        Task<ScalarModel> Scalar(Dictionary<string, string> dbParams);

        /// <summary>
        /// Queries the RAM cache using LINQ and returns a typed list.
        /// Maps to Dam.Read.
        /// </summary>
        /// <typeparam name="T">The model type to map the memory records to.</typeparam>
        Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new();

        /// <summary>
        /// LEVEL 1: Clears all records from RAM and deletes all associated JSON files from the device.
        /// </summary>
        Task<ScalarModel> ClearAllData();

        /// <summary>
        /// LEVEL 2: Resets the storage structure (equivalent to Drop Tables).
        /// </summary>
        Task<ScalarModel> DropAllTables();

        /// <summary>
        /// LEVEL 3: Full reset and deletion of the encrypted database/storage directory.
        /// </summary>
        Task<ScalarModel> DeleteDB();
    }
}
