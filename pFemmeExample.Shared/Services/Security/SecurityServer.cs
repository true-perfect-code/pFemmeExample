using Konscious.Security.Cryptography;
using pFemmeExample.Shared.Global;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Design;

namespace pFemmeExample.Shared.Services.Security
{
    public class SecurityServer : BlazorCore.Services.ServerShared.ISecurityServer, IDisposable
    {
        private bool _disposed;
        private byte[]? _key;


        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public SecurityServer()
        {
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public string GenerateEncryptedPepper()
        {
            byte[] key = GenerateKey(); // Deterministischer Schlüssel
            byte[] pepper = GenerateRandomPepper(32); // Zufälliger Pepper
            byte[] nonce = GenerateRandomNonce(12); // 96-Bit-Nonce
            byte[] tag = new byte[16]; // Authentifizierungs-Tag, 16 Bytes

            // Validierung der Tag-Größe
            int tagSizeInBytes = tag.Length;
            if (tagSizeInBytes != 12 && tagSizeInBytes != 13 && tagSizeInBytes != 14 &&
                tagSizeInBytes != 15 && tagSizeInBytes != 16)
            {
                throw new ArgumentException("Invalid tag size. Must be 12, 13, 14, 15, or 16 bytes.", nameof(tag));
            }

            using (var aesGcm = new AesGcm(key, tagSizeInBytes))
            {
                byte[] ciphertext = new byte[pepper.Length];
                aesGcm.Encrypt(nonce, pepper, ciphertext, tag);
                return Convert.ToBase64String(nonce.Concat(ciphertext).Concat(tag).ToArray());
            }
        }
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public async Task<string> GenerateEncryptedPepperAsync()
        {
            await Task.Delay(2);
            return GenerateEncryptedPepper();
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public byte[] DecryptPepper(string encryptedPepper = "")
        {
            if (string.IsNullOrEmpty(encryptedPepper))
            {
                // ACHTUNG: Hier Pepper generieren und unter 'pFemmeExample.Shared.Global' bei 'PepperApp' und 'PepperAppWasm' eintragen!
                if (string.IsNullOrEmpty(Configuration.ConfigGeneral.PepperApp))
                {
                    encryptedPepper = GenerateEncryptedPepper();
                    //await _appState.Log($"[Security] DecryptPepperAsync: Attention: Pepper has been newly generated; set this value in [Configuration.ConfigurationGeneral.PepperAppWasm], otherwise the local encryption will produce different values!: {encryptedPepper}");
                }
                else
                    encryptedPepper = Configuration.ConfigGeneral.PepperApp;
            }

            if (string.IsNullOrEmpty(encryptedPepper))
                throw new ArgumentException("Encrypted pepper cannot be null or empty.");

            try
            {
                _key ??= GenerateKey();
                if (!IsBase64String(encryptedPepper))
                    throw new ArgumentException("Encrypted pepper is not a valid Base64 string.");

                var encryptedBytes = Convert.FromBase64String(encryptedPepper);
                if (encryptedBytes.Length < 12 + 16 + 32) // Nonce (12) + Tag (16) + Ciphertext (32)
                    throw new ArgumentException($"Invalid encrypted pepper length. Expected at least {12 + 16 + 32} bytes, got {encryptedBytes.Length}.");

                var nonce = new byte[12];
                var cipherBytes = new byte[encryptedBytes.Length - 12 - 16];
                var tag = new byte[16];

                Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, 12);
                Buffer.BlockCopy(encryptedBytes, 12, cipherBytes, 0, cipherBytes.Length);
                Buffer.BlockCopy(encryptedBytes, 12 + cipherBytes.Length, tag, 0, 16);

                int tagSizeInBytes = tag.Length;
                if (tagSizeInBytes != 12 && tagSizeInBytes != 13 && tagSizeInBytes != 14 &&
                    tagSizeInBytes != 15 && tagSizeInBytes != 16)
                {
                    throw new ArgumentException("Invalid tag size. Must be 12, 13, 14, 15, or 16 bytes.", nameof(tag));
                }

                var plainBytes = new byte[cipherBytes.Length];

                try
                {
                    using (var aesGcm = new AesGcm(_key, tagSizeInBytes))
                    {
                        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes, associatedData: null);
                    }
                }
                catch (AuthenticationTagMismatchException ex)
                {
                    throw new CryptographicException("Authentication tag mismatch. The pepper data may be corrupted or the key is incorrect.", ex);
                }

                if (plainBytes.Length != 32)
                    throw new CryptographicException($"Decrypted pepper has invalid length. Expected 32 bytes, got {plainBytes.Length}.");

                return plainBytes;
            }
            catch (FormatException)
            {
                throw new ArgumentException("Encrypted pepper is not a valid Base64 string.");
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Pepper decryption failed. The data may be corrupted or the key is incorrect.", ex);
            }
        }
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public async Task<byte[]> DecryptPepperAsync(string encryptedPepper = "")
        {
            await Task.Delay(5);
            return DecryptPepper(encryptedPepper);
        }

