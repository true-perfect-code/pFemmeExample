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

namespace pFemmeExample.Shared.Services.Authentication
{
    /// <summary>
    /// Authentication service for Blazor WASM, Capacitor, and PWA platforms.
    /// Implements token-based authentication with secure storage and cloud validation.
    /// </summary>
    public class Authentication : AuthenticationStateProvider, IAuthenticationBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPlatformBase _platform;
        private readonly IPlatformStorageBase _platformStorage;
        private readonly IAppStateBase _appState;
        private readonly HttpClient _httpClient;

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
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
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
                // Remove existing token first
                result = await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);

                if (result != null && string.IsNullOrEmpty(result.out_err))
                {
                    // Store new token
                    result = await _platformStorage.SetAsync(_appState.Catalog.LocalStorage.oauth_token, token);

                    if (result != null && string.IsNullOrEmpty(result.out_err))
                        result.out_value_bool = true;
                }
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
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

            // Erstelle einen leeren Identity (nicht GetAuthenticationStateAsync aufrufen)
            var identity = new ClaimsIdentity();
            var authState = new AuthenticationState(new ClaimsPrincipal(identity));

            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        // =====================================================================
        // AUTHENTICATION STATE
        // =====================================================================

        /// <inheritdoc />
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            await _appState.Log("[WASM Authentication] START GetAuthenticationStateAsync");

            var identity = new ClaimsIdentity();

            try
            {
                if (_appState.Catalog.LocalStorage == null)
                    return new AuthenticationState(new ClaimsPrincipal(identity));

                // Read OAuth token from secure storage
                var oauthToken = await _platformStorage.GetAsync(_appState.Catalog.LocalStorage.oauth_token);

                await _appState.Log($"[WASM Authentication] oauthToken received: Success={string.IsNullOrEmpty(oauthToken?.out_err)}, HasValue={!string.IsNullOrEmpty(oauthToken?.out_value_str)}");

                if (oauthToken != null &&
                    string.IsNullOrEmpty(oauthToken.out_err) &&
                    !string.IsNullOrEmpty(oauthToken.out_value_str))
                {
                    // Deserialize token data
                    var tokenData = System.Text.Json.JsonSerializer.Deserialize(
                        oauthToken.out_value_str,
                        BlazorCore.JsonContext.Default.ClientStorageModel
                    );

                    await _appState.Log($"[WASM Authentication] tokenData: {tokenData}");

                    if (tokenData != null && string.IsNullOrEmpty(tokenData.out_err))
                    {
                        bool isInternetConnected = _appState.IsCloudConnected;

                        // If online and no token → force logout (aber NICHT rekursiv!)
                        if (isInternetConnected && string.IsNullOrEmpty(tokenData.WebApiToken))
                        {
                            // DIREKT löschen, ohne Logout() aufzurufen
                            await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                            _appState.ResetUserCredential();
                            // Kein NotifyAuthenticationStateChanged hier!
                        }
                        else
                        {
                            // Validate token (or accept local-only token)
                            bool isValid = false;
                            if (tokenData.WebApiToken != API_CONST.TOKEN_LOCAL_ONLY)
                                isValid = await IsTokenValidAsync(tokenData.WebApiToken);

                            if (isValid || tokenData.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                _appState.UpdateWebApiToken(tokenData.WebApiToken);
                                _appState.UpdateUnixTS(tokenData.UnixTS);
                                _appState.UpdateUserAccount(tokenData.Email);
                                _appState.UpdateIsAuthenticated(true);

                                var claims = new[] { new Claim(ClaimTypes.Name, tokenData.UnixTS) };
                                identity = new ClaimsIdentity(claims, isInternetConnected ? "Server authentication" : "Client authentication");
                            }
                            else
                            {
                                // DIREKT löschen, ohne Logout() aufzurufen
                                await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                                _appState.ResetUserCredential();
                            }
                        }

                        if (!identity.IsAuthenticated)
                            _appState.ResetUserCredential();
                    }
                    else
                    {
                        await _platformStorage.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                        _appState.ResetUserCredential();
                    }
                }
                else
                {
                    _appState.ResetUserCredential();
                }
            }
            catch (HttpRequestException ex)
            {
                await _appState.Log($"[WASM Authentication] ERROR GetAuthenticationStateAsync: {ex.Message}");
            }

            var authState = new AuthenticationState(new ClaimsPrincipal(identity));

            await _appState.Log($"[WASM Authentication] END GetAuthenticationStateAsync - IsAuthenticated: {authState.User.Identity?.IsAuthenticated}");

            // Auskommentiert lassen - NICHT hier aufrufen!
            // NotifyAuthenticationStateChanged(Task.FromResult(authState));

            return authState;
        }

        // =====================================================================
        // PASSWORD CHANGE
        // =====================================================================

        /// <inheritdoc />
        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user)
        {
            await _appState.Log("[WASM Authentication] ChangePassword() START");

            ScalarModel result = new();

            try
            {
                // Configure for cloud operation only
                if (db_para.ContainsKey(DB_CMD.NO_CLOUD))
                    db_para.Remove(DB_CMD.NO_CLOUD);

                if (!db_para.ContainsKey(DB_CMD.NO_LOCAL))
                    db_para.Add(DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString());

                await _appState.Log("[WASM Authentication] ChangePassword() db_para (cloud):", data: db_para);

                var dam = _serviceProvider.GetRequiredService<IDamBase>();
                result = await dam.ChangePassword(db_para)!;

                await _appState.Log("[WASM Authentication] ChangePassword() result (cloud):", data: result);

                // Local password change for native platforms (not WASM)
                var platform = _platform.GetCurrPlatform();
                await _appState.Log($"[WASM Authentication] ChangePassword() platform={platform}, StorageLocation={_appState.StorageLocation}");

                bool isNativePlatform = platform != PLATFORMS.WASM && platform != PLATFORMS.WINDOWS_SERVER;
                bool isHybridOrLocal = _appState.StorageLocation == STORAGE_LOCATION.CLOUD_LOCAL ||
                                       _appState.StorageLocation == STORAGE_LOCATION.LOCAL;

                if (isNativePlatform && isHybridOrLocal)
                {
                    if (string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str) &&
                        result.out_value_str.StartsWith("updated:"))
                    {
                        // Configure for local operation only
                        if (!db_para.ContainsKey(DB_CMD.NO_CLOUD))
                            db_para.Add(DB_CMD.NO_CLOUD, DB_CMD.NO_CLOUD.ToString());

                        if (db_para.ContainsKey(DB_CMD.NO_LOCAL))
                            db_para.Remove(DB_CMD.NO_LOCAL);

                        await _appState.Log("[WASM Authentication] ChangePassword() db_para (local):", data: db_para);

                        result = await dam.ChangePassword(db_para)!;
                        await _appState.Log("[WASM Authentication] ChangePassword() result (local):", data: result);
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[WASM Authentication] ChangePassword() ERROR: {ex.Message}");
                throw;
            }

            await _appState.Log("[WASM Authentication] ChangePassword() END");
            return result;
        }

        /// <inheritdoc />
        public async Task<bool> IsTokenValidAsync(string token)
        {
            try
            {
                var httpClient = _httpClient; // Bereits injectiert

                if (httpClient != null)
                {
                    var _globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();

                    using var request = new HttpRequestMessage(HttpMethod.Post, _globalState.ConfigWebapi.url_Authorizedconnection);

                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    // Optional: Body leeren oder ein Dummy-Objekt senden
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
                // Bei Netzwerkfehlern → Token als ungültig behandeln
                return false;
            }
        }
    }
}