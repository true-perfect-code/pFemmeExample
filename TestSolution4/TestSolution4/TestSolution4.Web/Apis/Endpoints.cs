//#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using BlazorCore.Services.ServerShared;
using TestSolution4.Shared.Global;
using System.Security.Claims;

namespace TestSolution4.Web.Apis
{
    public class ImageUploadDto
    {
        public string? Image { get; set; }
    }

    public class NotificationItem
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Identifier { get; set; }
        public string? UserID { get; set; }
        public long Timestamp { get; set; }
    }

    public static class Endpoints
    {
        static string pepperFilePath = string.Empty;
        static byte[]? Pepper;

        public static void SetPepper(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            //var _globalState = scope.ServiceProvider.GetRequiredService<TestSolution4.Shared.Services.GlobalState.IGlobalState>();
            //var _appState = scope.ServiceProvider.GetRequiredService<TestSolution4.Shared.Services.AppState.IAppState>();

            //string pepperFilePath = _globalState.SecurityConfigFile;
            var result = BlazorCore.ServerConfiguration.GetSecurityConfigurationFile(Configuration.ConfigGeneral);
            if (result != null && string.IsNullOrEmpty(result.out_err) && !string.IsNullOrEmpty(result.out_value_str))
            {
                var pepperFilePath = result.out_value_str;
                try
                {
                    using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                    {
                        // Hiermit einmalig verschlüsselten Papper generieren und in der Datei C:\...\_Connections\security.config.json speichern 
                        //string encryptedPepper = aes.GenerateEncryptedPepper();
                        Pepper = aes.GetPepper(pepperFilePath);
                    }
                }
                catch
                {
                    // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + ex.Message);
                }
            }
            // TODO: Log("Die Verbindung zum SQL Server ist fehlgeschlagen. Error = " + result.out_err);
        }

        public static void MapEndPoints(this WebApplication app)
        {
            app.MapGet("/login/google", async (HttpContext context) =>
            {
                //Console.WriteLine("[LOGIN/GOOGLE] Initiating Google login");
                await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties
                {
                    RedirectUri = "/" // Direkt zur Startseite nach erfolgreicher Anmeldung
                });
            }).AllowAnonymous();

            app.MapGet("/login/microsoft", async (HttpContext context) =>
            {
                //Console.WriteLine("[LOGIN/GOOGLE] Initiating Google login");
                await context.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme, new AuthenticationProperties
                {
                    RedirectUri = "/" // Direkt zur Startseite nach erfolgreicher Anmeldung
                });
            }).AllowAnonymous();

            app.MapGet("/login/apple", async (HttpContext context) =>
            {
                //Console.WriteLine("[LOGIN/APPLE] Initiating Apple login");
                await context.ChallengeAsync("Apple", new AuthenticationProperties
                {
                    RedirectUri = "/" // Direkt zur Startseite nach erfolgreicher Anmeldung
                });
            }).AllowAnonymous();

            app.MapPost("/api/login", async (HttpContext context,
                BlazorCore.Services.Dam.IDamBase _dam,
                BlazorCore.Services.SqlClient.ISqlClientBase sqlClient,
                BlazorCore.Services.AppState.IAppStateBase _appState,
                BlazorCore.Services.GlobalState.IGlobalStateBase _globalState,
                BlazorCore.Services.Platform.IPlatformBase platform) =>
            {
                try
                {
                    //RegistModel regist = new();
                    BlazorCore.Services.Authentication.LoginModel login = new();

                    var username = context.Request.Form["account"];
                    login.Account = string.IsNullOrEmpty(username) ? "" : username!;

                    var password = context.Request.Form["password"];
                    login.Password = string.IsNullOrEmpty(password) ? "" : password!;

                    var otpuserdigitinput = context.Request.Form["otpuserdigitinput"];
                    login.OtpUserDigitInput = string.IsNullOrEmpty(otpuserdigitinput) ? "" : otpuserdigitinput!;

                    var otpbackupcode = context.Request.Form["otpbackupcode"];
                    login.OtpBackupCode = string.IsNullOrEmpty(otpbackupcode) ? "" : otpbackupcode!;

                    if (!String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password))
                    {
                        login.Register = context.Request.Form.ContainsKey("register");
                        login.Termseula = context.Request.Form.ContainsKey("termseula");

                        if (!String.IsNullOrEmpty(login.Account) && !String.IsNullOrEmpty(login.Password))
                        {
                            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                            {
                                login.Password = aes.HashCredentials(login.Password, login.Account, Pepper!);
                                login.Account = aes.HashUsername(login.Account, Pepper!);
                            }

                            BlazorCore.Services.SqlClient.ScalarModel result = new();
                            //string unixTS = _appState.GenerateUniqueId();
                            string unixTS = BlazorCore.UnixTsGeneratorWebApi.Generate(Configuration.ConfigGeneral);

                            if (login.Register) // Registrierung
                            {
                                // R E G I S T R I E R U N G
                                Dictionary<string, string> db_para = new()
                                {
                                    { "@Case_", "Register>>AuthUsers" },
                                    { "@UnixTS", login.Register ? unixTS : "0" },
                                    { "@EmailHash", login.Account },
                                    { "@PasswordHash", login.Password },
                                    { "@Int__Registration", login.Register ? "1" : "0" },
                                    { "@TermsAccepted", login.Termseula ? "1" : "0" },
                                    { "@IdP", "tpc" },
                                    { "@active", "1" }
                                };
                                result = await _dam!.ExecQuery(db_para)!;
                            }
                            else // Login
                            {
                                // Prüfen, ob 2FA aktiviert ist
                                Dictionary<string, string> db_para = new()
                                {
                                    { "@Case_", "CheckAccount>>AuthUsers" },
                                    { "@EmailHash", login.Account },
                                    { "@PasswordHash", login.Password },
                                    //{ "@AuthUsers_UnixTS", AuthUsers_UnixTS },
                                    { "@OtpBackupCode", login.OtpBackupCode }
                                };
                                BlazorCore.Services.SqlClient.ScalarModel resultOtpCheck = await _dam.Scalar(db_para);
                                if (resultOtpCheck.out_value_bool)
                                {
                                    // Wenn 2FA aktiviert ist, dann müssen entweder 6-stelliger otp-Usercode oder Backupcode vorhanden sein
                                    if (string.IsNullOrEmpty(login.OtpUserDigitInput) && string.IsNullOrEmpty(login.OtpBackupCode))
                                    {
                                        //context.Response.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_userotp");
                                        return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_userotp");
                                    }
                                    else
                                    {
                                        // Wenn 6-stellige otp-Benutzereingabe, dann 2FA prüfen
                                        if (!string.IsNullOrEmpty(login.OtpUserDigitInput))
                                        {
                                            db_para["@Case_"] = "SelectOtp>>AuthUsers";
                                            BlazorCore.Services.SqlClient.ScalarModel resultSelectOtp = await _dam.Scalar(db_para);
                                            if (resultSelectOtp != null && resultSelectOtp.out_value_str != null && String.IsNullOrEmpty(resultSelectOtp.out_err))
                                            {
                                                switch (resultSelectOtp.out_value_str)
                                                {
                                                    case "no_user":
                                                        return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_user");


                                                    case "locked":
                                                        return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/locked");

                                                    case "no_otp":
                                                    case "":
                                                        return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/error_no_otp_empty");

                                                    default: // OTP Code zurückgeliefert
                                                        bool verifyTotp = VerifyTotp(resultSelectOtp.out_value_str, login.OtpUserDigitInput);
                                                        if (verifyTotp) // 2FA Authentifizierung erfolgreich
                                                        {
                                                            db_para["@Case_"] = "ResetLoginAttempts>>AuthUsers";
                                                            BlazorCore.Services.SqlClient.ScalarModel resultReset = await _dam.ExecQuery(db_para)!;
                                                            if (resultReset != null && !string.IsNullOrEmpty(resultReset.out_err))
                                                            {
                                                                return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/error_resetloginattempts");
                                                            }
                                                        }
                                                        else // 2FA Authentifizierung fehlgeschlagen
                                                        {
                                                            return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/{BlazorCore.MSG_CODES.verifytotp_failed.ToString()}");
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/error_selectotp");
                                            }
                                        }

                                        // Wenn Benutzer Backupcode vorhanden, dann 2FA zurücksetzen
                                        if (!string.IsNullOrEmpty(login.OtpBackupCode))
                                        {
                                            db_para["@Case_"] = "DeleteOtp>>AuthUsers";
                                            BlazorCore.Services.SqlClient.ScalarModel resultDeleteOtp = await _dam.ExecQuery(db_para);
                                            // Ausführung (und somit die Anmeldung) darf hier nur dann fortgesetzt werden, wenn backupcode+Account+Passwort stimmen!
                                            if (resultDeleteOtp != null && resultDeleteOtp.out_value_str != null && string.IsNullOrEmpty(resultDeleteOtp.out_err))
                                            {
                                                if (!resultDeleteOtp.out_value_str.Contains("updated:"))
                                                    return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/{BlazorCore.MSG_CODES.deleteotp_failed.ToString()}");
                                            }
                                            else
                                                return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/{BlazorCore.MSG_CODES.deleteotp_failed.ToString()}");
                                        }
                                    }
                                }

                                // A N M E L D U N G   ( L O G I N )
                                db_para = new()
                                {
                                    { "@Case_", "SelectAuthUsersEmail" },
                                    { "@EmailHash", login.Account },
                                    { "@PasswordHash", login.Password }
                                };
                                result = await _dam!.Scalar(db_para)!;
                            }

                            if (result == null)
                                return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_result");
                            else
                            {
                                if (!string.IsNullOrEmpty(result.out_err))
                                    return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/?error=" + result.out_err);
                                else
                                {
                                    if (string.IsNullOrEmpty(result.out_value_str))
                                        return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_result");
                                    else
                                    {
                                        if (result.out_value_str == "record_exists_no_adding")
                                            return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/record_exists_no_adding");
                                        else
                                        {
                                            if (result.out_value_str.Length > 30)
                                            {
                                                try
                                                {
                                                    var expiresUtc = DateTimeOffset.UtcNow.AddDays(30);

                                                    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                                                    identity.AddClaim(new Claim("unix_ts", result.out_value_str));
                                                    identity.AddClaim(new Claim(ClaimTypes.Expiration, expiresUtc.ToString("O")));

                                                    var claims = new ClaimsPrincipal(identity);

                                                    await context.SignInAsync(
                                                        CookieAuthenticationDefaults.AuthenticationScheme,
                                                        claims,
                                                        new AuthenticationProperties
                                                        {
                                                            IsPersistent = true,
                                                            IssuedUtc = DateTimeOffset.UtcNow,
                                                            ExpiresUtc = expiresUtc
                                                        });

                                                    return Results.Redirect(Configuration.ConfigGeneral.ApplicationUrl);
                                                }
                                                catch (Exception ex)
                                                {
                                                    return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/" + ex.Message);
                                                }
                                            }
                                            else
                                                return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_user");
                                        }
                                    }
                                }
                            }
                        }
                        else
                            return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_entry");
                    }
                    else
                    {
                        return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/no_entry");
                    }
                }
                catch (Exception ex)
                {
                    return Results.Redirect($"{Configuration.ConfigGeneral.ApplicationUrl}/" + ex.Message);
                }
            }).AllowAnonymous().RequireRateLimiting("LoginRateLimit");

            app.MapPost("/api/logout", async (HttpContext context, TestSolution4.Shared.Services.Authentication.IAuthentication auth, HttpClient httpClient, TestSolution4.Shared.Services.GlobalState.IGlobalState _globalState) =>
            {
                //Console.WriteLine($"[Logout Endpoint] Called at {DateTimeOffset.UtcNow:HH:mm:ss.fff}");
                try
                {
                    bool isGoogleLogin = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value != null && context.User.FindFirst("sub") != null;
                    bool isMicrosoftLogin = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value != null && context.User.FindFirst("oid") != null;
                    bool isAppleLogin = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value != null && context.User.FindFirst("sub") != null &&
                                      context.User.Identity?.AuthenticationType == "Apple";

                    if (isGoogleLogin)
                    {
                        //Console.WriteLine("[Logout Endpoint] Detected Google login, attempting to revoke access token");
                        string? accessToken = null;
                        try
                        {
                            accessToken = context.User.FindFirst(".access_token")?.Value;
                            if (string.IsNullOrEmpty(accessToken))
                            {
                                var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                accessToken = authResult?.Properties?.GetTokenValue("access_token");
                            }
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                var content = new FormUrlEncodedContent(new[]
                                {
                                    new KeyValuePair<string, string>("token", accessToken)
                                });
                                var response = await httpClient.PostAsync("https://oauth2.googleapis.com/revoke", content);
                            }
                            else
                            {
                                //Console.WriteLine("[Logout Endpoint] No Google access token found to revoke");
                            }
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"[Logout Endpoint] Error during Google token revoke: {ex.Message}");
                        }
                    }
                    else if (isMicrosoftLogin)
                    {
                        //Console.WriteLine("[Logout Endpoint] Detected Microsoft login, attempting to revoke access token");
                        string? accessToken = null;
                        try
                        {
                            accessToken = context.User.FindFirst(".access_token")?.Value;
                            if (string.IsNullOrEmpty(accessToken))
                            {
                                var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                accessToken = authResult?.Properties?.GetTokenValue("access_token");
                            }
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                var content = new FormUrlEncodedContent(new[]
                                {
                                    new KeyValuePair<string, string>("token", accessToken)
                                });
                                var response = await httpClient.PostAsync("https://graph.microsoft.com/v1.0/me/revokeSignInSessions", content);
                                //Console.WriteLine(response.IsSuccessStatusCode
                                //    ? "[Logout Endpoint] Microsoft access token revoked successfully"
                                //    : $"[Logout Endpoint] Failed to revoke Microsoft access token: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                            }
                            else
                            {
                                //Console.WriteLine("[Logout Endpoint] No Microsoft access token found to revoke");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Logout Endpoint] Error during Microsoft token revoke: {ex.Message}");
                        }
                    }
                    else if (isAppleLogin)
                    {
                        //Console.WriteLine("[Logout Endpoint] Detected Apple login, performing local logout only");
                        // Apple bietet keine Token-Revocation-API an, daher nur lokaler Logout
                    }
                    else
                    {
                        //Console.WriteLine("[Logout Endpoint] Detected local login, skipping token revoke");
                    }

                    //Console.WriteLine("[Logout Endpoint] Signing out local cookie");
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    //Console.WriteLine("[Logout Endpoint] Calling IAuthentication.Logout");
                    await auth.Logout();

                    //Console.WriteLine("[Logout Endpoint] Redirecting to Appl.WebUrl");
                    context.Response.Redirect(Configuration.ConfigGeneral.ApplicationUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Logout Endpoint] Error: {ex.Message}");
                    context.Response.Redirect(Configuration.ConfigGeneral.ApplicationUrl);
                }
            }).AllowAnonymous();

            app.MapPost("text", async (HttpContext context) =>
            {
                var text = await context.Request.ReadFromJsonAsync<string>();
                if (text != null)
                {
                    //Console.WriteLine($"Received text: {text}");
                    await context.Response.WriteAsync("Text received successfully");
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid input");
                }
            });

            app.MapPost("/api/upload", async (HttpContext context) =>
            {
                try
                {
                    var form = await context.Request.ReadFormAsync();
                    var file = form.Files.FirstOrDefault();

                    if (file != null && file.Length > 0)
                    {
                        var baseDirectory = Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                        var uploadDirectory = Path.Combine(baseDirectory!, "_Uploads", Configuration.ConfigGeneral.ApplicationName);

                        if (!Directory.Exists(uploadDirectory))
                        {
                            Directory.CreateDirectory(uploadDirectory);
                        }

                        var filePath = Path.Combine(uploadDirectory, file.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        await context.Response.WriteAsync("File uploaded successfully");
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Invalid input");
                    }
                }
                catch
                {
                    throw;
                }
            });

            app.MapGet("/api/notification", async (HttpContext context) =>
            {
                var timestampStr = context.Request.Query["timestamp"].ToString();
                var userId = context.Request.Query["userId"].ToString();

                if (long.TryParse(timestampStr, out long timestamp) && !string.IsNullOrEmpty(userId))
                {
                    var startTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

                    var notifications = new List<NotificationItem>
                    {
                        new NotificationItem
                        {
                            Title = "Neue Nachricht",
                            Body = "Sie haben eine neue Nachricht erhalten",
                            Identifier = "https://ihre-domain.com/messages/123",
                            UserID = "12345",
                            Timestamp = new DateTimeOffset(new DateTime(Configuration.ConfigGeneral.EpochYear, 1, 26, 13, 0, 0)).ToUnixTimeSeconds()
                        },
                        new NotificationItem
                        {
                            Title = "Wichtige Erinnerung",
                            Body = "Meeting mit Team in 30 Minuten",
                            Identifier = "https://ihre-domain.com/calendar/456",
                            UserID = "12345",
                            Timestamp = new DateTimeOffset(new DateTime(Configuration.ConfigGeneral.EpochYear, 1, 26, 13, 5, 0)).ToUnixTimeSeconds()
                        },
                        new NotificationItem
                        {
                            Title = "Sicherheitswarnung",
                            Body = "Ungewöhnliche Anmeldeaktivität festgestellt",
                            Identifier = "https://ihre-domain.com/security/202",
                            UserID = "67890",
                            Timestamp = new DateTimeOffset(new DateTime(Configuration.ConfigGeneral.EpochYear, 1, 26, 15, 49, 0)).ToUnixTimeSeconds()
                        }
                    };

                    var userNotifications = notifications
                        .Where(n => n.UserID == userId && n.Timestamp >= timestamp)
                        .ToList();

                    return Results.Ok(userNotifications);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid timestamp or userId");
                    return Results.StatusCode(400);
                }
            }).AllowAnonymous();

        }

        private static bool VerifyTotp(string secret, string userinputiode)
        {
            bool result = false;

            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
            {
                string decryptedBase32 = aes.DecryptBase32Secret(secret, Pepper!); // otp-Schlüssel entschlüsseln

                byte[] secretBytes = OtpNet.Base32Encoding.ToBytes(decryptedBase32);

                var totp = new OtpNet.Totp(secretBytes);
                result = totp.VerifyTotp(userinputiode, out _, new OtpNet.VerificationWindow(previous: 2, future: 2));
            }

            return result;
        }

    }
}
//#pragma warning restore CA1416