using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using BlazorCore.Services.Dam;
using BlazorCore.Services.Otp;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;

namespace TestSolution4.Shared.Services.Platform
{
    public class Platform : BlazorCore.Services.Platform.IPlatformBase, Shared.Services.Platform.IPlatform
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Shared.Services.GlobalState.IGlobalState _globalState;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;

        private PLATFORMS? _cachedPlatform = null;
        protected const string JsWebPrefix = "pE_Web";
        protected const string JsCapPrefix = "pE_Capacitor";

        public Platform(
            IServiceProvider serviceProvider,
            Shared.Services.GlobalState.IGlobalState globalState,
            NavigationManager navigationManager,
            IJSRuntime js)
        {
            _serviceProvider = serviceProvider;
            _globalState = globalState;
            _navigationManager = navigationManager;
            _jsRuntime = js;
        }

        /// <summary>
        /// New Initialization Method
        /// Calls pE_Capacitor.init and captures the result/errors.
        /// </summary>
        public async Task<ScalarModel> InitializeJSAsync()
        {
            ScalarModel result = new();
            try
            {
                // 1. Check if the object exists in window
                var exists = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.pE_Capacitor !== 'undefined'");

                if (!exists)
                {
                    // If we are in the web project, this is normal
                    //pE.Utility.Appl.InitErrorJSRuntime = "Capacitor bridge not found (Web Mode).";
                    //IsNativeReady = false;
                    result.out_err = "Capacitor bridge not found (Web Mode).";
                }

                // We use InvokeAsync here because the JS init is asynchronous (Plugins)
                result = await _jsRuntime.InvokeAsync<ScalarModel>("pE_Capacitor.init");

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    //pE.Utility.Appl.InitErrorJSRuntime = result.out_err;
                    //IsNativeReady = false;
                    result.out_err = result.out_err;
                }
                //else
                //{
                //    //IsNativeReady = result.out_value_bool;
                //    // Optional: Update cached platform after init
                //    _cachedPlatform = null;
                //    GetCurrPlatform();
                //}
            }
            catch (Exception ex)
            {
                //pE.Utility.Appl.InitErrorJSRuntime = "JS-Bridge Error during Init: " + ex.Message;
                //Console.WriteLine($"[pE] {pE.Utility.Appl.InitErrorJSRuntime}");
                //return "JS-Bridge Error during Init: " + ex.Message;
                result.out_err = "JS-Bridge Error during Init: " + ex.Message;
            }

