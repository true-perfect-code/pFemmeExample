using BlazorCore.Services.AppState;
using BlazorCore.Services.LocalJsonFile;
using BlazorCore.Services.Platform;
using BlazorCore.Services.ServerShared;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace pFemmeExample.Shared.Services.Platform
{
    /// <summary>
    /// Plattform-spezifische Implementierung von IPlatformStorage für WASM/PWA und Capacitor.
    /// Nutzt JS-Interop für localStorage/Cookies (Web) und Capacitor Preferences (Native).
    /// </summary>
    public class PlatformStorage : IPlatformStorageBase
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IServiceProvider _serviceProvider;

        private const string JsWebPrefix = "pE_Web";
        private const string JsCapPrefix = "pE_Capacitor";


        public PlatformStorage(IJSRuntime jsRuntime, IServiceProvider serviceProvider)
        {
            _jsRuntime = jsRuntime;
            _serviceProvider = serviceProvider;
        }

        private IPlatformBase _platformBase => _serviceProvider.GetRequiredService<IPlatformBase>();

        private bool IsNative => _platformBase.GetCurrPlatform() != PLATFORMS.WASM;

        private string GetPrefix() => IsNative ? JsCapPrefix : JsWebPrefix;

        // ========================================================================
        // SECURE STORAGE (asynchron)
        // ========================================================================

        /// <summary>
        /// Stores a value securely.
        /// WASM: Uses setCookie logic from app.js (Cookie with localStorage fallback).
        /// Native: Uses Capacitor Preferences via cap.js.
        /// </summary>
        public async Task<ScalarModel> SetAsync(string identifier, string value)
        {
            var prefix = GetPrefix();

            if (!IsNative)
            {
                //// Standard Web Logic
                //await _jsRuntime.InvokeVoidAsync($"{prefix}.setValue", identifier, value, 7);
                //return new ScalarModel { out_value_bool = true };

                // Neue Implementirung für WASM Blazor Pwa
                // ---------------------------------------
                var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();

                const string COOKIE_FOLDER = "Cookies";
                string fileName = $"{identifier}.json";

                // SecurityClient NUR HIER instanziieren (kein Service!)
                using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
                {
                    // Verschlüsseln (wie beim Pin-Cookie)
                    byte[] appPepper = await aes.DecryptPepperAsync();
                    string encryptedValue = await aes.EncryptBase32SecretAsync(value, appPepper);

                    await localJsonFile.WritePhysicalFileAsync(COOKIE_FOLDER, fileName, encryptedValue);
                }

                return new ScalarModel { out_value_bool = true };
            }
            else
            {
                // Native Logic with full error reporting
                var result = await _jsRuntime.InvokeAsync<ScalarModel>($"{prefix}.setStorage", identifier, value);

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    Console.WriteLine($"[Storage Error]: {result.out_err}");
                }

                return result;
            }
        }

        /// <summary>
        /// Removes a value from storage.
        /// </summary>
        public async Task<ScalarModel> RemoveAsync(string identifier)
        {
            var prefix = GetPrefix();

            try
            {
                if (!IsNative)
                {
                    //// Web Logic: resetCookie
                    ////await _jsRuntime.InvokeVoidAsync($"{prefix}.resetCookie", identifier);
                    //await _jsRuntime.InvokeVoidAsync($"{prefix}.deleteCookieAndLocalStorage", identifier);
                    //return new ScalarModel { out_value_bool = true };

                    // Neue Implementirung für WASM Blazor Pwa
                    // ---------------------------------------
                    var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();

                    const string COOKIE_FOLDER = "Cookies";
                    string fileName = $"{identifier}.json";

                    await localJsonFile.DeletePhysicalFileAsync(COOKIE_FOLDER, fileName);

                    return new ScalarModel { out_value_bool = true };
                }
                else
                {
                    // Native Logic
                    var result = await _jsRuntime.InvokeAsync<ScalarModel>($"{prefix}.removeStorage", identifier);

                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        Console.WriteLine($"[Storage] RemoveAsync Failed: {result.out_err}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = "RemoveAsync Exception: " + ex.Message, out_value_bool = false };
            }
        }

        /// <summary>
        /// Retrieves a value from storage.
        /// WASM: Uses getValue from app.js (Checks Cookie, then localStorage).
        /// Native: Uses getStorage from cap.js.
        /// </summary>
        public async Task<ScalarModel> GetAsync(string identifier)
        {
            var prefix = GetPrefix();

            try
            {
                if (!IsNative)
                {
                    //// Web Logic
                    //var val = await _jsRuntime.InvokeAsync<string?>($"{prefix}.getValue", identifier);
                    //return new ScalarModel { out_value_str = val ?? "", out_value_bool = true };

                    // Neue Implementirung für WASM Blazor Pwa
                    // ---------------------------------------
                    var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();

                    const string COOKIE_FOLDER = "Cookies";
                    string fileName = $"{identifier}.json";

                    // Aus IndexedDB lesen
                    string? encryptedValue = await localJsonFile.ReadFileAsync(COOKIE_FOLDER, fileName);

                    if (string.IsNullOrEmpty(encryptedValue))
                    {
                        return new ScalarModel { out_value_str = "", out_value_bool = true };
                    }

                    // SecurityClient NUR HIER instanziieren (kein Service!)
                    using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
                    {
                        // Entschlüsseln
                        byte[] appPepper = await aes.DecryptPepperAsync();
                        string value = await aes.DecryptBase32SecretAsync(encryptedValue, appPepper);

                        return new ScalarModel { out_value_str = value, out_value_bool = true };
                    }
                }
                else
                {
                    // Native Logic
                    var result = await _jsRuntime.InvokeAsync<ScalarModel>($"{prefix}.getStorage", identifier);

                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        Console.WriteLine($"[Storage] Native Storage Read Failed: {result.out_err}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = "GetAsync Exception: " + ex.Message, out_value_bool = false };
            }
        }

        // ========================================================================
        // PREFERENCES (synchron, unverschlüsselt)
        // ========================================================================

        // Lazy Cache: Wird bei Bedarf gefüllt, keine öffentliche Init-Methode!
        private readonly Dictionary<string, string> _preferenceCache = new();
        private readonly HashSet<string> _preferenceCacheMisses = new(); // Für nicht vorhandene Keys

        /// <summary>
        /// Sets a preference value synchronously (unencrypted).
        /// </summary>
        public void SetPreference(string key, string value)
        {
            // Cache sofort aktualisieren
            _preferenceCache[key] = value;
            _preferenceCacheMisses.Remove(key); // Key existiert jetzt

            if (!IsNative)
            {
                //// Web: Fallback to async? For now, we use async void or localStorage directly
                //// Since Web doesn't have sync JS interop for storage, we use async fire-and-forget
                //_ = _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.setValue", key, value, 365);

                // Async aufrufen (Fire-and-forget)
                _ = SetPreferenceAsync(key, value);
            }
            else
            {
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                jsInProcess.InvokeVoid($"{JsCapPrefix}.setStorageSync", key, value);
            }
        }

        private async Task SetPreferenceAsync(string key, string value)
        {
            try
            {
                var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();
                const string PREF_FOLDER = "Preferences";
                string fileName = $"{key}.json";

                await localJsonFile.WritePhysicalFileAsync(PREF_FOLDER, fileName, value);
            }
            catch (Exception ex)
            {
                // Nur loggen, nicht werfen (Fire-and-forget)
                // await _appState.Error($"[PlatformStorage] Failed to save preference {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a preference value synchronously.
        /// </summary>
        public string? GetPreference(string key)
        {
            // 1. Cache prüfen
            if (_preferenceCache.TryGetValue(key, out var cachedValue))
                return cachedValue;

            // 2. Bereits bekannter Fehlschlag (vermeidet wiederholte fehlgeschlagene Leseversuche)
            if (_preferenceCacheMisses.Contains(key))
                return null;

            if (!IsNative)
            {
                try
                {
                    var jsInProcess = (IJSInProcessRuntime)_jsRuntime;

                    // Versuche aus IndexedDB zu lesen (über den bestehenden LocalJsonFile)
                    // Aber Achtung: Das ist ASYNCHRON! Wir brauchen einen synchronen Weg.
                    // Daher: Verwende localStorage/Cookie als synchronen Fallback.

                    // Fallback 1: localStorage (synchron)
                    var value = jsInProcess.Invoke<string?>("localStorage.getItem", key);
                    if (value != null)
                    {
                        _preferenceCache[key] = value;
                        return value;
                    }

                    // Fallback 2: Cookie (synchron)
                    value = jsInProcess.Invoke<string?>($"{JsWebPrefix}.getCookie", key);
                    if (value != null)
                    {
                        _preferenceCache[key] = value;
                        return value;
                    }
                }
                catch { }

                // Kein Wert gefunden
                _preferenceCacheMisses.Add(key);
                return null;
            }
            else
            {
                //var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                //return jsInProcess.Invoke<string?>($"{JsCapPrefix}.getStorageSync", key);

                // Capacitor: Aus SecureStorage lesen
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                var value = jsInProcess.Invoke<string?>($"{JsCapPrefix}.getStorageSync", key);
                if (value != null)
                {
                    _preferenceCache[key] = value;
                    return value;
                }

                _preferenceCacheMisses.Add(key);
                return null;
            }
        }

        /// <summary>
        /// Removes a preference value synchronously.
        /// </summary>
        public void RemovePreference(string key)
        {
            // Aus Cache entfernen
            _preferenceCache.Remove(key);
            _preferenceCacheMisses.Add(key); // Als nicht vorhanden markieren

            if (!IsNative)
            {
                //_ = _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.resetCookie", key);

                // Async aus IndexedDB löschen
                _ = RemovePreferenceAsync(key);

                // Auch aus localStorage/Cookie löschen (Cleanup)
                try
                {
                    var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                    jsInProcess.InvokeVoid("localStorage.removeItem", key);
                    jsInProcess.InvokeVoid($"{JsWebPrefix}.resetCookie", key);
                }
                catch { }
            }
            else
            {
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                jsInProcess.InvokeVoid($"{JsCapPrefix}.removeStorageSync", key);
            }
        }

        private async Task RemovePreferenceAsync(string key)
        {
            try
            {
                var localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();
                const string PREF_FOLDER = "Preferences";
                string fileName = $"{key}.json";

                await localJsonFile.DeletePhysicalFileAsync(PREF_FOLDER, fileName);
            }
            catch (Exception ex)
            {
                //  await _appState.Error($"[PlatformStorage] Failed to remove preference {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a preference key exists.
        /// </summary>
        public bool ContainsPreference(string key)
        {
            // Cache prüfen
            if (_preferenceCache.ContainsKey(key))
                return true;

            // Bekannter Fehlschlag
            if (_preferenceCacheMisses.Contains(key))
                return false;

            if (!IsNative)
            {
                try
                {
                    var jsInProcess = (IJSInProcessRuntime)_jsRuntime;

                    // In localStorage prüfen
                    var val = jsInProcess.Invoke<string?>("localStorage.getItem", key);
                    if (val != null) return true;

                    // In Cookie prüfen
                    val = jsInProcess.Invoke<string?>($"{JsWebPrefix}.getCookie", key);
                    if (val != null) return true;
                }
                catch { }

                _preferenceCacheMisses.Add(key);
                return false;
            }
            else
            {
                //var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                //return jsInProcess.Invoke<bool>($"{JsCapPrefix}.containsStorageSync", key);

                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                var exists = jsInProcess.Invoke<bool>($"{JsCapPrefix}.containsStorageSync", key);
                if (!exists) _preferenceCacheMisses.Add(key);
                return exists;
            }
        }
    }
}