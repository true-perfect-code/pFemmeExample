using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using p11.UI.Models;
using p11.UI.Services;
//using BlazorCore.DbApp.Models;
using BlazorCore.Models;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Authentication;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using System.Reflection;
using System.Text.Json;

namespace BlazorCore.Services.AppInitializer
{
    public class AppInitializerBase : IAppInitializerBase
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        //private readonly IJSRuntime _jsRuntime;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalStateBase _globalState;
        private readonly IAppStateBase _appState;
        private readonly IPlatformBase _platform;
        private readonly IPlatformStorageBase _platformStorage;
        private readonly IThemeService _themeService;
        private readonly IAuthenticationBase _authentication;
        private readonly IDamBase _dam;
        private readonly IMessageBoxService _messageBoxService;

        //private bool _isInitialized;

        public AppInitializerBase(IServiceProvider serviceProvider)
        {
            //_jsRuntime = jsRuntime;

            _serviceProvider = serviceProvider;
            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            _platformStorage = _serviceProvider.GetRequiredService<IPlatformStorageBase>();
            _themeService = serviceProvider.GetRequiredService<IThemeService>();
            _authentication = serviceProvider.GetRequiredService<IAuthenticationBase>();
            _dam = serviceProvider.GetRequiredService<IDamBase>();
            _messageBoxService = serviceProvider.GetRequiredService<IMessageBoxService>();
        }