            return result;
        }

        // --- Hilfseigenschaft für die spätere Capacitor-Logik ---
        public bool IsNative => false;

        /// <summary>
        /// Retrieves the base directory for data storage.
        /// On WASM, returns the sandbox root "/".
        /// On Native platforms, returns the Capacitor DATA directory URI.
        /// </summary>
        public string GetBaseDirectory()
        {
            try
            {
                var platform = GetCurrPlatform();
                if (platform == PLATFORMS.WASM)
                {
                    return ""; // Root of the sandbox in WASM
                }
                else
                {
                    // Use IJSInProcessRuntime for a synchronous call to the cached JS variable
                    var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                    //return jsInProcess.Invoke<string>("pE_Capacitor.getBaseDirectory");
                    return jsInProcess.Invoke<string>($"{JsCapPrefix}.getBaseDirectory");
                }
            }
            catch (Exception ex)
            {
                // Fail-safe fallback if JS runtime is not yet fully available or call fails
                Console.WriteLine($"[pE] GetBaseDirectory failed: {ex.Message}");
                return "/";
            }
        }

        /// <summary>
        /// Retrieves the current platform using the Capacitor bridge or defaults to WASM.
        /// </summary>
        /// <returns>The current platform as a PLATFORMS enum value.</returns>
        public PLATFORMS GetCurrPlatform()
        {
            // Return cached value if already determined
            if (_cachedPlatform.HasValue)
                return _cachedPlatform.Value;

            try
            {
                // In Blazor WASM, we can cast IJSRuntime to IJSInProcessRuntime for synchronous calls
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;

                // Call the helper function in pE/cap.js
                string platformStr = jsInProcess.Invoke<string>("pE_Capacitor.getPlatform");

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
                // Fallback for startup phase or errors
                Console.WriteLine($"Error detecting platform, defaulting to WASM: {ex.Message}");
                return PLATFORMS.WASM;
            }

            return _cachedPlatform.Value;
        }

        public p11.UI.NativeDevice GetCurrDevice()
        {
            var currPlatform = GetCurrPlatform();
            switch (currPlatform)
            {
                case PLATFORMS.WASM:
                    return p11.UI.NativeDevice.WEB;

                case PLATFORMS.ANDROID:
                    return p11.UI.NativeDevice.ANDROID;

                case PLATFORMS.IOS:
                    return p11.UI.NativeDevice.IPHONE;

                case PLATFORMS.MAC_CLIENT:
                    return p11.UI.NativeDevice.MAC;
            }
            return p11.UI.NativeDevice.WEB;
        }

        // --- Local Storage (Stubs / Vorbereitet für JSInterop) ---
        //public async Task<bool> SavingAllowed()
        //{
        //    // Hier können Sie zusätzliche Logik hinzufügen, um zu überprüfen, ob das Speichern erlaubt ist
        //    return await Task.FromResult(true);
        //}

        /// Stores a value. 
        /// WASM: Uses the robust setCookie logic from app.js (Cookie with localStorage fallback).
        /// Native: Uses Capacitor Preferences via cap.js.
        /// </summary>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> SetAsync(string identifier, string value)
        {
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                // Standard Web Logic
                //await _jsRuntime.InvokeVoidAsync("setCookie", identifier, value, 7);
                await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.setValue", identifier, value, 7);
                return new ScalarModel { out_value_bool = true };
            }
            else
            {
                // Native Logic with full error reporting
                // var result = await _jsRuntime.InvokeAsync<BlazorCore.Services.SqlClient.ScalarModel>("pE_Capacitor.setStorage", identifier, value);
                var result = await _jsRuntime.InvokeAsync<ScalarModel>($"{JsCapPrefix}.setStorage", identifier, value);

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    // Log for debugging on the physical device
                    Console.WriteLine($"[Storage Error]: {result.out_err}");
                }

                return result;
            }
        }

        /// <summary>
        /// Removes a value from storage and returns the status as a ScalarModel.
        /// </summary>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> RemoveAsync(string identifier)
        {
            try
            {
                var platform = GetCurrPlatform();

                if (platform == PLATFORMS.WASM)
                {
                    // Web Logic: resetCookie usually doesn't return anything, so we create a success model
                    //await _jsRuntime.InvokeVoidAsync("resetCookie", identifier);
                    await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.resetCookie", identifier);
                    return new ScalarModel { out_value_bool = true };
                }
                else
                {
                    // Native Logic: We capture the ScalarModel from cap.js
                    var result = await _jsRuntime.InvokeAsync<BlazorCore.Services.SqlClient.ScalarModel>($"{JsCapPrefix}.removeStorage", identifier);

                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        Console.WriteLine($"[pE] RemoveAsync Failed: {result.out_err}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = "RemoveAsync Exception: " + ex.Message, out_value_bool = false };
            }
        }

        //public void RemoveAllAsync() { }
        //public string? Get(string identifier) => null;

        /// <summary>
        /// Retrieves a value.
        /// WASM: Uses getValue from app.js (Checks Cookie, then localStorage).
        /// Native: Uses getStorage from cap.js.
        /// </summary>
        /// <summary>
        /// Retrieves a value from storage with full error reporting.
        /// </summary>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> GetAsync(string identifier)
        {
            try
            {
                var platform = GetCurrPlatform();

                if (platform == PLATFORMS.WASM)
                {
                    // Direct call to your existing app.js logic
                    //var val = await _jsRuntime.InvokeAsync<string?>("pE_Web.getValue", identifier);
                    var val = await _jsRuntime.InvokeAsync<string?>($"{JsWebPrefix}.getValue", identifier);
                    return new ScalarModel { out_value_str = val ?? "", out_value_bool = true };
                }
                else
                {
                    // Call our new cap.js method
                    //var result = await _jsRuntime.InvokeAsync<BlazorCore.Services.SqlClient.ScalarModel>("pE_Capacitor.getStorage", identifier);
                    var result = await _jsRuntime.InvokeAsync<ScalarModel>($"{JsCapPrefix}.getStorage", identifier);

                    // If there's an error, we can handle it here or pass it up
                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        Console.WriteLine($"[pE] Native Storage Read Failed: {result.out_err}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = "GetAsync Exception: " + ex.Message, out_value_bool = false };
            }
        }

        // --- Utilities (Stubs) ---
        public void StartConnectivityMonitoring(BlazorCore.Services.AppState.IAppStateBase appState) { }
        public Task<bool> InternetConnectedAsync() => Task.FromResult(true);

        /// <summary>
        /// Satisfies the IPlatform interface. 
        /// Matches the MAUI implementation: public string GetDeviceInfo()
        /// </summary>
        public string GetDeviceInfo()
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                // Use your existing app.js function: setCookie(name, value, daysToExpire)
                //return jsInProcess.Invoke<string>("pE_Web.getDeviceInfo");
                return jsInProcess.Invoke<string>($"{JsWebPrefix}.getDeviceInfo");
            }
            else
            {
                // Native Capacitor logic via cap.js
                //return jsInProcess.Invoke<string>("pE_Capacitor.getDeviceInfo");
                return jsInProcess.Invoke<string>($"{JsCapPrefix}.getDeviceInfo");
            }
        }

        //public string GetFormFactor() => "Web";
        //public string GetFormFactor()
        //{
        //    var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
        //    var platform = GetCurrPlatform();

        //    if (platform == PLATFORMS.WASM)
        //    {
        //        // Use your existing app.js function: setCookie(name, value, daysToExpire)
        //        return "Wasm";
        //    }
        //    else
        //    {
        //        // Native Capacitor logic via cap.js
        //        return jsInProcess.Invoke<string>("pE_Capacitor.getDeviceType");
        //    }
        //}
        public async Task<string> GetFormFactor() // Jetzt Task<string> statt string
        {
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                return "Wasm";
            }
            else
            {
                // Wir nutzen InvokeAsync, damit C# auf das JavaScript-Promise wartet
                // Das verhindert, dass wir "unknown" erhalten, während die Bridge noch arbeitet
                //return await _jsRuntime.InvokeAsync<string>("pE_Capacitor.getDeviceType");
                return await _jsRuntime.InvokeAsync<string>($"{JsCapPrefix}.getDeviceType");
            }
        }

        //public Task<double> GetWindowWidth() => Task.FromResult(1920d);
        public async Task<double> GetWindowWidth()
        {
            // Wir nutzen direkt das globale window-Objekt des Browsers/WebView
            return await _jsRuntime.InvokeAsync<double>("eval", "window.innerWidth");
        }

        //public Task<double> GetWindowHeight() => Task.FromResult(1080d);
        public async Task<double> GetWindowHeight()
        {
            // Greift direkt auf die Höhe des Viewports zu, egal ob lokal oder nativ
            return await _jsRuntime.InvokeAsync<double>("eval", "window.innerHeight");
        }

        //public Task<string> GetIdiomPlatform() => Task.FromResult("Web-WASM");
        public async Task<string> GetIdiomPlatform()
        {
            var platform = GetCurrPlatform();

            // FALL 1: Native Verpackung (Capacitor)
            if (platform != PLATFORMS.WASM)
            {
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                // Wir nutzen den Cache in cap.js für sofortige Antwort
                //return jsInProcess.Invoke<string>("pE_Capacitor.getIdiomPlatform");
                return jsInProcess.Invoke<string>($"{JsCapPrefix}.getIdiomPlatform");
            }

            // FALL 2: Reines Web/WASM (Browser-Debugging)
            // Hier bleibt deine bisherige Logik als Fallback
            double screenWidth = await GetWindowWidth();
            string idiom = screenWidth switch
            {
                >= 1024 => "Desktop",
                >= 768 => "Tablet",
                _ => "Phone"
            };

            //string os = await _jsRuntime.InvokeAsync<string>("pE_Web.getOSVersion");
            string os = await _jsRuntime.InvokeAsync<string>($"{JsWebPrefix}.getOSVersion");
            return $"Browser-{idiom}-{os}";
        }

        //public Task CopyTextToClipboard(string text) => Task.CompletedTask;
        public async Task CopyTextToClipboard(string text)
        {
            var platform = GetCurrPlatform();

            try
            {
                if (platform != PLATFORMS.WASM)
                {
                    // Nativer Capacitor-Pfad
                    //await _jsRuntime.InvokeVoidAsync("pE_Capacitor.copyToClipboard", text);
                    await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.copyToClipboard", text);
                }
                else
                {
                    // Browser-Fallback (WASM Debugging)
                    // Nutzt die Standard JS-Web-API
                    await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clipboard error: {ex.Message}");
                // In einer Enterprise-App könntest du hier ein Toast-Service aufrufen
            }
        }

        // --- OTP (Stubs) ---
        //public Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        //    => Task.FromResult(new OtpModel { err = "Not implemented in WASM yet" });
        public async Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        {
            OtpModel result = new();

            try
            {
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "SaveOtp>>AuthUsers" },
                    { "@UnixTS", otpParameters.UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode },
                    { "@otp", "" }, // wird auf dem WebServer generiert, verschlüsselt und gesetzt
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS }
                };

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                // Achtung: Zu diesem Zeitpunkt (Zeitpunkt bei dem eine 2FA noch nicht erfolgt ist) dürfen wir
                // kein Token haben. Folglich müssen wir dann die 2FA Validierung über den Anonymen-Endpoint durchführen
                ScalarModel resultOtp = await _dam!.AnonymousQuery(db_para)!;

                if (resultOtp != null && resultOtp.out_value_str != null)
                {
                    result.secret = resultOtp.out_value_str;

                    if (string.IsNullOrEmpty(resultOtp.out_err) && resultOtp.out_value_str != null)
                    {
                        if (resultOtp.out_value_str.ToLower().StartsWith("updated:"))
                        {
                            result.err = "";
                        }
                    }
                    else
                    {
                        if (resultOtp.out_err != null)
                            result.err = resultOtp.out_err;
                    }
                }
                else
                    result.err = "no_otp";
            }
            catch (Exception ex)
            {
                result.err = ex.Message;
            }

            return result;
        }

        //public Task<bool> CheckServerLoginState(OtpParametersModel otpParameters)
        //    => Task.FromResult(false);
        public async Task<bool> CheckServerLoginState(OtpParametersModel otpParameters)
        {
            var result = false;

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "CheckAccount>>AuthUsers" },
                    { "@EmailHash", otpParameters.Account },
                    { "@PasswordHash", otpParameters.Password },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                    { DB_CMD.NO_LOCAL, string.Empty }
                };

                // Achtung: Zu diesem Zeitpunkt (Zeitpunkt bei dem eine 2FA noch nicht erfolgt ist) dürfen wir
                // kein Token haben. Folglich müssen wir dann die 2FA Validierung über den Anonymen-Endpoint durchführen
                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                ScalarModel resultCheckOtp = await dam!.AnonymousQuery(db_para)!;
                if (resultCheckOtp != null && string.IsNullOrEmpty(resultCheckOtp.out_err))
                    result = resultCheckOtp.out_value_bool;
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        //public Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        //    => Task.FromResult(new ScalarModel { out_err = "Not implemented" });
        public async Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        {
            ScalarModel result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "DeleteOtp>>AuthUsers" },
                    { "@EmailHash", otpParameters.Account },
                    { "@PasswordHash", otpParameters.Password },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode }, // Falls Benutzer die 2FA über Backupcode zurücksetzen muss
                    { "tmp_userinputiode", otpParameters.OtpUserDigitInput }, // ...oder halt über 6-stelligen otp-Eingabe
                    { DB_CMD.NO_LOCAL, string.Empty }
                };

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                // Achtung: Zu diesem Zeitpunkt (Zeitpunkt bei dem eine 2FA noch nicht erfolgt ist) dürfen wir
                // kein Token haben. Folglich müssen wir dann die 2FA Validierung über den Anonymen-Endpoint durchführen
                result = await _dam!.AnonymousQuery(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = $"[ERROR]={ex.Message}";
            }

            return result;
        }

        // --- Idp / Auth ---
        //public Task<string?> AuthenticateAsync(string authUrl) => Task.FromResult<string?>(null);
        public async Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false)
        {
            var platform = GetCurrPlatform();

            // Wir prüfen, ob wir im "Polling-Modus" sind (WASM & Capacitor)
            // oder im "Server-Modus" (Blazor Web)
            bool isPollingFlow = platform == PLATFORMS.WASM || platform == PLATFORMS.ANDROID || platform == PLATFORMS.IOS;

            if (isPollingFlow)
            {
                // CAPACITOR & WASM DEBUG FLOW
                // Wir dürfen die App NICHT entladen (kein forceLoad), 
                // weil wir die SignalR-Verbindung für die Polling-Bridge brauchen.

                if (platform == PLATFORMS.WASM)
                {
                    // Im Browser-Debug öffnen wir einen neuen Tab, damit der SignalR-Tab offen bleibt
                    await _jsRuntime.InvokeVoidAsync("eval", $"window.open('{authUrl}', '_blank')");
                }
                else
                {
                    // In Capacitor öffnen wir das native In-App Overlay
                    //await _jsRuntime.InvokeVoidAsync("pE_Capacitor.openAuthBrowser", authUrl);
                    await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.openAuthBrowser", authUrl);
                }

                // Der Polling-Loop startet jetzt in der Blazor-Komponente
            }
            else
            {
                // BLAZOR SERVER / WEB FLOW
                // Hier ist der klassische Weg über den Redirect zum Cookie-Endpoint richtig
                _navigationManager.NavigateTo(authUrl, forceLoad: true);
            }

            return null;
        }

        //public Task<string?> DirectoryPicker() => Task.FromResult<string?>(null);
        public async Task<string?> DirectoryPicker()
        {
            await Task.Delay(10);

            var platform = GetCurrPlatform();

            // 1. Fall: Reines Web (WASM im Browser / Server-Web)
            if (platform == PLATFORMS.WASM)
            {
                // Im Web gibt es keinen Pfad-String. 
                // Der User "wählt" den Ort erst beim Download-Dialog des Browsers.
                return string.Empty;
            }

            // 2. Fall: Capacitor (WASM als nativ verpackt)
            // Hier nutzen wir die JS-Bridge, um den stabilen Pfad der Sandbox zu holen.
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            //return jsInProcess.Invoke<string>("pE_Capacitor.getDirectoryPath");
            return jsInProcess.Invoke<string>($"{JsCapPrefix}.getDirectoryPath");
        }

        public async Task<ScalarModel> FilePicker(string filter, string title)
        {
            ScalarModel result = new();
            return result;
        }

        //public Task ShareText(string title, string text) => Task.CompletedTask;
        public async Task ShareText(string title, string text)
        {
            var platform = GetCurrPlatform();

            try
            {
                if (platform == PLATFORMS.WASM)
                {
                    // WASM Lokal: Nutzt deine existierende shareText in app.js
                    //await _jsRuntime.InvokeVoidAsync("pE_Web.share.shareText", title, text);
                    await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.shareText", title, text);
                }
                else
                {
                    // Capacitor Nativ: Nutzt die Bridge zur cap.js
                    //await _jsRuntime.InvokeVoidAsync("pE_Capacitor.shareText", title, text);
                    await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.shareText", title, text);
                }
            }
            catch (Exception ex)
            {
                // Falls der User das Share-Sheet abbricht, werfen manche Browser/Plugins Exceptions.
                // Das fangen wir hier ab, genau wie in MAUI.
                Console.WriteLine($"Sharing failed or was cancelled: {ex.Message}");
            }
        }

        //public Task<ScalarModel> Feedback(BlazorCore.Pages.ContactformModel ContactForm)
        //    => Task.FromResult(new ScalarModel { out_err = "Not implemented" });
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Feedback(BlazorCore.Pages.ContactformModel ContactForm)
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SaveFeedback>>AppMessages" },
                    { "@DisplayName", ContactForm.NameEmail },
                    { "@Title", ContactForm.Title },
                    { "@Body", ContactForm.Message },
                    { DB_CMD.NO_LOCAL, string.Empty } // Nur auf Cloud speichern
                };

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                // Achtung =>   hier über Plattform gelöst, weil einmal '_dam.AnonymousQuery(db_para)' und einmal
                //              '_dam.Save(db_para)' aufgerufen wird (je nach Plattform)
                result = await _dam.AnonymousQuery(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result;
        }

        //public Task OpenExternalUrl(string url) => Task.CompletedTask;
        public async Task OpenExternalUrl(string url)
        {
            var platform = GetCurrPlatform();

            if (platform == PLATFORMS.WASM)
            {
                // WASM Lokal: Standard Browser-Verhalten (Neuer Tab)
                // Wir nutzen hier direkt eval oder deine app.js
                await _jsRuntime.InvokeVoidAsync("eval", $"window.open('{url}', '_blank')");
            }
            else
            {
                // Capacitor Nativ: Nutzt das native Browser-Overlay aus der cap.js
                //await _jsRuntime.InvokeVoidAsync("pE_Capacitor.openExternalUrl", url);
                await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.openExternalUrl", url);
            }
        }

        //public Task<bool> IsTokenValidAsync(string token) => Task.FromResult(false);

        // --- Preferences ---
        //public void SetPreference(string key, string value) { }
        //public string? GetPreference(string key) => null;
        //public void RemovePreference(string key) { }
        // public bool ContainsPreference(string key) => false;

        public void SetPreference(string key, string value)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            // Wir nutzen die synchrone JS-Bridge
            //jsInProcess.InvokeVoid("pE_Capacitor.setStorageSync", key, value);
            jsInProcess.InvokeVoid($"{JsCapPrefix}.setStorageSync", key, value);
        }

        public string? GetPreference(string key)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            // Wir nutzen die synchrone JS-Bridge
            //return jsInProcess.Invoke<string?>("pE_Capacitor.getStorageSync", key);
            return jsInProcess.Invoke<string?>($"{JsCapPrefix}.getStorageSync", key);
        }

        public void RemovePreference(string key)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            //jsInProcess.InvokeVoid("pE_Capacitor.removeStorageSync", key);
            jsInProcess.InvokeVoid($"{JsCapPrefix}.removeStorageSync", key);
        }

        public bool ContainsPreference(string key)
        {
            var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
            //return jsInProcess.Invoke<bool>("pE_Capacitor.containsStorageSync", key);
            return jsInProcess.Invoke<bool>($"{JsCapPrefix}.containsStorageSync", key);
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            try
            {
                var _httpClient = _serviceProvider.GetRequiredService<HttpClient>();

                if (_httpClient != null)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, _globalState.ConfigWebapi.url_Authorizedconnection);

                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    // Optional: Body leeren oder ein Dummy-Objekt senden
                    request.Content = System.Net.Http.Json.JsonContent.Create(new { });

                    using var response = await _httpClient.SendAsync(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        return false;

                    response.EnsureSuccessStatusCode();
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                // Bei Netzwerkfehlern → Token als ungültig behandeln
                return false;
            }
        }


        // File
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();

            try
            {
                var platform = GetCurrPlatform();

                // Stream sicherheitshalber auf den Anfang setzen
                if (stream.CanSeek) stream.Position = 0;

                // 1. Fall: Reines Web (WASM im Browser / Lokal in Visual Studio)
                if (platform == PLATFORMS.WASM)
                {
                    // Nutzt deine bestehende pE_Web JS-Funktion für den Browser-Download
                    using var streamRef = new DotNetStreamReference(stream);
                    await _jsRuntime!.InvokeVoidAsync($"{JsWebPrefix}.downloadFileFromStream", filename, streamRef);

                    result.out_value_bool = true;
                    result.out_value_str = filename;
                    return result;
                }

                // 2. Fall: Capacitor (WASM als nativ verpackt auf iOS/Android)
                byte[] fileBytes;
                if (stream is MemoryStream ms)
                {
                    fileBytes = ms.ToArray();
                }
                else
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Konvertierung in Base64 für den JS-Bridge-Transfer
                string base64Data = Convert.ToBase64String(fileBytes);

                // 1. JS-Bridge aufrufen (liefert JSON-String)
                // Aufruf mit dem zusätzlichen Parameter
                string jsonResponse = await _jsRuntime!.InvokeAsync<string>(
                    $"{JsCapPrefix}.saveFileNative",
                    filename,
                    base64Data,
                    title
                );

                // AOT-sichere Deserialisierung
                result = System.Text.Json.JsonSerializer.Deserialize(
                    jsonResponse,
                    BlazorCore.JsonContext.Default.ScalarModel
                );

                // 3. Fallback, falls doch mal null zurückkommt (Sicherheit)
                return result ?? new BlazorCore.Services.SqlClient.ScalarModel { out_value_bool = false, out_err = "Deserialization failed" };
            }
            catch (Exception ex)
            {
                result!.out_value_bool = false;
                result.out_err = ex.Message;
                // Kontext für dein Error-Logging
                result.out_local = "WasmPlatformService: SaveFileNativeAsync Exception";
                return result;
            }
        }


        // --- Navigation & Native Bridge ---
        public async Task RegisterNativeNavigationAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
        {
            try
            {
                // Wir prüfen, ob wir auf einer nativen Plattform sind
                var platform = GetCurrPlatform();
                switch (platform)
                {
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                        // Das Delay bleibt hier, um der Capacitor-Bridge Zeit zu geben
                        await Task.Delay(250);
                        await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.app.registerNavigationHelper", dotNetRef);
                        break;

                    case PLATFORMS.WASM:
                        await _jsRuntime.InvokeVoidAsync("pE_Web.registerWebNavigationHelper", dotNetRef);
                        break;
                }
              
                if (platform == PLATFORMS.ANDROID || platform == PLATFORMS.IOS)
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] RegisterNativeNavigationAsync failed: {ex.Message}");
            }
        }

        public async Task SetSwipeBackStateAsync(bool enabled)
        {
            try
            {
                // Swipe-Logik ist meistens im Web-Teil definiert (pE_Web)
                await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.swipeBack.enable", enabled);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] SetSwipeBackStateAsync failed: {ex.Message}");
            }
        }

        public async Task ForceResetSwipeAsync()
        {
            try
            {
                // Der doppelte Reset zur Sicherheit, wie im ursprünglichen Cleanup-Task
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

        public async Task ExitAppAsync()
        {
            try
            {
                var platform = GetCurrPlatform();
                if (platform == PLATFORMS.ANDROID || platform == PLATFORMS.IOS)
                {
                    await _jsRuntime.InvokeVoidAsync($"{JsCapPrefix}.app.exitApp");
                }
                else
                {
                    // Fallback für Web/WASM (optional)
                    Console.WriteLine("[Platform] ExitAppAsync called in Web Mode - No action taken.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] ExitAppAsync failed: {ex.Message}");
            }
        }

        //public async Task CleanupNativeUIAsync()
        //{
        //    try
        //    {
        //        // 1. Alle Modals (inkl. Native) über die gemeinsame Funktion schließen
        //        await _jsRuntime.InvokeVoidAsync("TpcModalJs.clearAllModals");

        //        // 2. Body-Scroll-Lock zurücksetzen (Fallback für harte Navigation)
        //        await _jsRuntime.InvokeVoidAsync("eval", @"
        //            if (!document.body.classList.contains('lock-body-scroll')) {
        //                document.body.style.overflow = '';
        //                document.body.style.paddingRight = '';
        //            }
        //        ");

        //        Console.WriteLine("[Platform] CleanupNativeUIAsync: All modals cleared.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[Platform] CleanupNativeUIAsync failed: {ex.Message}");
        //    }
        //}

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

        public void Log(string message, bool isError = false)
        {

        }

    }
}
