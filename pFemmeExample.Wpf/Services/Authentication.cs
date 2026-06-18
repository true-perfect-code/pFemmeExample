using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Authentication;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;

namespace pFemmeExample.Wpf.Services
{
    /// <summary>
    /// Authentication service for WPF Desktop platform.
    /// Uses token-based authentication with secure local storage (file system/registry).
    /// </summary>
    public class Authentication : AuthenticationStateProvider, IAuthenticationBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPlatformBase _platform;
        private readonly IPlatformStorageBase _platformStorage;
        private readonly IAppStateBase _appState;
        private AuthenticationState _cachedState;

        /// <summary>
        /// Initializes a new instance of the <see cref="Authentication"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution.</param>
        public Authentication(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _platform = serviceProvider.GetRequiredService<IPlatformBase>();
            _platformStorage = serviceProvider.GetRequiredService<IPlatformStorageBase>();
            _appState = serviceProvider.GetRequiredService<IAppStateBase>();
            _cachedState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // =====================================================================
        // LOGIN / LOGOUT
        // =====================================================================

        /// <inheritdoc />
        public async Task<ScalarModel?> Login(string token)
        {
            if (_appState.Catalog.LocalStorage == null)
                return new ScalarModel();

            ScalarModel? result = new();

            try
            {
                // Remove existing token and store new one
                await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                result = await _platformStorage.SetAsync(_appState.Catalog.LocalStorage.oauth_token, token);

                if (result != null && string.IsNullOrEmpty(result.out_err))
                {
                    result.out_value_bool = true;

                    // Direkt den neuen AuthenticationState erstellen, ohne Server-Request
                    var claims = new[] { new Claim(ClaimTypes.Name, "User") };
                    var identity = new ClaimsIdentity(claims, "WPFAuth");
                    var authState = new AuthenticationState(new ClaimsPrincipal(identity));

                    NotifyAuthenticationStateChanged(Task.FromResult(authState));
                }
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
                await _appState.Error($"[WPF Authentication] Login error: {ex.Message}");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task Logout()
        {
            if (_appState.Catalog.LocalStorage == null)
                return;

            await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
            _appState.ResetUserCredential();

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            _cachedState = new AuthenticationState(anonymous);

            NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
        }

        // =====================================================================
        // AUTHENTICATION STATE
        // =====================================================================

        /// <inheritdoc />
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();

            try
            {
                if (_appState.Catalog.LocalStorage == null)
                    return new AuthenticationState(new ClaimsPrincipal(identity));

                // Read token from secure local storage (file system / registry)
                var oauthToken = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.oauth_token);

                if (oauthToken != null &&
                    string.IsNullOrEmpty(oauthToken.out_err) &&
                    !string.IsNullOrEmpty(oauthToken.out_value_str))
                {
                    // Deserialize token data (AOT-safe using JsonContext)
                    var tokenData = JsonSerializer.Deserialize(
                        oauthToken.out_value_str,
                        BlazorCore.JsonContext.Default.ClientStorageModel);

                    if (tokenData != null)
                    {
                        // Validate token with cloud API (or accept local-only token)
                        bool isValid = await IsTokenValidAsync(tokenData.WebApiToken);

                        if (isValid || tokenData.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                        {
                            _appState.UpdateWebApiToken(tokenData.WebApiToken);
                            _appState.UpdateUnixTS(tokenData.UnixTS);
                            _appState.UpdateUserAccount(tokenData.Email);
                            _appState.UpdateIsAuthenticated(true);

                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, tokenData.Email ?? "User"),
                                new Claim("unix_ts", tokenData.UnixTS ?? string.Empty),
                                new Claim("auth_type", "WPF-Native")
                            };

                            identity = new ClaimsIdentity(claims, "WPFAuth");
                        }
                        else
                        {
                            //await Logout();
                            // Direkt löschen, ohne Logout()
                            await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                            _appState.ResetUserCredential();
                            // Kein NotifyAuthenticationStateChanged hier!
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF Authentication] GetAuthenticationStateAsync error: {ex.Message}");
                _appState.ResetUserCredential();
            }

            _cachedState = new AuthenticationState(new ClaimsPrincipal(identity));
            return _cachedState;
        }

        /// <inheritdoc />
        public async Task<bool> IsTokenValidAsync(string token)
        {
            if (_serviceProvider == null) return false;

            try
            {
                var httpClient = _serviceProvider.GetRequiredService<System.Net.Http.HttpClient>();

                if (httpClient != null)
                {
                    var _globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();
                    using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, _globalState.ConfigWebapi.url_Authorizedconnection);

                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    // Send empty body
                    request.Content = System.Net.Http.Json.JsonContent.Create(new { });

                    using var response = await httpClient.SendAsync(request);

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
                // On network failure, treat token as invalid
                return false;
            }
        }

        // =====================================================================
        // PASSWORD CHANGE
        // =====================================================================

        /// <inheritdoc />
        /// <remarks>
        /// First updates password in cloud, then locally (if cloud update succeeded).
        /// Matches the behavior of WASM/Capacitor authentication.
        /// </remarks>
        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user)
        {
            await _appState.Log("[WPF Authentication] ChangePassword() START");

            var dam = _serviceProvider.GetRequiredService<IDamBase>();
            ScalarModel result = new();

            try
            {
                // 1. Cloud password change
                if (!db_para.ContainsKey(DB_CMD.NO_LOCAL))
                    db_para.Add(DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString());

                result = await dam.ChangePassword(db_para);

                await _appState.Log($"[WPF Authentication] Cloud result: {result?.out_err ?? "null"}");

                // 2. Local password change (if cloud succeeded)
                if (string.IsNullOrEmpty(result?.out_err) && result?.out_value_str != "not_updated:0:0")
                {
                    db_para.Remove(DB_CMD.NO_LOCAL);

                    if (!db_para.ContainsKey(DB_CMD.NO_CLOUD))
                        db_para.Add(DB_CMD.NO_CLOUD, DB_CMD.NO_CLOUD.ToString());

                    result = await dam.ChangePassword(db_para);

                    await _appState.Log($"[WPF Authentication] Local result: {result?.out_err ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WPF Authentication] ChangePassword() ERROR: {ex.Message}");
                throw;
            }

            await _appState.Log("[WPF Authentication] ChangePassword() END");

            return result ?? new ScalarModel { out_err = "Unknown error" };
        }
    }
}