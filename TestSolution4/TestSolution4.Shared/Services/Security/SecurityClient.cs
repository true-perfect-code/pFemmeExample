#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
using BlazorCore.Services.ServerShared;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Global;
using TestSolution4.Shared.Services.AppState;
using TestSolution4.Shared.Services.GlobalState;
using TestSolution4.Shared.Services.Platform;
using Microsoft.JSInterop;
using System.Security;
using System.Security.Cryptography;

namespace TestSolution4.Shared.Services.Security
{
    /// <summary>
    /// WASM implementation of SecurityServer.
    /// Redirects cryptographic operations to JavaScript (Web Crypto API) 
    /// via specific prefixes for PWA (Web) and Capacitor (Native).
    /// </summary>
    public class SecurityClient : ISecurityServer
    {
        private readonly IJSRuntime _js;
        private readonly IPlatform _platform;
        private readonly IGlobalState _globalState;
        private readonly IAppState _appState;

        private byte[]? _key;
        private bool _disposed;
        private int _iterations = 1000;

        // Einstiegspunkte für die JS-Dateien (analog zu LocalNotification)
        private const string JsCapPrefix = "pE_Capacitor.security";
        private const string JsWebPrefix = "pE_Web.security";

        public SecurityClient(IJSRuntime js, IPlatform platform, IGlobalState globalState, IAppState appState)
        {
            _js = js;
            _platform = platform;
            _globalState = globalState;
            _appState = appState;
        }

        private bool IsNative => false; // Weil Capacitor keine spezifische Funktionen dazu bietet // _platform.GetCurrPlatform() != PLATFORMS.WASM;

        /// <summary>
        /// Determines the correct JS prefix based on the current environment.
        /// </summary>
        private string GetPrefix() => IsNative ? JsCapPrefix : JsWebPrefix;

        // --- ISecurityServer Implementation ---

        /// <inheritdoc />
        public byte[] DecryptPepper(string encryptedPepper = "") => Array.Empty<byte>();
        public async Task<byte[]> DecryptPepperAsync(string encryptedPepper = "")
        {
            await _appState.Log($"[Security] DecryptPepperAsync: START. Input-Length: {encryptedPepper?.Length ?? 0}");

            if (string.IsNullOrEmpty(encryptedPepper))
            {
                //encryptedPepper = _globalState.ConfigGeneral.PepperAppWasm;
                //await _appState.Log("[Security] DecryptPepperAsync: Using Fallback PepperAppWasm.");
                if (string.IsNullOrEmpty(Configuration.ConfigGeneral.PepperAppWasm))
                    encryptedPepper = await GenerateEncryptedPepperAsync();
                else
                    encryptedPepper = Configuration.ConfigGeneral.PepperAppWasm;
            }

            if (string.IsNullOrEmpty(encryptedPepper))
            {
                await _appState.Error("[Security] DecryptPepperAsync: Encrypted pepper is null/empty.");
                throw new ArgumentException("Encrypted pepper cannot be null or empty.");
            }

            // Key sicherstellen
            if (_key == null)
            {
                await _appState.Log("[Security] DecryptPepperAsync: Generating new key (GenerateKey)...");
                using (Shared.Services.Security.SecurityServer aes = new())
                {
                    _key = aes.GenerateKey(_iterations);
                }
            }

            try
            {
                string keyBase64 = Convert.ToBase64String(_key);
                await _appState.Log("[Security] DecryptPepperAsync: Calling JS decryptPepper (Base64 Bridge)");

                // JS Aufruf
                var result = await _js.InvokeAsync<ScalarModel>($"{GetPrefix()}.decryptPepper", encryptedPepper, keyBase64);

                // Prüfung basierend auf ScalarModel
                if (result == null)
                {
                    await _appState.Error("[Security] DecryptPepperAsync: JS-Result is NULL.");
                    throw new CryptographicException("Unknown Decryption Error (Result was null)");
                }

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Error($"[Security] DecryptPepperAsync: JS-Error: {result.out_err}");
                    throw new CryptographicException(result.out_err);
                }

                if (string.IsNullOrEmpty(result.out_value_str))
                {
                    await _appState.Error("[Security] DecryptPepperAsync: Result out_value_str is empty.");
                    throw new CryptographicException("Decrypted pepper: out_value_str is empty.");
                }

                await _appState.Log($"[Security] DecryptPepperAsync: SUCCESS. Result length: {result.out_value_str.Length}");

                return Convert.FromBase64String(result.out_value_str);
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Security] DecryptPepper Exception: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public string HashUsername(string username, byte[] pepper)
        {
            return ExecutePbkdf2Hashing(username, username, pepper, "HashUsername");
        }
        public async Task<string> HashUsernameAsync(string username, byte[] pepper)
        {
            return await ExecutePbkdf2HashingAsync(username, username, pepper, "HashUsername");
        }

