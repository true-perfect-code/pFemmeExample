using Microsoft.JSInterop;
using BlazorCore.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.JsonHybridStorage;
using System.Security.Cryptography;
using System.Text;
using BlazorCore.Services.SqlClient;

namespace TestSolution4.Shared.Services.JsonHybridStorage
{
    public class JsonHybridStorageWasm : JsonHybridStorageBase
    {
        private readonly IJSRuntime _js;

        // Präfixe für die JS-Dateien
        private const string JsCapPrefix = "pE_Capacitor.storage";
        private const string JsWebPrefix = "pE_Web.storage";

        public JsonHybridStorageWasm(IServiceProvider serviceProvider, IJSRuntime js) : base(serviceProvider)
        {
            _js = js;
            DbName = TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName;
        }

        private bool IsNative => _platform.GetCurrPlatform() != PLATFORMS.WASM;

        private string GetPrefix() => IsNative ? JsCapPrefix : JsWebPrefix;

        #region Abstract Methods Implementation

        protected override async Task PreparePhysicalStorageAsync(string dbName, string userAccount)
        {
            try
            {
                var prefix = GetPrefix();
                // Wir rufen eine kleine Init-Methode in JS auf
                await _js.InvokeVoidAsync($"{prefix}.prepareStorage", dbName, userAccount);
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Hybrid-WASM] PrepareStorage failed: {ex.Message}");
            }
        }

        protected override async Task<List<string>> ReadTableFilesAsync(string userAccount, string tableName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            var prefix = GetPrefix();

            try
            {
                // NEU: DbName wird als erster Parameter mitgegeben
                var result = await _js.InvokeAsync<ScalarModel>($"{prefix}.readAllTableFiles",
                    DbName, accountHash, tableName);

                //if (result.out_value_bool && !string.IsNullOrEmpty(result.out_value_str))
                // Ein leerer out_err ist bei Read-Operationen das sicherste Zeichen für Erfolg
                if (string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(result.out_value_str) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Hybrid-WASM] Read Error in {tableName}: {ex.Message}");
            }
            return new List<string>();
        }

        protected override async Task<bool> WritePhysicalFileAsync(string userAccount, string fileName, string encryptedContent)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            var prefix = GetPrefix();

            try
            {
                // NEU: DbName wird als erster Parameter mitgegeben
                var result = await _js.InvokeAsync<ScalarModel>($"{prefix}.writeFile",
                    DbName, accountHash, fileName, encryptedContent);
                return result.out_value_bool;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Hybrid-WASM] Write Error {fileName}: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeletePhysicalFileAsync(string userAccount, string fileName)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            var prefix = GetPrefix();

            try
            {
                // NEU: DbName als erster Parameter, damit JS die richtige DB/Ordner findet
                var result = await _js.InvokeAsync<ScalarModel>($"{prefix}.deleteFile",
                    DbName, accountHash, fileName);
                return result.out_value_bool;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Hybrid-WASM] Delete Error {fileName}: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeleteAllFilesAsync(string userAccount)
        {
            string accountHash = GenerateShortHash(userAccount.Trim().ToLower());
            var prefix = GetPrefix();

            try
            {
                // NEU: DbName als erster Parameter für den Full-Purge des User-Accounts
                var result = await _js.InvokeAsync<ScalarModel>($"{prefix}.purgeUserStorage",
                    DbName, accountHash);
                return result.out_value_bool;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Hybrid-WASM] Purge Error: {ex.Message}");
                return false;
            }
        }

        protected override async Task<bool> DeletePhysicalStorageAsync(string userAccount)
        {
            // Diese Methode löscht die gesamte physische Präsenz eines Users.
            // Da DeleteAllFilesAsync bereits auf Account-Ebene (accountHash) löscht,
            // ist der Aufruf hier korrekt, sofern er den DbName-Parameter nun mitführt.
            return await DeleteAllFilesAsync(userAccount);
        }

        #endregion

        #region Helpers

        //private string GenerateShortHash(string input)
        //{
        //    using var sha256 = SHA256.Create();
        //    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        //    return BitConverter.ToString(bytes).Replace("-", "").ToLower().Substring(0, 10);
        //}

        #endregion
    }
}
