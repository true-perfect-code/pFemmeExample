using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace pFemmeExample.Wpf.Services
{
    public class Platform : BlazorCore.Services.Platform.IPlatformBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalStateBase _globalState;
        private readonly string _storagePath;
        private readonly string _prefPath;

        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes($"{Shared.Global.Configuration.ConfigGeneral.ApplicationName}.SecureStorage.v1");

        public Platform(IServiceProvider serviceProvider, IGlobalStateBase globalState)
        {
            _serviceProvider = serviceProvider;
            _globalState = globalState;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, _globalState.ConfigGeneral.ApplicationName);

            _storagePath = Path.Combine(appFolder, "Storage");
            _prefPath = Path.Combine(appFolder, "Storage", "Preferences");

            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
            if (!Directory.Exists(_prefPath)) Directory.CreateDirectory(_prefPath);
        }

        // ========================================================================
        // INITIALIZATION
        // ========================================================================

        /// <summary>
        /// Initializes the platform service.
        /// On WPF, no JS initialization is required.
        /// </summary>
        public Task<ScalarModel> InitializeJSAsync() => Task.FromResult(new ScalarModel());

        // ========================================================================
        // PLATFORM / DEVICE INFO
        // ========================================================================

        /// <summary>
        /// Returns the base directory for data storage using LocalAppData.
        /// </summary>
        public string GetBaseDirectory()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _globalState?.ConfigGeneral.ApplicationName ?? string.Empty);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Returns the current platform (always WINDOWS_CLIENT for WPF).
        /// </summary>
        public PLATFORMS GetCurrPlatform() => PLATFORMS.WINDOWS_CLIENT;

        /// <summary>
        /// Returns the native device type (always WINDOWS for WPF).
        /// </summary>
        public p11.UI.NativeDevice GetCurrDevice() => p11.UI.NativeDevice.WINDOWS;

        /// <summary>
        /// Returns device information (always "WPF-Host").
        /// </summary>
        public string GetDeviceInfo() => "WPF-Host";

        /// <summary>
        /// Returns the form factor (always "Desktop").
        /// </summary>
        public Task<string> GetFormFactor() => Task.FromResult("Desktop");

        /// <summary>
        /// Returns the idiom platform (always "WPF-Client").
        /// </summary>
        public Task<string> GetIdiomPlatform() => Task.FromResult("WPF-Client");

        // ========================================================================
        // WINDOW / UI INFO
        // ========================================================================

        /// <summary>
        /// Returns the current window width in pixels.
        /// </summary>
        public async Task<double> GetWindowWidth()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = Application.Current.MainWindow;
                return window?.ActualWidth ?? 800;
            });
        }

        /// <summary>
        /// Returns the current window height in pixels.
        /// </summary>
        public async Task<double> GetWindowHeight()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = Application.Current.MainWindow;
                return window?.ActualHeight ?? 600;
            });
        }

        // ========================================================================
        // CLIPBOARD & SHARING
        // ========================================================================

        /// <summary>
        /// Copies text to the clipboard.
        /// </summary>
        public async Task CopyTextToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Clipboard Error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Shares text by copying to clipboard and opening mailto link.
        /// </summary>
        public async Task ShareText(string title, string text)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    Clipboard.SetText($"{title}\n\n{text}");

                    string mailTo = $"mailto:?subject={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(text)}";

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = mailTo,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Share Error: {ex.Message}");
                }
            });
        }

        // ========================================================================
        // FILE & DIRECTORY PICKER
        // ========================================================================

        /// <summary>
        /// Opens a native directory picker dialog.
        /// </summary>
        public async Task<string?> DirectoryPicker()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "Please select a directory",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                bool? result = dialog.ShowDialog();
                return result == true ? dialog.FolderName : null;
            });
        }

        /// <summary>
        /// Opens a native file picker dialog.
        /// </summary>
        public async Task<ScalarModel> FilePicker(string filter, string title)
        {
            var result = new ScalarModel();

            try
            {
                string selectedFilePath = string.Empty;
                bool? dialogResult = false;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = filter,
                        Title = title,
                        Multiselect = false
                    };

                    dialogResult = openFileDialog.ShowDialog();
                    if (dialogResult == true)
                    {
                        selectedFilePath = openFileDialog.FileName;
                    }
                });

                if (dialogResult == true && !string.IsNullOrEmpty(selectedFilePath))
                {
                    byte[] bytes = await File.ReadAllBytesAsync(selectedFilePath);

                    result.out_value_bool = true;
                    result.out_bytes = bytes;
                }
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Saves a file natively to the specified path.
        /// </summary>
        public async Task<ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
        {
            var result = new ScalarModel();

            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    result.out_value_bool = false;
                    result.out_err = "No target directory selected.";
                    return result;
                }

                string fullPath = Path.IsPathRooted(filename) ? filename : Path.Combine(path, filename);

                if (stream.CanSeek) stream.Position = 0;

                using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                await stream.CopyToAsync(fileStream);

                result.out_value_bool = true;
                result.out_value_str = fullPath;
                return result;
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.Message;
                result.out_local = "WPF: SaveFileNativeAsync Exception";
                return result;
            }
        }

        // ========================================================================
        // EXTERNAL URL & AUTHENTICATION
        // ========================================================================

        /// <summary>
        /// Opens an external URL in the default browser.
        /// </summary>
        public async Task OpenExternalUrl(string url) => await AuthenticateAsync(url);

        /// <summary>
        /// Starts the authentication flow by opening the browser.
        /// </summary>
        public async Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false)
        {
            try
            {
                if (string.IsNullOrEmpty(authUrl)) return "URL is empty";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                return await Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Browser Error: {ex.Message}");
                return ex.Message;
            }
        }

        // ========================================================================
        // NAVIGATION & NATIVE BRIDGE
        // ========================================================================

        /// <summary>
        /// Registers the bridge for native navigation events.
        /// On WPF, no action is required.
        /// </summary>
        public Task RegisterNativeNavigationAsync<T>(Microsoft.JSInterop.DotNetObjectReference<T> dotNetRef) where T : class
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Controls the swipe-back gesture state.
        /// On WPF, no action is required.
        /// </summary>
        public Task SetSwipeBackStateAsync(bool enabled)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Forces a reset of the swipe gesture.
        /// On WPF, no action is required.
        /// </summary>
        public Task ForceResetSwipeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the WPF application.
        /// </summary>
        public async Task ExitAppAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WPF Shutdown Error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Navigates back (not implemented for WPF).
        /// </summary>
        public Task NavigateBackAsync()
        {
            return Task.CompletedTask;
        }

        // ========================================================================
        // CONNECTIVITY (NOT IMPLEMENTED)
        // ========================================================================

        /// <summary>
        /// Starts connectivity monitoring (not implemented on WPF).
        /// </summary>
        public void StartConnectivityMonitoring(BlazorCore.Services.AppState.IAppStateBase appState) { }

        /// <summary>
        /// Returns true (no actual connectivity detection).
        /// </summary>
        public Task<bool> InternetConnectedAsync() => Task.FromResult(true);
    }
}