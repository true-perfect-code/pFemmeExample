using BlazorCore.Services.AppState;
using BlazorCore.Services.LocalJsonFile;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pFemmeExample.Shared.Services.LocalJsonFile
{
    /// <summary>
    /// WASM/Capacitor implementation of ILocalJsonFile.
    /// Uses JavaScript interop to access IndexedDB (PWA) or native filesystem (Capacitor).
    /// </summary>
    public class LocalJsonFile : ILocalJsonFile
    {
        private readonly IJSRuntime _js;
        private readonly IPlatformBase _platform;
        private readonly IAppStateBase _appState;
        private readonly string _dbName;

        // JavaScript module prefixes for different platforms
        private const string JsCapPrefix = "pE_Capacitor.storage";
        private const string JsWebPrefix = "pE_Web.storage";

        public LocalJsonFile(IServiceProvider serviceProvider, IJSRuntime js)
        {
            _js = js;
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _dbName = pFemmeExample.Shared.Global.Configuration.ConfigGeneral.ApplicationName;
        }

        private bool IsNative => _platform.GetCurrPlatform() != PLATFORMS.WASM;
        private string GetPrefix() => IsNative ? JsCapPrefix : JsWebPrefix;

        /// <inheritdoc />
        public async Task<ScalarModel> PreparePhysicalStorageAsync(string dbName, string userAccount)
        {
            var result = new ScalarModel();
            try
            {
                string prefix = GetPrefix();
                await _js.InvokeVoidAsync($"{prefix}.prepareStorage", dbName, userAccount);
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<List<string>> ReadTableFilesAsync(string userAccount, string tableName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string prefix = GetPrefix();

            await _appState.Log("[LocalJsonFile ReadTableFilesAsync] START");

            try
            {
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.readAllTableFiles",
                    _dbName,
                    accountHash,
                    tableName);

                if (string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(result.out_value_str)
                           ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                await _appState.Error("[LocalJsonFile ReadTableFilesAsync] ERROR : " + ex.Message);
            }

            await _appState.Log("[LocalJsonFile ReadTableFilesAsync] END");
            return new List<string>();
        }

        /// <inheritdoc />
        public async Task<string?> ReadFileAsync(string userAccount, string fileName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string prefix = GetPrefix();

            await _appState.Log("[LocalJsonFile ReadFileAsync] START");

            try
            {
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.readFile",
                    _dbName,
                    accountHash,
                    fileName);

                if (string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
                {
                    await _appState.Log("[LocalJsonFile ReadFileAsync] END");
                    return result.out_value_str;
                }
            }
            catch (Exception ex)
            {
                await _appState.Error("[LocalJsonFile ReadFileAsync] ERROR : " + ex.Message);
            }
            await _appState.Log("[LocalJsonFile ReadFileAsync] END result=null");
            return null;
        }

        /// <inheritdoc />
        public async Task<ScalarModel> WritePhysicalFileAsync(string userAccount, string fileName, string encryptedContent)
        {
            ScalarModel result = new();

            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string prefix = GetPrefix();

            await _appState.Log("[LocalJsonFile WritePhysicalFileAsync] START");

            try
            {
                var result_write = await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.writeFile",
                    _dbName,
                    accountHash,
                    fileName,
                    encryptedContent);

                result.out_value_bool = result_write.out_value_bool;
                result.out_err = result_write.out_err;
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.ToString();
                await _appState.Error("[LocalJsonFile WritePhysicalFileAsync] ERROR : " + ex.Message);
            }

            await _appState.Log("[LocalJsonFile WritePhysicalFileAsync] END");

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> DeletePhysicalFileAsync(string userAccount, string fileName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string prefix = GetPrefix();

            //await _appState.Log("[LocalJsonFile DeletePhysicalFileAsync] START");

            try
            {
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.deleteFile",
                    _dbName,
                    accountHash,
                    fileName);

                // If the result is null or an explicit error message is returned from JS,
                // we treat it as a failed operation to protect RAM-to-Disk data consistency.
                if (result == null || !string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Log("[LocalJsonFile DeletePhysicalFileAsync] END result=false");
                    return false;
                }

                await _appState.Log($"[LocalJsonFile DeletePhysicalFileAsync] END result={result.out_value_bool}");
                return result.out_value_bool;
            }
            catch (Exception ex)
            {
                await _appState.Error("[LocalJsonFile DeletePhysicalFileAsync] ERROR : " + ex.Message);
                // Caught if JS interop itself fails (e.g., module missing or engine disposed)
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAllFilesAsync(string userAccount)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string prefix = GetPrefix();

            await _appState.Log("[LocalJsonFile DeleteAllFilesAsync] START");

            try
            {
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.purgeUserStorage",
                    _dbName,
                    accountHash);

                // Ensure the result object is valid and no technical errors were reported in out_err
                if (result == null || !string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Log("[LocalJsonFile DeleteAllFilesAsync] END result=false");
                    return false;
                }

                await _appState.Log($"[LocalJsonFile DeleteAllFilesAsync] END result={result.out_value_bool}");
                return result.out_value_bool;
            }
            catch (Exception ex)
            {
                await _appState.Error("[LocalJsonFile DeleteAllFilesAsync] ERROR : " + ex.Message);
                // Out-commented error logging as per project rules
                // await _appState.Error($"[LocalJsonFile-WASM] Purge error: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeletePhysicalStorageAsync(string userAccount)
        {
            // For WASM Web (IndexedDB), deleting physical storage maps directly to purging all keys.
            // For Capacitor, the underlying native purge implementation already removes the entire 
            // physical directory root recursively. Therefore, invoking DeleteAllFilesAsync is perfectly sufficient.
            return await DeleteAllFilesAsync(userAccount);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteTableFilesAsync(string userAccount, string tableName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            string prefix = GetPrefix();

            await _appState.Log("[LocalJsonFile DeleteTableFilesAsync] START");

            try
            {
                // Wir rufen eine dedizierte JS-Funktion auf, die den Ordner der Tabelle leert.
                // Das ist die logische Entsprechung zu 'purgeUserStorage', aber auf Tabellenebene.
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.purgeTable",
                    _dbName,
                    accountHash,
                    tableName);

                // Konsistenz-Check: Wenn JS meldet, dass etwas schiefgelaufen ist (result null oder error),
                // geben wir false zurück, damit das RAM-Update im Executor abgebrochen wird.
                if (result == null || !string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Log("[LocalJsonFile DeleteTableFilesAsync] END result=false");
                    return false;
                }

                await _appState.Log($"[LocalJsonFile DeleteTableFilesAsync] END result={result.out_value_bool}");
                return result.out_value_bool;
            }
            catch (Exception ex)
            {
                await _appState.Error("[LocalJsonFile DeleteTableFilesAsync] ERROR : " + ex.Message);
                // Fehler bei der JS-Interoperabilität (z.B. Timeout oder Modul-Fehler)
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
