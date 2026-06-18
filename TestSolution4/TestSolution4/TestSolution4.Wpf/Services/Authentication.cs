using BlazorCore.Services.Apis;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;

namespace TestSolution4.Wpf.Services
{
    public class Authentication : AuthenticationStateProvider, Shared.Services.Authentication.IAuthentication
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Shared.Services.Platform.IPlatform _platform;
        private readonly Shared.Services.AppState.IAppState _appState;
        private AuthenticationState _cachedState;

        public Authentication(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _platform = serviceProvider.GetRequiredService<Shared.Services.Platform.IPlatform>();
            _appState = serviceProvider.GetRequiredService<Shared.Services.AppState.IAppState>();
            _cachedState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public async Task<ScalarModel?> Login(string token)
        {
            if (_appState.Catalog.LocalStorage == null) return new ScalarModel();

            ScalarModel? result = new();
            try
            {
                // Altes Token entfernen und neues setzen (wie in WASM)
                await _platform.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                result = await _platform.SetAsync(_appState.Catalog.LocalStorage.oauth_token, token);

                if (result != null && string.IsNullOrEmpty(result.out_err))
                {
                    result.out_value_bool = true;
                    // UI informieren
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                }
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }
            return result;
        }

        public async Task Logout()
        {
            if (_appState.Catalog.LocalStorage == null) return;

            await _platform.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
            _appState.ResetUserCredential();

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            _cachedState = new AuthenticationState(anonymous);

            NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            try
            {
                if (_appState.Catalog.LocalStorage != null)
                {
                    // 1. Token aus lokalem Storage (Windows Dateisystem/Registry via IPlatform)
                    var oauthToken = await _platform.GetAsync(_appState.Catalog.LocalStorage.oauth_token);

                    if (oauthToken != null && !string.IsNullOrEmpty(oauthToken.out_value_str))
                    {
                        var tokenData = JsonSerializer.Deserialize<ClientStorageModel>(
                            oauthToken.out_value_str,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (tokenData != null)
                        {
                            // 2. Token Validierung (Web-API Proxy)
                            bool isValid = await _platform.IsTokenValidAsync(tokenData.WebApiToken);

                            if (isValid || tokenData.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                            {
                                _appState.UpdateWebApiToken(tokenData.WebApiToken);
                                _appState.UpdateUnixTS(tokenData.UnixTS);
                                _appState.UpdateUserAccount(tokenData.Email);
                                _appState.UpdateIsAuthenticated(true);

                                var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, tokenData.Email ?? "User"),
                                new Claim("unix_ts", tokenData.UnixTS ?? ""),
                                new Claim("auth_type", "WPF-Native")
                            };

                                identity = new ClaimsIdentity(claims, "WPFAuth");
                            }
                            else
                            {
                                await Logout();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                _appState.ResetUserCredential();
            }

            _cachedState = new AuthenticationState(new ClaimsPrincipal(identity));
            return _cachedState;
        }

        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user)
        {
            // Hier nutzen wir die Logik von WASM: Erst Cloud (via API), dann lokal (SQLite)
            ScalarModel result = new();
            var _dam = _serviceProvider.GetRequiredService<IDamBase>();

            // Cloud Sync
            if (!db_para.ContainsKey(DB_CMD.NO_LOCAL))
                db_para.Add(DB_CMD.NO_LOCAL, string.Empty);

            result = await _dam.ChangePassword(db_para);

            if (string.IsNullOrEmpty(result?.out_err) && result?.out_value_str != "not_updated:0:0")
            {
                // Lokales SQLite Update (WPF Debugger nutzt lokales File)
                db_para.Remove(DB_CMD.NO_LOCAL);
                if (!db_para.ContainsKey(DB_CMD.NO_CLOUD))
                    db_para.Add(DB_CMD.NO_CLOUD, string.Empty);

                result = await _dam.ChangePassword(db_para);
            }

            return result ?? new ScalarModel { out_err = "Unknown error" };
        }
    }
}
