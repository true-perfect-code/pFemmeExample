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
        private readonly IThemeService _themeService;
        private readonly IAuthenticationBase _authentication;
        private readonly IDamBase _dam;
        private readonly IMessageBoxService _messageBoxService;
        private readonly LocalNotification.ILocalNotification _localNotification;

        //private bool _isInitialized;

        public AppInitializerBase(IServiceProvider serviceProvider)
        {
            //_jsRuntime = jsRuntime;

            _serviceProvider = serviceProvider;
            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            _themeService = serviceProvider.GetRequiredService<IThemeService>();
            _authentication = serviceProvider.GetRequiredService<IAuthenticationBase>();
            _dam = serviceProvider.GetRequiredService<IDamBase>();
            _messageBoxService = serviceProvider.GetRequiredService<IMessageBoxService>();
            _localNotification = serviceProvider.GetRequiredService<LocalNotification.ILocalNotification>();
        }

        //public bool IsInitialized => _isInitialized;

        //public virtual async Task<bool> InitializeAsync(Assembly assembly)
        public async Task<bool> AppInitializeAsync(
            Assembly assembly, 
            bool LoadLanguages = true,
            bool LoadTranslation = true,
            bool InitCss = true,
            bool InitAccessibility = true)
        {
            await _semaphore.WaitAsync();

            await _appState.Log("[BLAZOR AppInitializer] START AppInitializeAsync");

            try
            {
                // A distinction is made here between general app initialization (languages, appearance, etc.) and initialization
                // of the app after a successful login (settings, sharing, etc.).
                // General initialization occurs once when the app starts, at which point ‘_appState.IsAppInitialized’
                // is set to TRUE. After that, only app initialization is performed (see the following IF statement)
                await _appState.Log($"[BLAZOR AppInitializer] _appState.IsAppInitialized: {_appState.IsAppInitialized}");
                if (_appState.IsAppInitialized)
                {
                    // 1. Authentication
                    await _authentication!.GetAuthenticationStateAsync();
                    await Task.Yield();
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                    // 2. App Initialisierung
                    if (_appState.IsAuthenticated)
                        return await InitializeAuthenticated();
                    else
                        return true;
                }

                // Load languages from app.json
                if (LoadLanguages)
                {
                    var additionalSources = new List<(Assembly Assembly, string ResourceName)>
                    {
                        (assembly, $"TestSolution4.Shared.wwwroot.languages.app.json")
                    };
                    await _globalState.SetTranslations(additionalSources);
                    await Task.Yield(); await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Bootstrap UI init from app.css (see Shared)
                if(InitCss)
                {
                    await InitCSS(assembly);
                    await Task.Yield(); await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Init accessibility options
                if (InitAccessibility)
                {
                    await InitAccessible();
                    await Task.Yield(); await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // 5. Authentication
                await _authentication!.GetAuthenticationStateAsync();
                await Task.Yield(); await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // Rest nur wenn User authorisiert ist!
                if (_appState.IsAuthenticated)
                {
                    if (!(await InitializeAuthenticated()))
                        return false;
                    await Task.Yield(); await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Load texts for the selected language
                if(LoadTranslation)
                {
                    await _appState.LoadTranslations(routesStateHasChanged: false);
                    await Task.Yield(); await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                _appState.UpdateIsAppInitialized(true);

                return true;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[BLAZOR AppInitializer] END ERROR AppInitializeAsync: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
                
        private async Task<bool> InitializeAuthenticated()
        {
            await _appState.Log("[BLAZOR AppInitializer.cs InitializeAuthenticated()] START");
            try
            {
                // Lokale notifikationen initialisieren
                await InitializeLocalNotification();

                // 6. Prüfen bei MAUI Blazor, ob Token noch gültig ist
                if (!(await IsAuthenticationValid()))
                    return false;

                _appState.UpdateIdP(await GetIdP());
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // 7. Prüfen ob Anmeldung über ext. Provider oder App-Account erfolgt
                if (_appState.IdP == "tpc")
                {
                    // Prüfen, ob 2FA aktiviert 
                    _appState.UpdateIs2FAActivated(await GetIs2FAActivated());
                    await Task.Yield();
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // ACHTUNG: Befindet sich auch in der  Login Komponente (Capacitor / Web) !!!
                // 8. Zufallsgenerator initialisieren
                _appState.InitializeUnixTSUser(_appState.UnixTS);
                _appState.InitializeUnixTSDeviceId(_platform!.GetDeviceInfo());
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // App-State initialisieren (z.B. Alias, Alias-Img...)
                await InitAppState();

                // 9. Usersettings
                _appState.UpdateSettings(await _appState.GetAppParameter());
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                // 10. Sharing initialisieren
                await InitSharing();
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer.cs InitializeAuthenticated()] ERROR={ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[BLAZOR AppInitializer.cs InitializeAuthenticated()] END");
            return true;
        }

        public async Task InitializeLocalNotification()
        {
            await _appState!.Log("[BLAZOR AppInitializer] START InitializeLocalNotification");
            try
            {
                await _localNotification!.InitializeAsync();
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer] END ERROR InitializeLocalNotification: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[BLAZOR AppInitializer] END InitializeLocalNotification");
        }

        public virtual async Task InitializeAfterSavingAsync()
        {
            await _appState.Log("[BLAZOR AppInitializer] START InitializeAfterSavingAsync");
            try
            {
                //// AppState setzen
                //_appState.UpdateStorageLocation(await GetStorageLocation());
                // Storage location
                if (_appState != null)
                {
                    await _appState.LoadStorageLocation();
                    await Task.Yield();
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                    // Settings in der app aktualisieren
                    _appState.UpdateSettings(await _appState.GetAppParameter());
                    await Task.Yield();
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                }

                // Selektierte Benutzersprache laden
                if (_appState != null)
                    await _appState.LoadTranslations(routesStateHasChanged: true);

                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer] ERROR InitializeAfterSavingAsync: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState!.Log("[BLAZOR AppInitializer] END InitializeAfterSavingAsync");
        }

        public async Task InitCSS(Assembly assembly, 
            string cssThemeFile = "app.css",
            string cssVarDefaultFile = "appvardefault.css",
            string cssVarMonoFile = "appvarmono.css")
        {
            if (_appState.Catalog.LocalStorage == null || _globalState == null) return;

            await _appState.Log("[BLAZOR AppInitializer] START InitCSS");
            try
            {
                if (assembly == null)
                {
                    var err = "An unexpected error has occurred. The assembly parameter cannot be null.";
                    await _appState.Log($"[BLAZOR AppInitializer] assembly == null: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState.T("Error"),
                        message: _appState.T(err)
                    );
                }
                else
                {
                    // Set the resource name (namespace + folder + filename)
                    // Note: set <ItemGroup> in the .csproj file to 'EmbeddedResource' for the CSS file
                    string resourceName = $"TestSolution4.Shared.wwwroot.{cssThemeFile}";
                    //_ = _themeService.SetThemeFromManifestResourceAsync(assembly, resourceName);

                    // app.css auch in die AppState speichern (wegen P11Monomode, um Default Theme zu setzen, wenn Monomode aus)
                    // Sicherstellen, dass die übergebene Assembly existiert


                    // Stream aus der übergebenen Assembly holen
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    await Task.Yield();
                    await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);

                    if (stream == null)
                    {
                        // Hilfreich: Verfügbare Ressourcen auflisten, falls nicht gefunden
                        var availableResources = assembly.GetManifestResourceNames();
                        var availableResourcesString = string.Join(", ", availableResources);

                        var err = "An unexpected error has occurred. Embedded resource 'TestSolution4.Shared.wwwroot.app.css' not found!";
                        await _appState.Log($"[BLAZOR AppInitializer] stream == null: {err}");
                        await _messageBoxService.ShowOkAsync(
                            title: _appState.T("Error"),
                            message: _appState.T(err)
                        );
                    }
                    else
                    {
                        // CSS-Theme auslesen
                        using var reader = new StreamReader(stream);
                        string csstheme = await reader.ReadToEndAsync();

                        // Default Variablen holen
                        resourceName = $"TestSolution4.Shared.wwwroot.{cssVarDefaultFile}";
                        using var streamdefault = assembly.GetManifestResourceStream(resourceName);
                        if (streamdefault == null)
                        {
                            var err = "An unexpected error has occurred. Embedded resource 'TestSolution4.Shared.wwwroot.appvardefault.css' not found!";
                            await _appState.Log($"[BLAZOR AppInitializer] streamdefault == null: {err}");
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

                            // Mono Variablen holen
                            resourceName = $"TestSolution4.Shared.wwwroot.{cssVarMonoFile}";
                            using var streammono = assembly.GetManifestResourceStream(resourceName);
                            if (streammono == null)
                            {
                                var err = "An unexpected error has occurred. Embedded resource 'TestSolution4.Shared.wwwroot.appvarmono.css' not found!";
                                await _appState.Log($"[BLAZOR AppInitializer] streammono == null: {err}");
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
                            await Task.Yield();
                            await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                        }

                        // Prüfen, welches Mode in Cookie/SecureStorage hinterlegt (Default oder Monochrome)
                        var resultThememode = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.thememode}{_globalState.ConfigGeneral.ApplicationName}");
                        await Task.Yield();
                        await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
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
                            else // ...ansonsten default Thema setzen
                                _themeService.SetCurrentThemeCss(_appState.AccessibilityConfig.cssDefault);
                        }
                        else // ...ansonsten default Thema setzen
                        {
                            if (resultThememode != null && !string.IsNullOrEmpty(resultThememode.out_err))
                            {
                                await _appState.Log($"[BLAZOR AppInitializer] streammono == null: {resultThememode.out_err}");
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
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer] ERROR InitCSS: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[BLAZOR AppInitializer] END InitCSS");
        }
        
        private async Task<string> GetIdP()
        {
            await _appState.Log("[BLAZOR AppInitializer] START GetIdP");

            var idP = string.Empty;

            // Prüfen, ob Internetverbindung existiert
            if (_appState.IsCloudConnected)
            {
                // ...wenn Ja, dann IdP Wert holen (damit wir 2FA anzeigen, falls Anmeldung über App-Account erfolgt)
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectIdP>>AuthUsers" },
                    { "@AuthUsers_UnixTS", _appState!.UnixTS },
                    { DB_CMD.NO_LOCAL, string.Empty } // IdP nur über Cloud
                };

                ScalarModel result = await _dam!.Scalar(db_para)!;
                await _appState.Log("[BLAZOR AppInitializer] SelectIdP>>AuthUsers:", data: result);
                if (result != null && result.out_value_str != null && string.IsNullOrEmpty(result.out_err))
                {
                    idP = result.out_value_str;
                }
                else
                {
                    var err = result != null ? result.out_err : "SelectIdP>>AuthUsers";
                    await _appState.Log($"[BLAZOR AppInitializer] ERROR GetIdP: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState!.T("Error"),
                        message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                    );
                }
            }

            await _appState.Log("[BLAZOR AppInitializer] END GetIdP");
            return idP;
        }

        private async Task<bool> GetIs2FAActivated()
        {
            await _appState.Log("[BLAZOR AppInitializer] START GetIs2FAActivated");

            var is2FAActivated = false;

            // Prüfen, ob Internetverbindung existiert
            if(_appState.IsCloudConnected)
            {
                // ...wenn Ja, dann prüfen, ob 2FA aktiviert ist
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "ExistsOtp>>AuthUsers" },
                    { "@AuthUsers_UnixTS", _appState!.UnixTS },
                    { DB_CMD.NO_LOCAL, string.Empty } // 2FA nur über Cloud
                };

                ScalarModel result = await _dam!.Scalar(db_para)!;
                await _appState.Log("[BLAZOR AppInitializer] ExistsOtp>>AuthUsers:", data: result);
                if (result != null && string.IsNullOrEmpty(result.out_err))
                {
                    is2FAActivated = result.out_value_bool;
                }
                else
                {
                    var err = result != null ? result.out_err : "ExistsOtp>>AuthUsers";
                    await _appState.Log($"[BLAZOR AppInitializer] ERROR GetIs2FAActivated: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState!.T("Error"),
                        message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                    );
                }
            }

            await _appState.Log("[BLAZOR AppInitializer] END GetIs2FAActivated");
            return is2FAActivated;
        }

        public async Task<bool> IsAuthenticationValid()
        {
            await _appState.Log("[BLAZOR AppInitializer.cs IsAuthenticationValid()] START");

            await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] StorageLocation={_appState.StorageLocation}");
            await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] WebApiToken={_appState.WebApiToken}");
            await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] IsNativeApp={_appState.IsNativeApp}");

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
                    // Nur lokalen Account prüfen
                    case STORAGE_LOCATION.LOCAL:
                        if (_appState.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                        {
                            // Lokalen Account prüfen
                            db_para[DB_CMD.NO_CLOUD] = string.Empty;
                            var resultExistsLocalUnixTS = await _dam!.Scalar(db_para);
                            await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] resultExistsLocalUnixTS:", data: resultExistsLocalUnixTS);
                            await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                            if (resultExistsLocalUnixTS != null && string.IsNullOrEmpty(resultExistsLocalUnixTS.out_err))
                            {
                                result = resultExistsLocalUnixTS.out_value_bool;
                            }
                            else
                            {
                                await _appState.Log("switch: result = false");
                                result = false;
                            }
                        }
                        else
                            result = false;
                        break;

                    // Nur Cloud-Account prüfen
                    case STORAGE_LOCATION.CLOUD_LOCAL:
                    case STORAGE_LOCATION.CLOUD:
                        if (_appState.IsNativeApp ?? false) // MAUI
                        {
                            if (_appState.WebApiToken != API_CONST.TOKEN_LOCAL_ONLY) 
                            {
                                db_para[DB_CMD.NO_LOCAL] = string.Empty;
                                var resultExistsCloudUnixTS = await _dam!.Scalar(db_para);
                                await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] _appState.IsNativeApp={_appState.IsNativeApp}, resultExistsCloudUnixTS:", data: resultExistsCloudUnixTS);
                                await Task.Yield();
                                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                                if (resultExistsCloudUnixTS != null && string.IsNullOrEmpty(resultExistsCloudUnixTS.out_err))
                                {
                                    if (resultExistsCloudUnixTS.out_value_bool)
                                    {
                                        // WebApi-Token prüfen
                                        UserWebApi userWebApi = new();
                                        userWebApi.Token = _appState.WebApiToken;
                                        result = await _dam.CheckToken(userWebApi);
                                        await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] _appState.IsNativeApp={_appState.IsNativeApp}, result (_dam.CheckToken):", data: result);
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
                        else // Web
                        {
                            // Cookie wird von Blazor automatisch geprüft, also nur Existenz des Accounts in der DB prüfen
                            db_para[DB_CMD.NO_LOCAL] = string.Empty;
                            var resultExistsCloudUnixTS = await _dam!.Scalar(db_para);
                            await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] _appState.IsNativeApp={_appState.IsNativeApp}, resultExistsCloudUnixTS:", data: resultExistsCloudUnixTS);
                            await Task.Yield();
                            await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
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
                await _appState.Log($"[BLAZOR AppInitializer.cs IsAuthenticationValid()] ERROR={ex.Message}");
                result = false;
                //throw;
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }

            // Pepper vorbereiten
            if (result && _globalState != null)
            {
                try
                {
                    using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
                    {
                        // 1. Der "wilde" Ursprungs-String
                        string rawInput = $"{_appState.UserAccount}_{_appState.UnixTS}";

                        // 2. Normalisierung durch SHA-256 (Sauberer Hex-String)
                        string safeUserSeed;
                        using (var sha256 = System.Security.Cryptography.SHA256.Create())
                        {
                            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(rawInput);
                            byte[] hashBytes = sha256.ComputeHash(inputBytes);
                            // Wir machen daraus einen Hex-String (nur 0-9 und a-f) -> 100% sicher für JS/Web/Windows
                            safeUserSeed = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                        }

                        // 3. Jetzt füttern wir das Framework mit dem "sauberen" Seed
                        // Dieser String hat keine $, /, + oder = mehr.
                        string derivedPepper = await aes.GenerateDeterministicPepperAsync(safeUserSeed);

                        // 4. Im AppState setzen
                        _appState.UpdatePepper(derivedPepper);

                        //var encryptedData = await aes.EncryptBase32SecretAsync("Halli Hallo", Convert.FromBase64String(_appState.Pepper));

                        //string decryptedText = await aes.DecryptBase32SecretAsync(
                        //    encryptedBase64: encryptedData,
                        //    pepper: Convert.FromBase64String(_appState.Pepper)
                        //);

                        await _appState.Log($"[Security] Safe Normalized Pepper initialized for Identity {_appState.UnixTS}.");
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }
            }

            await _appState.Log("[BLAZOR AppInitializer.cs IsAuthenticationValid()] END");
            return result;
        }

        public async Task InitAccessible()
        {
            if (_appState == null || _appState.Catalog.LocalStorage == null) return;

            await _appState.Log("[BLAZOR AppInitializer] START InitAccessibility");
            try
            {
                // Sprache => Cookie/SecureStorage prüfen
                var restultLanguage = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.language}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (restultLanguage != null && string.IsNullOrEmpty(restultLanguage.out_err) && !string.IsNullOrEmpty(restultLanguage.out_value_str))
                    _appState.UpdateSelectedLanguage(restultLanguage.out_value_str); // Sprache aus Cookie/SecurStorage setzen
                else // ...ansonsten Standardsprache setzen
                {
                    if (restultLanguage != null && !string.IsNullOrEmpty(restultLanguage.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {restultLanguage.out_err}"
                        );
                    }

                    _appState!.UpdateSelectedLanguage(_globalState.ConfigGeneral.DefaultLanguage);
                }   

                // LTR - RTL laden
                var resultLtrrtl = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.ltrrtl}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultLtrrtl != null && string.IsNullOrEmpty(resultLtrrtl.out_err) && !string.IsNullOrEmpty(resultLtrrtl.out_value_str))
                {
                    _appState.UpdateLtrRtl(resultLtrrtl.out_value_str == LTR_RTL.RTL.ToString() ? LTR_RTL.RTL : LTR_RTL.LTR);
                    //await _jsRuntime!.InvokeVoidAsync("setRtl", ltrrtl == LTR_RTL.RTL.ToString() ? true : false); // Cannot invoke JavaScript outside of a WebView context
                }
                else
                {
                    if (resultLtrrtl != null && !string.IsNullOrEmpty(resultLtrrtl.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultLtrrtl.out_err}"
                        );
                    }

                    _appState!.UpdateLtrRtl(LTR_RTL.LTR);
                }

                // Sprache auslesen
                _appState.AccessibilityConfig.SelectedLanguage = _appState.SelectedLanguage;

                // FontFamily
                var resultFontfamily = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.fontfamily}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
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

                    _appState!.AccessibilityConfig.SelectedFontFamily = "Cinzel";
                }

                // FontSize
                var resultFontsize = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.fontsize}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
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

                // FontWeight
                var resultFontweigh = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.fontweigh}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontweigh != null && string.IsNullOrEmpty(resultFontweigh.out_err) && !string.IsNullOrEmpty(resultFontweigh.out_value_str))
                {
                    _appState!.AccessibilityConfig.SelectedFontWeight = JsonSerializer.Deserialize(
                        resultFontweigh.out_value_str,
                        JsonContext.Default.FontWeightModel
                    ) ?? new FontWeightModel();
                }
                else
                {
                    if (resultFontweigh != null && !string.IsNullOrEmpty(resultFontweigh.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontweigh.out_err}"
                        );
                    }
                }

                // FontSpacing
                var resultFontspacing = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.fontspacing}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontspacing != null && string.IsNullOrEmpty(resultFontspacing.out_err) && !string.IsNullOrEmpty(resultFontspacing.out_value_str))
                {
                    if (double.TryParse(resultFontspacing.out_value_str, out double spacing))
                    {
                        _appState!.AccessibilityConfig.SelectedSpacingValue = spacing;
                    }
                }
                else
                {
                    if (resultFontspacing != null && !string.IsNullOrEmpty(resultFontspacing.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontspacing.out_err}"
                        );
                    }
                }

                // FontLineHeight
                var resultFontlineheight = await _platform!.GetAsync($"{_appState.Catalog.LocalStorage.fontlineheight}{_globalState.ConfigGeneral.ApplicationName}");
                await Task.Yield();
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (resultFontlineheight != null && string.IsNullOrEmpty(resultFontlineheight.out_err) && !string.IsNullOrEmpty(resultFontlineheight.out_value_str))
                {
                    if (double.TryParse(resultFontlineheight.out_value_str, out double lineheight))
                    {
                        _appState!.AccessibilityConfig.SelectedLineHeightValue = lineheight;
                    }
                }
                else
                {
                    if (resultFontlineheight != null && !string.IsNullOrEmpty(resultFontlineheight.out_err))
                    {
                        await _messageBoxService!.ShowOkAsync(
                            title: _appState?.T("Error") ?? "Error",
                            message: $"{_appState?.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {resultFontlineheight.out_err}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer] ERROR InitAccessibility: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState!.Log("[BLAZOR AppInitializer] END InitAccessibility");
        }

        protected virtual async Task InitSharing()
        {
            await _appState.Log("[BLAZOR AppInitializer] START InitSharing");
            try
            {
                if (_globalState != null && _appState.StorageLocation != STORAGE_LOCATION.LOCAL)
                {
                    _globalState.SharingRequests.Clear();

                    // StorageLocation aus der DB holen
                    Dictionary<string, string> db_para = new()
                    {
                        { "@Case_", "SelectRequest>>SharingUsers" },
                        { DB_CMD.NO_LOCAL, string.Empty } // Kein Sharing wenn kein Internet
                    };

                    BlazorCore.Services.SqlClient.ReadModel<SharingUsersModel?> result = await _dam.ReadData<SharingUsersModel>(db_para)!; // Abfrage ausführen
                    await _appState.Log("[BLAZOR AppInitializer] SelectRequest>>SharingUsers:", data: result);
                    if (result != null && result.out_list != null) // Resultat prüfen
                    {
                        foreach(var item in result.out_list)
                            _globalState.SharingRequests.Add(item!.AuthUsers_ShareTo_UnixTS!);
                    }
                    else
                    {
                        string? err = result != null && !string.IsNullOrEmpty(result.out_err) ? result.out_err : "SelectRequest>>SharingUsers";
                        await _appState.Log($"[BLAZOR AppInitializer] ERROR InitSharing: {err}");
                        await _messageBoxService.ShowOkAsync(
                            title: _appState!.T("Error"),
                            message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                        );
                    }

                    //// Erinnerungen ermitteln
                    //if (_globalState != null && _appState != null && _globalState.SharingRequests.Contains(_appState.UnixTS))
                    //{
                    //    _appState.IncreaseNumberCurrentReminders();
                    //}
                }
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer] END ERROR InitSharing: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[BLAZOR AppInitializer] END InitSharing");
        }

        protected virtual async Task InitAppState()
        {
            await _appState.Log("[BLAZOR AppInitializer InitAppState] START");
            try
            {
                // AuthusersExtend (Userbild, Alias)
                // Parameter setzen
                Dictionary<string, string> db_para = new()
                    {
                        { "@Case_", "Select>>AuthUsersExtend" },
                        { "@AuthUsers_UnixTS", _appState.UnixTS }
                    };
                ReadModel<AuthUsersExtendModel?> result = await _dam.ReadData<AuthUsersExtendModel>(db_para)!; // Abfrage ausführen
                await _appState.Log("[BLAZOR AppInitializer InitAppState] Select>>AuthUsersExtend:", data: result);
                await Task.Delay(_globalState.ConfigGeneral.TaskDelay_Capacitor);
                if (result != null && string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
                {
                    if (result.out_list != null && result.out_list.Any())
                    {
                        _appState.UpdateAlias(result.out_list.FirstOrDefault()!.DisplayName!);
                        _appState.UpdateAliasImg(result.out_list.FirstOrDefault()!.imgJpegThumbnail!);
                    }
                }
                else
                {
                    string? err = result != null && !string.IsNullOrEmpty(result.out_err) ? result.out_err : "Select>>AuthUsersExtend";
                    await _appState.Log($"[BLAZOR AppInitializer InitAppState] ERROR Select>>AuthUsersExtend: {err}");
                    await _messageBoxService.ShowOkAsync(
                        title: _appState!.T("Error"),
                        message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {err}"
                    );
                }
            }
            catch (Exception ex)
            {
                await _appState.Log($"[BLAZOR AppInitializer InitAppState] ERROR: {ex.Message}");
                await _messageBoxService.ShowOkAsync(
                    title: _appState!.T("Error"),
                    message: $"{_appState.T(_globalState != null ? _globalState.ConfigGeneral.ErrorText : "We’re sorry, but an error occurred!")} {ErrorHelper.GetErrorContext(ex.Message)}"
                );
            }
            await _appState.Log("[BLAZOR AppInitializer InitAppState] END");
        }

        


    }
}
