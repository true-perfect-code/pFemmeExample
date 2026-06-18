using BlazorCore.Services.Apis;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace TestSolution4.Shared.Services.Authentication
{
    public class Authentication : AuthenticationStateProvider, Shared.Services.Authentication.IAuthentication
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Shared.Services.Platform.IPlatform _platform;
        private readonly Shared.Services.AppState.IAppState _appState;
        //private readonly Shared.Services.GlobalState.IGlobalState _globalState;
        private readonly HttpClient _httpClient;

        public Authentication(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _platform = serviceProvider.GetRequiredService<TestSolution4.Shared.Services.Platform.IPlatform>();
            _appState = serviceProvider.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>();
            //_globalState = _serviceProvider.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>();
            _httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        }

        public async Task<ScalarModel?> Login(string token)
        {
            if (_appState.Catalog.LocalStorage == null) return new ScalarModel();

            ScalarModel? result = new();
            try
            {
                result = await _platform!.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                if (result != null && string.IsNullOrEmpty(result.out_err))
                {
                    result = await _platform!.SetAsync(_appState.Catalog.LocalStorage.oauth_token, token);
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

        public async Task Logout()
        {
            if (_appState.Catalog.LocalStorage == null) return;

            await _platform!.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);

            _appState.ResetUserCredential(); // Benutzerinformationen zurücksetzen
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            await _appState.Log("[WASM Authentication] START GetAuthenticationStateAsync");
            var identity = new ClaimsIdentity();
            try
            {
                // Security Storage vom Gerät auslesen
                if (_appState.Catalog.LocalStorage != null)
                {
                    var oauthToken = await _platform!.GetAsync(_appState.Catalog.LocalStorage.oauth_token);
                    await _appState.Log($"[WASM Authentication] oauthToken erhalten: Success={string.IsNullOrEmpty(oauthToken?.out_err)}, HasValue={!string.IsNullOrEmpty(oauthToken?.out_value_str)}");
                    if (oauthToken != null && string.IsNullOrEmpty(oauthToken.out_err) && !string.IsNullOrEmpty(oauthToken.out_value_str))
                    {
                        // Seucity Storage Eintrag deserialisieren
                        var tokenData = System.Text.Json.JsonSerializer.Deserialize(
                            oauthToken.out_value_str,
                            BlazorCore.JsonContext.Default.ClientStorageModel
                        );
                        await _appState.Log($"[WASM Authentication] tokenData: {tokenData}");

                        // Deserialisierten Eintrag in die userMana platzieren (Email AuthUsers_ID)
                        if (tokenData != null)
                        {
                            if (String.IsNullOrEmpty(tokenData.out_err))
                            {
                                bool InternetConnected = _appState.IsCloudConnected;
                                if (InternetConnected && string.IsNullOrEmpty(tokenData.WebApiToken))
                                {
                                    await Logout();
                                }
                                else
                                {
                                    // Prüfen, ob Token gültig
                                    bool isValid = await _platform.IsTokenValidAsync(tokenData.WebApiToken);
                                    if (isValid || tokenData.WebApiToken == API_CONST.TOKEN_LOCAL_ONLY)
                                    {
                                        string? unixts = tokenData.UnixTS;

                                        _appState.UpdateWebApiToken(tokenData.WebApiToken);
                                        _appState.UpdateUnixTS(unixts);
                                        _appState.UpdateUserAccount(tokenData.Email);
                                        _appState.UpdateIsAuthenticated(true);

                                        var claims = new[] { new Claim(ClaimTypes.Name, unixts) };
                                        identity = new ClaimsIdentity(claims, (InternetConnected ? "Server authentication" : "Client authentication"));
                                    }
                                    else
                                    {
                                        await Logout();
                                    }
                                }

                                tokenData = null;

                                if (!identity.IsAuthenticated)
                                    _appState.ResetUserCredential();
                            }
                            else
                                await _platform!.RemoveAsync(_appState.Catalog.LocalStorage.oauth_token);
                        }
                    }
                    else
                    {
                        //if (oauthToken != null && !string.IsNullOrEmpty(oauthToken.out_err))
                        //{
                        //    await _messageBoxService!.ShowOkAsync(
                        //        title: _appState?.T("Error") ?? "Error",
                        //        message: $"{_appState?.T(pE.Utility.Appl.DefaultErrorText)} {oauthToken.out_err}"
                        //    );
                        //}

                        _appState.ResetUserCredential();

                        // Erzwinge eine Garbage Collection und warte darauf
                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        identity = new ClaimsIdentity();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                await _appState.Log($"[WASM Authentication] ERROR GetAuthenticationStateAsync: {ex.ToString()}");
                //Console.WriteLine("Request failed:" + ex.ToString());
            }

            var authState = new AuthenticationState(new ClaimsPrincipal(identity));
            // Nur loggen, ob der User authentifiziert ist, statt das ganze Objekt zu senden
            await _appState.Log($"[WASM Authentication] END GetAuthenticationStateAsync - IsAuthenticated: {authState.User.Identity?.IsAuthenticated}");
            NotifyAuthenticationStateChanged(Task.FromResult(authState)); // Benachrichtigung
            return authState; // Gib dieselbe Instanz zurück
        }

        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user)
        {
            await _appState!.Log("[BLAZOR WASM Authentication ChangePassword()] START");

            ScalarModel result = new();

            try
            {
                // Auf dem Cloud-Server das Passwort ändern
                if (db_para.ContainsKey(DB_CMD.NO_CLOUD))
                    db_para.Remove(DB_CMD.NO_CLOUD);
                if (!db_para.ContainsKey(DB_CMD.NO_LOCAL))
                    db_para.Add(DB_CMD.NO_LOCAL, string.Empty); // Nicht lokal ausführen

                await _appState!.Log("[BLAZOR WASM Authentication ChangePassword()] db_para (cloud):", data: db_para);

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();
                result = await _dam!.ChangePassword(db_para)!;
                await _appState!.Log("[BLAZOR WASM Authentication ChangePassword()] result (cloud):", data: result);

                // Lokal das Passwort ändern, wenn:
                // - auf dem Cloud-Server kein Fehler aufgetreten ist
                // - kein WASM lokal
                // - Speichermodus CLOUD_LOCAL oder LOCAL
                var platform = _platform!.GetCurrPlatform();
                await _appState!.Log($"[BLAZOR WASM Authentication ChangePassword()] platform={platform}, StorageLocation={_appState!.StorageLocation}");
                if (platform != BlazorCore.Services.Platform.PLATFORMS.WASM
                    && platform != BlazorCore.Services.Platform.PLATFORMS.WINDOWS_SERVER
                    && (_appState!.StorageLocation == BlazorCore.Services.AppState.STORAGE_LOCATION.CLOUD_LOCAL || _appState!.StorageLocation == BlazorCore.Services.AppState.STORAGE_LOCATION.LOCAL))
                {
                    if (string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
                    {
                        if (result.out_value_str.StartsWith("updated:"))
                        {
                            // Lokal das Passwort ändern
                            if (!db_para.ContainsKey(DB_CMD.NO_CLOUD))
                                db_para.Add(DB_CMD.NO_CLOUD, string.Empty); // Nicht auf dem Cloud ausführen
                            if (!db_para.ContainsKey(DB_CMD.NO_LOCAL))
                                db_para.Remove(DB_CMD.NO_LOCAL);

                            await _appState!.Log("[BLAZOR WASM Authentication ChangePassword()] db_para (local):", data: db_para);
                            result = await _dam!.ChangePassword(db_para)!;
                            await _appState!.Log("[BLAZOR WASM Authentication ChangePassword()] result (local):", data: result);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            await _appState!.Log("[BLAZOR WASM Authentication ChangePassword()] END");
            return result;
        }
    }
}