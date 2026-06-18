using BlazorCore.Services.Dam;
using BlazorCore.Services.Otp;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace TestSolution4.Wpf.Services
{
    public class Platform : BlazorCore.Services.Platform.IPlatformBase, Shared.Services.Platform.IPlatform
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly Shared.Services.GlobalState.IGlobalState? _globalState;

        // Simulierter lokaler Speicher für Preferences/Cookies in WPF
        private readonly Dictionary<string, string> _internalStorage = new();
        private readonly string? _storagePath;
        private readonly string? _prefPath;

        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes($"{Shared.Global.Configuration.ConfigGeneral.ApplicationName}.SecureStorage.v1");

        //public Platform(IServiceProvider serviceProvider, Extensions.Services.GlobalState.IGlobalState globalState)
        //{
        //    _serviceProvider = serviceProvider;
        //    _globalState = globalState;

        //    // Wir nutzen deine Methode direkt hier
        //    string baseDir = GetBaseDirectory();

        //    // Wir erstellen einen Unterordner "Storage", damit das Root-Verzeichnis sauber bleibt
        //    _storagePath = Path.Combine(baseDir, "Storage");
        //    _prefPath = Path.Combine(baseDir, "Storage", "Preferences");

        //    // Sicherstellen, dass der Ordner existiert
        //    if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        //    if (!Directory.Exists(_prefPath)) Directory.CreateDirectory(_prefPath);


        //}
        public Platform(IServiceProvider serviceProvider, Shared.Services.GlobalState.IGlobalState globalState)
        {
            _serviceProvider = serviceProvider;
            _globalState = globalState;

            // FALSCH: string baseDir = GetBaseDirectory();

            // RICHTIG: Nutze LocalApplicationData für MSIX-Kompatibilität
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, _globalState.ConfigGeneral.ApplicationName);

            // Jetzt die Unterordner im beschreibbaren Bereich definieren
            _storagePath = Path.Combine(appFolder, "Storage");
            _prefPath = Path.Combine(appFolder, "Storage", "Preferences");

            // Hier hat die App volle Schreibrechte, auch im Sideload/Store-Szenario
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
            if (!Directory.Exists(_prefPath)) Directory.CreateDirectory(_prefPath);
        }

        public Platform()
        {

        }

        // --- Initialization ---
        public Task<ScalarModel> InitializeJSAsync() => Task.FromResult(new ScalarModel());

        //public bool IsNative => true;

        // --- Storage (Wichtig für das Debugging der Dateipfade) ---
        //public string GetBaseDirectory() => AppDomain.CurrentDomain.BaseDirectory;
        // In deiner WPF Platform.cs (die das Interface implementiert):
        public string GetBaseDirectory()
        {
            
            // Wir behalten den Namen aus dem Interface bei, 
            // liefern aber den MSIX-sicheren Pfad aus LocalAppData.
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _globalState != null ? _globalState.ConfigGeneral.ApplicationName : string.Empty);

            // Sicherstellen, dass der Ordner existiert, damit nachfolgende 
            // File-Operationen nicht ins Leere laufen.
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public PLATFORMS GetCurrPlatform() => PLATFORMS.WINDOWS_CLIENT;

        public p11.UI.NativeDevice GetCurrDevice() => p11.UI.NativeDevice.WINDOWS; 

        public async Task<ScalarModel> SetAsync(string key, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return await RemoveAsync(key);

                // 1. String in Bytes umwandeln
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(value);

                // 2. Verschlüsseln via DPAPI (DataProtectionScope.CurrentUser)
                // Das entspricht dem "SecureStorage" von MAUI/Capacitor
                byte[] encryptedData = ProtectedData.Protect(dataToEncrypt, _entropy, DataProtectionScope.CurrentUser);

                // 3. Als Datei speichern (Base64 codiert)
                string filePath = Path.Combine(_storagePath, $"{key}.bin");
                await File.WriteAllBytesAsync(filePath, encryptedData);

                return new ScalarModel { out_value_bool = true };
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = "DPAPI-Error: " + ex.Message, out_value_bool = false };
            }
        }

        public async Task<ScalarModel> RemoveAsync(string identifier)
        {
            try
            {
                // 1. Pfad zur entsprechenden .bin Datei ermitteln
                string filePath = Path.Combine(_storagePath, $"{identifier}.bin");

                // 2. Prüfen, ob die Datei existiert und löschen
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // 3. Falls du einen internen Cache (_internalStorage) verwendest, diesen auch bereinigen
                // if (_internalStorage.ContainsKey(identifier)) 
                // {
                //     _internalStorage.Remove(identifier);
                // }

                return await Task.FromResult(new ScalarModel { out_value_bool = true });
            }
            catch (Exception ex)
            {
                return new ScalarModel
                {
                    out_value_bool = false,
                    out_err = $"WPF-Remove-Error: {ex.Message}"
                };
            }
        }

        public async Task<ScalarModel> GetAsync(string identifier)
        {
            try
            {
                // 1. Pfad zur verschlüsselten Datei
                string filePath = Path.Combine(_storagePath, $"{identifier}.bin");

                // 2. Falls die Datei nicht existiert -> Fallback auf Cache oder Leer
                if (!File.Exists(filePath))
                {
                    // Optional: Falls du einen Cache nutzt, hier prüfen. 
                    // Sonst einfach "nicht gefunden" zurückgeben.
                    //return new ScalarModel { out_value_str = "", out_value_bool = false, out_err = "Not found" };
                    return new ScalarModel();
                }

                // 3. Verschlüsselte Bytes von der Platte lesen
                byte[] encryptedData = await File.ReadAllBytesAsync(filePath);

                // 4. Mit DPAPI und deiner Entropy entschlüsseln
                // WICHTIG: Die Entropy muss exakt dieselbe sein wie beim Speichern!
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    _entropy,
                    DataProtectionScope.CurrentUser);

                // 5. Bytes zurück in einen String wandeln
                string value = Encoding.UTF8.GetString(decryptedData);

                return new ScalarModel
                {
                    out_value_str = value,
                    out_value_bool = true
                };
            }
            catch (Exception ex)
            {
                // Falls z.B. die Entropy falsch ist oder die Datei korrupt
                return new ScalarModel
                {
                    out_value_str = "",
                    out_value_bool = false,
                    out_err = $"WPF-Decrypt-Error: {ex.Message}"
                };
            }
        }

        // --- OTP (Hier migriert aus deiner WASM-Vorlage) ---
        public async Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters)
        {
            try
            {
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "SaveOtp>>AuthUsers" },
                    { "@UnixTS", otpParameters.UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode },
                    { "@otp", "" },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS }
                };

                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                ScalarModel resultOtp = await dam.AnonymousQuery(db_para)!;

                if (resultOtp != null && resultOtp.out_value_str != null)
                {
                    if (string.IsNullOrEmpty(resultOtp.out_err) && resultOtp.out_value_str.ToLower().StartsWith("updated:"))
                    {
                        return new OtpModel { secret = resultOtp.out_value_str, err = "" };
                    }
                    return new OtpModel { secret = resultOtp.out_value_str, err = resultOtp.out_err ?? "" };
                }
                return new OtpModel { err = "no_otp" };
            }
            catch (Exception ex) { return new OtpModel { err = ex.Message }; }
        }

        public async Task<bool> CheckServerLoginState(OtpParametersModel otpParameters)
        {
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

                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                ScalarModel resultCheckOtp = await dam.AnonymousQuery(db_para)!;
                return resultCheckOtp != null && string.IsNullOrEmpty(resultCheckOtp.out_err) && resultCheckOtp.out_value_bool;
            }
            catch { return false; }
        }

        public async Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false)
        {
            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "DeleteOtp>>AuthUsers" },
                    { "@EmailHash", otpParameters.Account },
                    { "@PasswordHash", otpParameters.Password },
                    { "@AuthUsers_UnixTS", otpParameters.AuthUsers_UnixTS },
                    { "@OtpBackupCode", otpParameters.OtpBackupCode },
                    { "tmp_userinputiode", otpParameters.OtpUserDigitInput },
                    { DB_CMD.NO_LOCAL, string.Empty }
                };
                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                return await dam.AnonymousQuery(db_para)!;
            }
            catch (Exception ex) { return new ScalarModel { out_err = ex.Message }; }
        }

        // --- Utilities & Preferences ---
        public void SetPreference(string key, string value)
        {
            // 1. In den RAM-Cache schreiben
            _internalStorage[key] = value;

            // 2. Nativ in Datei schreiben (Klartext, da keine sensiblen Daten)
            try
            {
                string filePath = Path.Combine(_prefPath, $"{key}.pref");
                File.WriteAllText(filePath, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Preference {key}: {ex.Message}");
            }
        }

        public string? GetPreference(string key)
        {
            // 1. Zuerst im Cache schauen (schnellster Zugriff)
            if (_internalStorage.TryGetValue(key, out var val))
            {
                return val;
            }

            // 2. Falls nicht im Cache, auf der Platte suchen (z.B. nach Neustart)
            string filePath = Path.Combine(_prefPath, $"{key}.pref");
            if (File.Exists(filePath))
            {
                try
                {
                    string value = File.ReadAllText(filePath);
                    _internalStorage[key] = value; // Für nächsten Zugriff cachen
                    return value;
                }
                catch { return null; }
            }

            return null;
        }

        public void RemovePreference(string key)
        {
            // 1. Aus Cache entfernen
            _internalStorage.Remove(key);

            // 2. Datei löschen
            string filePath = Path.Combine(_prefPath, $"{key}.pref");
            if (File.Exists(filePath))
            {
                try { File.Delete(filePath); } catch { }
            }
        }

        public bool ContainsPreference(string key)
        {
            // Entweder im Cache oder auf der Platte vorhanden
            if (_internalStorage.ContainsKey(key)) return true;

            return File.Exists(Path.Combine(_prefPath, $"{key}.pref"));
        }


        public async Task<bool> IsTokenValidAsync(string token)
        {
            if (_serviceProvider == null) return false;

            try
            {
                var _httpClient = _serviceProvider.GetRequiredService<HttpClient>();

                if (_httpClient != null && _globalState != null)
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

        // --- UI & System ---
        public async Task CopyTextToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Wir nutzen den Application Dispatcher, um sicherzustellen, 
            // dass wir im UI-Thread (STA) operieren.
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // SetText kann scheitern, wenn ein anderes Programm 
                    // das Clipboard gerade blockiert.
                    System.Windows.Clipboard.SetText(text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Clipboard Error: {ex.Message}");
                    // Optional: Ein kleiner Retry-Mechanismus, falls das Clipboard gelockt war
                }
            });
        }

        public async Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false)
        {
            try
            {
                if (string.IsNullOrEmpty(authUrl)) return "URL is empty";

                // Wir starten den Prozess. Dank UseShellExecute weiß Windows, 
                // dass es die URL mit dem Standard-Browser (Chrome, Edge, etc.) öffnen soll.
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                // Wir geben null zurück, da das eigentliche "Ergebnis" (Token) 
                // über deine Polling-Bridge Architektur in die Datenbank/SecureStorage geschrieben wird.
                return await Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                // Falls z.B. kein Browser installiert ist oder die Shell blockiert
                System.Diagnostics.Debug.WriteLine($"Browser Error: {ex.Message}");
                return ex.Message;
            }
        }

        public async Task OpenExternalUrl(string url) => await AuthenticateAsync(url);

        public async Task<string> GetFormFactor() => "Desktop";

        public async Task<string> GetIdiomPlatform() => "WPF-Client";

        public string GetDeviceInfo() => "WPF-Host";

        public async Task<double> GetWindowWidth()
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = System.Windows.Application.Current.MainWindow;
                // ActualWidth ist der reale Wert in Pixeln, Width könnte NaN sein
                return window?.ActualWidth ?? 800;
            });
        }

        public async Task<double> GetWindowHeight()
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = System.Windows.Application.Current.MainWindow;
                return window?.ActualHeight ?? 600;
            });
        }

        public void StartConnectivityMonitoring(BlazorCore.Services.AppState.IAppStateBase appState) { }

        public Task<bool> InternetConnectedAsync() => Task.FromResult(true);

        //public async Task<string?> DirectoryPicker()
        //{
        //    return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        // Wir nutzen den OpenFileDialog im "Ordner-Modus" (verfügbar ab .NET Core 3.0+ / .NET 5+)
        //        var dialog = new Microsoft.Win32.OpenFolderDialog
        //        {
        //            Title = "Bitte wählen Sie ein Verzeichnis aus",
        //            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
        //        };

        //        bool? result = dialog.ShowDialog();

        //        if (result == true)
        //        {
        //            return dialog.FolderName;
        //        }

        //        return null; // Benutzer hat abgebrochen
        //    });
        //}
        public async Task<string?> DirectoryPicker()
        {
            return await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Please select a directory",
                    // Besser: Starte im Benutzer-Verzeichnis (Dokumente)
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                bool? result = dialog.ShowDialog();
                return result == true ? dialog.FolderName : null;
            });
        }

        /// <summary>
        /// Überschreibt den PickPhotoAsync speziell für WPF, 
        /// um den nativen Windows OpenFileDialog zu nutzen.
        /// </summary>
        public async Task<ScalarModel> FilePicker(string filter, string title)
        {
            ScalarModel result = new();

            bool? dialogResult = false;
            string selectedFilePath = string.Empty;

            try
            {
                // WPF-Dialoge müssen im UI-Thread (STA) ausgeführt werden.
                // Blazor-Calls kommen oft aus dem Worker-Pool.
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
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
                    byte[] bytes = await System.IO.File.ReadAllBytesAsync(selectedFilePath);

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

        public async Task ShareText(string title, string text)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // Option A: In die Zwischenablage (da es am Desktop oft der nächste Schritt ist)
                    System.Windows.Clipboard.SetText($"{title}\n\n{text}");

                    // Option B: MailTo-Link öffnen (Das kommt dem "Share" am nächsten)
                    // Windows öffnet dann Outlook/Mail-App mit Betreff und Inhalt
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

        public async Task<BlazorCore.Services.SqlClient.ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "")
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();

            try
            {
                // Sicherstellen, dass wir einen Pfad haben (vom DirectoryPicker geliefert)
                if (string.IsNullOrEmpty(path))
                {
                    result.out_value_bool = false;
                    result.out_err = "Kein Zielverzeichnis ausgewählt.";
                    return result;
                }

                // Der vollständige Pfad wird hier final zusammengesetzt, 
                // falls filename nicht bereits den vollen Pfad enthält.
                string fullPath = Path.IsPathRooted(filename) ? filename : Path.Combine(path, filename);

                // Sicherstellen, dass der Quell-Stream am Anfang steht
                if (stream.CanSeek) stream.Position = 0;

                // Datei physisch auf die Festplatte schreiben
                using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await stream.CopyToAsync(fileStream);
                }

                result.out_value_bool = true;
                result.out_value_str = fullPath;
                return result;
            }
            catch (Exception ex)
            {
                result.out_value_bool = false;
                result.out_err = ex.Message;
                result.out_local = "WPF SaveFileNativeAsync Exception";
                return result;
            }
        }


        // --- Navigation & Native Bridge ---

        public async Task RegisterNativeNavigationAsync<T>(Microsoft.JSInterop.DotNetObjectReference<T> dotNetRef) where T : class
        {
            // WPF hat keine native Back-Button-Bridge (wie Capacitor).
            // Wir geben einfach Task.CompletedTask zurück.
            await Task.CompletedTask;
        }

        public async Task SetSwipeBackStateAsync(bool enabled)
        {
            // Swipe-Gesten sind in der WPF-Desktop-Umgebung in der Regel nicht aktiv/nötig.
            await Task.CompletedTask;
        }

        public async Task ForceResetSwipeAsync()
        {
            // Kein Reset nötig, da keine JS-Swipe-Geste aktiv ist.
            await Task.CompletedTask;
        }

        public async Task ExitAppAsync()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // Beendet die gesamte WPF-Anwendung sauber
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WPF Shutdown Error: {ex.Message}");
                }
            });
        }

        //public async Task CleanupNativeUIAsync()
        //{
        //    // Da wir in WPF das WebView2 nutzen, können wir JS-Cleanup aufrufen,
        //    // falls wir sichergehen wollen, dass keine Modale offen bleiben.
        //    // Wir müssen hier jedoch prüfen, ob die IJSRuntime verfügbar ist.
        //    try
        //    {
        //        var js = _serviceProvider.GetService<IJSRuntime>();
        //        if (js != null)
        //        {
        //            await js.InvokeVoidAsync("window.TpcModalJs.clearAllModals");
        //        }
        //    }
        //    catch { /* In WPF ignorieren wir Fehler beim JS-Cleanup beim Schließen */ }
        //}

        public async Task NavigateBackAsync()
        {
            try
            {
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Platform] NavigateBackAsync failed: {ex.Message}");
            }
        }

        // Log

        // WpfPlatform.cs
        public void Log(string message, bool isError = false)
        {
            try
            {
                // Wir nutzen den Namen der App für den Ordner in AppData
                string logFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    _globalState != null ? _globalState.ConfigGeneral.ApplicationName : string.Empty,
                    "Logs");

                if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);

                string filePath = Path.Combine(logFolder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
                string prefix = isError ? "[ERROR]" : "[INFO]";
                string line = $"{prefix} [{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";

                // FileShare.ReadWrite erlaubt uns, das Log zu lesen, während die App schreibt
                File.AppendAllText(filePath, line);
            }
            catch
            {
                // Wenn das Loggen fehlschlägt (z.B. keine Berechtigung), 
                // darf die App trotzdem nicht abstürzen.
            }
        }



    }
}
