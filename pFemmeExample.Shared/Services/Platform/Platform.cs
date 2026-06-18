using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace pFemmeExample.Shared.Services.Platform
{
    public class Platform : BlazorCore.Services.Platform.IPlatformBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalStateBase _globalState;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;

        private PLATFORMS? _cachedPlatform = null;
        private const string JsWebPrefix = "pE_Web";
        private const string JsCapPrefix = "pE_Capacitor";

        public Platform(
            IServiceProvider serviceProvider,
            IGlobalStateBase globalState,
            NavigationManager navigationManager,
            IJSRuntime js)
        {
            _serviceProvider = serviceProvider;
            _globalState = globalState;
            _navigationManager = navigationManager;
            _jsRuntime = js;
        }

        // ========================================================================
        // INITIALIZATION
        // ========================================================================

        /// <summary>
        /// Initializes the JS bridge for Capacitor platform.
        /// Calls pE_Capacitor.init and captures the result/errors.
        /// </summary>
        public async Task<ScalarModel> InitializeJSAsync()
        {
            ScalarModel result = new();

            try
            {
                var exists = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.pE_Capacitor !== 'undefined'");

                if (!exists)
                {
                    result.out_err = "Capacitor bridge not found (Web Mode).";
                }

                result = await _jsRuntime.InvokeAsync<ScalarModel>($"{JsCapPrefix}.init");

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    result.out_err = result.out_err;
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"JS-Bridge Error during Init: {ex.Message}";
            }

            return result;
        }

        // ========================================================================
        // PLATFORM / DEVICE INFO
        // ========================================================================

        /// <summary>
        /// Always returns false for WASM/Capacitor.
        /// Used to distinguish native platform in higher-level logic.
        /// </summary>
        public bool IsNative => false;

        /// <summary>
        /// Retrieves the base directory for data storage.
        /// On WASM, returns empty string (sandbox root).
        /// On native platforms, returns the Capacitor DATA directory URI.
        /// </summary>
        public string GetBaseDirectory()
        {
            try
            {
                var platform = GetCurrPlatform();

                if (platform == PLATFORMS.WASM)
                {
                    return "";
                }

                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                return jsInProcess.Invoke<string>($"{JsCapPrefix}.getBaseDirectory");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] GetBaseDirectory failed: {ex.Message}");
                return "/";
            }
        }

        /// <summary>
        /// Retrieves the current platform using the Capacitor bridge or defaults to WASM.
        /// </summary>
        public PLATFORMS GetCurrPlatform()
        {
            if (_cachedPlatform.HasValue)
                return _cachedPlatform.Value;

            try
            {
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                string platformStr = jsInProcess.Invoke<string>($"{JsCapPrefix}.getPlatform");

                _cachedPlatform = platformStr.ToLower() switch
                {
                    "android" => PLATFORMS.ANDROID,
                    "ios" => PLATFORMS.IOS,
                    "ios-mac" => PLATFORMS.MAC_CLIENT,
                    "wasm" => PLATFORMS.WASM,
                    _ => PLATFORMS.WASM
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting platform, defaulting to WASM: {ex.Message}");
                return PLATFORMS.WASM;
            }

            return _cachedPlatform.Value;
        }

        /// <summary>
        /// Returns the native device type based on the current platform.
        /// </summary>
        public p11.UI.NativeDevice GetCurrDevice()
        {
            return GetCurrPlatform() switch
            {
                PLATFORMS.WASM => p11.UI.NativeDevice.WEB,
                PLATFORMS.ANDROID => p11.UI.NativeDevice.ANDROID,
                PLATFORMS.IOS => p11.UI.NativeDevice.IPHONE,
                PLATFORMS.MAC_CLIENT => p11.UI.NativeDevice.MAC,
                _ => p11.UI.NativeDevice.WEB
            };
        }

        /// <summary>
        /// Returns device information string (OS, version, model).
        /// </summary>
        public string GetDeviceInfo()
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            var platform = GetCurrPlatform();

            return platform == PLATFORMS.WASM
                ? jsInProcess.Invoke<string>($"{JsWebPrefix}.getDeviceInfo")
                : jsInProcess.Invoke<string>($"{JsCapPrefix}.getDeviceInfo");
        }

        /// <summary>
        /// Returns the form factor (Phone, Tablet, Desktop, Wasm).
        /// </summary>
        public async Task<string> GetFormFactor()
        {
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                return "Wasm";
            }

            return await _jsRuntime.InvokeAsync<string>($"{JsCapPrefix}.getDeviceType");
        }

        // ========================================================================
        // WINDOW / UI INFO
        // ========================================================================

        /// <summary>
        /// Returns the current window width in pixels.
        /// </summary>
        public async Task<double> GetWindowWidth()
        {
            return await _jsRuntime.InvokeAsync<double>("eval", "window.innerWidth");
        }

        /// <summary>
        /// Returns the current window height in pixels.
        /// </summary>
        public async Task<double> GetWindowHeight()
        {
            return await _jsRuntime.InvokeAsync<double>("eval", "window.innerHeight");
        }

        /// <summary>
        /// Returns the idiom platform (e.g., Browser-Desktop-Windows, Browser-Phone-iOS).
        /// </summary>
        public async Task<string> GetIdiomPlatform()
        {
            var platform = GetCurrPlatform();

            // Native (Capacitor)
            if (platform != PLATFORMS.WASM)
            {
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                return jsInProcess.Invoke<string>($"{JsCapPrefix}.getIdiomPlatform");
            }

            // Web/WASM fallback
            double screenWidth = await GetWindowWidth();
            string idiom = screenWidth switch
            {
                >= 1024 => "Desktop",
                >= 768 => "Tablet",
                _ => "Phone"
            };

            string os = await _jsRuntime.InvokeAsync<string>($"{JsWebPrefix}.getOSVersion");
            return $"Browser-{idiom}-{os}";
        }

        // ========================================================================
        // CLIPBOARD & SHARING
        // ========================================================================

        /// <summary>
        /// Copies text to the clipboard.
        /// </summary>
        public async Task CopyTextToClipboard(string text)
        {
            var platform = GetCurrPlatform();

            try
            {
                if (platform != PLATFORMS.WASM)
                {
                    await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.copyToClipboard", text);
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clipboard error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shares text using the native sharing dialog or Web Share API.
        /// </summary>
        public async Task ShareText(string title, string text)
        {
            var platform = GetCurrPlatform();

            try
            {
                if (platform == PLATFORMS.WASM)
                {
                    await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.shareText", title, text);
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.shareText", title, text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sharing failed or was cancelled: {ex.Message}");
            }
        }

        // ========================================================================
        // FILE & DIRECTORY PICKER
        // ========================================================================

        /// <summary>
        /// Opens a native directory picker.
        /// On WASM, returns empty string (no native directory access).
        /// </summary>
        public async Task<string?> DirectoryPicker()
        {
            await Task.Delay(10);
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                return string.Empty;
            }

            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            return jsInProcess.Invoke<string>($"{JsCapPrefix}.getDirectoryPath");
        }

        /// <summary>
        /// Opens a native file picker.
        /// NOT IMPLEMENTED on WASM/Capacitor.
        /// </summary>
        public Task<ScalarModel> FilePicker(string filter, string title)
        {
            return Task.FromResult(new ScalarModel());
        }

        /// <summary>
        /// Saves a file natively on the current platform.
        /// On WASM: triggers browser download.
        /// On Capacitor: saves to device storage via JS bridge.
        /// </summary>
        public async Task<ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
        {
            var result = new ScalarModel();

            try
            {
                var platform = GetCurrPlatform();

                if (stream.CanSeek) stream.Position = 0;

                // Web/WASM: Browser download
                if (platform == PLATFORMS.WASM)
                {
                    using var streamRef = new DotNetStreamReference(stream);
                    await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.downloadFileFromStream", filename, streamRef);

                    result.out_value_bool = true;
                    result.out_value_str = filename;
                    return result;
                }

                // Capacitor: Native file save
                byte[] fileBytes = stream is MemoryStream ms
                    ? ms.ToArray()
                    : await ReadStreamToByteArrayAsync(stream);

                string base64Data = Convert.ToBase64String(fileBytes);
                string jsonResponse = await _jsRuntime.InvokeAsync<string>($"{JsCapPrefix}.saveFileNative", filename, base64Data, title);

                result = System.Text.Json.JsonSerializer.Deserialize(
                    jsonResponse,
                    BlazorCore.JsonContext.Default.ScalarModel) ?? new ScalarModel { out_value_bool = false, out_err = "Deserialization failed" };
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.Message;
                result.out_local = "Platform: SaveFileNativeAsync Exception";
            }

            return result;
        }

        private static async Task<byte[]> ReadStreamToByteArrayAsync(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        // ========================================================================
        // EXTERNAL URL & AUTHENTICATION
        // ========================================================================

        /// <summary>
        /// Opens an external URL in the default browser or native in-app browser.
        /// </summary>
        public async Task OpenExternalUrl(string url)
        {
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                await _jsRuntime.InvokeVoidAsync("eval", $"window.open('{url}', '_blank')");
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.openExternalUrl", url);
            }
        }

        /// <summary>
        /// Starts the authentication flow for native platform (opens external browser).
        /// Returns null as token is handled via polling.
        /// </summary>
        public async Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false)
        {
            var platform = GetCurrPlatform();
            bool isPollingFlow = platform == PLATFORMS.WASM || platform == PLATFORMS.ANDROID || platform == PLATFORMS.IOS;

            if (isPollingFlow)
            {
                if (platform == PLATFORMS.WASM)
                {
                    await _jsRuntime.InvokeVoidAsync("eval", $"window.open('{authUrl}', '_blank')");
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.openAuthBrowser", authUrl);
                }
            }
            else
            {
                _navigationManager.NavigateTo(authUrl, forceLoad: true);
            }

            return null;
        }

        // ========================================================================
        // NAVIGATION & NATIVE BRIDGE
        // ========================================================================

        /// <summary>
        /// Registers the bridge for native navigation events (e.g., Android Back-Button).
        /// </summary>
        public async Task RegisterNativeNavigationAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
        {
            try
            {
                var platform = GetCurrPlatform();

                switch (platform)
                {
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                        await Task.Delay(250);
                        await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.app.registerNavigationHelper", dotNetRef);
                        break;

                    case PLATFORMS.WASM:
                        await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.registerWebNavigationHelper", dotNetRef);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] RegisterNativeNavigationAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Controls the swipe-back gesture state (primarily for iOS/Web).
        /// </summary>
        public async Task SetSwipeBackStateAsync(bool enabled)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.swipeBack.enable", enabled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] SetSwipeBackStateAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a reset of the swipe gesture (cleanup after navigation).
        /// </summary>
        public async Task ForceResetSwipeAsync()
        {
            try
            {
                await Task.Delay(50);
                await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.swipeBack.forceReset");
                await Task.Delay(150);
                await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.swipeBack.forceReset");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] ForceResetSwipeAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the app or the current window (platform-dependent).
        /// On Web/WASM, no action is taken.
        /// </summary>
        public async Task ExitAppAsync()
        {
            try
            {
                var platform = GetCurrPlatform();

                if (platform == PLATFORMS.ANDROID || platform == PLATFORMS.IOS)
                {
                    await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.app.exitApp");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] ExitAppAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigates back using browser history or native back navigation.
        /// </summary>
        public async Task NavigateBackAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("history.back");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] NavigateBackAsync failed: {ex.Message}");
            }
        }

        // ========================================================================
        // STORAGE & PREFERENCES (DEPRECATED - MIGRATED TO IPlatformStorage)
        // ========================================================================

        /// <summary>
        /// Sets a preference value synchronously.
        /// NOTE: This method is deprecated. Use IPlatformStorage.SetPreference instead.
        /// </summary>
        public void SetPreference(string key, string value)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            jsInProcess.InvokeVoid($"{JsCapPrefix}.setStorageSync", key, value);
        }

        /// <summary>
        /// Gets a preference value synchronously.
        /// NOTE: This method is deprecated. Use IPlatformStorage.GetPreference instead.
        /// </summary>
        public string? GetPreference(string key)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            return jsInProcess.Invoke<string?>($"{JsCapPrefix}.getStorageSync", key);
        }

        /// <summary>
        /// Removes a preference value synchronously.
        /// NOTE: This method is deprecated. Use IPlatformStorage.RemovePreference instead.
        /// </summary>
        public void RemovePreference(string key)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            jsInProcess.InvokeVoid($"{JsCapPrefix}.removeStorageSync", key);
        }

        /// <summary>
        /// Checks if a preference key exists.
        /// NOTE: This method is deprecated. Use IPlatformStorage.ContainsPreference instead.
        /// </summary>
        public bool ContainsPreference(string key)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            return jsInProcess.Invoke<bool>($"{JsCapPrefix}.containsStorageSync", key);
        }
    }
}