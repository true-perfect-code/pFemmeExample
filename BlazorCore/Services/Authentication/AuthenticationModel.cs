using System.Diagnostics.CodeAnalysis;

namespace BlazorCore.Services.Authentication
{
    /// <summary>
    /// Represents the status of OTP (One-Time Password) login attempts.
    /// </summary>
    public enum OtpLoginStatus
    {
        /// <summary>User exists and is authenticated.</summary>
        user,

        /// <summary>User does not exist in the system.</summary>
        no_user,

        /// <summary>User account is locked due to too many failed attempts.</summary>
        locked,

        /// <summary>OTP is required for this user.</summary>
        otp,

        /// <summary>No OTP is configured for this user.</summary>
        no_otp,

        /// <summary>Unknown or unhandled OTP login status.</summary>
        unknown
    }

    /// <summary>
    /// Represents supported external identity providers.
    /// </summary>
    public enum IdentityProvider
    {
        /// <summary>Google OAuth provider.</summary>
        Google,

        /// <summary>Microsoft OAuth provider.</summary>
        Microsoft,

        /// <summary>Apple OAuth provider.</summary>
        Apple,

        /// <summary>True Perfect Code internal authentication provider.</summary>
        TPC
    }

    /// <summary>
    /// Base class for authentication models.
    /// Used as a root for other authentication models to prevent trimming of base members.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class AuthenticationModelBase
    {
    }

    /// <summary>
    /// Contains account and password data for authentication operations.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public class AccountPasswordModel
    {
        /// <summary>Gets or sets the user account (email or username).</summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>Gets or sets the user's password.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Gets or sets the password confirmation (used for registration/password change).</summary>
        public string PasswordRepeat { get; set; } = string.Empty;
    }

    /// <summary>
    /// Contains complete login data including OTP, registration flags, and identity provider.
    /// Inherits from <see cref="AccountPasswordModel"/>.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public class LoginModel : AccountPasswordModel
    {
        /// <summary>Gets or sets the preferred language for the session (e.g., "EN", "DE").</summary>
        public string Language { get; set; } = "EN";

        /// <summary>Gets or sets the 6-digit OTP code entered by the user.</summary>
        public string OtpUserDigitInput { get; set; } = string.Empty;

        /// <summary>Gets or sets the backup code for OTP recovery.</summary>
        public string OtpBackupCode { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether this is a registration request.</summary>
        public bool Register { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether terms and conditions / EULA are accepted.</summary>
        public bool Termseula { get; set; } = false;

        /// <summary>Gets or sets the external identity provider to use for authentication.</summary>
        public IdentityProvider Idp { get; set; } = IdentityProvider.TPC;
    }

    /// <summary>
    /// Represents a password change request.
    /// Inherits from <see cref="AccountPasswordModel"/>.
    /// </summary>
    public class ChangePasswordRequestModel : AccountPasswordModel
    {
        /// <summary>Gets or sets the desired new password.</summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>Gets or sets the new password confirmation (must match NewPassword).</summary>
        public string NewPasswordRepeat { get; set; } = string.Empty;
    }
}