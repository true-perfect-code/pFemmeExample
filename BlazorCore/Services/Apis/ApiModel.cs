using System.Text.Json.Serialization;

namespace BlazorCore.Services.Apis
{
    public class ApiModel
    {
    }

    public static class API_CONST
    {
        /// <summary>
        /// Represents a local-only authentication state without a valid Web-API cloud token.
        /// </summary>
        public const string TOKEN_LOCAL_ONLY = "local";

        /// <summary>
        /// Boolean true represented as string flag.
        /// </summary>
        public const string TRUE_VALUE = "1";

        /// <summary>
        /// Boolean false represented as string flag.
        /// </summary>
        public const string FALSE_VALUE = "0";
    }

    public class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 0;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "";

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = "";

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = "";
    }

    public class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string sub { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = "";

        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("picture")]
        public string PictureUrl { get; set; } = "";
    }

    /// <summary>
    /// Generisches Model für Random-Zahlen (siehe https://passwordwolf.com/)
    /// </summary>
    public class PasswordwolfModel
    {
        [JsonPropertyName("Password")]
        public string Password { get; set; } = "";

        [JsonPropertyName("Phonetic")]
        public string Phonetic { get; set; } = "";
    }
    public class GeneratedPasswordModel
    {
        public string Password { get; set; } = "";

        public string out_err { get; set; } = "";
    }

    /// <summary>
    /// Container für die Kommunikation zwischen WebApi und client
    /// </summary>
    public class UserWebApi
    {
        // Command
        public string DisplayError { get; set; } = "0";
        public string EncryptDecrypt { get; set; } = "1";
        public string IsByte { get; set; } = "0";

        // Data
        public string JsonPara { get; set; } = "";
        public string JsonSearch { get; set; } = "";
        public string JsonAddInfo { get; set; } = "";
        public string Token { get; set; } = "";
    }

    public class ChangePasswordModel
    {
        public string Username { get; set; } = "";
        public string OldUserpassword { get; set; } = "";
        public string NewUserpassword { get; set; } = "";
        public string ReenterUserpassword { get; set; } = "";
        public bool ChangePasswordVisible { get; set; } = false;
    }

}
