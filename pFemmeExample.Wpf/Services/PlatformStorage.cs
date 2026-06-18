using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pFemmeExample.Wpf.Services
{
    /// <summary>
    /// Plattform-spezifische Implementierung von IPlatformStorage für WPF.
    /// - Secure Storage: Nutzt DPAPI (ProtectedData) mit Dateispeicherung
    /// - Preferences: Nutzt Dateisystem (Klartext) mit RAM-Cache
    /// </summary>
    public class PlatformStorage : IPlatformStorageBase
    {
        private readonly IGlobalStateBase _globalState;

        // RAM-Cache für schnellen Zugriff
        private readonly Dictionary<string, string> _internalStorage = new();

        // Pfade für Dateispeicherung
        private readonly string _storagePath;   // Für verschlüsselte Secure Storage Dateien
        private readonly string _prefPath;      // Für Klartext Preferences Dateien

        // Entropy für DPAPI (muss beim Entschlüsseln identisch sein)
        private readonly byte[] _entropy;

        public PlatformStorage(IGlobalStateBase globalState)
        {
            _globalState = globalState;

            // Entropy basierend auf App-Namen (konsistent für alle Benutzer)
            _entropy = Encoding.UTF8.GetBytes($"{_globalState.ConfigGeneral.ApplicationName}.SecureStorage.v1");

            // Basisverzeichnis im LocalApplicationData (MSIX-kompatibel)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(localAppData, _globalState.ConfigGeneral.ApplicationName);

            _storagePath = Path.Combine(appFolder, "Storage");
            _prefPath = Path.Combine(appFolder, "Storage", "Preferences");

            // Verzeichnisse erstellen falls nicht vorhanden
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
            if (!Directory.Exists(_prefPath)) Directory.CreateDirectory(_prefPath);
        }

        // ========================================================================
        // SECURE STORAGE (asynchron, verschlüsselt mit DPAPI)
        // ========================================================================

        /// <summary>
        /// Speichert einen Wert verschlüsselt im Dateisystem.
        /// Nutzt DPAPI (DataProtectionScope.CurrentUser).
        /// </summary>
        public async Task<ScalarModel> SetAsync(string identifier, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return await RemoveAsync(identifier);

                // 1. String in Bytes umwandeln
                byte[] dataToEncrypt = Encoding.UTF8.GetBytes(value);

                // 2. Verschlüsseln via DPAPI
                byte[] encryptedData = ProtectedData.Protect(dataToEncrypt, _entropy, DataProtectionScope.CurrentUser);

                // 3. Als Datei speichern
                string filePath = Path.Combine(_storagePath, $"{identifier}.bin");
                await File.WriteAllBytesAsync(filePath, encryptedData);

                return new ScalarModel { out_value_bool = true };
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = $"DPAPI-Encrypt-Error: {ex.Message}", out_value_bool = false };
            }
        }

        /// <summary>
        /// Liest einen verschlüsselten Wert aus dem Dateisystem.
        /// </summary>
        public async Task<ScalarModel> GetAsync(string identifier)
        {
            try
            {
                string filePath = Path.Combine(_storagePath, $"{identifier}.bin");

                if (!File.Exists(filePath))
                {
                    return new ScalarModel { out_value_str = "", out_value_bool = false };
                }

                // Verschlüsselte Bytes lesen
                byte[] encryptedData = await File.ReadAllBytesAsync(filePath);

                // Entschlüsseln
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);

                // Bytes zurück in String wandeln
                string value = Encoding.UTF8.GetString(decryptedData);

                return new ScalarModel
                {
                    out_value_str = value,
                    out_value_bool = true
                };
            }
            catch (Exception ex)
            {
                return new ScalarModel
                {
                    out_value_str = "",
                    out_value_bool = false,
                    out_err = $"DPAPI-Decrypt-Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Entfernt einen verschlüsselten Wert aus dem Dateisystem.
        /// </summary>
        public async Task<ScalarModel> RemoveAsync(string identifier)
        {
            try
            {
                string filePath = Path.Combine(_storagePath, $"{identifier}.bin");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Auch aus dem RAM-Cache entfernen (falls vorhanden)
                if (_internalStorage.ContainsKey(identifier))
                {
                    _internalStorage.Remove(identifier);
                }

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

        // ========================================================================
        // PREFERENCES (synchron, Klartext, mit RAM-Cache)
        // ========================================================================

        /// <summary>
        /// Setzt einen Preference-Wert (Klartext, synchron).
        /// Schreibt zuerst in RAM-Cache, dann in Datei.
        /// </summary>
        public void SetPreference(string key, string value)
        {
            // 1. In den RAM-Cache schreiben
            _internalStorage[key] = value;

            // 2. Nativ in Datei schreiben (Klartext)
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

        /// <summary>
        /// Liest einen Preference-Wert.
        /// Zuerst RAM-Cache, dann Dateisystem.
        /// </summary>
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

        /// <summary>
        /// Entfernt einen Preference-Wert.
        /// Löscht aus RAM-Cache und Dateisystem.
        /// </summary>
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

        /// <summary>
        /// Prüft, ob ein Preference-Key existiert.
        /// Prüft RAM-Cache und Dateisystem.
        /// </summary>
        public bool ContainsPreference(string key)
        {
            // Entweder im Cache oder auf der Platte vorhanden
            if (_internalStorage.ContainsKey(key)) return true;

            string filePath = Path.Combine(_prefPath, $"{key}.pref");
            return File.Exists(filePath);
        }
    }
}
