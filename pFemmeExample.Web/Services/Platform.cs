#pragma warning disable CA1416

using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace pFemmeExample.Web.Services
{
    public class Platform : BlazorCore.Services.Platform.IPlatformBase
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IGlobalStateBase _globalState;
        private readonly IServiceProvider _serviceProvider;
        private readonly NavigationManager _navigationManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public Platform(
            IJSRuntime jsRuntime,
            IGlobalStateBase globalState,
            IServiceProvider serviceProvider,
            NavigationManager navigationManager,
            IHttpClientFactory httpClientFactory)
        {
            _jsRuntime = jsRuntime;
            _globalState = globalState;
            _serviceProvider = serviceProvider;
            _navigationManager = navigationManager;
            _httpClientFactory = httpClientFactory;
        }

        // ========================================================================
        // INITIALIZATION
        // ========================================================================

        /// <summary>
        /// Initializes the JS bridge for Blazor Server.
        /// On Web platform, no special initialization is required.
        /// </summary>
        public async Task<ScalarModel> InitializeJSAsync()
        {
            await Task.Delay(10);
            return new ScalarModel();
        }

        // ========================================================================
        // PLATFORM / DEVICE INFO
        // ========================================================================

        /// <summary>
        /// Returns the base directory of the application.
        /// </summary>
        public string GetBaseDirectory()
        {
            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// Returns the current platform (always WINDOWS_SERVER for Blazor Server).
        /// </summary>
        public PLATFORMS GetCurrPlatform()
        {
            return PLATFORMS.WINDOWS_SERVER;
        }

        /// <summary>
        /// Returns the native device type (always WEB for Blazor Server).
        /// </summary>
        public p11.UI.NativeDevice GetCurrDevice() => p11.UI.NativeDevice.WEB;

        /// <summary>
        /// Returns a pseudo-device ID for the current session.
        /// </summary>
        public string GetDeviceInfo()
        {
            byte[] buffer = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
            int randomValue = Math.Abs(BitConverter.ToInt32(buffer, 0)) % 9_000_000 + 1_000_000;
            return randomValue.ToString();
        }

        /// <summary>
        /// Returns the form factor (always "Web" for Blazor Server).
        /// </summary>
        public Task<string> GetFormFactor()
        {
            return Task.FromResult("Web");
        }

        // ========================================================================
        // WINDOW / UI INFO
        // ========================================================================

        /// <summary>
        /// Returns the current window width in pixels.
        /// </summary>
        public async Task<double> GetWindowWidth()
        {
            var dimensions = await _jsRuntime.InvokeAsync<WindowDimensions>("pE_Web.getWindowDimensions");
            return dimensions.Width;
        }

        /// <summary>
        /// Returns the current window height in pixels.
        /// </summary>
        public async Task<double> GetWindowHeight()
        {
            var dimensions = await _jsRuntime.InvokeAsync<WindowDimensions>("pE_Web.getWindowDimensions");
            return dimensions.Height;
        }

        /// <summary>
        /// Returns the idiom platform (e.g., Browser-Desktop-Windows, Browser-Phone-macOS).
        /// </summary>
        public async Task<string> GetIdiomPlatform()
        {
            try
            {
                double screenWidth = await GetWindowWidth();

                string idiom = screenWidth switch
                {
                    >= 1024 => "Browser-Desktop",
                    >= 768 => "Browser-Tablet",
                    _ => "Browser-Phone"
                };

                string platform = await _jsRuntime.InvokeAsync<string>("pE_Web.getOSVersion");
                return $"{idiom}-{platform}";
            }
            catch
            {
                throw;
            }
        }

        // ========================================================================
        // CLIPBOARD & SHARING
        // ========================================================================

        /// <summary>
        /// Copies text to the clipboard.
        /// </summary>
        public async Task CopyTextToClipboard(string text)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("pE_Web.copyClipboard", text);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Shares text using the Web Share API.
        /// </summary>
        public async Task ShareText(string title, string text)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("pE_Web.share.shareText", title, text);
            }
            catch
            {
                throw;
            }
        }

        // ========================================================================
        // FILE & DIRECTORY PICKER
        // ========================================================================

        /// <summary>
        /// Opens a native directory picker.
        /// On Blazor Server, returns empty string (no native directory access on server).
        /// </summary>
        public async Task<string?> DirectoryPicker()
        {
            await Task.Delay(10);
            return string.Empty;
        }

        /// <summary>
        /// Opens a native file picker.
        /// NOT IMPLEMENTED on Blazor Server.
        /// </summary>
        public Task<ScalarModel> FilePicker(string filter, string title)
        {
            return Task.FromResult(new ScalarModel());
        }

        /// <summary>
        /// Saves a file natively on the current platform.
        /// On Blazor Server, triggers a browser download via JS stream reference.
        /// </summary>
        public async Task<ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
        {
            var result = new ScalarModel();

            try
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                using var streamRef = new DotNetStreamReference(stream);
                await _jsRuntime.InvokeVoidAsync("pE_Web.downloadFileFromStream", filename, streamRef);

                result.out_value_bool = true;
                result.out_value_str = filename;
                return result;
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.Message;
                result.out_local = "Platform: SaveFileNativeAsync Exception";
                return result;
            }
        }

        // ========================================================================
        // EXTERNAL URL & AUTHENTICATION
        // ========================================================================

        /// <summary>
        /// Opens an external URL in the default browser.
        /// </summary>
        public async Task OpenExternalUrl(string url)
        {
            await _jsRuntime.InvokeVoidAsync("pE_Web.openExternalUrl", url);
        }

        /// <summary>
        /// Starts the authentication flow.
        /// If openInNewTab is true, opens a new tab; otherwise navigates the current page.
        /// </summary>
        public async Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false)
        {
            if (openInNewTab)
            {
                await _jsRuntime.InvokeVoidAsync("pE_Web.openExternalUrl", authUrl);
                return null;
            }

            _navigationManager.NavigateTo(authUrl, forceLoad: true);
            return null;
        }

        // ========================================================================
        // NAVIGATION & NATIVE BRIDGE
        // ========================================================================

        /// <summary>
        /// Registers the bridge for native navigation events.
        /// On Blazor Server, no action is required.
        /// </summary>
        public Task RegisterNativeNavigationAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Controls the swipe-back gesture state.
        /// On Blazor Server, no action is required.
        /// </summary>
        public Task SetSwipeBackStateAsync(bool enabled)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Forces a reset of the swipe gesture.
        /// On Blazor Server, no action is required.
        /// </summary>
        public Task ForceResetSwipeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the app or the current window.
        /// On Blazor Server, no action is taken.
        /// </summary>
        public Task ExitAppAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Navigates back using browser history.
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
    }
}

#pragma warning restore CA1416