        private bool IsBase64String(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            try
            {
                Convert.FromBase64String(input);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private byte[] GenerateRandomPepper(int length)
        {
            byte[] pepper = new byte[length];
            RandomNumberGenerator.Fill(pepper);
            return pepper;
        }

        private byte[] GenerateRandomNonce(int length)
        {
            byte[] nonce = new byte[length];
            RandomNumberGenerator.Fill(nonce);
            return nonce;
        }

        /// <summary>
        /// Hashes a username using Argon2id with a deterministic salt derived from the username,
        /// Appl.AppName, Appl.AppNameShort, and Appl.TableSchema, and Appl.Pepper as KnownSecret.
        /// Returns a PHC-formatted string containing parameters, salt, and hash.
        /// </summary>
        /// <param name="username">The username to hash.</param>
        /// <returns>PHC-formatted Argon2id hash string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if Appl.Pepper is not initialized.</exception>
        /// <exception cref="ArgumentException">Thrown if username is null or empty.</exception>
        public string HashUsername(string username, byte[] pepper)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (pepper == null)
                throw new InvalidOperationException("Pepper not initialized.");

            try
            {
                // Konvertiere Benutzername in Bytes
                byte[] usernameBytes = Encoding.UTF8.GetBytes(username);

                // Generiere Salt aus Benutzername und Appl-Parametern
                byte[] usernameTransformed = CheckInput(username);
                byte[] appNameTransformed = CheckInput(Configuration.ConfigGeneral.ApplicationName);
                byte[] schemaTransformed = SolveCipher(Configuration.ConfigGeneral.TableSchema);
                byte[] shortNameTransformed = ClearData(new string(Configuration.ConfigGeneral.ApplicationName.Reverse().ToArray()));

                byte[] combinedInput = new byte[usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length + shortNameTransformed.Length];
                Buffer.BlockCopy(usernameTransformed, 0, combinedInput, 0, usernameTransformed.Length);
                Buffer.BlockCopy(appNameTransformed, 0, combinedInput, usernameTransformed.Length, appNameTransformed.Length);
                Buffer.BlockCopy(schemaTransformed, 0, combinedInput, usernameTransformed.Length + appNameTransformed.Length, schemaTransformed.Length);
                Buffer.BlockCopy(shortNameTransformed, 0, combinedInput, usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length, shortNameTransformed.Length);

                byte[] salt = SHA256.HashData(combinedInput).Take(16).ToArray(); // 16-Byte-Salt

                // Konfiguriere Argon2id
                using (var argon2 = new Argon2id(usernameBytes))
                {
                    argon2.Salt = salt;
                    argon2.KnownSecret = pepper; // Pepper als KnownSecret
                    argon2.DegreeOfParallelism = 2; // 2 Threads
                    argon2.MemorySize = 65536; // 64 MB
                    argon2.Iterations = 4; // 4 Iterationen

                    // Generiere Hash (32 Bytes)
                    byte[] hashBytes = argon2.GetBytes(32);

                    // Erstelle PHC-String: $argon2id$v=19$m=65536,t=4,p=2$salt$hash
                    string phcString = $"$argon2id$v=19$m={argon2.MemorySize},t={argon2.Iterations},p={argon2.DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hashBytes)}";
                    return phcString;
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to hash username.", ex);
            }
        }
        public async Task<string> HashUsernameAsync(string username, byte[] pepper)
        {
            await Task.Delay(5);
            return HashUsername(username, pepper);
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public string GenerateClientHash(string text, string accountpassword)
        {
            string hash = string.Empty;
            try
            {
                byte[] pepper = null; // DecryptPepper(accountpassword);

                hash = HashUsername(text, pepper!);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to hash text", ex);
            }
            return hash;
        }

        /// <summary>
        /// Hashes a password using Argon2id with a deterministic salt derived from the username,
        /// Appl.AppName, Appl.AppNameShort, and Appl.TableSchema, and Appl.Pepper as KnownSecret.
        /// Returns a PHC-formatted string containing parameters, salt, and hash.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="username">The username to derive the salt from.</param>
        /// <returns>PHC-formatted Argon2id hash string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if Appl.Pepper is not initialized.</exception>
        /// <exception cref="ArgumentException">Thrown if password or username is null or empty.</exception>
        public string HashCredentials(string password, string username, byte[] pepper)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (pepper == null)
                throw new InvalidOperationException("Pepper not initialized.");

            try
            {
                // Konvertiere Passwort in Bytes
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                // Generiere Salt aus Benutzername und Appl-Parametern
                byte[] usernameTransformed = CheckInput(username);
                byte[] appNameTransformed = CheckInput(Configuration.ConfigGeneral.ApplicationName);
                byte[] schemaTransformed = SolveCipher(Configuration.ConfigGeneral.TableSchema);
                byte[] shortNameTransformed = ClearData(new string(Configuration.ConfigGeneral.ApplicationName.Reverse().ToArray()));

                byte[] combinedInput = new byte[usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length + shortNameTransformed.Length];
                Buffer.BlockCopy(usernameTransformed, 0, combinedInput, 0, usernameTransformed.Length);
                Buffer.BlockCopy(appNameTransformed, 0, combinedInput, usernameTransformed.Length, appNameTransformed.Length);
                Buffer.BlockCopy(schemaTransformed, 0, combinedInput, usernameTransformed.Length + appNameTransformed.Length, schemaTransformed.Length);
                Buffer.BlockCopy(shortNameTransformed, 0, combinedInput, usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length, shortNameTransformed.Length);

                byte[] salt = SHA256.HashData(combinedInput).Take(16).ToArray(); // 16-Byte-Salt

                // Konfiguriere Argon2id
                using (var argon2 = new Argon2id(passwordBytes))
                {
                    argon2.Salt = salt;
                    argon2.KnownSecret = pepper; // Pepper als KnownSecret
                    argon2.DegreeOfParallelism = 2; // 2 Threads
                    argon2.MemorySize = 65536; // 64 MB
                    argon2.Iterations = 4; // 4 Iterationen

                    // Generiere Hash (32 Bytes)
                    byte[] hashBytes = argon2.GetBytes(32);

                    // Erstelle PHC-String: $argon2id$v=19$m=65536,t=4,p=2$salt$hash
                    string phcString = $"$argon2id$v=19$m={argon2.MemorySize},t={argon2.Iterations},p={argon2.DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hashBytes)}";
                    return phcString;
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to hash credentials.", ex);
            }
        }
        public async Task<string> HashCredentialsAsync(string password, string username, byte[] pepper)
        {
            await Task.Delay(5);
            return HashCredentials(password, username, pepper);
        }

        /// <summary>
        /// Verifies if a password matches a stored Argon2id hash for a given username.
        /// The storedHash is retrieved from the database (AuthUsers.PasswordHash).
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="username">The username used to derive the salt.</param>
        /// <param name="storedHash">The stored PHC-formatted password hash from the database.</param>
        /// <returns>True if the password matches the stored hash, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if password, username, or storedHash is null or empty.</exception>
        public bool VerifyCredentials(string password, string username, string storedHash, byte[] pepper)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrEmpty(storedHash))
                throw new ArgumentException("Stored hash cannot be null or empty.", nameof(storedHash));

            try
            {
                // Generiere neuen Hash mit denselben Eingaben
                string computedHash = HashCredentials(password, username, pepper);

                // Sicherer Vergleich (konstante Zeit)
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHash),
                    Encoding.UTF8.GetBytes(storedHash));
            }
            catch
            {
                return false; // Bei Fehlern (z. B. ungültiger Hash) false zurückgeben
            }
        }

