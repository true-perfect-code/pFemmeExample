#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OtpNet;
using BlazorCore.Services.Dam;
using BlazorCore.Services.Otp;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;

namespace TestSolution4.Web.Services
{
    public class Platform : BlazorCore.Services.Platform.IPlatformBase, TestSolution4.Shared.Services.Platform.IPlatform
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly TestSolution4.Shared.Services.GlobalState.IGlobalState _globalState;
        //private readonly IAppState _appState;
        private readonly IServiceProvider _serviceProvider;
        private readonly NavigationManager _navigationManager;
        private static BlazorCore.Services.AppState.IAppStateBase? _appStateStaticReference;
        private readonly IHttpClientFactory _httpClientFactory;

        private BlazorCore.Services.AppState.IAppStateBase? _appState;

        private static string? _cachedDeviceId;

        public Platform(
            IJSRuntime jsRuntime, 
            TestSolution4.Shared.Services.GlobalState.IGlobalState globalState, 
            IServiceProvider serviceProvider, 
            NavigationManager navigationManager,
            IHttpClientFactory httpClientFactory)
        {
            _jsRuntime = jsRuntime;
            _globalState = globalState;
            //_appState = appState;
            _serviceProvider = serviceProvider;
            _navigationManager = navigationManager;
            _httpClientFactory = httpClientFactory;

            // WICHTIG: Die statische Referenz setzen, damit JS die aktuell aktive Instanz findet.
            _appStateStaticReference = _appState; // ACHTUNG: _appState ist im Konstruktor noch NULL!
        }

        // --- Core Initialization ---
        public async Task<ScalarModel> InitializeJSAsync()
        {
            // On Web, we usually don't have to initialize anything for the bridge.
            // We just ensure the JS runtime is responsive.
            //await Task.CompletedTask;
            await Task.Delay(10);

            return new ScalarModel();
        }

        /// <summary>
        /// Gets the base directory of the application.
        /// </summary>
        /// <returns>
        /// A string representing the full path of the application's base directory.
        /// </returns>
        public string GetBaseDirectory()
        {
            return AppContext.BaseDirectory;
        }

        public PLATFORMS GetCurrPlatform()
        {
            return PLATFORMS.WINDOWS_SERVER;
        }

        public p11.UI.NativeDevice GetCurrDevice() => p11.UI.NativeDevice.WEB;
                
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> SetAsync(string identifier, string value)
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();
            try
            {
                // Wir nutzen deine bestehende JS-Funktion
                await _jsRuntime.InvokeVoidAsync("pE_Web.setCookie", identifier, value, 365);

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

        public async Task<BlazorCore.Services.SqlClient.ScalarModel> GetAsync(string identifier)
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();
            try
            {
                var val = await _jsRuntime.InvokeAsync<string?>("pE_Web.getValue", identifier);

                result.out_value_str = val ?? "";
                result.out_value_bool = true; // Auch wenn leer, war der Aufruf erfolgreich
            }
            catch (Exception ex)
            {
                result.out_err = $"Web-Get-Error: {ex.Message}";
                result.out_value_bool = false;
            }
            return result;
        }

