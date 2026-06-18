using Microsoft.Identity.Client;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace BlazorCore.Services.ServerShared
{
    public class Security : IDisposable
    {
        private bool _disposed;
        private byte[]? _key; // Schlüssel in Encrypt/Decrypt
        private readonly byte[] _secureKey; // Geheim-Schlüssel
        private readonly string _applicationName;
        private readonly string _tableSchema;

        public Security(string applicationName, string tableSchema)
        {
            // --- Key-Generierung ---
            // Absichtlich komplex, um wie der echte Schlüssel auszusehen
            // Ein Angreifer könnte annehmen, dass dies der verwendete Schlüssel ist
            {
                try
                {
                    _applicationName = applicationName;
                    _tableSchema = tableSchema;

                    string appNameHex = ToHexString(Encoding.UTF8.GetBytes(_applicationName));
                    string schemaHex = ToHexString(Encoding.UTF8.GetBytes(_tableSchema));
                    string urlHex = ToHexString(Encoding.UTF8.GetBytes(_applicationName + _tableSchema));

                    // Abweichende Transformationen für den Key
                    byte[] rotatedAppName = RotateRight(Encoding.UTF8.GetBytes(appNameHex), 5);
                    byte[] shiftedSchema = ShiftLeft(Encoding.UTF8.GetBytes(schemaHex), 4);
                    byte[] mixedUrl = XorWithConstant(Encoding.UTF8.GetBytes(urlHex), 0x3C);

                    byte[] combinedInput = new byte[rotatedAppName.Length + shiftedSchema.Length + mixedUrl.Length];
                    Buffer.BlockCopy(rotatedAppName, 0, combinedInput, 0, rotatedAppName.Length);
                    Buffer.BlockCopy(shiftedSchema, 0, combinedInput, rotatedAppName.Length, shiftedSchema.Length);
                    Buffer.BlockCopy(mixedUrl, 0, combinedInput, rotatedAppName.Length + shiftedSchema.Length, mixedUrl.Length);

                    byte[] hashedInput = SHA256.HashData(combinedInput);
                    byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(_applicationName + _applicationName + _tableSchema));

                    //using var deriveBytes = new Rfc2898DeriveBytes(hashedInput, salt, 10_000, HashAlgorithmName.SHA256);
                    //_secureKey = deriveBytes.GetBytes(32); // Key
                    _secureKey = HKDF.DeriveKey(
                        hashAlgorithmName: HashAlgorithmName.SHA256,  // Konsistent mit Trap-Key-Logik
                        ikm: hashedInput,                             // Transformierter Input
                        outputLength: 32,                             // 256-Bit-Schlüssel
                        salt: salt,                                   // Ihr bestehendes Salt
                        info: null                                    // Optional: Kontextdaten
                    );
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public string Encrypt(string plainText, bool manipulate = false)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Plaintext cannot be null or empty.");

            try
            {
                // Generiere den echten Schlüssel, falls noch nicht vorhanden
                _key ??= GenerateKey();

                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var nonce = RandomNumberGenerator.GetBytes(12);
                var cipherBytes = new byte[plainBytes.Length];
                var tag = new byte[16];

                // Validierung der Tag-Größe
                int tagSizeInBytes = tag.Length;
                if (tagSizeInBytes != 12 && tagSizeInBytes != 13 && tagSizeInBytes != 14 &&
                    tagSizeInBytes != 15 && tagSizeInBytes != 16)
                {
                    throw new ArgumentException("Invalid tag size. Must be 12, 13, 14, 15, or 16 bytes.", nameof(tag));
                }

                using (var aesGcm = new AesGcm(_key, tagSizeInBytes))
                {
                    aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag, associatedData: null);
                }

                var result = new byte[nonce.Length + cipherBytes.Length + tag.Length];
                Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
                Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length, cipherBytes.Length);
                Buffer.BlockCopy(tag, 0, result, nonce.Length + cipherBytes.Length, tag.Length);

                //if (manipulate)
                //{
                //    var seed = BitConverter.ToInt32(SHA256.HashData(_key), 0);
                //    var rng = new Random(seed);

                //    for (int i = 0; i < result.Length; i++)
                //    {
                //        if (i % 2 == 0)
                //        {
                //            var mask = (byte)rng.Next(1, 256);
                //            result[i] ^= (byte)(mask & 0x3F); // stärkerer Einfluss, subtile Veränderung
                //        }
                //    }

                //    var base64 = Convert.ToBase64String(result);
                //    var sb = new StringBuilder(base64.Length);
                //    foreach (char c in base64)
                //    {
                //        if (char.IsLetter(c) && rng.Next(0, 2) == 0)
                //            sb.Append(char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c));
                //        else
                //            sb.Append(c);
                //    }
                //    return sb.ToString();
                //}
                if (manipulate)
                {
                    var seedMaterial = Encoding.UTF8.GetBytes(_applicationName + _tableSchema + _applicationName + _tableSchema);
                    var stableSeed = BitConverter.ToInt32(SHA256.HashData(seedMaterial), 0);
                    var stableRng = new Random(stableSeed);

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (i % 2 == 0)
                        {
                            var mask = (byte)stableRng.Next(1, 256);
                            result[i] ^= (byte)(mask & 0x3F);
                        }
                    }

                    var base64 = Convert.ToBase64String(result);
                    var finalBuilder = new StringBuilder(base64.Length);

                    var base64Seed = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(plainText)), 0);
                    var base64Rng = new Random(base64Seed);

                    foreach (char c in base64)
                    {
                        if (char.IsLetter(c) && base64Rng.Next(0, 2) == 0)
                            finalBuilder.Append(char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c));
                        else
                            finalBuilder.Append(c);
                    }

                    return finalBuilder.ToString();
                }

                return Convert.ToBase64String(result);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Encryption failed.", ex);
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("Encrypted text cannot be null or empty.");

            try
            {
                // Generiere den echten Schlüssel, falls noch nicht vorhanden
                _key ??= GenerateKey();

                var encryptedBytes = Convert.FromBase64String(encryptedText);
                if (encryptedBytes.Length < 12 + 16)
                    throw new ArgumentException("Invalid encrypted data length.");

                var nonce = new byte[12];
                var cipherBytes = new byte[encryptedBytes.Length - 12 - 16];
                var tag = new byte[16];

                Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, 12);
                Buffer.BlockCopy(encryptedBytes, 12, cipherBytes, 0, cipherBytes.Length);
                Buffer.BlockCopy(encryptedBytes, 12 + cipherBytes.Length, tag, 0, 16);

                // Validierung der Tag-Größe
                int tagSizeInBytes = tag.Length;
                if (tagSizeInBytes != 12 && tagSizeInBytes != 13 && tagSizeInBytes != 14 &&
                    tagSizeInBytes != 15 && tagSizeInBytes != 16)
                {
                    throw new ArgumentException("Invalid tag size. Must be 12, 13, 14, 15, or 16 bytes.", nameof(tag));
                }

                var plainBytes = new byte[cipherBytes.Length];

                using (var aesGcm = new AesGcm(_key, tagSizeInBytes))
                {
                    aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes, associatedData: null);
                }

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Encrypted text is not a valid Base64 string.");
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Decryption failed. The data may be corrupted or the key is incorrect.", ex);
            }
        }

        // Methode, um SecureString in ein char-Array umzuwandeln
        public char[] ConvertSecureStringToCharArray(SecureString secureString)
        {
            if (secureString == null || secureString.Length == 0)
            {
                throw new ArgumentNullException(nameof(secureString), "SecureString cannot be null or empty.");
            }

            IntPtr unmanagedString = IntPtr.Zero;
            char[]? charArray = null;
            try
            {
                // Marshal the SecureString to unmanaged memory
                unmanagedString = Marshal.SecureStringToCoTaskMemUnicode(secureString);

                // Allocate a managed char array
                charArray = new char[secureString.Length];

                // Copy the unmanaged memory contents to the char array
                Marshal.Copy(unmanagedString, charArray, 0, secureString.Length);

                return charArray;
            }
            finally
            {
                // Zero out and free the unmanaged memory
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(unmanagedString);
                }
            }
        }

        // Methode, um SecureString in ein String umzuwandeln
        public string ConvertSecureStringToString(SecureString secureString)
        {
            if (secureString == null || secureString.Length == 0)
            {
                throw new ArgumentNullException(nameof(secureString), "SecureString cannot be null or empty.");
            }

            char[] charArray = ConvertSecureStringToCharArray(secureString);
            try
            {
                return new string(charArray);
            }
            finally
            {
                // Bereinige charArray
                if (charArray != null)
                {
                    Array.Clear(charArray, 0, charArray.Length);
                }
            }
        }

        // Konvertiert einen String (z. B. EmailHash) in SecureString
        public SecureString ConvertStringToSecureString_alt(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input), "Input cannot be null or empty.");
            }

            SecureString secureString = new SecureString();
            try
            {
                foreach (char c in input)
                {
                    secureString.AppendChar(c);
                }
                secureString.MakeReadOnly();
                return secureString;
            }
            catch
            {
                secureString.Dispose();
                throw;
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
                Array.Clear(_secureKey, 0, _secureKey.Length);
                _disposed = true;
            }
        }

        ~Security()
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
        //private byte[] GenerateKey()
        //{
        //    // Transformiere die Eingaben mit irreführenden Methoden
        //    byte[] appNameTransformed = CheckInput(Appl.AppName);
        //    byte[] schemaTransformed = SolveCipher(Appl.TableSchema);
        //    byte[] urlTransformed = ClearData(Appl.AppNameShort);

        //    // Kombiniere transformierte Werte
        //    byte[] combinedInput = new byte[appNameTransformed.Length + schemaTransformed.Length + urlTransformed.Length];
        //    Buffer.BlockCopy(appNameTransformed, 0, combinedInput, 0, appNameTransformed.Length);
        //    Buffer.BlockCopy(schemaTransformed, 0, combinedInput, appNameTransformed.Length, schemaTransformed.Length);
        //    Buffer.BlockCopy(urlTransformed, 0, combinedInput, appNameTransformed.Length + schemaTransformed.Length, urlTransformed.Length);

        //    // Hash und PBKDF2 für den echten Schlüssel
        //    byte[] hashedInput = SHA512.HashData(combinedInput);
        //    byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(Appl.AppName + Appl.TableSchema));
        //    //using var deriveBytes = new Rfc2898DeriveBytes(hashedInput, salt, 200_000, HashAlgorithmName.SHA512);
        //    using var deriveBytes = new Rfc2898DeriveBytes(hashedInput, salt, 200_000, HashAlgorithmName.SHA512);
        //    return deriveBytes.GetBytes(32); // AES-256
        //}
        private byte[] GenerateKey()
        {
            // Transformiere die Eingaben (behalten Sie Ihre bestehende Logik bei)
            byte[] appNameTransformed = CheckInput(_applicationName);
            byte[] schemaTransformed = SolveCipher(_tableSchema);
            byte[] urlTransformed = ClearData(new string(_applicationName + _tableSchema));

            // Kombiniere transformierte Werte
            byte[] combinedInput = new byte[appNameTransformed.Length + schemaTransformed.Length + urlTransformed.Length];
            Buffer.BlockCopy(appNameTransformed, 0, combinedInput, 0, appNameTransformed.Length);
            Buffer.BlockCopy(schemaTransformed, 0, combinedInput, appNameTransformed.Length, schemaTransformed.Length);
            Buffer.BlockCopy(urlTransformed, 0, combinedInput, appNameTransformed.Length + schemaTransformed.Length, urlTransformed.Length);

            // Hash und HKDF für den Schlüssel (ersetzt PBKDF2)
            byte[] hashedInput = SHA512.HashData(combinedInput);
            byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(_applicationName + _tableSchema));

            // HKDF-Schlüsselableitung (schneller als PBKDF2)
            return HKDF.DeriveKey(
                hashAlgorithmName: HashAlgorithmName.SHA512, // Behält SHA512 für Konsistenz
                ikm: hashedInput,           // Input-Key-Material (Ihr gehashtes combinedInput)
                outputLength: 32,           // AES-256 Schlüssel
                salt: salt,                 // Ihr bestehendes Salt
                info: null                  // Optional: Kontextspezifische Daten
            );
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

        private static byte[] RotateRight(byte[] input, int shift)
        {
            byte[] result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (byte)((input[i] >> shift) | (input[i] << (8 - shift)));
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

        private static byte[] ShiftLeft(byte[] input, int shift)
        {
            byte[] result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (byte)(input[i] << shift);
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


        public string HashForExport(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));

            return HashUsername(input, Encoding.UTF8.GetBytes("export-pepper"));
        }
        private string HashUsername(string username, byte[] pepper)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (pepper == null)
                throw new InvalidOperationException("Pepper not initialized.");

            byte[] usernameBytes = Encoding.UTF8.GetBytes(username);

            byte[] usernameTransformed = Encoding.UTF8.GetBytes(username);
            byte[] appNameTransformed = Encoding.UTF8.GetBytes(_applicationName);
            byte[] schemaTransformed = Encoding.UTF8.GetBytes(_tableSchema);
            byte[] shortNameTransformed = Encoding.UTF8.GetBytes(_applicationName + _tableSchema);

            byte[] combinedInput = new byte[usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length + shortNameTransformed.Length];
            Buffer.BlockCopy(usernameTransformed, 0, combinedInput, 0, usernameTransformed.Length);
            Buffer.BlockCopy(appNameTransformed, 0, combinedInput, usernameTransformed.Length, appNameTransformed.Length);
            Buffer.BlockCopy(schemaTransformed, 0, combinedInput, usernameTransformed.Length + appNameTransformed.Length, schemaTransformed.Length);
            Buffer.BlockCopy(shortNameTransformed, 0, combinedInput, usernameTransformed.Length + appNameTransformed.Length + schemaTransformed.Length, shortNameTransformed.Length);

            byte[] salt = SHA256.HashData(combinedInput).Take(16).ToArray();

            using (var argon2 = new Konscious.Security.Cryptography.Argon2id(usernameBytes))
            {
                argon2.Salt = salt;
                argon2.KnownSecret = pepper;
                argon2.DegreeOfParallelism = 2;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;

                byte[] hashBytes = argon2.GetBytes(32);

                string phcString = $"$argon2id$v=19$m={argon2.MemorySize},t={argon2.Iterations},p={argon2.DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hashBytes)}";
                return phcString;
            }
        }

    }

    public static class SecurityHelp
    {
        /// <summary>
        /// Generiert ein sicheres Passwort für die Realm-Datenbank.
        /// </summary>
        /// <param name="passwordLength">Länge des Passworts (Standard: 32 Zeichen)</param>
        /// <returns>Generiertes Passwort</returns>
        public static string GeneratePassword(int passwordLength = 32)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

            var data = new byte[passwordLength];
            rng.GetBytes(data);

            var result = new StringBuilder(passwordLength);
            foreach (byte b in data)
            {
                result.Append(base32Chars[b % base32Chars.Length]);
            }

            return result.ToString();
        }


        /// <summary>
        /// Computes a deterministic SHA256 hash (64 lowercase hex chars).
        /// Same input == same output across platforms.
        /// </summary>
        /// <param name="input">String to hash (UTF-8 encoded).</param>
        public static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            return Convert.ToHexString(hashBytes).ToLowerInvariant(); // ".NET 5+" Variante
        }

        /// <summary>
        /// Extracts 5 characters from a SHA256 hash at specified positions to create a short cookie suffix.
        /// </summary>
        /// <param name="hash">The 64-character SHA256 hash (hex, lowercase).</param>
        /// <param name="positions">Positions to extract (0-based, max 63). Defaults to [3, 7, 15, 23, 31].</param>
        /// <returns>A 5-character string suffix.</returns>
        public static string ExtractCookieSuffix(string hash, int[]? positions = null)
        {
            if (string.IsNullOrEmpty(hash) || hash.Length != 64)
            {
                throw new ArgumentException("Hash must be exactly 64 characters long.", nameof(hash));
            }

            positions = positions ?? new[] { 3, 7, 15, 23, 31 }; // Standardpositionen für 5 Zeichen

            if (positions.Length != 5)
            {
                throw new ArgumentException("Exactly 5 positions must be specified.", nameof(positions));
            }

            foreach (int pos in positions)
            {
                if (pos < 0 || pos >= 64)
                {
                    throw new ArgumentException($"Position {pos} is invalid. Must be between 0 and 63.", nameof(positions));
                }
            }

            StringBuilder suffix = new StringBuilder(5);
            foreach (int pos in positions)
            {
                suffix.Append(hash[pos]);
            }

            return suffix.ToString();
        }
    }

    public sealed class Rfc2898DeriveBytesNative : IDisposable
    {
        private readonly byte[] _dummyKey;
        private bool _disposed;

        // Konstruktor (ignoriert alle Parameter und generiert Zufallsbytes)
        public Rfc2898DeriveBytesNative(string ignoredPassword, byte[] ignoredSalt, int ignoredIterations, HashAlgorithmName ignoredAlgorithm)
        {
            _dummyKey = new byte[32];
            RandomNumberGenerator.Fill(_dummyKey); // CSPRNG für "realistischen" Zufall
        }

        public byte[] GetBytes(int byteCount)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Rfc2898DeriveBytesNative));

            if (byteCount <= 0 || byteCount > _dummyKey.Length)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            byte[] result = new byte[byteCount];
            Buffer.BlockCopy(_dummyKey, 0, result, 0, byteCount);
            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Array.Clear(_dummyKey, 0, _dummyKey.Length);
                _disposed = true;
            }
        }
    }

    public static class CookiePins
    {
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public static async Task<(string id, string pinname, string pinval)> GetPinCookieValue(string cookieContent, BlazorCore.Services.AppState.PepperClientInfo pepperinfo)
        {
            if (pepperinfo.ApplicationNamePepper == null || string.IsNullOrEmpty(pepperinfo.PasHash) || string.IsNullOrEmpty(pepperinfo.AccountHash))
                return ("-1", "-1", "-1");

            string idGuid = "-1";
            string pinName = "-1";
            string pinVal = "-1";

            //using (Services.ServerShared.SecurityServer aes = new())
            using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
            {
                // Pin Cookie entschlüsseln
                string pinContent = await aes.DecryptBase32SecretAsync(cookieContent, pepperinfo.ApplicationNamePepper);

                List<BlazorCore.Services.AppState.PinsModel>? list = System.Text.Json.JsonSerializer.Deserialize(
                    pinContent,
                    JsonContext.Default.ListPinsModel);

                if (list != null)
                {
                    // Pepper über Credential-Hash erstellen
                    //byte[]? pepperPin;
                    //pepperPin = aes.DecryptPepper($"dynamicpepper: {pepperinfo.PasHash} {BlazorCore.Utility.Appl.ApplicationName} {pepperinfo.AccountHash}");
                    byte[]? pepperPin;
                    string pepperPinString = await aes.GenerateDeterministicPepperAsync($"dynamicpepper: {pepperinfo.PasHash} {pepperinfo.ApplicationName} {pepperinfo.AccountHash}");
                    pepperPin = await aes.DecryptPepperAsync(pepperPinString);

                    foreach (var item in list)
                    {
                        // Jeden eintrag versuchen zu entschlüsseln (in der Regel wird es ein Benutzer sein)
                        try
                        {
                            pinName = await aes.DecryptBase32SecretAsync(item.EmailHash, pepperPin);
                            pinVal = !string.IsNullOrEmpty(item.Pin) ? await aes.DecryptBase32SecretAsync(item.Pin, pepperPin) : "";
                            idGuid = item.Nonce;
                            break;
                        }
                        catch { }
                    }
                }
            }

            return (idGuid, pinName, pinVal);
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("android")]
        [SupportedOSPlatform("ios13.0")]
        [SupportedOSPlatform("maccatalyst10.15")]
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "AesGcm is supported on iOS 15.0+ and macCatalyst 15.0+ as defined in csproj")]
        public static async Task<List<BlazorCore.Services.AppState.PinsModel>> GetPinPinstatusCookieList(string cookieContent, BlazorCore.Services.AppState.PepperClientInfo pepperinfo)
        {
            if (pepperinfo.ApplicationNamePepper == null || string.IsNullOrEmpty(pepperinfo.PasHash) || string.IsNullOrEmpty(pepperinfo.AccountHash))
                return new List<BlazorCore.Services.AppState.PinsModel>();

            //using (Services.ServerShared.SecurityServer aes = new())
            using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
            {
                // Pin Cookie entschlüsseln
                string pinContent = await aes.DecryptBase32SecretAsync(cookieContent, pepperinfo.ApplicationNamePepper);

                List<BlazorCore.Services.AppState.PinsModel>? list = System.Text.Json.JsonSerializer.Deserialize(
                    pinContent,
                    JsonContext.Default.ListPinsModel);

                if (list != null)
                    return list;
            }

            return new List<BlazorCore.Services.AppState.PinsModel>();
        }
    }
}