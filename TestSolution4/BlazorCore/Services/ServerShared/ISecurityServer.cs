using System.Security;

namespace BlazorCore.Services.ServerShared
{
    /// <summary>
    /// Defines security operations for encryption, decryption, and hashing across different platforms.
    /// This interface ensures that platform-specific implementations (e.g., WASM WebCrypto vs. Native .NET)
    /// can be swapped seamlessly.
    /// </summary>
    public interface ISecurityServer : IDisposable
    {
        /// <summary>
        /// Generates a new random pepper, encrypts it using a deterministic key, 
        /// and returns the result as a Base64 string.
        /// </summary>
        /// <returns>Base64 encoded string containing the encrypted pepper and required metadata (nonce/IV, tag/HMAC).</returns>
        string GenerateEncryptedPepper();
        Task<string> GenerateEncryptedPepperAsync();

        /// <summary>
        /// Decrypts an encrypted pepper string using the platform's cryptographic implementation.
        /// </summary>
        /// <param name="encryptedPepper">The Base64 encoded encrypted pepper string.</param>
        /// <returns>The decrypted pepper as a byte array (usually 32 bytes).</returns>
        byte[] DecryptPepper(string encryptedPepper = "");
        Task<byte[]> DecryptPepperAsync(string encryptedPepper = "");

        /// <summary>
        /// Hashes a username using Argon2id with a deterministic salt and a pepper.
        /// </summary>
        /// <param name="username">The username to hash.</param>
        /// <param name="pepper">The secret pepper used as a known secret in the hashing process.</param>
        /// <returns>A PHC-formatted Argon2id hash string.</returns>
        string HashUsername(string username, byte[] pepper);
        Task<string> HashUsernameAsync(string username, byte[] pepper);

        /// <summary>
        /// Decrypts an account password (which contains a pepper) and hashes the provided text with it.
        /// </summary>
        /// <param name="text">The text to be hashed (e.g., a username or identifier).</param>
        /// <param name="accountpassword">The encrypted pepper string associated with the account.</param>
        /// <returns>The resulting hash as a string.</returns>
        string GenerateClientHash(string text, string accountpassword);

        /// <summary>
        /// Hashes user credentials (password) using Argon2id with a salt derived from the username and a pepper.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        /// <param name="username">The username used for salt derivation.</param>
        /// <param name="pepper">The secret pepper.</param>
        /// <returns>A PHC-formatted Argon2id hash string.</returns>
        string HashCredentials(string password, string username, byte[] pepper);
        Task<string> HashCredentialsAsync(string password, string username, byte[] pepper);

        /// <summary>
        /// Verifies a password against a stored PHC-formatted hash using a constant-time comparison.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="username">The username used for salt derivation.</param>
        /// <param name="storedHash">The stored hash string from the database.</param>
        /// <param name="pepper">The secret pepper.</param>
        /// <returns>True if the credentials match; otherwise, false.</returns>
        bool VerifyCredentials(string password, string username, string storedHash, byte[] pepper);

        /// <summary>
        /// Encrypts a Base32 encoded OTP secret (Two-Factor Authentication).
        /// </summary>
        /// <param name="base32Secret">The plain Base32 secret string.</param>
        /// <param name="pepper">The pepper used as associated data or additional entropy.</param>
        /// <returns>Base64 encoded encrypted secret.</returns>
        string EncryptBase32Secret(string base32Secret, byte[] pepper);
        Task<string> EncryptBase32SecretAsync(string base32Secret, byte[] pepper);

        /// <summary>
        /// Decrypts an encrypted Base32 OTP secret.
        /// </summary>
        /// <param name="encryptedBase64">The Base64 encoded encrypted secret.</param>
        /// <param name="pepper">The pepper used during encryption.</param>
        /// <returns>The decrypted Base32 secret string.</returns>
        string DecryptBase32Secret(string encryptedBase64, byte[] pepper);
        Task<string> DecryptBase32SecretAsync(string encryptedBase64, byte[] pepper);

        /// <summary>
        /// Loads and decrypts the global system pepper from a configuration file.
        /// </summary>
        /// <param name="pepperFilePath">The physical path to the security.config.json file.</param>
        /// <returns>The decrypted global pepper, or null if loading failed.</returns>
        byte[]? GetPepper(string pepperFilePath);

        /// <summary>
        /// Generates a unique passphrase/key from a secure email hash for specific database operations.
        /// </summary>
        /// <param name="emailHashSecure">The email hash as a SecureString.</param>
        /// <returns>A Base64 encoded derived key.</returns>
        string GeneratePassphrase(SecureString emailHashSecure);

        /// <summary>
        /// Safely converts a SecureString to a UTF-8 byte array.
        /// </summary>
        /// <param name="secureString">The SecureString to convert.</param>
        /// <returns>A byte array. The caller is responsible for clearing this array after use.</returns>
        byte[] ConvertSecureStringToBytes(SecureString secureString);

        /// <summary>
        /// Generiert einen deterministischen Pepper basierend auf dem User-Input.
        /// Diese Implementierung wird von Blazor Server und WPF genutzt.
        /// </summary>
        string GenerateDeterministicPepper(string userInput);
        Task<string> GenerateDeterministicPepperAsync(string userInput);
    }
}