        public async Task<BlazorCore.Services.SqlClient.ScalarModel> RemoveAsync(string identifier)
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();
            try
            {
                await _jsRuntime.InvokeVoidAsync("pE_Web.deleteCookieAndLocalStorage", identifier);

                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                result.out_err = $"Web-Remove-Error: {ex.Message}";
                result.out_value_bool = false;
            }
            return result;
        }

        public string GetDeviceInfo()
        {
            // Für Web: zufällige Geräte-ID, kryptografisch sicher
            byte[] buffer = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);

            // 4 Bytes → 32 Bit → int (max ca. 2,1 Milliarden)
            int randomValue = Math.Abs(BitConverter.ToInt32(buffer, 0)) % 9_000_000 + 1_000_000;
            return randomValue.ToString(); // 7-stellige "Pseudo-Device-ID"
        }

        public Task<string> GetFormFactor()
        {
            // Da wir hier kein asynchrones JS brauchen, 
            // geben wir das Ergebnis direkt als abgeschlossenen Task zurück.
            return Task.FromResult("Web");
        }

        public async Task<double> GetWindowWidth()
        {
            WindowDimensions dimension = await _jsRuntime!.InvokeAsync<WindowDimensions>("pE_Web.getWindowDimensions");
            return dimension.Width;
        }

        public async Task<double> GetWindowHeight()
        {
            WindowDimensions dimension = await _jsRuntime!.InvokeAsync<WindowDimensions>("pE_Web.getWindowDimensions");
            return dimension.Height;
        }

        public async Task<string> GetIdiomPlatform()
        {
            try
            {
                // Bestimme die Bildschirmbreite
                double screenWidth = await GetWindowWidth();

                // Bestimme das Idiom basierend auf der Bildschirmbreite
                string idiom;
                if (screenWidth >= 1024)
                {
                    idiom = "Browser-Desktop";
                }
                else if (screenWidth >= 768)
                {
                    idiom = "Browser-Tablet";
                }
                else
                {
                    idiom = "Browser-Phone";
                }

                // Hole die Plattforminformationen
                //string platform = Environment.OSVersion.ToString();
                string platform = await _jsRuntime.InvokeAsync<string>("pE_Web.getOSVersion");

                // Kombiniere Idiom und Plattform
                return $"{idiom}-{platform}";
            }
            catch
            {
                // Fehlerbehandlung, falls nötig
                throw;
            }
        }

        public async Task CopyTextToClipboard(string text)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("pE_Web.copyClipboard", text);
            }
            catch (Exception)
            {
                throw;
            }
        }


        //---


        // Otp
        // Sicherheitsrelevanter Programmcode (darf nur auf dem Server ausgeführt werden)
        public async Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        {
            OtpModel result = new();
            result.err = "no_otp_code"; // Default-Wert

            try
            {
                // Pepper auslesen => Achtung: darf aus Sicherheitsgründen nur auf dem Server erfolgen!!
                byte[]? Pepper = null;
                //using (SecurityServer aes = new())
                //{
                //    Pepper = aes.GetPepper(_globalState.SecurityConfigFile);
                //}
                var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);
                if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
                {
                    var pepperFilePath = resultConfigurationFile.out_value_str;
                    try
                    {
                        // Pepper setzen
                        using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                        {
                            // Hiermit einmalig verschlüsselten Papper generieren und in der Datei C:\...\_Connections\security.config.json speichern 
                            //string encryptedPepper = aes.GenerateEncryptedPepper();
                            Pepper = aes.GetPepper(pepperFilePath);
                        }
                    }
                    catch
                    {
                        // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + ex.Message);
                    }
                }


                if (Pepper != null)
                {
                    // OTP - Generierung
                    byte[] secretKey = KeyGeneration.GenerateRandomKey();
                    string base32Secret = Base32Encoding.ToString(secretKey).TrimEnd('=');

                    if (!string.IsNullOrEmpty(base32Secret))
                    {
                        result.secret = base32Secret; // Darf zum client nur einmal gesendet werden => otp-Setup

                        using (var aes = new TestSolution4.Shared.Services.Security.SecurityServer())
                        {
                            base32Secret = aes.EncryptBase32Secret(base32Secret, Pepper!);
                        }

                        // 3. Datenbank-Speicherung
                        //var dam = context.RequestServices.GetRequiredService<Dam>();
                        var db_para = new Dictionary<string, string>
                        {
                            { "@Case_", "SaveOtp>>AuthUsers" },
                            { "@UnixTS", otpParameters.UnixTS },
                            { "@OtpBackupCode", otpParameters.OtpBackupCode },
                            { "@otp", base32Secret },
                            { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                            { DB_CMD.NO_LOCAL, string.Empty }
                        };

                        var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                        ScalarModel result_otp = await _dam.Save(db_para)!;
                        if (string.IsNullOrEmpty(result_otp.out_err) && result_otp.out_value_str != null)
                        {
                            if (result_otp.out_value_str.ToLower().StartsWith("updated:"))
                            {
                                result.err = "";
                            }
                        }
                        else
                        {
                            if (result_otp.out_err != null)
                                result.err = result_otp.out_err;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.err = ex.Message;
            }

            return result;
        }

        // Sicherheitsrelevanter Programmcode (darf nur auf dem Server ausgeführt werden)
        //public async Task<BlazorCore.Services.Authentication.OtpLoginStatus> CheckServerLoginState(BlazorCore.Services.Authentication.LoginModel login, bool isHashed = false)
        public async Task<bool> CheckServerLoginState(OtpParametersModel login)
        {
            bool result = false;

            byte[]? Pepper = null;
            BlazorCore.Services.Authentication.LoginModel login_clone = new();

            // Pepper auslesen => Achtung: darf aus Sicherheitsgründen nur auf dem Server erfolgen!!
            //using (SecurityServer aes = new())
            //{
            //    Pepper = aes.GetPepper(_globalState.SecurityConfigFile);
            //}
            var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);
            if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
            {
                var pepperFilePath = resultConfigurationFile.out_value_str;
                try
                {
                    // Pepper setzen
                    using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                    {
                        // Hiermit einmalig verschlüsselten Papper generieren und in der Datei C:\...\_Connections\security.config.json speichern 
                        //string encryptedPepper = aes.GenerateEncryptedPepper();
                        Pepper = aes.GetPepper(pepperFilePath);
                    }
                }
                catch
                {
                    // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + ex.Message);
                }
            }

            if (Pepper != null)
            {
                using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                {
                    login_clone.Password = aes.HashCredentials(login.Password, login.Account, Pepper!);
                    login_clone.Account = aes.HashUsername(login.Account, Pepper!);
                }
            }

            Dictionary<string, string> db_para = new()
            {
                { "@Case_", "CheckAccount>>AuthUsers" },
                { "@EmailHash", login_clone.Account! },
                { "@PasswordHash", login_clone.Password! },
                { DB_CMD.NO_LOCAL, string.Empty } // Nur auf Cloud ausführen
            };

            var _dam = _serviceProvider.GetRequiredService<IDamBase>();
            ScalarModel resultOtp = await _dam.Scalar(db_para)!;

            if (resultOtp != null && string.IsNullOrEmpty(resultOtp.out_err))
            {
                result = resultOtp.out_value_bool;
            }

            return result;

        }   

        // Sicherheitsrelevanter Programmcode (darf nur auf dem Server ausgeführt werden)
        public async Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        {
            ScalarModel result = new();

            try
            {
                byte[]? Pepper = null;
                OtpParametersModel otpParameters_clone = new();

                otpParameters_clone.AuthUsers_UnixTS = otpParameters.AuthUsers_UnixTS;
                otpParameters_clone.OtpBackupCode = otpParameters.OtpBackupCode;

                var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);
                if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
                {
                    var pepperFilePath = resultConfigurationFile.out_value_str;
                    try
                    {
                        // Pepper setzen
                        using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                        {
                            // Hiermit einmalig verschlüsselten Papper generieren und in der Datei C:\...\_Connections\security.config.json speichern 
                            //string encryptedPepper = aes.GenerateEncryptedPepper();
                            Pepper = aes.GetPepper(pepperFilePath);
                        }
                    }
                    catch
                    {
                        // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + ex.Message);
                    }
                }

                if (Pepper != null)
                {
                    // Läuft die Validierung über Account/Passwort oder UnixTS
                    if (!string.IsNullOrEmpty(otpParameters.Account) && !string.IsNullOrEmpty(otpParameters.Password))
                    {
                        if (isHashed)
                        {
                            otpParameters_clone.Password = otpParameters.Password;
                            otpParameters_clone.Account = otpParameters.Account;
                        }
                        else
                        {
                            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                            {
                                otpParameters_clone.Password = aes.HashCredentials(otpParameters.Password, otpParameters.Account, Pepper!);
                                otpParameters_clone.Account = aes.HashUsername(otpParameters.Account, Pepper!);
                            }
                        }
                    }

                    Dictionary<string, string> db_para = new()
                    {
                        { "@Case_", "DeleteOtp>>AuthUsers" },
                        { "@EmailHash", otpParameters_clone.Account },
                        { "@PasswordHash", otpParameters_clone.Password },
                        { "@AuthUsers_UnixTS", otpParameters_clone.AuthUsers_UnixTS },
                        { "@OtpBackupCode", otpParameters_clone.OtpBackupCode }, // Falls Benutzer die 2FA über Backupcode zurücksetzen muss
                        { DB_CMD.NO_LOCAL, string.Empty }
                    };

                    var _dam = _serviceProvider.GetRequiredService<IDamBase>();

                    // Wenn kein 6-stelliger otp-code angegeben, dann nutzt man otp-Backupcode
                    if (string.IsNullOrEmpty(otpParameters.OtpUserDigitInput))
                    {
                        result = await _dam.ExecQuery(db_para)!;
                    }
                    else
                    {
                        // Wenn 2FA über 6-stellige otp-Eingabe erfolgt
                        db_para["@Case_"] = "SelectOtp>>AuthUsers";
                        ScalarModel resultSelectOtp = await _dam.Scalar(db_para);

                        if (resultSelectOtp != null && resultSelectOtp.out_value_str != null && String.IsNullOrEmpty(resultSelectOtp.out_err))
                        {
                            switch (resultSelectOtp.out_value_str)
                            {
                                case "no_user":
                                case "locked":
                                case "no_otp":
                                case "":
                                    resultSelectOtp.out_value_bool = false;
                                    break;

                                default: // OTP Code zurückgeliefert

                                    bool verifyTotp = VerifyTotp(resultSelectOtp.out_value_str, otpParameters.OtpUserDigitInput, Pepper);
                                    resultSelectOtp.out_value_str = verifyTotp ? "1" : "0";

                                    if (verifyTotp)
                                    {
                                        db_para["@Case_"] = "DeleteOtpByAuthUsers_UnixTS>>AuthUsers";
                                        result = await _dam.ExecQuery(db_para);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.out_err = $"[ERROR]={ex.Message}";
            }

            return result;
        }
        private static bool VerifyTotp(string secret, string userinputiode, byte[] pepper)
        {
            bool result = false;

            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
            {
                string decryptedBase32 = aes.DecryptBase32Secret(secret, pepper!); // otp-Schlüssel entschlüsseln

                byte[] secretBytes = Base32Encoding.ToBytes(decryptedBase32);

                var totp = new Totp(secretBytes);
                //userinputiode = totp.ComputeTotp(); //=> Zum Testen die richtige 6-stellige Otp-Eingabe
                result = totp.VerifyTotp(userinputiode, out _, new VerificationWindow(previous: 2, future: 2));
            }

            return result;
        }
        
        /// <summary>
        /// Startet den Authentifizierungsfluss für eine Blazor Web-Anwendung,
        /// indem der Browser des Benutzers direkt zu der Authentifizierungs-URL umgeleitet wird.
        /// </summary>
        /// <param name="authUrl">Die URL zu deinem Backend-Endpoint, der die Authentifizierung initiiert.</param>
        /// <param name="openInNewTab">Optional: true = URL in neuem Tab öffnen (via JS), false = aktuelle Seite umleiten (Standard).</param>
        /// <returns>Ein abgeschlossener Task.</returns>
        public async Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false)
        {
            if (openInNewTab)
            {
                // Neuen Tab via JS öffnen
                await _jsRuntime.InvokeVoidAsync("pE_Web.openExternalUrl", authUrl);
                return null;
            }

            // Standard: aktuelle Seite umleiten (abwärtskompatibel)
            _navigationManager.NavigateTo(authUrl, forceLoad: true);
            return null;
        }

        // Web liefert kein Folder, die Methode ist nur wegen Interface hier
        public async Task<string?> DirectoryPicker()
        {
            await Task.Delay(10);
            return "";
        }

        public async Task<ScalarModel> FilePicker(string filter, string title)
        {
            ScalarModel result = new();
            return result;
        }


        // Share
        public async Task ShareText(string title, string text)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("pE_Web.share.shareText", title, text);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Feedback
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Feedback(BlazorCore.Pages.ContactformModel ContactForm)
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                // Parameter setzen
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
                result = await _dam.Save(db_para)!; // Abfrage ausführen
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result;
        }

        // Open external url
        public async Task OpenExternalUrl(string url)
        {
            await _jsRuntime.InvokeVoidAsync("pE_Web.openExternalUrl", url);
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            await Task.Delay(100);

            return true;
        }



        // Preference- Dummy Implementierung weil es nur in MAUI gebraucht wird wegen Realm Löschung
        // -----------------------------
        public void SetPreference(string key, string value) { }

        public string? GetPreference(string key) => null;

        public void RemovePreference(string key) { }

        public bool ContainsPreference(string key) => false;

        public async Task<BlazorCore.Services.SqlClient.ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();

            try
            {
                // Sicherstellen, dass der Stream am Anfang steht, bevor er an JS übergeben wird
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                // In Blazor Server ist DotNetStreamReference der Standardweg, 
                // um Daten effizient vom Server zum Client-Browser zu streamen.
                using var streamRef = new DotNetStreamReference(stream);

                // Wir rufen die zentrale pE_Web Funktion auf, die in deiner app.js liegt.
                // Diese Funktion erstellt den Blob und triggert den Browser-Download-Dialog.
                await _jsRuntime!.InvokeVoidAsync("pE_Web.downloadFileFromStream", filename, streamRef);

                result.out_value_bool = true;
                result.out_value_str = filename;
                return result;
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.Message;
                // Kontext für das Error-Handling in der UI
                result.out_local = "BlazorServerPlatformService: SaveFileNativeAsync Exception";
                return result;
            }
        }

        // --- Navigation & Native Bridge (Auf Server meist wirkungslos) ---

        public async Task RegisterNativeNavigationAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
        {
            // Auf dem Server gibt es keinen Capacitor-Navigation-Helper.
            await Task.CompletedTask;
        }

        public async Task SetSwipeBackStateAsync(bool enabled)
        {
            // Blazor Server benötigt keine Swipe-Gesten-Steuerung für native Back-Events.
            // Falls du im Browser dennoch JS-Effekte willst, müsstest du prüfen, 
            // ob du dich im Render-Context befindest.
            await Task.CompletedTask;
        }

        public async Task ForceResetSwipeAsync()
        {
            // Keine Aktion auf Server nötig.
            await Task.CompletedTask;
        }

        public async Task ExitAppAsync()
        {
            // Ein "Beenden" der App schließt am Server den Prozess - das wollen wir hier nicht.
            // Optional könnte man den User auf eine "Logout/Goodbye" Seite leiten.
            await Task.CompletedTask;
        }

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
#pragma warning restore CA1416