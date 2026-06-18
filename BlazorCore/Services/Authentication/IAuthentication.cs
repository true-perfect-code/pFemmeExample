using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Authentication
{
    /// <summary>
    /// Provides authentication services for the application.
    /// Handles login, logout, authentication state, and password changes.
    /// </summary>
    public interface IAuthenticationBase
    {
        /// <summary>
        /// Authenticates a user using a security token.
        /// </summary>
        /// <param name="token">The authentication token (JWT or provider-specific token).</param>
        /// <returns>A ScalarModel containing the authentication result or error information.</returns>
        Task<ScalarModel?> Login(string token);

        /// <summary>
        /// Logs out the currently authenticated user.
        /// Clears all session-related authentication data.
        /// </summary>
        Task Logout();

        /// <summary>
        /// Gets the current authentication state asynchronously.
        /// </summary>
        /// <returns>AuthenticationState containing user identity and authentication status.</returns>
        Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync();

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        /// <param name="db_para">Database parameters for the password change operation.</param>
        /// <param name="oldpassword">The user's current password.</param>
        /// <param name="newpassword">The desired new password.</param>
        /// <param name="user">The user identifier (email or username).</param>
        /// <returns>A ScalarModel containing the operation result or error information.</returns>
        /// <remarks>
        /// In Blazor Server, this executes the password change logic.
        /// In MAUI/Capacitor (native), this method is typically empty as password changes
        /// are handled through the web-based authentication flow.
        /// </remarks>
        Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user);

        /// <summary>
        /// Validates a security token against the server.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>True if the token is valid, false otherwise.</returns>
        Task<bool> IsTokenValidAsync(string token);
    }
}