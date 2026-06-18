#pragma warning disable CA1416

using BlazorCore;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Authentication;
using BlazorCore.Services.Dam;
using BlazorCore.Services.EventAggregator;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace pFemmeExample.Web.Services
{
    /// <summary>
    /// Authentication service for Blazor Server (Web) platform.
    /// Uses cookie-based authentication with external provider support (Google, Microsoft, Apple).
    /// Inherits from RevalidatingServerAuthenticationStateProvider for periodic validation.
    /// </summary>
    public class Authentication : RevalidatingServerAuthenticationStateProvider, IAuthenticationBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAppStateBase _appState;
        private readonly IGlobalStateBase _globalState;
        private readonly IDamBase _dam;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private AuthenticationState? _cachedAuthState;
        private string _lastUserUnixTS;

        /// <summary>
        /// Initializes a new instance of the <see cref="Authentication"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger factory for the base class.</param>
        /// <param name="serviceProvider">Service provider for dependency resolution.</param>
        public Authentication(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _eventAggregator = serviceProvider.GetRequiredService<IEventAggregator>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _globalState = serviceProvider.GetRequiredService<IGlobalStateBase>();
            _dam = serviceProvider.GetRequiredService<IDamBase>();
            _httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            _lastUserUnixTS = string.Empty;
        }

        /// <summary>
        /// Interval for revalidating the authentication state.
        /// Set to 30 minutes as a balance between security and performance.
        /// </summary>
        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

        // =====================================================================
        // LOGIN / LOGOUT
        // =====================================================================

        /// <inheritdoc />
        /// <remarks>
        /// For Blazor Server, login is handled via cookie authentication.
        /// This method exists only for interface compatibility and returns success by default.
        /// </remarks>
        public async Task<ScalarModel?> Login(string token)
        {
            // Blazor Server uses cookie authentication - token parameter is not used
            await Task.CompletedTask;

            return new ScalarModel { out_value_bool = true };
        }

        /// <inheritdoc />
        public async Task Logout()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            _appState.ResetUserCredential();
            _cachedAuthState = null;
            _lastUserUnixTS = string.Empty;
        }

        // =====================================================================
        // AUTHENTICATION STATE
        // =====================================================================

        /// <inheritdoc />
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Trigger loading spinner on landing page during authentication
            _eventAggregator.RoutesStateHasChanged();

            var httpContext = _httpContextAccessor.HttpContext;

            // Return cached state if still valid
            if (_cachedAuthState != null && httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return _cachedAuthState;
            }

            var identity = new ClaimsIdentity();

            try
            {
                var user = httpContext?.User;

                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
                    var os = ParseOperatingSystem(userAgent);

                    // Handle external provider claims (Google, Microsoft, Apple)
                    var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                                  ?? user.FindFirst("sub")      // Apple uses "sub" claim
                                  ?? user.FindFirst(ClaimTypes.Email); // Fallback for Apple

                    if (subClaim != null)
                    {
                        // Get pepper from security configuration
                        var configResult = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);

                        if (configResult != null &&
                            string.IsNullOrEmpty(configResult.out_err) &&
                            !string.IsNullOrEmpty(configResult.out_value_str))
                        {
                            string pepperFilePath = configResult.out_value_str;
                            string? unixTS = null;

                            try
                            {
                                string issuer = subClaim.Issuer;
                                byte[] pepper;

                                using (var aes = new pFemmeExample.Shared.Services.Security.SecurityServer())
                                {
                                    pepper = aes.GetPepper(pepperFilePath);
                                    string hashedEmail = aes.HashUsername(subClaim.Value, pepper);
                                    unixTS = await GetOrCreateUserUnixTS(hashedEmail, os, issuer);
                                }

                                if (!string.IsNullOrEmpty(unixTS))
                                {
                                    identity = BuildClaimsIdentity(user, unixTS);
                                    _appState.UpdateUnixTS(unixTS);
                                    _appState.UpdateIsAuthenticated(true);
                                }
                                else
                                {
                                    _appState.ResetUserCredential();
                                }
                            }
                            catch (Exception ex)
                            {
                                await _appState.Error($"[Web Authentication] Error processing external login: {ex.Message}");
                                _appState.ResetUserCredential();
                            }
                        }
                        else
                        {
                            await _appState.Error($"[Web Authentication] Failed to load security configuration: {configResult?.out_err ?? "Unknown error"}");
                            _appState.ResetUserCredential();
                        }
                    }
                    else
                    {
                        // Internal login (cookie already contains unix_ts claim)
                        var unixTsClaim = user.FindFirst("unix_ts");
                        string unixTS = unixTsClaim?.Value ?? _lastUserUnixTS;

                        if (!string.IsNullOrEmpty(unixTS))
                        {
                            identity = new ClaimsIdentity(user.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            _appState.UpdateUnixTS(unixTS);
                            _appState.UpdateIsAuthenticated(true);
                        }
                        else
                        {
                            _appState.ResetUserCredential();
                        }
                    }
                }
                else
                {
                    _appState.ResetUserCredential();
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Web Authentication] GetAuthenticationStateAsync error: {ex.Message}");
                _appState.ResetUserCredential();
            }

            var authState = new AuthenticationState(new ClaimsPrincipal(identity));
            _cachedAuthState = authState;
            return authState;
        }

        /// <summary>
        /// Gets or creates a user record in the database based on external provider login.
        /// </summary>
        /// <param name="hashedEmail">The hashed email address.</param>
        /// <param name="os">The operating system from user agent.</param>
        /// <param name="issuer">The external provider issuer (Google, Microsoft, Apple).</param>
        /// <returns>The user's UnixTS, or null if operation failed.</returns>
        private async Task<string?> GetOrCreateUserUnixTS(string hashedEmail, string os, string issuer)
        {
            // Check if user exists
            var db_para = new Dictionary<string, string>
            {
                { "@Case_", "SelectAuthUsersEmail" },
                { "@EmailHash", hashedEmail },
                { "@PasswordHash", hashedEmail },
                { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() },
            };

            if (!_dam.IsInitializeSql())
                _dam.InitializeSql();

            var result = await _dam.Scalar(db_para);

            if (result != null && string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
            {
                if (result.out_value_str != "no_user")
                {
                    // User exists
                    _lastUserUnixTS = result.out_value_str;
                    return result.out_value_str;
                }
                else
                {
                    // User does not exist → register new user
                    string newUnixTS = BlazorCore.UnixTsGeneratorWebApi.Generate(_globalState.ConfigGeneral);

                    db_para = new Dictionary<string, string>
                    {
                        { "@Case_", "Register>>AuthUsers" },
                        { "@UnixTS", newUnixTS },
                        { "@EmailHash", hashedEmail },
                        { "@PasswordHash", hashedEmail },
                        { "@Int__Registration", "1" },
                        { "@Int__TwoFA", "0" },
                        { "@active", "1" },
                        { "@IdP", $"Web-{os}-{issuer.ToLower()}" }
                    };

                    var registerResult = await _dam.ExecQuery(db_para);

                    if (registerResult != null && string.IsNullOrEmpty(registerResult.out_err))
                    {
                        _lastUserUnixTS = newUnixTS;
                        return newUnixTS;
                    }

                    await _appState.Error($"[Web Authentication] User registration failed: {registerResult?.out_err ?? "Unknown error"}");
                    return null;
                }
            }

            await _appState.Error($"[Web Authentication] User lookup failed: {result?.out_err ?? "Unknown error"}");
            return null;
        }

        /// <summary>
        /// Builds a ClaimsIdentity for the authenticated user.
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal.</param>
        /// <param name="unixTS">The user's UnixTS.</param>
        /// <returns>A new ClaimsIdentity with expiration claim.</returns>
        private ClaimsIdentity BuildClaimsIdentity(ClaimsPrincipal user, string unixTS)
        {
            var claims = new List<Claim>(user.Claims);
            claims.Add(new Claim("unix_ts", unixTS));

            var expiresUtc = DateTimeOffset.UtcNow.AddDays(30);
            claims.Add(new Claim(ClaimTypes.Expiration, expiresUtc.ToString("O")));

            return new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Validates the current authentication state.
        /// Called periodically based on RevalidationInterval.
        /// </summary>
        /// <param name="authenticationState">The current authentication state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the user is still authenticated, otherwise false.</returns>
        protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;
            return Task.FromResult(user?.Identity?.IsAuthenticated == true);
        }

        /// <inheritdoc />
        public async Task<bool> IsTokenValidAsync(string token)
        {
            // Blazor Server does not use client-side tokens.
            // Authentication is handled via cookies/session.
            // Therefore, always return true.
            await Task.Delay(100); // Cosmetic delay for consistency with other platforms

            return true;
        }

        // =====================================================================
        // PASSWORD CHANGE
        // =====================================================================

        /// <inheritdoc />
        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user)
        {
            await _appState.Log("[Web Authentication] ChangePassword() START");

            var result = new ScalarModel();

            try
            {
                // Load pepper from security configuration (server only!)
                var configResult = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);

                if (configResult != null &&
                    string.IsNullOrEmpty(configResult.out_err) &&
                    !string.IsNullOrEmpty(configResult.out_value_str))
                {
                    string pepperFilePath = configResult.out_value_str;

                    using (var aes = new pFemmeExample.Shared.Services.Security.SecurityServer())
                    {
                        byte[] pepper = aes.GetPepper(pepperFilePath);

                        if (db_para.ContainsKey("@EmailHash") &&
                            db_para.ContainsKey("@PasswordHash") &&
                            db_para.ContainsKey("@PasswordHashNew"))
                        {
                            string emailHash = db_para.GetValueOrDefault("@EmailHash", "");
                            string oldPasswordHash = db_para.GetValueOrDefault("@PasswordHash", "");
                            string newPasswordHash = db_para.GetValueOrDefault("@PasswordHashNew", "");

                            db_para["@EmailHash"] = aes.HashUsername(emailHash, pepper);
                            db_para["@PasswordHash"] = aes.HashCredentials(oldPasswordHash, emailHash, pepper);
                            db_para["@PasswordHashNew"] = aes.HashCredentials(newPasswordHash, emailHash, pepper);

                            result = await _dam.Save(db_para)!;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Web Authentication] ChangePassword() ERROR: {ex.Message}");
                throw;
            }

            await _appState.Log("[Web Authentication] ChangePassword() END");
            return result;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Parses the operating system from the User-Agent string.
        /// </summary>
        /// <param name="userAgent">The User-Agent header value.</param>
        /// <returns>The detected operating system name.</returns>
        private string ParseOperatingSystem(string userAgent)
        {
            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                return "Windows";
            if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
                return "macOS";
            if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                return "Linux";
            if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
                return "iOS";
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                return "Android";
            return "Unknown";
        }
    }
}
#pragma warning restore CA1416