using BlazorCore.Services.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorCore.Services.LocalJsonFile
{
    /// <summary>
    /// Interface for JSON-based file storage (one file per record).
    /// Pure file operations - no business logic, no RAM cache.
    /// </summary>
    public interface ILocalJsonFile
    {
        /// <summary>
        /// Prepares the physical storage (creates directories, initializes JS modules).
        /// </summary>
        Task<ScalarModel> PreparePhysicalStorageAsync(string dbName, string userAccount);

        /// <summary>
        /// Reads ALL JSON files in a table directory.
        /// Returns list of JSON content strings.
        /// </summary>
        Task<List<string>> ReadTableFilesAsync(string userAccount, string tableName);

        /// <summary>
        /// Reads a single JSON file from storage.
        /// </summary>
        /// <param name="userAccount">The user account identifier</param>
        /// <param name="fileName">The full file path (e.g., "AuthUsers/123456.json")</param>
        /// <returns>JSON content string, or null if file doesn't exist</returns>
        Task<string?> ReadFileAsync(string userAccount, string fileName);

        /// <summary>
        /// Writes a single JSON file (encrypted if configured).
        /// </summary>
        Task<ScalarModel> WritePhysicalFileAsync(string userAccount, string fileName, string encryptedContent);

        /// <summary>
        /// Deletes a single JSON file.
        /// </summary>
        Task<bool> DeletePhysicalFileAsync(string userAccount, string fileName);

        /// <summary>
        /// Deletes ALL files for a user (entire storage directory).
        /// </summary>
        Task<bool> DeleteAllFilesAsync(string userAccount);

        /// <summary>
        /// Deletes the entire physical storage for a user.
        /// </summary>
        Task<bool> DeletePhysicalStorageAsync(string userAccount);

        /// <summary>
        /// Deletes all files within a specific table directory for a user.
        /// </summary>
        /// <param name="userAccount">The user account identifier</param>
        /// <param name="tableName">The name of the table/directory to clear</param>
        /// <returns>True if the directory was successfully cleared or didn't exist</returns>
        Task<bool> DeleteTableFilesAsync(string userAccount, string tableName);
    }
}