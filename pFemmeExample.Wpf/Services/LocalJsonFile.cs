using BlazorCore.Services.LocalJsonFile;
using BlazorCore.Services.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace pFemmeExample.Wpf.Services
{
    /// <summary>
    /// WPF-specific implementation of ILocalJsonFile.
    /// Handles physical file I/O using System.IO.
    /// </summary>
    /// <remarks>
    /// Storage Path: %LocalAppData%/{ApplicationName}/Storage/{UserHash}/{TableName}/{RecordId}.json
    /// </remarks>
    public class LocalJsonFile : ILocalJsonFile
    {
        private readonly string _baseStoragePath;
        private readonly string _dbName;

        public LocalJsonFile(IServiceProvider serviceProvider)
        {
            _dbName = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.ApplicationName;

            // Use LocalApplicationData for parity with SQLite storage
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _dbName);

            _baseStoragePath = Path.Combine(appDataPath, "Storage");
        }

        /// <inheritdoc />
        public Task<ScalarModel> PreparePhysicalStorageAsync(string dbName, string userAccount)
        {
            // Für WPF: Verzeichnisstruktur wird bei Bedarf automatisch erstellt
            return Task.FromResult(new ScalarModel { out_value_bool = true });
        }

        /// <inheritdoc />
        public async Task<List<string>> ReadTableFilesAsync(string userAccount, string tableName)
        {
            var results = new List<string>();

            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string tablePath = Path.Combine(_baseStoragePath, accountHash, tableName);

            if (!Directory.Exists(tablePath))
            {
                Directory.CreateDirectory(tablePath);
                return results;
            }

            string[] files = Directory.GetFiles(tablePath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    string content = await File.ReadAllTextAsync(file);
                    results.Add(content);
                }
                catch (Exception ex)
                {
                    // Log error if needed
                }
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<string?> ReadFileAsync(string userAccount, string fileName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string fullPath = Path.Combine(_baseStoragePath, accountHash, fileName);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                return await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                // Log error if needed
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<ScalarModel> WritePhysicalFileAsync(string userAccount, string fileName, string encryptedContent)
        {
            ScalarModel result = new();

            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string fullPath = Path.Combine(_baseStoragePath, accountHash, fileName);
                string? directory = Path.GetDirectoryName(fullPath);

                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Atomic write: write to temp file first, then rename
                string tempFile = fullPath + ".tmp";
                await File.WriteAllTextAsync(tempFile, encryptedContent);

                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                File.Move(tempFile, fullPath);

                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                result.out_err = $"[WPF-LocalJsonFile] WritePhysicalFileAsync error: {ex.Message}";
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> DeletePhysicalFileAsync(string userAccount, string fileName)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string fullPath = Path.Combine(_baseStoragePath, accountHash, fileName);

                // If the file does not exist, our cleanup goal is already successfully met.
                if (!File.Exists(fullPath))
                {
                    return true;
                }

                // Execute physical deletion
                File.Delete(fullPath);

                // Verify with the OS that the file is truly gone to guarantee 100% RAM-to-Disk consistency
                return !File.Exists(fullPath);
            }
            catch (Exception ex)
            {
                // Log error if needed (e.g., file is locked by another process, unauthorized access)
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAllFilesAsync(string userAccount)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string userPath = Path.Combine(_baseStoragePath, accountHash);

                // If the user directory doesn't even exist, all files are already successfully gone.
                if (!Directory.Exists(userPath))
                {
                    return true;
                }

                // 1. Delete all subdirectories (tables) recursively
                string[] subDirectories = Directory.GetDirectories(userPath);
                foreach (var dir in subDirectories)
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }

                // 2. Delete any remaining files directly at the root user level
                string[] rootFiles = Directory.GetFiles(userPath);
                foreach (var file in rootFiles)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                // 3. Final safety verification: Are all subdirectories and files truly gone?
                bool standardTablesCleared = Directory.GetDirectories(userPath).Length == 0;
                bool rootFilesCleared = Directory.GetFiles(userPath).Length == 0;

                return standardTablesCleared && rootFilesCleared;
            }
            catch (Exception ex)
            {
                // Log error if needed (e.g., persistent I/O locks, unauthorized access)
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeletePhysicalStorageAsync(string userAccount)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string userPath = Path.Combine(_baseStoragePath, accountHash);

                // If the entire user directory doesn't exist, our deletion goal is already met.
                if (!Directory.Exists(userPath))
                {
                    return true;
                }

                // Trigger recursive deletion of the entire user directory and all its contents
                Directory.Delete(userPath, true);

                // Hard validation check against the OS: Ensure the directory is truly gone
                return !Directory.Exists(userPath);
            }
            catch (Exception ex)
            {
                // Log error if needed (e.g., open file handle from another thread, anti-virus lock)
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteTableFilesAsync(string userAccount, string tableName)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string tablePath = Path.Combine(_baseStoragePath, accountHash, tableName);

                // If the table directory doesn't exist, our cleanup goal is already successfully met.
                if (!Directory.Exists(tablePath))
                {
                    return true;
                }

                // 1. Delete all files within the table directory
                string[] files = Directory.GetFiles(tablePath);
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                // 2. Safety verification: Ensure all files are truly gone
                bool filesCleared = Directory.GetFiles(tablePath).Length == 0;

                // 3. Optional: If you want to ensure the directory structure stays intact 
                // for future writes, the directory itself remains. 
                // If it was successfully cleared, return true.
                return filesCleared;
            }
            catch (Exception ex)
            {
                // Log error if needed (e.g., file lock, unauthorized access)
                return false;
            }
        }

        /// <summary>
        /// Generates a short hash from a string (for folder names).
        /// </summary>
        private string GenerateShortHash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes).Substring(0, 16);
        }
    }
}