        public async Task<bool> AppInitializeAsync(
            Assembly assembly, 
            bool loadLanguages = true,
            bool loadTranslation = true,
            bool initCss = true,
            bool initAccessibility = true)
        {
            await _semaphore.WaitAsync();

            await _appState.Log("[AppInitializerBase AppInitializeAsync] START");

            try
            {
                // Distinguish between first-time app initialization and post-login initialization.
                // First-time init happens once (sets _appState.IsAppInitialized = true).
                // Subsequent calls only refresh authentication and user-specific data.
                await _appState.Log($"[AppInitializerBase AppInitializeAsync] _appState.IsAppInitialized: {_appState.IsAppInitialized}");
                if (_appState.IsAppInitialized)
                {
                    // App already initialized → just refresh authentication
                    await _authentication!.GetAuthenticationStateAsync();

                    // NOTE: Task.Delay was added for Capacitor on older mobile devices to prevent bridge overload.
                    // If you experience issues on old devices, uncomment the following line:
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                    if (_appState.IsAuthenticated)
                        return await InitializeAuthenticated();
                    else
                        return true;
                }

                // ========== FIRST-TIME INITIALIZATION ==========

                // Load language packs from app.json
                if (loadLanguages)
                {
                    var additionalSources = new List<(Assembly Assembly, string ResourceName)>
                    {
                        (assembly, $"pFemmeExample.Shared.wwwroot.languages.app.json")
                    };
                    await _globalState.SetTranslations(additionalSources);

                    // NOTE: Task.Delay for Capacitor bridge - uncomment if needed
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Initialize CSS/theme system
                if (initCss)
                {
                    await InitCSS(assembly);
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Init accessibility options
                if (initAccessibility)
                {
                    await InitAccessible();
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Get authentication state (checks stored token/session)
                await _authentication!.GetAuthenticationStateAsync();
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // User-specific initialization (only if authenticated)
                if (_appState.IsAuthenticated)
                {
                    if (!(await InitializeAuthenticated()))
                        return false;
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Load translation texts for selected language
                if (loadTranslation)
                {
                    await _appState.LoadTranslations(routesStateHasChanged: false);
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                _appState.UpdateIsAppInitialized(true);

                await _appState.Log("[AppInitializerBase AppInitializeAsync] END");
                return true;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[AppInitializerBase AppInitializeAsync] ERROR : {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
                
        private async Task<bool> InitializeAuthenticated()
        {
            await _appState.Log("[AppInitializerBase InitializeAuthenticated] START");
            try
            {
                // Initialize local notifications (platform-specific)
                await InitializeLocalNotification();

                // Check if authentication token is still valid (MAUI Blazor / Capacitor / WPF)
                if (!(await IsAuthenticationValid()))
                    return false;

                // Update Identity Provider (e.g., "tpc", "google", "microsoft", "apple")
                _appState.UpdateIdP(await GetIdP());
                // NOTE: Task.Delay for Capacitor bridge - uncomment if needed
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // 2FA check only for internal "tpc" accounts (external providers handle 2FA themselves)
                if (_appState.IdP == "tpc" && _appState.StorageLocation != STORAGE_LOCATION.LOCAL)
                {
                    _appState.UpdateIs2FAActivated(await GetIs2FAActivated());
                    // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // NOTE: The following initialization also exists in Login component (Capacitor/Web)
                // Initialize random generators for user and device
                _appState.InitializeUnixTSUser(_appState.UnixTS);
                _appState.InitializeUnixTSDeviceId(_platform!.GetDeviceInfo());
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // Initialize app state (alias, alias image, etc.)
                await InitAppState();

                // Load user settings from database
                _appState.UpdateSettings(await _appState.GetAppParameter());
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                //// Initialize sharing functionality
                //await InitSharing();
                //// await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
            }
            catch (Exception ex)
            {
                await _appState.Log($"[AppInitializerBase InitializeAuthenticated] ERROR={ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
                return false;
            }
            await _appState.Log("[AppInitializerBase InitializeAuthenticated] END");
            return true;
        }

        public async Task<bool> InitializeLocalNotification()
        {
            await _appState.Log("[AppInitializerBase InitializeLocalNotification] START");

            try
            {
                //await _localNotification!.InitializeAsync();
                await _appState.Log("[AppInitializerBase InitializeLocalNotification] END");
                return true;
            }
            catch (Exception ex)
            {
                await _appState.Log($"[AppInitializerBase InitializeLocalNotification] END ERROR : {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState.ConfigGeneral.ErrorText)} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
                return false;
            }
        }

        public virtual async Task InitializeAfterSavingAsync()
        {
            await _appState.Log("[AppInitializerBase InitializeAfterSavingAsync] START");

            try
            {
                // Reload storage location (user may have changed Cloud/Local setting)
                await _appState.LoadStorageLocation();
                // NOTE: Task.Delay for Capacitor bridge - uncomment if needed
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // Reload user settings from database
                _appState.UpdateSettings(await _appState.GetAppParameter());
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // Reload translations for selected language (force UI update)
                await _appState.LoadTranslations(routesStateHasChanged: true);
                // await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
            }
            catch (Exception ex)
            {
                await _appState.Log($"[AppInitializerBase InitializeAfterSavingAsync] END ERROR : {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState.ConfigGeneral.ErrorText)} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            await _appState.Log("[AppInitializerBase InitializeAfterSavingAsync] END");
        }

        public async Task InitCSS(Assembly assembly, 
            string cssThemeFile = "app.css",
            string cssVarDefaultFile = "appvardefault.css",
            string cssVarMonoFile = "appvarmono.css")
        {
            if (_appState.Catalog.LocalStorage == null || _globalState == null) return;

            await _appState.Log("[AppInitializerBase InitCSS] START");
            try
            {
                if (assembly == null)
                {
                    var err = "An unexpected error has occurred. The assembly parameter cannot be null.";
                    await _appState.Error($"[AppInitializerBase InitCSS] assembly == null: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState.T("Error"),
                        message: _appState.T(err)
                    );
                }
                else
                {
                    // Load main CSS theme
                    string resourceName = $"pFemmeExample.Shared.wwwroot.{cssThemeFile}";
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    await Task.Yield();
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                    if (stream == null)
                    {
                        var availableResources = assembly.GetManifestResourceNames();
                        var availableResourcesString = string.Join(", ", availableResources);

                        var err = "An unexpected error has occurred. Embedded resource 'pFemmeExample.Shared.wwwroot.app.css' not found!";
                        await _appState.Error($"[AppInitializerBase InitCSS] stream == null: {err}");
                        await _messageBoxService.ShowOkAsync(
                            title: _appState.T("Error"),
                            message: _appState.T(err)
                        );

                        return;
                    }

                    using var reader = new StreamReader(stream);
                    string csstheme = await reader.ReadToEndAsync();

                    // Load default theme variables
                    resourceName = $"pFemmeExample.Shared.wwwroot.{cssVarDefaultFile}";
                    using var streamdefault = assembly.GetManifestResourceStream(resourceName);
                    if (streamdefault == null)
                    {
                        var err = "An unexpected error has occurred. Embedded resource 'pFemmeExample.Shared.wwwroot.appvardefault.css' not found!";
                        await _appState.Error($"[AppInitializerBase InitCSS] streamdefault == null: {err}");
                        await _messageBoxService.ShowOkAsync(
                            title: _appState.T("Error"),
                            message: _appState.T(err)
                        );
                    }
                    else
                    {
                        using var readerdefault = new StreamReader(streamdefault);
                        string cssvardefault = await readerdefault.ReadToEndAsync();

                        _appState.AccessibilityConfig.cssDefault = $"{cssvardefault}\n{csstheme}";
                    }

                    // Load mono theme variables
                    resourceName = $"pFemmeExample.Shared.wwwroot.{cssVarMonoFile}";
                    using var streammono = assembly.GetManifestResourceStream(resourceName);
                    if (streammono == null)
                    {
                        var err = "An unexpected error has occurred. Embedded resource 'pFemmeExample.Shared.wwwroot.appvarmono.css' not found!";
                        await _appState.Error($"[AppInitializerBase InitCSS] streammono == null: {err}");
                        await _messageBoxService.ShowOkAsync(
                            title: _appState.T("Error"),
                            message: _appState.T(err)
                        );
                    }
                    else
                    {
                        using var readermono = new StreamReader(streammono);
                        string cssvarmono = await readermono.ReadToEndAsync();

                        _appState.AccessibilityConfig.cssMono = $"{cssvarmono}\n{csstheme}";
                    }

                    // Check which mode is stored in the cookie/SecureStorage (Default or Monochrome)
                    var resultThememode = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.thememode);
                    if (resultThememode != null && string.IsNullOrEmpty(resultThememode.out_err) && !string.IsNullOrEmpty(resultThememode.out_value_str))
                    {
                        if (Enum.TryParse<THEMEMODE>(resultThememode.out_value_str, true, out var themeModeEnum))
                        {
                            switch (themeModeEnum)
                            {
                                case THEMEMODE.DEFAULT:
                                    _themeService.SetCurrentThemeCss(_appState.AccessibilityConfig.cssDefault);
                                    break;

                                case THEMEMODE.MONOCHROME:
                                    _themeService.SetCurrentThemeCss(_appState.AccessibilityConfig.cssMono);
                                    _appState.AccessibilityConfig.IsActivatedMonochrome = true;
                                    break;

                                default:
                                    _themeService.SetCurrentThemeCss(_appState.AccessibilityConfig.cssDefault);
                                    break;
                            }
                        }
                        else // ...otherwise set the default theme
                            _themeService.SetCurrentThemeCss(_appState.AccessibilityConfig.cssDefault);
                    }
                    else // ...otherwise set the default theme
                    {
                        if (resultThememode != null && !string.IsNullOrEmpty(resultThememode.out_err))
                        {
                            await _appState.Error($"[AppInitializerBase InitCSS] streammono == null: {resultThememode.out_err}");
                            await _messageBoxService!.ShowOkAsync(
                                title: _appState?.T("Error") ?? "Error",
                                message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultThememode.out_err}"
                            );
                        }

                        _themeService.SetCurrentThemeCss(_appState!.AccessibilityConfig.cssDefault);
                        await Task.Yield();
                        await Task.Delay(_globalState!.ConfigGeneral.TaskDelay_Capacitor);
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[AppInitializerBase InitCSS] END ERROR : {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[AppInitializerBase InitCSS] END");
        }
        
        private async Task<string> GetIdP()
        {
            await _appState.Log("[AppInitializerBase GetIdP] START");

            var idP = string.Empty;

            // Only check IdP if cloud connection is available
            if (_appState.IsCloudConnected)
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectIdP>>AuthUsers" },
                    { "@AuthUsers_UnixTS", _appState!.UnixTS },
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() } // IdP via cloud only
                };

                ScalarModel result = await _dam!.Scalar(db_para)!;
                await _appState.Log("[AppInitializerBase GetIdP] SelectIdP>>AuthUsers:", data: result);
                if (result != null && result.out_value_str != null && string.IsNullOrEmpty(result.out_err))
                {
                    idP = result.out_value_str;
                }
                else
                {
                    var err = result != null ? result.out_err : "SelectIdP>>AuthUsers";
                    await _appState.Error($"[AppInitializerBase GetIdP] ERROR GetIdP: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState!.T("Error"),
                        message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                    );
                }
            }

            await _appState.Log("[AppInitializerBase GetIdP] END");
            return idP;
        }

        private async Task<bool> GetIs2FAActivated()
        {
            await _appState.Log("[AppInitializerBase GetIs2FAActivated] START");

            var is2FAActivated = false;

            // Only check 2FA if cloud connection is available
            if (_appState.IsCloudConnected)
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "ExistsOtp>>AuthUsers" },
                    { "@AuthUsers_UnixTS", _appState!.UnixTS },
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() } // 2FA via the cloud only
                };

                ScalarModel result = await _dam!.Scalar(db_para)!;
                await _appState.Log("[AppInitializerBase GetIs2FAActivated] ExistsOtp>>AuthUsers:", data: result);
                if (result != null && string.IsNullOrEmpty(result.out_err))
                {
                    is2FAActivated = result.out_value_bool;
                }
                else
                {
                    var err = result != null ? result.out_err : "ExistsOtp>>AuthUsers";
                    await _appState.Error($"[AppInitializerBase GetIs2FAActivated] ERROR GetIs2FAActivated: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState!.T("Error"),
                        message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                    );
                    // Note: Returns false on error - caller must handle
                }
            }

            await _appState.Log("[AppInitializerBase GetIs2FAActivated] END");
            return is2FAActivated;
        }

        public async Task<bool> IsAuthenticationValid()
        {
            await _appState.Log("[AppInitializerBase IsAuthenticationValid] START");

            await _appState.Log($"[AppInitializerBase IsAuthenticationValid] StorageLocation={_appState.StorageLocation}");
            await _appState.Log($"[AppInitializerBase IsAuthenticationValid] WebApiToken={_appState.WebApiToken}");
            await _appState.Log($"[AppInitializerBase IsAuthenticationValid] IsNativeApp={_appState.IsNativeApp}");

            bool result = false;

            try
            {
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "ExistsUnixTS>>AuthUsers" },
                    { "@UnixTS", _appState.UnixTS },
                };

                switch (_appState.StorageLocation)
                {
                    // LOCAL STORAGE ONLY - Check local account
                    case STORAGE_LOCATION.LOCAL:
                        // Memory type cannot be checked (data not persistent)
                        if (_globalState.ConfigGeneral.LocalStorageType == LOCAL_STORAGE_TYPE.MEMORY)
                            return true;

                        if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                        {
                            db_para[DB_CMD.NO_CLOUD] = DB_CMD.NO_CLOUD.ToString();
                            var resultExistsLocalUnixTS = await _dam!.Scalar(db_para);
                            await _appState.Log($"[AppInitializerBase IsAuthenticationValid] resultExistsLocalUnixTS:", data: resultExistsLocalUnixTS);
                            await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                            if (resultExistsLocalUnixTS != null && string.IsNullOrEmpty(resultExistsLocalUnixTS.out_err))
                            {
                                result = resultExistsLocalUnixTS.out_value_bool;
                            }
                            else
                            {
                                await _appState.Log("[AppInitializerBase IsAuthenticationValid] switch: result = false");
                                result = false;
                            }
                        }
                        else
                            result = false;
                        break;

                    // CLOUD or CLOUD_LOCAL STORAGE
                    case STORAGE_LOCATION.CLOUD_LOCAL:
                    case STORAGE_LOCATION.CLOUD:
                        if (_appState.IsNativeApp ?? false) // MAUI
                        {
                            if (_appState.WebApiToken != API_CONST.TOKEN_LOCAL_ONLY) 
                            {
                                db_para[DB_CMD.NO_LOCAL] = DB_CMD.NO_LOCAL.ToString();
                                var resultExistsCloudUnixTS = await _dam!.Scalar(db_para);
                                await _appState.Log($"[AppInitializerBase IsAuthenticationValid] _appState.IsNativeApp={_appState.IsNativeApp}, resultExistsCloudUnixTS:", data: resultExistsCloudUnixTS);
                                if (resultExistsCloudUnixTS != null && string.IsNullOrEmpty(resultExistsCloudUnixTS.out_err))
                                {
                                    if (resultExistsCloudUnixTS.out_value_bool)
                                    {
                                        // WebApi-Token prüfen
                                        UserWebApi userWebApi = new();
                                        userWebApi.Token = _appState.WebApiToken;
                                        result = await _dam.CheckToken(userWebApi);
                                        await _appState.Log($"[AppInitializerBase IsAuthenticationValid] _appState.IsNativeApp={_appState.IsNativeApp}, result (_dam.CheckToken):", data: result);
                                    }
                                    else
                                        result = false;
                                }
                                else
                                    result = false;
                            }
                            else
                                result = false;
                        }
                        else // Web (Blazor Server)
                        {
                            // Cookie wird von Blazor automatisch geprüft, also nur Existenz des Accounts in der DB prüfen
                            db_para[DB_CMD.NO_LOCAL] = DB_CMD.NO_LOCAL.ToString();
                            var resultExistsCloudUnixTS = await _dam!.Scalar(db_para);
                            await _appState.Log($"[AppInitializerBase IsAuthenticationValid] _appState.IsNativeApp={_appState.IsNativeApp}, resultExistsCloudUnixTS:", data: resultExistsCloudUnixTS);
                            if (resultExistsCloudUnixTS != null && string.IsNullOrEmpty(resultExistsCloudUnixTS.out_err))
                            {
                                result = resultExistsCloudUnixTS.out_value_bool;
                            }
                        }
                        break;

                    case STORAGE_LOCATION.Unknown:
                        result = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[AppInitializerBase IsAuthenticationValid] ERROR={ex.Message}");
                result = false;
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            // Prepare Pepper for local encryption (required for offline 2FA protection)
            if (_appState.NativeDeviceType != p11.UI.NativeDevice.WEB && result && _globalState != null)
            {
                try
                {
                    using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
                    {
                        // Generate a safe seed from UserAccount + UnixTS (SHA-256 normalized)
                        string rawInput = $"{_appState.UserAccount}_{_appState.UnixTS}";

                        string safeUserSeed;
                        using (var sha256 = System.Security.Cryptography.SHA256.Create())
                        {
                            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(rawInput);
                            byte[] hashBytes = sha256.ComputeHash(inputBytes);
                            safeUserSeed = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                        }

                        string derivedPepper = await aes.GenerateDeterministicPepperAsync(safeUserSeed);

                        // Set in AppState
                        _appState.UpdatePepper(derivedPepper);

                        await _appState.Log($"[AppInitializerBase IsAuthenticationValid] Safe Normalized Pepper initialized for Identity {_appState.UnixTS}.");
                    }
                }
                catch (Exception ex)
                {
                    await _appState.Error($"[AppInitializerBase IsAuthenticationValid] Pepper generation failed: {ex.Message}");
                    throw; // Re-throw because pepper is critical for local encryption
                }
            }

            await _appState.Log("[AppInitializerBase IsAuthenticationValid] END");
            return result;
        }

        public async Task InitAccessible()
        {
            if (_appState == null || _globalState == null || _appState.Catalog.LocalStorage == null || _globalState == null) 
                return;

            await _appState.Log("[AppInitializerBase InitAccessible] START");
            try
            {
                // Load Language setting
                var resultLanguage = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.language);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultLanguage != null && string.IsNullOrEmpty(resultLanguage.out_err) && !string.IsNullOrEmpty(resultLanguage.out_value_str))
                    _appState.UpdateSelectedLanguage(resultLanguage.out_value_str); // Sprache aus Cookie/SecurStorage setzen
                else // ...otherwise set the default language
                {
                    if (resultLanguage != null && !string.IsNullOrEmpty(resultLanguage.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultLanguage.out_err}"
                        );
                    }

                    _appState!.UpdateSelectedLanguage(_globalState.ConfigGeneral.DefaultLanguage);
                }
                await _appState.LoadTranslations(routesStateHasChanged: false, _appState!.SelectedLanguage);

                // Load LTR/RTL setting
                var resultLtrRtl = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.ltrrtl);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultLtrRtl != null && string.IsNullOrEmpty(resultLtrRtl.out_err) && !string.IsNullOrEmpty(resultLtrRtl.out_value_str))
                {
                    _appState.UpdateLtrRtl(resultLtrRtl.out_value_str == LTR_RTL.RTL.ToString() ? LTR_RTL.RTL : LTR_RTL.LTR);
                }
                else
                {
                    if (resultLtrRtl != null && !string.IsNullOrEmpty(resultLtrRtl.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultLtrRtl.out_err}"
                        );
                    }

                    _appState!.UpdateLtrRtl(LTR_RTL.LTR);
                }

                // Apply selected language to accessibility config
                _appState.AccessibilityConfig.SelectedLanguage = _appState.SelectedLanguage;

                // Load Font Family
                var resultFontfamily = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.fontfamily);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontfamily != null && string.IsNullOrEmpty(resultFontfamily.out_err) && !string.IsNullOrEmpty(resultFontfamily.out_value_str))
                    _appState.AccessibilityConfig.SelectedFontFamily = resultFontfamily.out_value_str;
                else
                {
                    if (resultFontfamily != null && !string.IsNullOrEmpty(resultFontfamily.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontfamily.out_err}"
                        );
                    }

                    _appState!.AccessibilityConfig.SelectedFontFamily = _globalState.ConfigGeneral.DefaultFontFamily;
                }

                // Load Font Size
                var resultFontsize = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.fontsize);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontsize != null && string.IsNullOrEmpty(resultFontsize.out_err) && !string.IsNullOrEmpty(resultFontsize.out_value_str))
                {
                    _appState.AccessibilityConfig.SelectedFontSize = JsonSerializer.Deserialize(
                        resultFontsize.out_value_str,
                        JsonContext.Default.FontSizeModel
                    ) ?? new FontSizeModel();
                }
                else
                {
                    if (resultFontsize != null && !string.IsNullOrEmpty(resultFontsize.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontsize.out_err}"
                        );
                    }
                }

                // Load Font Weight
                var resultFontWeight = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.fontweight);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontWeight != null && string.IsNullOrEmpty(resultFontWeight.out_err) && !string.IsNullOrEmpty(resultFontWeight.out_value_str))
                {
                    _appState!.AccessibilityConfig.SelectedFontWeight = JsonSerializer.Deserialize(
                        resultFontWeight.out_value_str,
                        JsonContext.Default.FontWeightModel
                    ) ?? new FontWeightModel();
                }
                else
                {
                    if (resultFontWeight != null && !string.IsNullOrEmpty(resultFontWeight.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontWeight.out_err}"
                        );
                    }
                }

                // Load Font Spacing
                var resultFontSpacing = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.fontspacing);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontSpacing != null && string.IsNullOrEmpty(resultFontSpacing.out_err) && !string.IsNullOrEmpty(resultFontSpacing.out_value_str))
                {
                    if (double.TryParse(resultFontSpacing.out_value_str, out double spacing))
                    {
                        _appState!.AccessibilityConfig.SelectedSpacingValue = spacing;
                    }
                }
                else
                {
                    if (resultFontSpacing != null && !string.IsNullOrEmpty(resultFontSpacing.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontSpacing.out_err}"
                        );
                    }
                }

                // Load Font Line Height
                var resultFontLineHeight = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.fontlineheight);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontLineHeight != null && string.IsNullOrEmpty(resultFontLineHeight.out_err) && !string.IsNullOrEmpty(resultFontLineHeight.out_value_str))
                {
                    if (double.TryParse(resultFontLineHeight.out_value_str, out double lineheight))
                    {
                        _appState!.AccessibilityConfig.SelectedLineHeightValue = lineheight;
                    }
                }
                else
                {
                    if (resultFontLineHeight != null && !string.IsNullOrEmpty(resultFontLineHeight.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontLineHeight.out_err}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[AppInitializerBase InitAccessible] ERROR InitAccessibility: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[AppInitializerBase InitAccessible] END");
        }
                
        protected virtual async Task InitAppState()
        {
            await _appState.Log("[AppInitializerBase InitAppState] START");
            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Select>>AuthUsersExtend" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS }
                };
                ReadModel<AuthUsersExtendModel?> result = await _dam.ReadData<AuthUsersExtendModel>(db_para)!; // Abfrage ausführen
                await _appState.Log("[AppInitializerBase InitAppState] Select>>AuthUsersExtend:", data: result);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (result != null && string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
                {
                    if (result.out_list != null && result.out_list.Any())
                    {
                        var firstItem = result.out_list.FirstOrDefault();
                        if (firstItem != null)
                        {
                            if (!string.IsNullOrEmpty(firstItem.DisplayName))
                                _appState.UpdateAlias(firstItem.DisplayName);
                            if (!string.IsNullOrEmpty(firstItem.imgJpegThumbnail))
                                _appState.UpdateAliasImg(firstItem.imgJpegThumbnail);
                        }
                    }
                }
                else
                {
                    string? err = result != null && !string.IsNullOrEmpty(result.out_err) ? result.out_err : "Select>>AuthUsersExtend";
                    await _appState.Error($"[AppInitializerBase InitAppState] ERROR Select>>AuthUsersExtend: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState!.T("Error"),
                        message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                    );
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[AppInitializerBase InitAppState] ERROR: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[AppInitializerBase InitAppState] END");
        }

        


    }
}
