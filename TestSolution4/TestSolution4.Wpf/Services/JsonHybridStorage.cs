using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BlazorCore.Models;
using TestSolution4.Shared.Services.JsonHybridStorage;
using Microsoft.Extensions.DependencyInjection;

namespace TestSolution4.Wpf.Services
{
    /// <summary>
    /// WPF-specific implementation of the JsonHybridStorage.
    /// Handles physical file IO using System.IO.
    /// Storage Path: %LocalAppData%/{ApplicationName}/Storage/{UserHash}/{TableName}/{RecordId}.json
    /// </summary>
    public class JsonHybridStorage : JsonHybridStorageBase
    {
        private readonly string _baseStoragePath;

        public JsonHybridStorage(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            // Dynamischer Zugriff ohne magische Strings
            //string dbName = _globalState?.ConfigGeneral?.ApplicationName ?? "TpcDefaultApp";

            DbName = TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName;

            // Wechsel auf LocalApplicationData (Parität zu SQLite)
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                DbName);

            _baseStoragePath = Path.Combine(appDataPath, "Storage");
        }

        #region Abstract Methods Implementation (System.IO)

        protected override async Task<List<string>> ReadTableFilesAsync(string userAccount, string tableName)
        {
            var results = new List<string>();

            // Hash-Logik für den User-Ordner (Parität zu SQLite)
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string tablePath = Path.Combine(_baseStoragePath, accountHash, tableName);

            if (!Directory.Exists(tablePath))
                return results;

            // Wir lesen alle .json Dateien im Tabellen-Verzeichnis
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
                    await _appState.Error($"[WPF-Storage] Error reading file {file}: {ex.Message}");
                }
            }

            return results;
        }

        protected override async Task<bool> WritePhysicalFileAsync(string userAccount, string fileName, string encryptedContent)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string fullPath = Path.Combine(_baseStoragePath, accountHash, fileName);
                string directory = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Atomic Write: Erst in Temp-Datei, dann umbenennen (für maximale Datensicherheit)
                string tempFile = fullPath + ".tmp";
                await File.WriteAllTextAsync(tempFile, encryptedContent);

                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                File.Move(tempFile, fullPath);
                return true;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF-Storage] Write Error: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeletePhysicalFileAsync(string userAccount, string fileName)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string fullPath = Path.Combine(_baseStoragePath, accountHash, fileName);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF-Storage] Delete Error: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeleteAllFilesAsync(string userAccount)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string userPath = Path.Combine(_baseStoragePath, accountHash);

                if (Directory.Exists(userPath))
                {
                    // Wir löschen alle Unterordner (Tabellen)
                    foreach (var dir in Directory.GetDirectories(userPath))
                        Directory.Delete(dir, true);

                    foreach (var file in Directory.GetFiles(userPath))
                        File.Delete(file);
                }
                return true;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF-Storage] ClearAll Error: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeletePhysicalStorageAsync(string userAccount)
        {
            try
            {
                string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
                string userPath = Path.Combine(_baseStoragePath, accountHash);

                if (Directory.Exists(userPath))
                {
                    Directory.Delete(userPath, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF-Storage] DeleteDB Error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helpers (Identity Hash)

        ///// <summary>
        ///// Erzeugt einen 10-stelligen SHA-256 Hash für den Dateinamen.
        ///// Parität zur JavaScript Krypto-Implementierung.
        ///// </summary>
        //private string GenerateShortHash(string input)
        //{
        //    using var sha256 = SHA256.Create();
        //    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        //    return BitConverter.ToString(bytes).Replace("-", "").ToLower().Substring(0, 10);
        //}

        #endregion
    }
}