        public string EncryptBase32Secret(string base32Secret, byte[] pepper)
        {
            if (string.IsNullOrEmpty(base32Secret))
                throw new ArgumentException("OTP secret must not be null or empty.", nameof(base32Secret));
            if (pepper == null || pepper.Length == 0)
                throw new ArgumentException("Pepper must be provided.", nameof(pepper));

            byte[] key = GenerateKey();
            byte[] nonce = GenerateRandomNonce(12);
            byte[] plaintext = Encoding.UTF8.GetBytes(base32Secret);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            int tagSizeInBytes = tag.Length;
            using var aesGcm = new AesGcm(key, tagSizeInBytes);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData: pepper);

            byte[] result = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

            return Convert.ToBase64String(result);
        }
        public async Task<string> EncryptBase32SecretAsync(string base32Secret, byte[] pepper)
        {
            await Task.Delay(5);
            return EncryptBase32Secret(base32Secret, pepper);
        }

        public string DecryptBase32Secret(string encryptedBase64, byte[] pepper)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
                throw new ArgumentException("Encrypted OTP secret must not be null or empty.", nameof(encryptedBase64));
            if (pepper == null || pepper.Length == 0)
                throw new ArgumentException("Pepper must be provided.", nameof(pepper));

            byte[] key = GenerateKey();
            byte[] data = Convert.FromBase64String(encryptedBase64);

            if (data.Length < 12 + 16)
                throw new ArgumentException("Invalid encrypted data.");

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[data.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(data, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(data, nonce.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(data, nonce.Length + ciphertext.Length, tag, 0, tag.Length);

            byte[] plaintext = new byte[ciphertext.Length];

            int tagSizeInBytes = tag.Length;
            using var aesGcm = new AesGcm(key, tagSizeInBytes);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData: pepper);

            return Encoding.UTF8.GetString(plaintext);
        }
        public async Task<string> DecryptBase32SecretAsync(string encryptedBase64, byte[] pepper)
        {
            await Task.Delay(5);
            return DecryptBase32Secret(encryptedBase64, pepper);
        }

        /// <summary>
        /// Methode, die Pepper liefert
        /// </summary>
        /// <param name="_sql">MSSQL Objekt für die Verbindung mit der DB</param>
        /// <param name="_connectionFilePath">Dateipfad der ConnectionString Json Datei</param>
        /// <returns>Rückgabewert ist Error String</returns>
        //public static string SetConnectionString(ref Sql _sql, string _connectionFilePath)
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public byte[]? GetPepper(string pepperFilePath)
        {
            byte[]? result = null;

            bool debugLogServer = false;
            string debugLogPathServer = @"C:\inetpub\vhosts\true-perfect-code.ch\logs\debug.log";
            try
            {
                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, $"🔍 GetPepper('{pepperFilePath}') called\n");

                // Lese security.config.json
                string jsonContent = System.IO.File.ReadAllText(pepperFilePath);
                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, $"✅ File read successfully, length: {jsonContent.Length} chars\n");

                var jsonData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent)
                    ?? throw new InvalidOperationException("Failed to read security.config.json.");

                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, $"✅ JSON deserialized successfully, keys: {string.Join(", ", jsonData.Keys)}\n");

                // Extrahiere Pepper-Wert
                if (!jsonData.TryGetValue("Pepper", out string? encryptedPepper) || string.IsNullOrEmpty(encryptedPepper))
                {
                    if (debugLogServer)
                        File.AppendAllText(debugLogPathServer, "❌ Pepper key not found in JSON\n");
                    throw new InvalidOperationException("Pepper not found in security.config.json.");
                }

                if (debugLogServer)
                {
                    File.AppendAllText(debugLogPathServer, $"✅ Pepper found in JSON, length: {encryptedPepper.Length} chars\n");
                    File.AppendAllText(debugLogPathServer, $"🔐 Encrypted pepper preview: {encryptedPepper.Substring(0, Math.Min(50, encryptedPepper.Length))}...\n");
                }

                // Entschlüssle Pepper und setze statische Variable
                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, "🔑 Starting decryption with Security()...\n");

                //using (var security = new Security())
                //{
                //    result = DecryptPepper(encryptedPepper);
                //}
                result = DecryptPepper(encryptedPepper);

                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, "✅ Pepper decrypted successfully!\n");
            }
            catch (Exception ex)
            {
                if (debugLogServer)
                {
                    File.AppendAllText(debugLogPathServer, $"💥 Exception in inner try: {ex.GetType().Name}: {ex.Message}\n");
                    if (ex.InnerException != null)
                        File.AppendAllText(debugLogPathServer, $"💥 Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n");
                }
                throw new InvalidOperationException("Failed to initialize pepper from security.config.json.", ex);
            }

            return result;
        }

        // GeneratePassphrase, angepasst für EmailHash
        public string GeneratePassphrase(SecureString emailHashSecure)
        {
            if (emailHashSecure == null || emailHashSecure.Length == 0)
            {
                throw new ArgumentNullException(nameof(emailHashSecure), "Email hash cannot be null or empty.");
            }

            byte[]? hashBytes = null;
            try
            {
                // Hash in Bytes konvertieren
                hashBytes = ConvertSecureStringToBytes(emailHashSecure);

                // Salt aus EmailHash + Appl.* (benutzerspezifisch)
                string hashFallback = Configuration.ConfigGeneral.ApplicationName;
                byte[] saltInput = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(hashBytes) + Configuration.ConfigGeneral.ApplicationName + new string(Configuration.ConfigGeneral.ApplicationName.Reverse().ToArray()) + Configuration.ConfigGeneral.TableSchema);
                byte[] salt = SHA256.HashData(saltInput).Take(16).ToArray();

                // Appl.* als global verfügbare Komponente
                byte[] appContext = Encoding.UTF8.GetBytes(Configuration.ConfigGeneral.ApplicationName + new string(Configuration.ConfigGeneral.ApplicationName.Reverse().ToArray()));

                // Argon2id konfigurieren
                var argon2 = new Argon2id(hashBytes)
                {
                    Salt = salt,
                    AssociatedData = appContext,
                    MemorySize = 65536, // 64 MB
                    Iterations = 4,
                    DegreeOfParallelism = 2
                };

                // 32-Byte-Hash erzeugen
                byte[] hash = argon2.GetBytes(32);

                // Base64 für MS SQL Server
                return Convert.ToBase64String(hash);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to generate passphrase.", ex);
            }
            finally
            {
                // Temporäre Daten löschen
                if (hashBytes != null)
                {
                    Array.Clear(hashBytes, 0, hashBytes.Length);
                }
            }
        }

        // Bestehende Methode aus 22. April 2025
        public byte[] ConvertSecureStringToBytes(SecureString secureString)
        {
            if (secureString == null || secureString.Length == 0)
            {
                throw new ArgumentNullException(nameof(secureString), "SecureString cannot be null or empty.");
            }

            IntPtr unmanagedString = IntPtr.Zero;
            char[]? charArray = null;
            byte[]? result = null;
            try
            {
                unmanagedString = Marshal.SecureStringToCoTaskMemUnicode(secureString);
                charArray = new char[secureString.Length];
                Marshal.Copy(unmanagedString, charArray, 0, secureString.Length);
                result = Encoding.UTF8.GetBytes(charArray);
                return result;
            }
            finally
            {
                if (charArray != null)
                {
                    Array.Clear(charArray, 0, charArray.Length);
                }
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(unmanagedString);
                }
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Keine verwalteten Ressourcen
                }
                if (_key != null)
                    Array.Clear(_key, 0, _key.Length);
                //Array.Clear(_secureKey, 0, _secureKey.Length);
                _disposed = true;
            }
        }

        ~SecurityServer()
        {
            Dispose(false);
        }

        // --- Transformationsmethoden für den echten Schlüssel und Salt ---
        // Irreführende Namen, um die wahre Funktion zu verschleiern

        // Transformiert Input (HEX-Umwandlung und Linksrotation)
        private byte[] CheckInput(string input)
        {
            string hex = ToHexString(Encoding.UTF8.GetBytes(input));
            return RotateLeft(Encoding.UTF8.GetBytes(hex), 7);
        }

        // Transformiert Input (HEX-Umwandlung und Rechtsverschiebung)
        private byte[] SolveCipher(string input)
        {
            string hex = ToHexString(Encoding.UTF8.GetBytes(input));
            return ShiftRight(Encoding.UTF8.GetBytes(hex), 3);
        }

        // Transformiert Input (HEX-Umwandlung und XOR)
        private byte[] ClearData(string input)
        {
            string hex = ToHexString(Encoding.UTF8.GetBytes(input));
            return XorWithConstant(Encoding.UTF8.GetBytes(hex), 0x5A);
        }

        // --- Generierung des echten Schlüssels ---
        // Berechnet den echten Schlüssel aus transformierten Werten
        public byte[] GenerateKey(int iterations = 200_000)
        {
            // Transformiere die Eingaben mit irreführenden Methoden
            byte[] appNameTransformed = CheckInput(Configuration.ConfigGeneral.ApplicationName);
            byte[] schemaTransformed = SolveCipher(Configuration.ConfigGeneral.TableSchema);
            byte[] urlTransformed = ClearData(new string(Configuration.ConfigGeneral.ApplicationName.Reverse().ToArray()));

            // Kombiniere transformierte Werte
            byte[] combinedInput = new byte[appNameTransformed.Length + schemaTransformed.Length + urlTransformed.Length];
            Buffer.BlockCopy(appNameTransformed, 0, combinedInput, 0, appNameTransformed.Length);
            Buffer.BlockCopy(schemaTransformed, 0, combinedInput, appNameTransformed.Length, schemaTransformed.Length);
            Buffer.BlockCopy(urlTransformed, 0, combinedInput, appNameTransformed.Length + schemaTransformed.Length, urlTransformed.Length);

            // Hash und PBKDF2 für den echten Schlüssel
            byte[] hashedInput = SHA512.HashData(combinedInput);
            byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(Configuration.ConfigGeneral.ApplicationName + Configuration.ConfigGeneral.TableSchema));
            using var deriveBytes = new Rfc2898DeriveBytes(hashedInput, salt, iterations, HashAlgorithmName.SHA512);
            return deriveBytes.GetBytes(32); // AES-256
        }

        // --- Hilfsfunktionen für Transformationen ---
        // Verwendet von Schlüssel- und Salt-Generierung

        private static string ToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private static byte[] RotateLeft(byte[] input, int shift)
        {
            byte[] result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (byte)((input[i] << shift) | (input[i] >> (8 - shift)));
            }
            return result;
        }

        private static byte[] ShiftRight(byte[] input, int shift)
        {
            byte[] result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (byte)(input[i] >> shift);
            }
            return result;
        }

        private static byte[] XorWithConstant(byte[] input, byte constant)
        {
            byte[] result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (byte)(input[i] ^ constant);
            }
            return result;
        }

        public byte[] CalculateUsernameSalt(string username)
        {
            byte[] usernameTransformed = CheckInput(username);
            byte[] appNameTransformed = CheckInput(Configuration.ConfigGeneral.ApplicationName);
            byte[] schemaTransformed = SolveCipher(Configuration.ConfigGeneral.TableSchema);
            byte[] shortNameTransformed = ClearData(new string(Configuration.ConfigGeneral.ApplicationName.Reverse().ToArray()));

            byte[] combinedInput = new byte[usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length + shortNameTransformed.Length];
            // ... BlockCopy wie gehabt ...

            return SHA256.HashData(combinedInput).Take(16).ToArray();
        }


        // ------- Repper aus Userangabemn AES GCM (geht nicht im Browser z.B. bei WASM) -------

        #region AES_GCM Blazor Wpf
        /// <summary>
        /// Generiert einen deterministischen Pepper basierend auf dem User-Input.
        /// Diese Implementierung wird von Blazor Server und WPF genutzt.
        /// </summary>
        public string GenerateDeterministicPepper(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
                throw new ArgumentException("User input cannot be null or empty.");

            // 1. Deterministischen Pepper ableiten
            byte[] pepper = DeriveDeterministicPepper(userInput);

            // 2. Key für die Verschlüsselung sicherstellen
            _key ??= GenerateKey();

            // 3. Deterministischen Nonce ableiten
            byte[] nonce = DeriveDeterministicNonce(userInput);

            // 4. Verschlüsseln mit AES-GCM
            using (var aesGcm = new AesGcm(_key, 16)) // 16 Bytes Tag
            {
                byte[] ciphertext = new byte[pepper.Length];
                byte[] tag = new byte[16];

                aesGcm.Encrypt(nonce, pepper, ciphertext, tag);

                // Ergebnis zusammenbauen: Nonce (12) + Ciphertext (n) + Tag (16)
                byte[] result = new byte[nonce.Length + ciphertext.Length + tag.Length];
                Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
                Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

                return Convert.ToBase64String(result);
            }
        }
        public async Task<string> GenerateDeterministicPepperAsync(string userInput)
        {
            await Task.Delay(5);
            return GenerateDeterministicPepper(userInput);
        }

        private byte[] DeriveDeterministicPepper(string userInput)
        {
            // PBKDF2 mit 100.000 Iterationen (Server/WPF können das problemlos)
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                userInput,
                salt: new byte[0],
                iterations: 100_000,
                hashAlgorithm: HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }

        private byte[] DeriveDeterministicNonce(string userInput)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(userInput + "_nonce"));
                byte[] nonce = new byte[12];
                Array.Copy(hash, nonce, 12);
                return nonce;
            }
        }
        #endregion


    }
}
