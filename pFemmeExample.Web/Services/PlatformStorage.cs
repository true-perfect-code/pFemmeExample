using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.JSInterop;

namespace pFemmeExample.Web.Services
{
    /// <summary>
    /// Plattform-spezifische Implementierung von IPlatformStorage für Blazor Server.
    /// Nutzt JS-Interop für Browser Local Storage / Cookies.
    /// </summary>
    public class PlatformStorage : IPlatformStorageBase
    {
        private readonly IJSRuntime _jsRuntime;
        private const string JsPrefix = "pE_Web";

        public PlatformStorage(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // ========================================================================
        // SECURE STORAGE (asynchron)
        // ========================================================================

        public async Task<ScalarModel> SetAsync(string identifier, string value)
        {
            var result = new ScalarModel();
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{JsPrefix}.setValue", identifier, value, 365);
                result.out_value_str = value;
                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                result.out_err = $"Web-Set-Error: {ex.Message}";
                result.out_value_bool = false;
            }
            return result;
        }

        public async Task<ScalarModel> GetAsync(string identifier)
        {
            var result = new ScalarModel();
            try
            {
                var val = await _jsRuntime.InvokeAsync<string?>($"{JsPrefix}.getValue", identifier);
                result.out_value_str = val ?? "";
                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                result.out_err = $"Web-Get-Error: {ex.Message}";
                result.out_value_bool = false;
            }
            return result;
        }

        public async Task<ScalarModel> RemoveAsync(string identifier)
        {
            var result = new ScalarModel();
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{JsPrefix}.deleteCookieAndLocalStorage", identifier);
                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                result.out_err = $"Web-Remove-Error: {ex.Message}";
                result.out_value_bool = false;
            }
            return result;
        }

        // ========================================================================
        // PREFERENCES (NOCH NICHT IMPLEMENTIERT - WIRD KONSOLIDIERT)
        // ========================================================================

        /// <summary>
        /// Setzt einen Preference-Wert.
        /// AKTUELL: Nicht implementiert auf Blazor Server.
        /// TODO: Konsolidierung mit einer plattformübergreifenden Preference-Lösung (z. B. server-seitige Indexed DB).
        /// </summary>
        public void SetPreference(string key, string value)
        {
            // Noch nicht implementiert – wird in zukünftiger Version konsolidiert
        }

        /// <summary>
        /// Liest einen Preference-Wert.
        /// AKTUELL: Nicht implementiert auf Blazor Server.
        /// TODO: Konsolidierung mit einer plattformübergreifenden Preference-Lösung.
        /// </summary>
        public string? GetPreference(string key)
        {
            // Noch nicht implementiert – wird in zukünftiger Version konsolidiert
            return null;
        }

        /// <summary>
        /// Entfernt einen Preference-Wert.
        /// AKTUELL: Nicht implementiert auf Blazor Server.
        /// TODO: Konsolidierung mit einer plattformübergreifenden Preference-Lösung.
        /// </summary>
        public void RemovePreference(string key)
        {
            // Noch nicht implementiert – wird in zukünftiger Version konsolidiert
        }

        /// <summary>
        /// Prüft, ob ein Preference-Key existiert.
        /// AKTUELL: Nicht implementiert auf Blazor Server.
        /// TODO: Konsolidierung mit einer plattformübergreifenden Preference-Lösung.
        /// </summary>
        public bool ContainsPreference(string key)
        {
            // Noch nicht implementiert – wird in zukünftiger Version konsolidiert
            return false;
        }
    }
}
