#pragma warning disable CA1416
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using BlazorCore.Services.Apis;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using System.Security.Claims;

namespace TestSolution4.Web.Services
{
    public class Authentication : RevalidatingServerAuthenticationStateProvider, TestSolution4.Shared.Services.Authentication.IAuthentication
    {
        private readonly IServiceProvider _serviceProvider;
        private AuthenticationState? _cachedAuthState; // Cache für Authentifizierungsstatus
        private string _lastUserUnixTS; // Cache für letzte Benutzer-UnixTS

        public Authentication(ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _lastUserUnixTS = string.Empty;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromDays(30);

        public async Task<ScalarModel?> Login(string token)
        {
            ScalarModel? result = new();

            await Task.Delay(10);

            result.out_value_bool = true;

            return result;
        }
                
        public async Task Logout()
        {
            IHttpContextAccessor? _httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
            TestSolution4.Shared.Services.AppState.IAppState? _appState = _serviceProvider.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>();

            await Task.Delay(10);           

            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            // AppState und Cache zurücksetzen
            _appState.ResetUserCredential();
            _cachedAuthState = null;
            _lastUserUnixTS = string.Empty;
        }
                
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            TestSolution4.Shared.Services.AppState.IAppState? _appState = _serviceProvider.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>();

            _appState!.RoutesStateHasChanged(); // Wegen der Anzeige vom Loading-Status auf Routes.razor

            IHttpContextAccessor? _httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
            TestSolution4.Shared.Services.GlobalState.IGlobalState? _globalState = _serviceProvider.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>();
            IDamBase? _dam = _serviceProvider.GetRequiredService<IDamBase>();
            

            // Prüfe den Cache (unverändert)
            if (_cachedAuthState != null && _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return _cachedAuthState;
            }

            var identity = new ClaimsIdentity();

            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
                    var os = ParseOperatingSystem(userAgent!); // Eigene Logik oder Library
                    string? issuer = null;
                    string? sub = null;
                    string? unixTS = null;

                    // --- GEÄNDERT: Claim-Abfrage für Apple-Kompatibilität ---
                    var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                        ?? user.FindFirst("sub")  // Apple verwendet manchmal "sub"
                        ?? user.FindFirst(ClaimTypes.Email);  // Fallback für Apple (falls "sub" fehlt)

                    if (subClaim != null)
                    {
                        var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);
                        if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
                        {
                            var pepperFilePath = resultConfigurationFile.out_value_str;
                            try
                            {
                                issuer = subClaim.Issuer;
                                byte[]? Pepper;
                                using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                {
                                    Pepper = aes.GetPepper(pepperFilePath);
                                    sub = aes.HashUsername(subClaim.Value, Pepper!);
                                }

                                unixTS = BlazorCore.UnixTsGeneratorWebApi.Generate(_globalState.ConfigGeneral);

                                // Rest der Methode (unverändert) -->
                                if (string.IsNullOrEmpty(_lastUserUnixTS))
                                {
                                    var db_para = new Dictionary<string, string>
                                    {
                                        { "@Case_", "SelectAuthUsersEmail" },
                                        { "@EmailHash", sub },
                                        { "@PasswordHash", sub },
                                        { DB_CMD.NO_LOCAL, string.Empty },
                                    };

                                    if (!_dam.IsInitializeSql())
                                        _dam.InitializeSql();
                                    var result = await _dam.Scalar(db_para);


                                    if (result != null && string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
                                    {
                                        if (result.out_value_str != "no_user")
                                        {
                                            unixTS = result.out_value_str;
                                        }
                                        else
                                        {
                                            // Kein Benutzer, dann registrieren
                                            db_para = new()
                                            {
                                                { "@Case_", "Register>>AuthUsers" },
                                                { "@UnixTS", unixTS },
                                                { "@EmailHash", sub },
                                                { "@PasswordHash", sub },
                                                { "@Int__Registration", "1" },
                                                { "@Int__TwoFA", "0" },
                                                { "@active", "1" },
                                                { "@IdP", "Web-" + os + "-" + issuer.ToLower() }
                                                //{ DB_CMD.NO_LOCAL, string.Empty }, // Wird nicht benötigt, wir wollen auch loikalen Benutzer erstellen
                                            };
                                            result = await _dam.ExecQuery(db_para);
                                        }
                                    }
                                    else
                                    {
                                        // ERROR
                                        //Console.WriteLine($"[DB Error] Select query failed: {result?.Error ?? "Unknown error"}");
                                    }

                                    if (result != null && !string.IsNullOrEmpty(result.out_value_str))
                                        _lastUserUnixTS = result.out_value_str;

                                }
                                else
                                {
                                    unixTS = _lastUserUnixTS;
                                }

                                if (!string.IsNullOrEmpty(unixTS))
                                {
                                    var claims = new List<Claim>(user.Claims);
                                    claims.Add(new Claim("unix_ts", unixTS ?? BlazorCore.UnixTsGeneratorWebApi.Generate(_globalState.ConfigGeneral)));

                                    var expiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                                    claims.Add(new Claim(ClaimTypes.Expiration, expiresUtc.ToString("O")));

                                    identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                }
                            }
                            catch
                            {
                                // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + ex.Message);
                            }
                        }
                        // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + resultConfigurationFile.out_err);
                    }
                    else
                    {
                        // Fall 2: Eigene Anmeldung (unverändert)
                        var unixTsClaim = user.FindFirst("unix_ts");
                        unixTS = unixTsClaim?.Value ?? string.Empty;

                        if (!string.IsNullOrEmpty(unixTS))
                        {
                            identity = new ClaimsIdentity(user.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            if(string.IsNullOrEmpty(unixTS) && !string.IsNullOrEmpty(_lastUserUnixTS))
                                unixTS = _lastUserUnixTS;
                        }
                    }

                    if (!string.IsNullOrEmpty(unixTS))
                    {
                        _appState.UpdateUnixTS(unixTS ?? string.Empty);
                        _appState.UpdateIsAuthenticated(true);
                    }
                    else
                    {
                        _appState.ResetUserCredential();
                    }
                }
                else
                {
                    _appState.ResetUserCredential();
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    //Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                _appState.ResetUserCredential();
            }

            var authState = new AuthenticationState(new ClaimsPrincipal(identity));
            _cachedAuthState = authState;
            return authState;
        }

        protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        // Hilfsklasse zum Vergleichen von Claims
        private class ClaimEqualityComparer : IEqualityComparer<Claim>
        {
            public bool Equals(Claim x, Claim y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.Type == y.Type && x.Value == y.Value;
            }

            public int GetHashCode(Claim obj)
            {
                return (obj.Type + obj.Value).GetHashCode();
            }
        }

        private string ParseOperatingSystem(string userAgent)
        {
            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                return "Windows";
            else if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
                return "macOS";
            else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                return "Linux";
            else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
                return "iOS";
            else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                return "Android";
            else
                return "Unknown";
        }


        public async Task<ScalarModel> ChangePassword(Dictionary<string, string> db_para, string oldpassword, string newpassword, string user)
        {
            ScalarModel result = new();

            try
            {
                TestSolution4.Shared.Services.GlobalState.IGlobalState? _globalState = _serviceProvider.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>();
                IDamBase? _dam = _serviceProvider.GetRequiredService<IDamBase>();

                // 1. Pepper auslesen => Achtung: darf aus Sicherheitsgründen nur auf dem Server erfolgen!!
                byte[]? Pepper = null;

                var resultConfigurationFile = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(_globalState.ConfigGeneral);
                if (resultConfigurationFile != null && string.IsNullOrEmpty(resultConfigurationFile.out_err) && !string.IsNullOrEmpty(resultConfigurationFile.out_value_str))
                {
                    var pepperFilePath = resultConfigurationFile.out_value_str;
                    try
                    {
                        // Pepper setzen
                        using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                        {
                            // Hiermit einmalig verschlüsselten Papper generieren und in der Datei C:\...\_Connections\security.config.json speichern 
                            Pepper = aes.GetPepper(pepperFilePath);
                        }
                    }
                    catch
                    {
                        // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + ex.Message);
                    }
                }

                if (Pepper != null)
                {
                    ChangePasswordModel ChangePassword = new();

                    if (db_para.ContainsKey("@EmailHash"))
                    {
                        ChangePassword.Username = _globalState.ConvertStrPara<string>(db_para["@EmailHash"]);
                        if (db_para.ContainsKey("@PasswordHash"))
                        {
                            ChangePassword.OldUserpassword = _globalState.ConvertStrPara<string>(db_para["@PasswordHash"]);
                            if (db_para.ContainsKey("@PasswordHashNew"))
                            {
                                ChangePassword.NewUserpassword = _globalState.ConvertStrPara<string>(db_para["@PasswordHashNew"]);
                                using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                {
                                    db_para["@EmailHash"] = aes.HashUsername(ChangePassword.Username, Pepper!); // Email hashen
                                    db_para["@PasswordHash"] = aes.HashCredentials(ChangePassword.OldUserpassword, ChangePassword.Username, Pepper!); // Passwort alt hashen
                                    db_para["@PasswordHashNew"] = aes.HashCredentials(ChangePassword.NewUserpassword, ChangePassword.Username, Pepper!); // Passwort neu hashen
                                }
                            }
                        }
                    }

                    if (db_para.ContainsKey("@EmailHash") && db_para.ContainsKey("@PasswordHash") && db_para.ContainsKey("@PasswordHashNew"))
                    {
                        result = await _dam!.Save(db_para)!;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }
    }
}
#pragma warning restore CA1416