        /// <inheritdoc />
        public string HashCredentials(string password, string username, byte[] pepper)
        {
            return ExecutePbkdf2Hashing(password, username, pepper, "HashCredentials");
        }
        public async Task<string> HashCredentialsAsync(string password, string username, byte[] pepper)
        {
            return await ExecutePbkdf2HashingAsync(password, username, pepper, "HashCredentials");
        }

        /// <summary>
        /// Zentralisierte Logik für das PBKDF2 Hashing via JS-Brücke.
        /// </summary>
        private string ExecutePbkdf2Hashing(string plainText, string usernameForSalt, byte[] pepper, string context) => "";
        private async Task<string> ExecutePbkdf2HashingAsync(string plainText, string usernameForSalt, byte[] pepper, string context)
        {
            await _appState.Log($"[Security] {context}: PBKDF2 Hashing START (User: {usernameForSalt})");

            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(usernameForSalt))
            {
                await _appState.Error($"[Security] {context}: Inputs cannot be empty.");
                throw new ArgumentException($"{context}: Inputs cannot be empty.");
            }

            if (pepper == null)
            {
                await _appState.Error($"[Security] {context}: Pepper not initialized.");
                throw new InvalidOperationException($"{context}: Pepper not initialized.");
            }

            try
            {
                byte[] salt;
                // 1. Salt-Berechnung (Shared Logic)
                using (Shared.Services.Security.SecurityServer sharedAes = new())
                {
                    salt = sharedAes.CalculateUsernameSalt(usernameForSalt);
                }

                await _appState.Log($"[Security] {context}: Salt calculated ({salt.Length} bytes). Converting to Base64...");

                // --- UMSTELLUNG AUF STRING-TRANSFER ---
                string saltBase64 = Convert.ToBase64String(salt);
                string pepperBase64 = Convert.ToBase64String(pepper);

                await _appState.Log($"[Security] {context}: Invoking JS hashPbkdf2 (Base64 Bridge)");

                // JS Aufruf mit echtem await
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{GetPrefix()}.hashPbkdf2",
                    plainText,
                    saltBase64,
                    pepperBase64
                );

                // Prüfung basierend auf dem ScalarModel
                if (result == null)
                {
                    await _appState.Error($"[Security] {context}: JS-Result is NULL.");
                    throw new CryptographicException($"{context} failed: Result was null");
                }

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Error($"[Security] {context}: JS-Error: {result.out_err}");
                    throw new CryptographicException(result.out_err);
                }

                if (string.IsNullOrEmpty(result.out_value_str))
                {
                    await _appState.Error($"[Security] {context}: out_value_str is empty.");
                    throw new CryptographicException($"{context} failed: Empty hash string received");
                }

                await _appState.Log($"[Security] {context}: SUCCESS. Hash received (PHC-Length: {result.out_value_str.Length})");

                return result.out_value_str!;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Security] {context} Exception: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public string DecryptBase32Secret(string encryptedBase64, byte[] pepper) => "";
        public async Task<string> DecryptBase32SecretAsync(string encryptedBase64, byte[] pepper)
        {
            await _appState.Log($"[Security] DecryptBase32SecretAsync: START. Input-Length: {encryptedBase64?.Length ?? 0}");

            if (string.IsNullOrEmpty(encryptedBase64))
            {
                await _appState.Error("[Security] DecryptBase32SecretAsync: Encrypted secret is null/empty.");
                throw new ArgumentException("Encrypted OTP secret must not be null or empty.");
            }

            if (pepper == null || pepper.Length == 0)
            {
                await _appState.Error("[Security] DecryptBase32SecretAsync: Pepper is missing.");
                throw new ArgumentException("Pepper must be provided.");
            }

            // 1. Key sicherstellen
            if (_key == null)
            {
                await _appState.Log("[Security] DecryptBase32SecretAsync: Key not found, generating (GenerateKey)...");
                using (Shared.Services.Security.SecurityServer sharedAes = new())
                {
                    _key = sharedAes.GenerateKey(_iterations);
                }
            }

            try
            {
                // --- UMSTELLUNG AUF STRING-TRANSFER ---
                string keyBase64 = Convert.ToBase64String(_key);
                string pepperBase64 = Convert.ToBase64String(pepper);

                await _appState.Log("[Security] DecryptBase32SecretAsync: Invoking JS decryptAesGcm (Base64 Bridge)");

                // 2. JS-Brücke aufrufen (Asynchron!)
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{GetPrefix()}.decryptAesGcm",
                    encryptedBase64,
                    keyBase64,
                    pepperBase64
                );

                // Prüfung basierend auf ScalarModel (out_err check)
                if (result == null)
                {
                    await _appState.Error("[Security] DecryptBase32SecretAsync: JS-Result is NULL.");
                    throw new CryptographicException("OTP decryption failed: Result was null");
                }

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Error($"[Security] DecryptBase32SecretAsync: JS-Error: {result.out_err}");
                    throw new CryptographicException(result.out_err);
                }

                if (string.IsNullOrEmpty(result.out_value_str))
                {
                    await _appState.Error("[Security] DecryptBase32SecretAsync: Result out_value_str is empty.");
                    throw new CryptographicException("OTP decryption failed: Result value is empty");
                }

                await _appState.Log("[Security] DecryptBase32SecretAsync: SUCCESS. Secret decrypted.");

                return result.out_value_str!;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Security] DecryptBase32Secret Exception: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public string EncryptBase32Secret(string base32Secret, byte[] pepper) => "";
        public async Task<string> EncryptBase32SecretAsync(string base32Secret, byte[] pepper)
        {
            await _appState.Log($"[Security] EncryptBase32SecretAsync: START. Input-Length: {base32Secret?.Length ?? 0}");

            if (string.IsNullOrEmpty(base32Secret))
            {
                await _appState.Error("[Security] EncryptBase32SecretAsync: OTP secret is null/empty.");
                throw new ArgumentException("OTP secret must not be null or empty.");
            }

            if (pepper == null || pepper.Length == 0)
            {
                await _appState.Error("[Security] EncryptBase32SecretAsync: Pepper is missing.");
                throw new ArgumentException("Pepper must be provided.");
            }

            // Key sicherstellen (Shared Logik)
            if (_key == null)
            {
                await _appState.Log("[Security] EncryptBase32SecretAsync: Generating new key (GenerateKey)...");
                using (Shared.Services.Security.SecurityServer sharedAes = new())
                {
                    _key = sharedAes.GenerateKey(_iterations);
                }
            }

            try
            {
                // Umwandlung für den stabilen String-Transport
                string keyBase64 = Convert.ToBase64String(_key);
                string pepperBase64 = Convert.ToBase64String(pepper);

                await _appState.Log("[Security] EncryptBase32SecretAsync: Invoking JS encryptAesGcm (Base64 Bridge)");

                // Aufruf der JS-Verschlüsselung (Echtes await!)
                var result = await _js.InvokeAsync<ScalarModel>(
                    $"{GetPrefix()}.encryptAesGcm",
                    base32Secret,
                    keyBase64,
                    pepperBase64
                );

                // Prüfung via out_err (dein ScalarModel Standard)
                if (result == null)
                {
                    await _appState.Error("[Security] EncryptBase32SecretAsync: JS-Result is NULL.");
                    throw new CryptographicException("OTP encryption failed: Result was null");
                }

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Error($"[Security] EncryptBase32SecretAsync: JS-Error: {result.out_err}");
                    throw new CryptographicException(result.out_err);
                }

                if (string.IsNullOrEmpty(result.out_value_str))
                {
                    await _appState.Error("[Security] EncryptBase32SecretAsync: Result out_value_str is empty.");
                    throw new CryptographicException("OTP encryption failed: Empty cipher string received");
                }

                await _appState.Log($"[Security] EncryptBase32SecretAsync: SUCCESS. Cipher-Length: {result.out_value_str.Length}");

                // Das Ergebnis ist bereits ein Base64-String (Nonce+Cipher+Tag)
                return result.out_value_str!;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Security] EncryptBase32Secret Exception: {ex.Message}");
                throw;
            }
        }

        public string GenerateDeterministicPepper(string userInput) => "";
        public async Task<string> GenerateDeterministicPepperAsync(string userInput)
        {
            await _appState.Log($"[Security] GenerateDeterministicPepperAsync: START (User input length: {userInput?.Length ?? 0})");

            if (string.IsNullOrEmpty(userInput))
            {
                await _appState.Error("[Security] GenerateDeterministicPepperAsync: User input is null/empty.");
                throw new ArgumentException("User input cannot be null or empty.");
            }

            try
            {
                // 1. Pepper ableiten via JS (PBKDF2)
                await _appState.Log("[Security] GenerateDeterministicPepperAsync: Step 1 - Invoking JS deriveBytesPbkdf2");

                var pepperResult = await _js.InvokeAsync<ScalarModel>(
                    $"{GetPrefix()}.deriveBytesPbkdf2", userInput, 10000);

                if (pepperResult == null || !string.IsNullOrEmpty(pepperResult.out_err) || string.IsNullOrEmpty(pepperResult.out_value_str))
                {
                    await _appState.Error($"[Security] GenerateDeterministicPepperAsync: Step 1 Failed. Error: {pepperResult?.out_err ?? "Result null or empty"}");
                    throw new CryptographicException(pepperResult?.out_err ?? "Pepper derivation failed.");
                }

                string pepperRawBase64 = pepperResult.out_value_str;
                await _appState.Log($"[Security] GenerateDeterministicPepperAsync: Step 1 SUCCESS (Derived length: {pepperRawBase64.Length})");

                // 2. Key für die Verschlüsselung sicherstellen
                if (_key == null)
                {
                    await _appState.Log("[Security] GenerateDeterministicPepperAsync: Generating new master key...");
                    using (Shared.Services.Security.SecurityServer sharedAes = new())
                    {
                        _key = sharedAes.GenerateKey(_iterations);
                    }
                }
                string keyBase64 = Convert.ToBase64String(_key);

                // 3. Deterministische Nonce (SHA-256) in C# berechnen
                await _appState.Log("[Security] GenerateDeterministicPepperAsync: Step 2 - Calculating deterministic nonce in C#");
                string nonceBase64;
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userInput + "_nonce"));
                    byte[] nonce = new byte[12];
                    Array.Copy(hash, 0, nonce, 0, 12);
                    nonceBase64 = Convert.ToBase64String(nonce);
                }

                // 4. Verschlüsseln via JS (AES-GCM mit fester Nonce)
                await _appState.Log("[Security] GenerateDeterministicPepperAsync: Step 3 - Invoking JS encryptAesGcmDeterministic");

                var encryptResult = await _js.InvokeAsync<ScalarModel>(
                    $"{GetPrefix()}.encryptAesGcmDeterministic",
                    pepperRawBase64,
                    keyBase64,
                    nonceBase64
                );

                if (encryptResult == null || !string.IsNullOrEmpty(encryptResult.out_err))
                {
                    await _appState.Error($"[Security] GenerateDeterministicPepperAsync: Step 3 Failed. Error: {encryptResult?.out_err ?? "Result null"}");
                    throw new CryptographicException(encryptResult?.out_err ?? "Deterministic encryption failed");
                }

                if (string.IsNullOrEmpty(encryptResult.out_value_str))
                {
                    await _appState.Error("[Security] GenerateDeterministicPepperAsync: Step 3 SUCCESS but out_value_str is empty.");
                    throw new CryptographicException("Deterministic encryption failed: Empty result");
                }

                await _appState.Log($"[Security] GenerateDeterministicPepperAsync: ALL STEPS SUCCESS (Final length: {encryptResult.out_value_str.Length})");

                return encryptResult.out_value_str!;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Security] GenerateDeterministicPepper Exception: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public string GenerateEncryptedPepper() => "";
        public async Task<string> GenerateEncryptedPepperAsync()
        {
            await _appState.Log("[Security] GenerateEncryptedPepperAsync: START (Base64 Bridge)");

            try
            {
                // 1. Key generieren (falls nötig) via Shared-Logik
                if (_key == null)
                {
                    await _appState.Log($"[Security] GenerateEncryptedPepperAsync: Generating Key with {_iterations} iterations...");
                    using (Shared.Services.Security.SecurityServer sharedAes = new())
                    {
                        _key = sharedAes.GenerateKey(_iterations);
                    }
                }

                // 2. Key für die Brücke in Base64 umwandeln
                string keyBase64 = Convert.ToBase64String(_key);
                await _appState.Log($"[Security] GenerateEncryptedPepperAsync: Key ready (Base64 length: {keyBase64.Length}). Invoking JS...");

                // 3. JS-Brücke aufrufen (Pure String Transfer)
                string jsMethod = $"{GetPrefix()}.generateEncryptedPepper";
                var result = await _js.InvokeAsync<ScalarModel>(jsMethod, keyBase64);

                // Prüfung basierend auf ScalarModel
                if (result == null)
                {
                    await _appState.Error("[Security] GenerateEncryptedPepperAsync: JS-Result is NULL.");
                    throw new CryptographicException("Pepper generation failed: Result was null");
                }

                if (!string.IsNullOrEmpty(result.out_err))
                {
                    await _appState.Error($"[Security] GenerateEncryptedPepperAsync: JS-Error: {result.out_err}");
                    throw new CryptographicException(result.out_err);
                }

                if (string.IsNullOrEmpty(result.out_value_str))
                {
                    await _appState.Error("[Security] GenerateEncryptedPepperAsync: out_value_str is empty.");
                    throw new CryptographicException("Pepper generation failed: Result value is empty");
                }

                string pepper = result.out_value_str;

                // Finaler Log statt Console.WriteLine
                await _appState.Log($"[Security] GenerateEncryptedPepperAsync: SUCCESS. Received Pepper length: {pepper.Length}");

                return pepper;
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Security] GenerateEncryptedPepperAsync Exception: {ex.Message}");
                throw;
            }
        }



        /// <inheritdoc />
        public bool VerifyCredentials(string password, string username, string storedHash, byte[] pepper)
        {
            return false;
        }

        /// <inheritdoc />
        public string GenerateClientHash(string text, string accountpassword)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public byte[]? GetPepper(string pepperFilePath)
        {
            // Wird in WASM meist nicht direkt über Dateipfad gelöst
            return null;
        }

        /// <inheritdoc />
        public string GeneratePassphrase(SecureString emailHashSecure)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public byte[] ConvertSecureStringToBytes(SecureString secureString)
        {
            // Hilfsmethode zur sicheren Konvertierung
            return Array.Empty<byte>();
        }

        // --- Internal Helpers ---

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_key != null) Array.Clear(_key, 0, _key.Length);
                _disposed = true;
            }
        }
    }
}
#pragma warning restore CA1416
