#pragma warning disable CA1416 // Unterdrückt CA1416 für den Encrypt-Aufruf
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using BlazorCore.Services.Apis;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.ServerShared;
using BlazorCore.Services.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlazorCore;

namespace pWebApi
{
    public static class Endpoints
    {
        public static bool ShowConnectionStringByError = false;
        public static string WebapiExceptionError = "";

        /// <summary>
        /// Container für Pepper 
        /// </summary>
        public static byte[]? Pepper;

        ///// <summary>
        ///// Objekt für die Kommunikation mit MSSQL Datenbank
        ///// </summary>
        static string? SymmetricSecurityKey; // siehe appsettings.json

        /// <summary>
        /// Generiert ConnectionString aus der Connection-Datei und Pepper aus der Server Config Datei
        /// </summary>
        /// <param name="app">Objekt app</param>
        public static void SetEndPointsConfiguration(this WebApplication app)
        {
            //var env = app.Services.GetRequiredService<IWebHostEnvironment>();
            try
            {
                var configuration = app.Configuration; // Zugriff auf IConfiguration
                var globalState = app.Services.GetRequiredService<IGlobalStateBase>();

                //string pepperFilePath = (globalState.GetSecurityConfigurationFile().Success
                //    && globalState.GetSecurityConfigurationFile().ValString != null
                //    ? globalState.GetSecurityConfigurationFile().ValString!
                //    : $@"C:\inetpub\vhosts\true-perfect-code.ch\_Connections\{Appl.ApplicationName}.security.config.json");

                // ACHTUNG ------------------------------------------------------
                // ConnectionString und/oder Pepper einmalig an dieser Stelle generieren 
                // ACHTUNG -----------------------------------------------------

                var result = ServerConfiguration.GetSecurityConfigurationFile(TestSolution4.Shared.Global.Configuration.ConfigGeneral);
                if (result != null)
                {
                    var pepperFilePath = !string.IsNullOrEmpty(result.out_value_str) && string.IsNullOrEmpty(result.out_err)
                        ? result.out_value_str
                        : $@"..\_Connections\{TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName}.security.config.json"; // Lokales Debug
                    try
                    {
                        // Pepper setzen
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


                // JWT-Einstellungen setzen
                SymmetricSecurityKey = configuration["JwtSettings:SymmetricSecurityKey"];
                // siehe appsettings.json
                //Issuer = configuration["JwtSettings:Issuer"];
                //Audience = configuration["JwtSettings:Audience"];
            }
            catch
            {
                throw; // Wirf die Ausnahme weiter, damit du sie im Plesk-Log siehst
            }
        }


        public static void MapEndPoints(this WebApplication app)
        {
            var globalState = app.Services.GetRequiredService<IGlobalStateBase>();

            ShowConnectionStringByError = TestSolution4.Shared.Global.Configuration.ConfigGeneral.ShowConnectionStringByError;
            WebapiExceptionError = TestSolution4.Shared.Global.Configuration.ConfigGeneral.WebapiExceptionError;

            // "/security/tokentpc"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_GetTokenDataTPCuser, (UserWebApi user, ISqlClientBase _sqlClient, IConfiguration config) => TokenTPCUser(user, _sqlClient, globalState, config)).AllowAnonymous();

            // "/security/changepassword"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_ChangePassword, (UserWebApi user, ISqlClientBase _sqlClient) => ChangePassword(user, _sqlClient, globalState)).RequireAuthorization();
            
            // "/api/spscalar"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_GetScalar, (UserWebApi user, ISqlClientBase _sqlClient) => Scalar(user, _sqlClient, globalState)).RequireAuthorization();
            
            // "/api/spnonquery"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_SetData, (UserWebApi user, ISqlClientBase _sqlClient) => NonQuery(user, _sqlClient, globalState)).RequireAuthorization();

            // "/api/spreader"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_GetRows, (UserWebApi user, ISqlClientBase _sqlClient) => Reader(user, _sqlClient, globalState)).RequireAuthorization();
            
            // "/api/anonymous"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_Anonymous, (UserWebApi user, ISqlClientBase _sqlClient) => AnonymousQuery(user, _sqlClient, globalState)).AllowAnonymous();



            // "/security/tokenidp"
            app.MapPost(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_GetTokenDataIDPuser, (UserWebApi user, ISqlClientBase _sqlClient) => TokenIDPUser(user, _sqlClient, globalState)).AllowAnonymous();

            app.MapGet("/auth/external", async ([Microsoft.AspNetCore.Mvc.FromQuery] string? state, HttpContext context) =>
            {
                string? pollingIdToPass = null;
                string? deviceTypePlatform = null;
                string? idP = null;
                string? redirectUri = null;

                // 1. Den 'state'-Parameter aus der URL-Abfrage abrufen
                string? stateParam = context.Request.Query["state"].ToString();

                if (!string.IsNullOrEmpty(stateParam))
                {
                    try
                    {
                        // 2. Den Base64-String dekodieren
                        string decodedState = Encoding.UTF8.GetString(Convert.FromBase64String(stateParam));

                        // 3. Die einzelnen Teile mittels '|' trennen: {deviceTypePlatform}|{IdP}|{pollingId}
                        var parts = decodedState.Split('|');

                        if (parts.Length == 3)
                        {
                            deviceTypePlatform = parts[0]; // Desktop-WinUI, Mobile-Android
                            idP = parts[1]; // IdP ist in parts[1] (z.B. "google", "microsoft")
                            pollingIdToPass = parts[2]; // PollingId ist in parts[2]

                            // Dynamisch den RedirectUri basierend auf IdP setzen
                            redirectUri = $"/auth/{idP.ToLower()}/callback";
                        }
                        else
                        {
                            //Console.WriteLine($"[Backend /auth/external] Warnung: 'state'-Parameter hat unerwartetes Format: {decodedState}");
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Invalid 'state' parameter format.");
                            return Results.BadRequest("Invalid 'state' parameter format.");
                        }
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"[Backend /auth/external] Fehler beim Base64-Dekodieren des 'state'-Parameters: {ex.Message}");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Invalid 'state' parameter format (Base64 decoding failed).");
                        return Results.BadRequest("Invalid 'state' parameter format (Base64 decoding failed).");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Backend /auth/external] Unerwarteter Fehler beim Parsen des 'state'-Parameters: {ex.Message}");
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("An internal error occurred while processing 'state'.");
                        return Results.BadRequest("An internal error occurred while processing 'state'.");
                    }
                }
                else
                {
                    //Console.WriteLine("[Backend /auth/external] Warnung: 'state'-Parameter fehlt oder ist leer.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Missing 'state' parameter.");
                    return Results.BadRequest("Missing 'state' parameter.");
                }

                // AuthenticationProperties für den Redirect vorbereiten
                var props = new AuthenticationProperties
                {
                    RedirectUri = redirectUri // Dynamischer Callback-Pfad
                };

                if (!string.IsNullOrEmpty(pollingIdToPass))
                {
                    props.Items["pollingId"] = pollingIdToPass;
                    props.Items["idP"] = idP;
                    props.Items["DevicePlatform"] = deviceTypePlatform;
                }


                // --- WESENTLICHE ÄNDERUNG HIER: Plattform-basierte Logik ---
                if (deviceTypePlatform != null
                    && (deviceTypePlatform.ToLower().Contains("win") || deviceTypePlatform.ToLower().Contains("android"))) // Prüfen, ob es ein Windows-Client ist
                {
                    // Serialisiere die AuthenticationProperties in einen QueryString
                    var authPropsQuery = new Microsoft.AspNetCore.Http.Extensions.QueryBuilder();
                    foreach (var kvp in props.Items)
                    {
                        authPropsQuery.Add(kvp.Key, kvp.Value);
                    }

                    var redirectToStartUrl = "/auth/external/start" + authPropsQuery.ToQueryString();

                    // HTML-Seite mit Spinner rendern
                    var htmlContent = $@"
            <!DOCTYPE html>
            <html lang=""de"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Weiterleitung zur Anmeldung</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background-color: #f5f5f5;
                        display: flex;
                        flex-direction: column;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                    }}
                    .spinner {{
                        border: 8px solid #f3f3f3;
                        border-top: 8px solid #3498db;
                        border-radius: 50%;
                        width: 60px;
                        height: 60px;
                        animation: spin 1s linear infinite;
                    }}
                    @keyframes spin {{
                        0% {{ transform: rotate(0deg); }}
                        100% {{ transform: rotate(360deg); }}
                    }}
                    .text {{
                        margin-top: 20px;
                        font-size: 1.2em;
                        color: #333;
                    }}
                </style>
            </head>
            <body>
                <div class=""spinner""></div>
                <div class=""text"">Please wait... Registration is being prepared.</div>
                <script>
                    setTimeout(function() {{
                        window.location.href = '{redirectToStartUrl}';
                    }}, 600);
                </script>
            </body>
            </html>
            ";

                    //context.Response.ContentType = "text/html";
                    //await context.Response.WriteAsync(htmlContent);

                    // Hier ist die Korrektur: Wir geben Results.Content zurück,
                    // um sicherzustellen, dass alle Pfade ein IResult zurückgeben.
                    return Results.Content(htmlContent, "text/html");
                }
                else // Für mobile Clients (Android, iOS): Direkter Challenge
                {
                    // Mapping von IdP zu Authentifizierungsschema (direkt hier, da kein /start-Umweg)
                    var authSchemeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                                    { "google", GoogleDefaults.AuthenticationScheme },
                                    { "microsoft", MicrosoftAccountDefaults.AuthenticationScheme },
                                    { "apple", "Apple" }
                    };

                    if (string.IsNullOrEmpty(idP) || !authSchemeMapping.ContainsKey(idP.ToLower()))
                    {
                        // If we are in an async lambda that is expected to return IResult (from app.MapGet),
                        // and we encounter an error, we MUST return an IResult.
                        // We cannot just write to response and return.
                        // So we return a BadRequest result.
                        return Results.BadRequest("Invalid or missing IdP for direct challenge.");
                    }
                    string authScheme = authSchemeMapping[idP.ToLower()];

                    // Führt den Challenge direkt aus, um den Browser zum IdP umzuleiten
                    return Results.Challenge(props, new[] { authScheme });
                }

            });
            
            app.MapGet("/auth/external/start", (HttpContext context) =>
            {
                // Mapping von IdP zu Authentifizierungsschema
                var authSchemeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "google", GoogleDefaults.AuthenticationScheme },
                    { "microsoft", MicrosoftAccountDefaults.AuthenticationScheme },
                    { "apple", "Apple" }
                    //{ "apple", AppleAuthenticationDefaults.AuthenticationScheme }
                };

                string? idP = context.Request.Query["idP"];
                if (string.IsNullOrEmpty(idP) || !authSchemeMapping.ContainsKey(idP.ToLower()))
                {
                    return Results.BadRequest("Invalid or missing IdP.");
                }

                var props = new AuthenticationProperties
                {
                    RedirectUri = $"/auth/{idP.ToLower()}/callback"
                };

                // Alle Query-Parameter in AuthenticationProperties übernehmen
                foreach (var kvp in context.Request.Query)
                {
                    props.Items[kvp.Key] = kvp.Value!;
                }

                // Das Authentifizierungsschema basierend auf IdP auswählen
                string authScheme = authSchemeMapping[idP.ToLower()];

                return Results.Challenge(props, new[] { authScheme });
            });

            // Endpoint für die Seite, die den Benutzer zum Schließen des Tabs auffordert
            app.MapGet("/auth/close-browser", async (HttpContext context) =>
            {
                // Generiere das HTML für die "Tab schließen"-Seite
                var htmlContent = @"
    <!DOCTYPE html>
    <html lang=""de"">
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Authentifizierung abgeschlossen</title>
        <style>
            body {
                font-family: Arial, sans-serif;
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                min-height: 100vh;
                margin: 0;
                background-color: #f0f2f5;
                color: #333;
                text-align: center;
            }
            .container {
                background-color: #fff;
                padding: 40px 30px;
                border-radius: 8px;
                box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
                max-width: 500px;
                width: 90%;
            }
            h1 {
                color: #28a745; /* Grüne Farbe für Erfolg */
                margin-bottom: 20px;
            }
            p {
                font-size: 1.1em;
                line-height: 1.6;
                margin-bottom: 30px;
            }
            .logo {
                margin-bottom: 20px;
                /* Wenn ein Logo, dann hier einbinden */
                /* Beispiel: img src=""/path//logo.png"" alt=""App Logo"" style=""max-width: 150px;"" */
            }
            .instructions {
                font-weight: bold;
                color: #555;
            }
        </style>
    </head>
    <body>
        <div class=""container"">
            <div class=""logo"">
                </div>
            <h1>Authentication successful!</h1>
            <p>
                You have been successfully logged in. Your token has been transferred to the application.
            </p>
            <p class=""instructions"">
                You can now close this browser window.
            </p>
        </div>
        <script>
            // Optional: Versuch, das Fenster automatisch zu schließen.
            // Beachte: Browser-Sicherheitseinstellungen können dies verhindern,
            // wenn das Fenster nicht von einem Script geöffnet wurde.
            // Ein expliziter Hinweis ist daher immer am besten.
            // try {
            //     window.onload = function() {
            //         setTimeout(function() {
            //             window.close();
            //         }, 2000); // Versuch, nach 2 Sekunden zu schließen
            //     };
            // } catch (e) {
            //     console.error('Auto-close failed:', e);
            // }
        </script>
    </body>
    </html>";

                // Setze den Content-Type auf text/html, damit der Browser es als HTML rendert
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(htmlContent);
            });


            // "/api/unauthorizedconnection"
            app.MapPost("/api/unauthorizedconnection", async () =>
            {
                return await Task.FromResult("Successful unauthorized connection to the Webapi");
            }).AllowAnonymous();

            // "/api/unauthorizedconnection" GET
            app.MapGet("/api/unauthorizedconnection", async () =>
            {
                return await Task.FromResult("Successful unauthorized connection to the Webapi");
            }).AllowAnonymous();

            // "/api/authorizedconnection"
            app.MapPost("/api/authorizedconnection", async  () =>
            {
                return await Task.FromResult("Successful authorized connection to the Webapi");
            }).RequireAuthorization();

            // "/api/check"
            app.MapGet(TestSolution4.Shared.Global.Configuration.ConfigWebapi.endpoint_CheckToken, (ClaimsPrincipal user) =>
            {
                // Die Tatsache, dass dieser Codeblock erreicht wird, 
                // beweist, dass die Authentifizierung (RequireAuthorization) erfolgreich war.

                // Wir geben einfach den HTTP Status Code 200 (OK) zurück.
                return Results.Ok();

            }).RequireAuthorization();

        }

        /// <summary>
        /// Generiert ein Token
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist token</returns>
        public static async Task<IResult> TokenTPCUser(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState, IConfiguration config)
        {
            bool sendErr = false;
            try
            {
                ScalarModel res = new();
                string user2FAinput = string.Empty;

                if (!string.IsNullOrEmpty(_user.DisplayError))
                    sendErr = (_user.DisplayError == "1" ? true : false);

                // Parameter entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    try
                    {
                        using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                        {
                            _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                        }
                    }
                    catch (Exception ex)
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: sendErr,
                            message: "Try to decrypt _user.JsonPara: " + ex.Message,
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                    }
                }

                // Sind Client-Daten vorhanden
                if (!String.IsNullOrEmpty(_user.JsonPara))
                {
                    // Client-Daten deserialisieren
                    Dictionary<string, string> recoveredDict;
                    try
                    {
                        recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);
                    }
                    catch (Exception ex)
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: sendErr,
                            message: "Try to restore dictionary: " + ex.Message,
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                    }

                    // Prüfen, ob Token-Abfrage korrekt (SQL-Injection verhindern, weil Endpoint => AllowAnonymous)
                    if (recoveredDict.ContainsKey("@Case_"))
                    {
                        string case_ = globalState.ConvertStrPara<string>(recoveredDict["@Case_"]);
                        if (case_ != "Register>>AuthUsers")
                        {
                            return ResponseFactory.CreateEncryptedResponse(
                                sendResponse: sendErr,
                                message: "No tpc token query",
                                token: "",
                                encryptFlag: _user.EncryptDecrypt
                            );
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: sendErr,
                            message: "Try to get tpc token query",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                    }

                    // Email und Passwort hashen
                    if (recoveredDict.ContainsKey("@EmailHash"))
                    {
                        string emailhash = globalState.ConvertStrPara<string>(recoveredDict["@EmailHash"]);
                        if (recoveredDict.ContainsKey("@PasswordHash"))
                        {
                            string passwordhash = globalState.ConvertStrPara<string>(recoveredDict["@PasswordHash"]);

                            try
                            {
                                using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                {
                                    recoveredDict["@EmailHash"] = aes.HashUsername(emailhash, Pepper!); // Email hashen
                                    recoveredDict["@PasswordHash"] = aes.HashCredentials(passwordhash, emailhash, Pepper!); // Passwort hashen
                                }
                            }
                            catch (Exception ex)
                            {
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: sendErr,
                                    message: "Try to hash user and password: " + ex.Message,
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                            }
                        }
                    }

                    if (recoveredDict.ContainsKey("@EmailHash") && recoveredDict.ContainsKey("@PasswordHash"))
                    {
                        bool registration = false;
                        if (recoveredDict.ContainsKey("@Int__Registration"))
                            registration = globalState.ConvertStrPara<string>(recoveredDict["@Int__Registration"]) == "1" ? true : false;

                        // Benutzer registrieren oder aus der DB holen
                        if (registration)
                        {
                            try
                            {
                                // R E G I S T R I E R U N G
                                res = await _sqlClient.NonQuery(recoveredDict);
                            }
                            catch (Exception ex)
                            {
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: sendErr,
                                    message: "Try to register user: " + ex.Message,
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                            }
                        }
                        else
                        {
                            // Prüfen, ob 2FA aktiviert ist
                            recoveredDict["@Case_"] = "CheckAccount>>AuthUsers";
                            ScalarModel resultOtpCheck = await _sqlClient.Scalar(recoveredDict);
                            if(resultOtpCheck.out_value_bool)
                            {
                                // 6-stellige otp-Benutzereingabe
                                string otpUserDigitInput = string.Empty;
                                if (recoveredDict.ContainsKey("tmp_userinputiode"))
                                {
                                    otpUserDigitInput = recoveredDict["tmp_userinputiode"];
                                    recoveredDict.Remove("tmp_userinputiode");
                                }
                                // User otp-Backupcode
                                string otpBackupCode = string.Empty;
                                if (recoveredDict.ContainsKey("@OtpBackupCode"))
                                {
                                    otpBackupCode = recoveredDict["@OtpBackupCode"];
                                    //recoveredDict.Remove("@OtpBackupCode"); => wird bei "DeleteOtp>>AuthUsers" benötigt
                                }

                                // Wenn 2FA aktiviert ist, dann müssen entweder 6-stelliger otp-Usercode oder Backupcode vorhanden sein
                                if (string.IsNullOrEmpty(otpUserDigitInput) && string.IsNullOrEmpty(otpBackupCode))
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: sendErr,
                                        message: ((int)MSG_CODES.no_userotp).ToString(), // "no_userotp",
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                }
                                else
                                {
                                    // Wenn 6-stellige otp-Benutzereingabe, dann 2FA prüfen
                                    if (!string.IsNullOrEmpty(otpUserDigitInput))
                                    {
                                        recoveredDict["@Case_"] = "SelectOtp>>AuthUsers";
                                        ScalarModel resultSelectOtp = await _sqlClient.Scalar(recoveredDict);
                                        if (resultSelectOtp != null && resultSelectOtp.out_value_str != null && String.IsNullOrEmpty(resultSelectOtp.out_err))
                                        {
                                            switch (resultSelectOtp.out_value_str)
                                            {
                                                case "no_user":
                                                    return ResponseFactory.CreateEncryptedResponse(
                                                        sendResponse: sendErr,
                                                        message: ((int)MSG_CODES.no_user).ToString(), // "no_user",
                                                        token: "",
                                                        encryptFlag: _user.EncryptDecrypt
                                                    );

                                                case "locked":
                                                    return ResponseFactory.CreateEncryptedResponse(
                                                        sendResponse: sendErr,
                                                        message: ((int)MSG_CODES.locked).ToString(), // "locked",
                                                        token: "",
                                                        encryptFlag: _user.EncryptDecrypt
                                                    );

                                                case "no_otp":
                                                case "":
                                                    return ResponseFactory.CreateEncryptedResponse(
                                                        sendResponse: sendErr,
                                                        message: ((int)MSG_CODES.error_no_otp_empty).ToString(), // "error_no_otp_empty",
                                                        token: "",
                                                        encryptFlag: _user.EncryptDecrypt
                                                    );

                                                default: // OTP Code zurückgeliefert
                                                    bool verifyTotp = VerifyTotp(resultSelectOtp.out_value_str, otpUserDigitInput);
                                                    if (verifyTotp) // 2FA Authentifizierung erfolgreich
                                                    {
                                                        recoveredDict["@Case_"] = "ResetLoginAttempts>>AuthUsers";
                                                        ScalarModel resultReset = await _sqlClient.NonQuery(recoveredDict)!;
                                                        if (resultReset != null && !string.IsNullOrEmpty(resultReset.out_err))
                                                        {
                                                            return ResponseFactory.CreateEncryptedResponse(
                                                                sendResponse: sendErr,
                                                                message: ((int)MSG_CODES.error_resetloginattempts).ToString(), // "error_resetloginattempts",
                                                                token: "",
                                                                encryptFlag: _user.EncryptDecrypt
                                                            );
                                                        }
                                                    }
                                                    else // 2FA Authentifizierung fehlgeschlagen
                                                    {
                                                        return ResponseFactory.CreateEncryptedResponse(
                                                            sendResponse: sendErr,
                                                            message: ((int)MSG_CODES.verifytotp_failed).ToString(), // "verifytotp_failed",
                                                            token: "",
                                                            encryptFlag: _user.EncryptDecrypt
                                                        );
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            return ResponseFactory.CreateEncryptedResponse(
                                                sendResponse: sendErr,
                                                message: ((int)MSG_CODES.error_selectotp).ToString(), // "error_selectotp",
                                                token: "",
                                                encryptFlag: _user.EncryptDecrypt
                                            );
                                        }
                                    }

                                    // Wenn Benutzer Backupcode vorhanden, dann 2FA zurücksetzen
                                    if (!string.IsNullOrEmpty(otpBackupCode))
                                    {
                                        recoveredDict["@Case_"] = "DeleteOtp>>AuthUsers";
                                        ScalarModel resultDeleteOtp = await _sqlClient.NonQuery(recoveredDict);
                                        // Ausführung (und somit die Anmeldung) darf hier nur dann fortgesetzt werden, wenn backupcode+Account+Passwort stimmen!
                                        if (resultDeleteOtp != null && resultDeleteOtp.out_value_str != null && string.IsNullOrEmpty(resultDeleteOtp.out_err))
                                        {
                                            if (!resultDeleteOtp.out_value_str.Contains("updated:"))
                                            {
                                                return ResponseFactory.CreateEncryptedResponse(
                                                    sendResponse: sendErr,
                                                    message: ((int)MSG_CODES.deleteotp_failed).ToString(), // "error_deleteotp",
                                                    token: "",
                                                    encryptFlag: _user.EncryptDecrypt
                                                );
                                            }
                                        }
                                        else
                                        {
                                            return ResponseFactory.CreateEncryptedResponse(
                                                sendResponse: sendErr,
                                                message: ((int)MSG_CODES.deleteotp_failed).ToString(), // "error_deleteotp",
                                                token: "",
                                                encryptFlag: _user.EncryptDecrypt
                                            );
                                        }
                                        //if (resultDeleteOtp != null && !string.IsNullOrEmpty(resultDeleteOtp.out_err))
                                        //{
                                        //    return ResponseFactory.CreateEncryptedResponse(
                                        //        sendResponse: sendErr,
                                        //        message: ((int)MSG_CODES.error_deleteotp).ToString(), // "error_deleteotp",
                                        //        token: "",
                                        //        encryptFlag: _user.EncryptDecrypt
                                        //    );
                                        //}
                                    }
                                }
                            }

                            try
                            {
                                // L O G I N
                                recoveredDict["@Case_"] = "SelectAuthUsersEmail";
                                res = await _sqlClient.Scalar(recoveredDict); // Anmeldung
                            }
                            catch (Exception ex)
                            {
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: sendErr,
                                    message: "Try to sign in user: " + ex.Message,
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                            }
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: sendErr,
                            message: ((int)MSG_CODES.empty_email_passwordhash).ToString(), // "empty_email_passwordhash",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                    }

                    // DB-Rückgabe prüfen
                    if (String.IsNullOrEmpty(res.out_err))
                    {
                        if (!String.IsNullOrEmpty(res.out_value_str))
                        {
                            if (res.out_value_str == "record_exists_no_adding")
                            {
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: true,
                                    message: ((int)MSG_CODES.record_exists_no_adding).ToString(), // "record_exists_no_adding",
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                            }
                            else
                            {
                                if(!string.IsNullOrEmpty(res.out_value_str))
                                {
                                    if (res.out_value_str == "no_user") // Benutzer nicht vorhanden
                                    {
                                        return ResponseFactory.CreateEncryptedResponse(
                                            sendResponse: true,
                                            message: ((int)MSG_CODES.no_user).ToString(), // "no_user",
                                            token: "",
                                            encryptFlag: _user.EncryptDecrypt
                                        );
                                    }
                                    else
                                    {
                                        try
                                        {
                                            //var claims = new[]
                                            //{
                                            //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                            //    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                                            //    new Claim("unix_ts", res.out_value_str), // Benutzerdefiniert: UnixTS
                                            //};

                                            //var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Endpoints.SymmetricSecurityKey!));
                                            //var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

                                            //var tokeOptions = new JwtSecurityToken(
                                            //    claims: claims,
                                            //    expires: DateTime.Now.AddDays(30),
                                            //    signingCredentials: signinCredentials
                                            //);
                                            //var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
                                            var unixTS = res.out_value_str;
                                            var tokenString = JwtHelper.CreateTokenFromExternalAuth(config, unixTS);

                                            // S E R V E R   R E S P O N S E (Antwort)
                                            return ResponseFactory.CreateEncryptedResponse(
                                                sendResponse: true,
                                                message: "",
                                                token: tokenString,
                                                encryptFlag: _user.EncryptDecrypt
                                            );
                                        }
                                        catch (Exception ex)
                                        {
                                            return ResponseFactory.CreateEncryptedResponse(
                                                sendResponse: sendErr,
                                                message: "Try to create claims and tpc-token: " + ex.Message + " , out_value_str: " + res.out_value_str + " , UnixTS: " + res.out_value_str,
                                                token: "",
                                                encryptFlag: _user.EncryptDecrypt
                                            );
                                        }
                                    }
                                }
                                else
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: sendErr,
                                        message: ((int)MSG_CODES.mssql_result_wrong_format).ToString(), // "mssql_result_wrong_format",
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                }
                            }
                        }
                        else
                        {
                            return ResponseFactory.CreateEncryptedResponse(
                                sendResponse: sendErr,
                                message: ((int)MSG_CODES.empty_mssql_result).ToString(), // "empty_mssql_result",
                                token: "",
                                encryptFlag: _user.EncryptDecrypt
                            );
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: sendErr,
                            message: $"{WebapiExceptionError} , {res.out_err}",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: sendErr,
                        message: ((int)MSG_CODES.empty_json).ToString(), // "empty_json",
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: sendErr,
                    message: $"{WebapiExceptionError} , First TRY..CATCH: {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
            }
        }

        /// <summary>
        /// Ändert Benutzerpasswort
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist Meldung über die Passwortänderung</returns>
        public static async Task<IResult> ChangePassword(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState)
        {
            ScalarModel result = new();

            bool err = false;
            try
            {
                // Parameter entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                    {
                        _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                    }
                }

                err = (_user.DisplayError == "1" ? true : false);

                if (!String.IsNullOrEmpty(_user.JsonPara))
                {
                    Dictionary<string, string> recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);
                    ChangePasswordModel ChangePassword = new();

                    if (recoveredDict.ContainsKey("@EmailHash"))
                    {
                        ChangePassword.Username = globalState.ConvertStrPara<string>(recoveredDict["@EmailHash"]);
                        if (recoveredDict.ContainsKey("@PasswordHash"))
                        {
                            ChangePassword.OldUserpassword = globalState.ConvertStrPara<string>(recoveredDict["@PasswordHash"]);
                            if (recoveredDict.ContainsKey("@PasswordHashNew"))
                            {
                                ChangePassword.NewUserpassword = globalState.ConvertStrPara<string>(recoveredDict["@PasswordHashNew"]);
                                using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                {
                                    recoveredDict["@EmailHash"] = aes.HashUsername(ChangePassword.Username, Pepper!); // Email hashen
                                    recoveredDict["@PasswordHash"] = aes.HashCredentials(ChangePassword.OldUserpassword, ChangePassword.Username, Pepper!); // Passwort alt hashen
                                    recoveredDict["@PasswordHashNew"] = aes.HashCredentials(ChangePassword.NewUserpassword, ChangePassword.Username, Pepper!); // Passwort neu hashen
                                }
                            }
                        }
                    }

                    if (recoveredDict.ContainsKey("@EmailHash") && recoveredDict.ContainsKey("@PasswordHash") && recoveredDict.ContainsKey("@PasswordHashNew"))
                    {
                        result = await _sqlClient.NonQuery(recoveredDict);

                        if (!string.IsNullOrEmpty(result.out_err))
                        {
                            return ResponseFactory.CreateEncryptedResponse(
                                sendResponse: err,
                                message: $"ChangePassword -> _sqlClient.NonQuery : {result.out_err}",
                                token: "",
                                encryptFlag: _user.EncryptDecrypt
                            );
                            //if (err)
                            //{
                            //    string exec = (ShowConnectionStringByError ? _sqlClient.CreateExec(recoveredDict) : "");
                            //    result.out_err = (result.out_err == "" ? "" : result.out_err + " , ") + "if (!String.IsNullOrEmpty(result.out_err)) = " + exec;
                            //}
                            //else
                            //    return Results.Unauthorized();
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: ((int)MSG_CODES.no_user).ToString(),
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string exec = (ShowConnectionStringByError ? _sqlClient.CreateExec(recoveredDict) : "");
                        //    result.out_err = "Error: " + err + "if (recoveredDict.ContainsKey(EmailHash) && recoveredDict.ContainsKey(PasswordHash)) = " + exec;
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: err,
                        message: ((int)MSG_CODES.empty_json).ToString(),
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                    //if (err)
                    //    result.out_err = ((int)MSG_CODES.empty_json).ToString(); // "Error: empty_json";
                    //else
                    //    return Results.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: err,
                    message: $"ChangePassword -> try catch : {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
                //if (err)
                //    result.out_err = "Error: " + ex.Message;
                //else
                //    return Results.Unauthorized();
            }

            // Verschlüsseln
            if (_user.EncryptDecrypt == "1")
            {
                using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                {
                    result.out_err = string.IsNullOrEmpty(result.out_err) ? "" : aes.Encrypt(result.out_err);
                    result.out_value_str = string.IsNullOrEmpty(result.out_value_str) ? "" : aes.Encrypt(result.out_value_str!);
                }
            }

            return await Task.FromResult(Results.Ok(result));
        }

        /// <summary>
        /// Liefert einen Scalar-Wert aus der Datenbank
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ScalarModel</returns>
        public static async Task<IResult> Scalar(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState)
        {
            ScalarModel result = new();
            bool err = false;

            try
            {
                // Entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                    {
                        _user.IsByte = (String.IsNullOrEmpty(_user.IsByte) ? "" : aes.Decrypt(_user.IsByte));

                        _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                    }
                }

                err = (_user.DisplayError == "1" ? true : false);

                if (_sqlClient.isConnected == true)
                {
                    if (!String.IsNullOrEmpty(_user.JsonPara))
                    {
                        Dictionary<string, string> recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);

                        if (_user.IsByte == "1")
                        {
                            result = await _sqlClient.Bytes(recoveredDict);
                            if (result.out_bytes != null)
                                result.out_value_str = BitConverter.ToString(result.out_bytes).Replace("-", "");
                        }
                        else
                        {
                            result = await _sqlClient.Scalar(recoveredDict);
                        }

                        if (!String.IsNullOrEmpty(result.out_err))
                        {
                            return ResponseFactory.CreateEncryptedResponse(
                                sendResponse: err,
                                message: $"Scalar -> _sqlClient.Scalar : {result.out_err}",
                                token: "",
                                encryptFlag: _user.EncryptDecrypt
                            );
                            //if (err)
                            //{
                            //    string exec = (ShowConnectionStringByError ? _sqlClient.CreateExec(recoveredDict) : "");
                            //    result.out_err = "if (!String.IsNullOrEmpty(RES.out_err)) = " + result.out_err + " , Exec: " + exec;
                            //}
                            //else
                            //    return Results.Unauthorized();
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: ((int)MSG_CODES.empty_json).ToString(),
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    result.out_err = "if (!String.IsNullOrEmpty(_user.JsonPara)) = ''";
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: err,
                        message: ((int)MSG_CODES.no_connection).ToString(),
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                    //if (err)
                    //    result.out_err = "if(sql.isConnected == true) = " + _sqlClient.isConnected.ToString();
                    //else
                    //    return Results.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: err,
                    message: $"Scalar -> try catch : {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
                //if (err)
                //    result.out_err = "catch (Exception ex) = " + ex.Message;
                //else
                //    return Results.Unauthorized();
            }

            // Verschlüsseln
            if (_user.EncryptDecrypt == "1")
            {
                using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                {
                    result.out_err = string.IsNullOrEmpty(result.out_err) ? "" : aes.Encrypt(result.out_err!);
                    result.out_value_str = string.IsNullOrEmpty(result.out_value_str) ? "" : aes.Encrypt(result.out_value_str!);
                }
            }

            return await Task.FromResult(Results.Ok(result));
        }

        /// <summary>
        /// Führt eine Aktualisierungsabfrage aus
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ScalarModel</returns>
        public static async Task<IResult> NonQuery(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState)
        {
            ScalarModel result = new();
            bool err = false;

            try
            {
                // Entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                    {
                        _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                    }
                }

                err = (_user.DisplayError == "1" ? true : false);

                if (!String.IsNullOrEmpty(_user.JsonPara))
                {
                    Dictionary<string, string> recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);

                    result = await _sqlClient.NonQuery(recoveredDict);

                    if (!String.IsNullOrEmpty(result.out_err))
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: $"NonQuery -> _sqlClient.NonQuery : {result.out_err}",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string exec = (ShowConnectionStringByError ? _sqlClient.CreateExec(recoveredDict) : "");
                        //    RES.out_err = (RES.out_err == "" ? "" : RES.out_err + " , ") + "if (!String.IsNullOrEmpty(RES.out_err)) = " + exec;
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: err,
                        message: ((int)MSG_CODES.empty_json).ToString(),
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                    //if (err)
                    //{
                    //    RES.out_err = (RES.out_err == "" ? "" : RES.out_err + " , ") + "if (!String.IsNullOrEmpty(_user.JsonPara)) = ''";
                    //}
                    //else
                    //    return Results.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: err,
                    message: $"NonQuery -> try catch : {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
                //if (err)
                //    RES.out_err = (RES.out_err == "" ? "" : RES.out_err + " , ") + "catch (Exception ex) = " + ex.Message;
                //else
                //    return Results.Unauthorized();
            }

            // Verschlüsseln
            if (_user.EncryptDecrypt == "1")
            {
                using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                {
                    result.out_err = string.IsNullOrEmpty(result.out_err) ? "" : aes.Encrypt(result.out_err!);
                    result.out_value_str = string.IsNullOrEmpty(result.out_value_str) ? "" : aes.Encrypt(result.out_value_str!);
                }
            }

            return await Task.FromResult(Results.Ok(result));
        }

        /// <summary>
        /// Liest Daten aus der Datenbank aus
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ScalarModel</returns>
        public static async Task<IResult> Reader(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState)
        {
            ReaderDynamicModel result = new();

            bool err = false;

            try
            {
                // Entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                    {
                        _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                    }
                }

                err = (_user.DisplayError == "1" ? true : false);

                if (!String.IsNullOrEmpty(_user.JsonPara))
                {
                    Dictionary<string, string> recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);

                    result = await _sqlClient.Reader(recoveredDict);

                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: $"Reader -> _sqlClient.Reader : {result.out_err}",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string exec = (ShowConnectionStringByError ? _sqlClient.CreateExec(recoveredDict) : "");
                        //    result.out_err = "if (!String.IsNullOrEmpty(res.out_err)) = " + result.out_err + " , Exec: " + exec;
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: err,
                        message: ((int)MSG_CODES.empty_json).ToString(),
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                    //if (err)
                    //{
                    //    result.out_err = "if (!String.IsNullOrEmpty(_user.JsonPara)) = ''";
                    //}
                    //else
                    //    return Results.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: err,
                    message: $"Reader -> try catch : {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
                //if (err)
                //    result.out_err = "catch (Exception ex) = " + ex.Message;
                //else
                //    return Results.Unauthorized();
            }

            // Verschlüsseln
            if (_user.EncryptDecrypt == "1")
            {
                using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                {
                    result.out_err = string.IsNullOrEmpty(result.out_err) ? "" : aes.Encrypt(result.out_err!);
                    result.out_json = string.IsNullOrEmpty(result.out_json) ? "" : aes.Encrypt(result.out_json!);
                }
            }

            return await Task.FromResult(Results.Ok(result));
        }

        /// <summary>
        /// Anonyme Abfrage an Datenbank
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist ScalarModel</returns>
        public static async Task<IResult> AnonymousQuery(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState)
        {
            ScalarModel result = new();
            bool err = false;

            try
            {
                string old_EncryptDecrypt = _user.EncryptDecrypt;
                // Entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                    {
                        _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                    }
                }

                err = (_user.DisplayError == "1" ? true : false);

                if (!String.IsNullOrEmpty(_user.JsonPara))
                {
                    Dictionary<string, string> recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);

                    // Prüfen, ob Feedback Abfrage korrekt (SQL-Injection verhindern, weil Endpoint => AllowAnonymous)
                    if (recoveredDict.ContainsKey("@Case_"))
                    {
                        _user.EncryptDecrypt = "0"; // Weil z.B. NonQuery(_user); aufgerufen wird (siehe weiter unten)
                        string case_ = globalState.ConvertStrPara<string>(recoveredDict["@Case_"]);

                        switch (case_)
                        {
                            case "MapTablesMSSQL":
                                result.out_value_str = "tables_mapped";
                                await _sqlClient.MapApplTbls();
                                break;

                            case "SaveFeedback>>AppMessages":
                                var resultFeedback = await NonQuery(_user, _sqlClient, globalState);
                                if (resultFeedback is Microsoft.AspNetCore.Http.HttpResults.Ok<ScalarModel> resultfeedback)
                                {
                                    if (resultfeedback.Value != null)
                                        result = resultfeedback.Value;
                                    else
                                    {
                                        return ResponseFactory.CreateEncryptedResponse(
                                            sendResponse: err,
                                            message: ((int)MSG_CODES.no_feedback_value).ToString(),
                                            token: "",
                                            encryptFlag: _user.EncryptDecrypt
                                        );
                                    }
                                }
                                else
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: ((int)MSG_CODES.no_feedback_result).ToString(),
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                    //if (err)
                                    //    result.out_err = ((int)MSG_CODES.no_feedback_result).ToString(); // "No NonQuery result";
                                    //else
                                    //    return Results.Unauthorized();
                                }
                                break;

                            case "SelectStoreUrl>>AppParameter":
                                var resultStoreUrl = await Reader(_user, _sqlClient, globalState);
                                if (resultStoreUrl is Microsoft.AspNetCore.Http.HttpResults.Ok<ReaderDynamicModel> resultstoreurl)
                                {
                                    if (resultstoreurl.Value != null)
                                    {
                                        result.out_err = resultstoreurl.Value.out_err;
                                        result.out_value_str = resultstoreurl.Value.out_json;
                                    }
                                    else
                                    {
                                        return ResponseFactory.CreateEncryptedResponse(
                                            sendResponse: err,
                                            message: ((int)MSG_CODES.no_storeurl_value).ToString(),
                                            token: "",
                                            encryptFlag: _user.EncryptDecrypt
                                        );
                                    }
                                }
                                else
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: ((int)MSG_CODES.no_storeurl_result).ToString(),
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                    //if (err)
                                    //    result.out_err = ((int)MSG_CODES.no_storeurl_result).ToString(); // "No NonQuery result";
                                    //else
                                    //    return Results.Unauthorized();
                                }
                                break;

                            case "SaveOtp>>AuthUsers":
                                try
                                {
                                    result.out_value_str = "not_updated"; // Standardwert setzen

                                    // OTP - Generierung
                                    byte[] secretKey = KeyGeneration.GenerateRandomKey();
                                    string base32Secret = Base32Encoding.ToString(secretKey).TrimEnd('=');

                                    if (!string.IsNullOrEmpty(base32Secret))
                                    {
                                        result.out_value_str = base32Secret;

                                        // Passwort verschlüsseln und Dictionary *otp* anpassen
                                        using (var aes = new TestSolution4.Shared.Services.Security.SecurityServer())
                                        {
                                            if (recoveredDict.ContainsKey("@otp"))
                                            {
                                                recoveredDict["@otp"] = aes.EncryptBase32Secret(base32Secret, Pepper!);
                                            }
                                        }

                                        // Datenbank-Speicherung
                                        ScalarModel resultSaveOtp = await _sqlClient.NonQuery(recoveredDict);
                                        result.out_err = resultSaveOtp.out_err;
                                    }
                                    else
                                    {
                                        return ResponseFactory.CreateEncryptedResponse(
                                            sendResponse: err,
                                            message: ((int)MSG_CODES.empty_secret).ToString(),
                                            token: "",
                                            encryptFlag: _user.EncryptDecrypt
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: $"AnonymousQuery -> switch (case_) -> try catch -> SaveOtp>>AuthUsers : {ex.Message}",
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                    //if (err)
                                    //    result.out_err = "Error CheckAccount>>AuthUsers: " + ex.Message;
                                    //else
                                    //    return Results.Unauthorized();
                                }
                                break;

                            case "CheckAccount>>AuthUsers":
                                try
                                {
                                    result.out_value_str = "0"; // Standardwert setzen

                                    if (recoveredDict.ContainsKey("@EmailHash"))
                                    {
                                        string _emailHash = globalState.ConvertStrPara<string>(recoveredDict["@EmailHash"]);
                                        if (recoveredDict.ContainsKey("@PasswordHash"))
                                        {
                                            string _passwordHash = globalState.ConvertStrPara<string>(recoveredDict["@PasswordHash"]);
                                            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                            {
                                                recoveredDict["@EmailHash"] = aes.HashUsername(_emailHash, Pepper!); // Email hashen
                                                recoveredDict["@PasswordHash"] = aes.HashCredentials(_passwordHash, _emailHash, Pepper!); // Passwort alt hashen
                                            }

                                            result = await _sqlClient.Scalar(recoveredDict);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: $"AnonymousQuery -> switch (case_) -> try catch -> CheckAccount>>AuthUsers : {ex.Message}",
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                }
                                break;
                                                            
                            case "DeleteOtp>>AuthUsers":
                                try
                                {
                                    result.out_value_str = "0"; // Standardwert setzen

                                    string tmp_userinputiode = string.Empty;
                                    if (recoveredDict.ContainsKey("tmp_userinputiode"))
                                    {
                                        tmp_userinputiode = recoveredDict["tmp_userinputiode"];
                                        recoveredDict.Remove("tmp_userinputiode");
                                    }

                                    // Useraccount und Passwort zuerst hashen
                                    if (recoveredDict.ContainsKey("@EmailHash"))
                                    {
                                        string emailHash = globalState.ConvertStrPara<string>(recoveredDict["@EmailHash"]);
                                        if(!string.IsNullOrEmpty(emailHash))
                                        {
                                            if (recoveredDict.ContainsKey("@PasswordHash"))
                                            {
                                                string password = globalState.ConvertStrPara<string>(recoveredDict["@PasswordHash"]);
                                                using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                                {
                                                    recoveredDict["@EmailHash"] = aes.HashUsername(emailHash, Pepper!); // Email hashen
                                                    recoveredDict["@PasswordHash"] = aes.HashCredentials(password, emailHash, Pepper!); // Passwort hashen
                                                }
                                            }
                                        }
                                    }

                                    // Wenn kein 6-stelliger otp-code angegeben, dann nutzt man otp-Backupcode
                                    if (string.IsNullOrEmpty(tmp_userinputiode))
                                    {
                                        recoveredDict["@Case_"] = "DeleteOtp>>AuthUsers";
                                        result = await _sqlClient.NonQuery(recoveredDict);
                                    }
                                    else
                                    {
                                        // Wenn 2FA über 6-stellige otp-Eingabe erfolgt
                                        recoveredDict["@Case_"] = "SelectOtp>>AuthUsers";
                                        result = await _sqlClient.Scalar(recoveredDict);

                                        if (result != null && result.out_value_str != null && string.IsNullOrEmpty(result.out_err))
                                        {
                                            switch (result.out_value_str)
                                            {
                                                case "no_user":
                                                case "locked":
                                                case "no_otp":
                                                case "":
                                                    result.out_value_bool = false;
                                                    break;

                                                default: // OTP Code zurückgeliefert

                                                    bool verifyTotp = VerifyTotp(result.out_value_str, tmp_userinputiode);
                                                    result.out_value_str = verifyTotp ? "1" : "0";

                                                    if (verifyTotp)
                                                    {
                                                        recoveredDict["@Case_"] = "DeleteOtpByAuthUsers_UnixTS>>AuthUsers";
                                                        result = await _sqlClient.NonQuery(recoveredDict);
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            return ResponseFactory.CreateEncryptedResponse(
                                                sendResponse: err,
                                                message: ((int)MSG_CODES.no_result).ToString(),
                                                token: "",
                                                encryptFlag: _user.EncryptDecrypt
                                            );
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: $"AnonymousQuery -> switch (case_) -> try catch -> DeleteOtp>>AuthUsers : {ex.Message}",
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                    //if (err)
                                    //    result.out_err = "[ERROR]=DeleteOtp>>AuthUsers: " + ex.Message;
                                    //else
                                    //    return Results.Unauthorized();
                                }
                                break;

                            case "ExistsEmailHashPasswordHash>>AuthUsers":
                                try
                                {
                                    result.out_value_str = "0"; // Standardwert setzen

                                    if (recoveredDict.ContainsKey("@EmailHash"))
                                    {
                                        string _emailHash = globalState.ConvertStrPara<string>(recoveredDict["@EmailHash"]);
                                        if (recoveredDict.ContainsKey("@PasswordHash"))
                                        {
                                            string _passwordHash = globalState.ConvertStrPara<string>(recoveredDict["@PasswordHash"]);
                                            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                                            {
                                                recoveredDict["@EmailHash"] = aes.HashUsername(_emailHash, Pepper!); // Email hashen
                                                recoveredDict["@PasswordHash"] = aes.HashCredentials(_passwordHash, _emailHash, Pepper!); // Passwort alt hashen
                                            }

                                            result = await _sqlClient.Scalar(recoveredDict);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: $"AnonymousQuery -> switch (case_) -> try catch -> ExistsEmailHashPasswordHash>>AuthUsers : {ex.Message}",
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                }
                                break;

                            default:
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: err,
                                    message: ((int)MSG_CODES.no_case).ToString(),
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                                //if (err)
                                //    result.out_err = "No Feedback Case_";
                                //else
                                //    return Results.Unauthorized();
                                //break;
                        }

                        _user.EncryptDecrypt = old_EncryptDecrypt; // Verschlüsselung wieder aktivieren
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: ((int)MSG_CODES.no_case).ToString(),
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //    result.out_err = ((int)MSG_CODES.no_case).ToString(); // "No Feedback Case_";
                        //else
                        //    return Results.Unauthorized();
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: err,
                        message: ((int)MSG_CODES.empty_json).ToString(),
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                    //if (err)
                    //    result.out_err = ((int)MSG_CODES.empty_json).ToString(); // "No JsonPara";
                    //else
                    //    return Results.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: err,
                    message: $"AnonymousQuery -> try catch : {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
                //if (err)
                //    result.out_err = (result.out_err == "" ? "" : result.out_err + " , ") + "catch (Exception ex) = " + ex.Message;
                //else
                //    return Results.Unauthorized();
            }

            // Verschlüsseln
            if (_user.EncryptDecrypt == "1")
            {
                using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                {
                    result.out_err = string.IsNullOrEmpty(result.out_err) ? "" : aes.Encrypt(result.out_err!);
                    result.out_value_str = string.IsNullOrEmpty(result.out_value_str) ? "" : aes.Encrypt(result.out_value_str!);
                }
            }

            return await Task.FromResult(Results.Ok(result));
        }

        private static bool VerifyTotp(string secret, string userinputiode)
        {
            bool result = false;

            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
            {
                string decryptedBase32 = aes.DecryptBase32Secret(secret, Pepper!); // otp-Schlüssel entschlüsseln

                byte[] secretBytes = Base32Encoding.ToBytes(decryptedBase32);

                var totp = new Totp(secretBytes);
                //userinputiode = totp.ComputeTotp(); //=> Zum Testen die richtige 6-stellige Otp-Eingabe
                result = totp.VerifyTotp(userinputiode, out _, new VerificationWindow(previous: 2, future: 2));
            }

            return result;
        }


        /// <summary>
        /// Holt aus der Datenbank ein generiertes Identity Provider Token (Google, Microsoft oder Apple
        /// </summary>
        /// <param name="_user">Model Objekt enthält alle Informationen vom Client</param>
        /// <returns>Rückgabewert ist IdP Token</returns>
        public static async Task<IResult> TokenIDPUser(UserWebApi _user, ISqlClientBase _sqlClient, IGlobalStateBase globalState)
        {
            bool err = false;
            try
            {
                ScalarModel result = new();

                if (!string.IsNullOrEmpty(_user.DisplayError))
                    err = (_user.DisplayError == "1" ? true : false);

                // Parameter entschlüsseln
                if (_user.EncryptDecrypt == "1")
                {
                    try
                    {
                        using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                        {
                            _user.JsonPara = (String.IsNullOrEmpty(_user.JsonPara) ? "" : aes.Decrypt(_user.JsonPara));
                        }
                    }
                    catch (Exception ex)
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: $"TokenIDPUser -> EncryptDecrypt -> try catch : {ex.Message}",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string message = "Try to decrypt _user.JsonPara: " + ex.Message;
                        //    string tokenString = "";
                        //    // Verschlüsseln
                        //    if (_user.EncryptDecrypt == "1")
                        //    {
                        //        using (Security aes = new())
                        //        {
                        //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                        //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                        //        }
                        //    }
                        //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }
                }

                if (!String.IsNullOrEmpty(_user.JsonPara))
                {
                    Dictionary<string, string> recoveredDict;

                    try
                    {
                        recoveredDict = globalState.DeserializeDictionaryTpc(_user.JsonPara);
                    }
                    catch (Exception ex)
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: $"TokenIDPUser -> DeserializeDictionaryTpc -> try catch : {ex.Message}",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string message = "Try to restore dictionary: " + ex.Message;
                        //    string tokenString = "";
                        //    // Verschlüsseln
                        //    if (_user.EncryptDecrypt == "1")
                        //    {
                        //        using (Security aes = new())
                        //        {
                        //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                        //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                        //        }
                        //    }
                        //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }

                    // Prüfen, ob IdP-Token Abfrage korrekt (SQL-Injection verhindern, weil Endpoint => AllowAnonymous)
                    if (recoveredDict.ContainsKey("@Case_"))
                    {
                        string case_ = globalState.ConvertStrPara<string>(recoveredDict["@Case_"]);
                        if (case_ != "SelectByIdPClientIdent>>AuthUsers")
                        {
                            return ResponseFactory.CreateEncryptedResponse(
                                sendResponse: err,
                                message: ((int)MSG_CODES.no_case).ToString(),
                                token: "",
                                encryptFlag: _user.EncryptDecrypt
                            );
                            //if (err)
                            //{
                            //    string message = ((int)MSG_CODES.empty_json).ToString(); // "No idp token query";
                            //    string tokenString = "";
                            //    // Verschlüsseln
                            //    if (_user.EncryptDecrypt == "1")
                            //    {
                            //        using (Security aes = new())
                            //        {
                            //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                            //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                            //        }
                            //    }
                            //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                            //}
                            //else
                            //    return Results.Unauthorized();
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: ((int)MSG_CODES.no_case).ToString(),
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string message = "Try to get idp token query";
                        //    string tokenString = "";
                        //    // Verschlüsseln
                        //    if (_user.EncryptDecrypt == "1")
                        //    {
                        //        using (Security aes = new())
                        //        {
                        //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                        //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                        //        }
                        //    }
                        //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }


                    if (recoveredDict.ContainsKey("@IdPClientIdent"))
                    {
                        try
                        {
                            result = await _sqlClient.Scalar(recoveredDict); // Idp-Token holen
                        }
                        catch (Exception ex)
                        {
                            return ResponseFactory.CreateEncryptedResponse(
                                sendResponse: err,
                                message: $"TokenIDPUser -> _sqlClient.Scalar -> try catch : {ex.Message}",
                                token: "",
                                encryptFlag: _user.EncryptDecrypt
                            );
                            //if (err)
                            //{
                            //    string message = "Try to get idp-token: " + ex.Message;
                            //    string tokenString = "";
                            //    // Verschlüsseln
                            //    if (_user.EncryptDecrypt == "1")
                            //    {
                            //        using (Security aes = new())
                            //        {
                            //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                            //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                            //        }
                            //    }
                            //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                            //}
                            //else
                            //    return Results.Unauthorized();
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: ((int)MSG_CODES.empty_pollingid).ToString(),
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string message = "empty_pollingid";
                        //    string tokenString = "";
                        //    // Verschlüsseln
                        //    if (_user.EncryptDecrypt == "1")
                        //    {
                        //        using (Security aes = new())
                        //        {
                        //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                        //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                        //        }
                        //    }
                        //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }


                    if (string.IsNullOrEmpty(result.out_err))
                    {
                        if (!string.IsNullOrEmpty(result.out_value_str))
                        {
                            
                            if (result.out_value_str == "no_user") // Benutzer nicht vorhanden
                            {
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: err,
                                    message: ((int)MSG_CODES.no_user).ToString(),
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                            }
                            else
                            {
                                if (result.out_value_str.Length > 30)
                                {
                                    try
                                    {
                                        var claims = new[]
                                        {
                                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                                            new Claim("unix_ts", result.out_value_str), // Benutzerdefiniert: UnixTS
                                        };

                                        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Endpoints.SymmetricSecurityKey!));
                                        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

                                        var tokeOptions = new JwtSecurityToken(
                                            //issuer: url,
                                            //audience: url,
                                            claims: claims,
                                            expires: DateTime.Now.AddDays(30),
                                            signingCredentials: signinCredentials
                                        );
                                        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);

                                        string message = "";
                                        // Verschlüsseln
                                        if (_user.EncryptDecrypt == "1")
                                        {
                                            using (Security aes = new(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema))
                                            {
                                                message = string.IsNullOrEmpty(message) ? "" : aes.Encrypt(message);
                                                tokenString = string.IsNullOrEmpty(tokenString) ? "" : aes.Encrypt(tokenString);
                                            }
                                        }

                                        return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                                    }
                                    catch (Exception ex)
                                    {
                                        return ResponseFactory.CreateEncryptedResponse(
                                            sendResponse: err,
                                            message: $"TokenIDPUser -> Claim -> try catch : {ex.Message}",
                                            token: "",
                                            encryptFlag: _user.EncryptDecrypt
                                        );
                                        //if (err)
                                        //{
                                        //    string message = "Try to create claims and idp-token: " + ex.Message + " , out_value_str: " + res.out_value_str + " , UnixTS: " + res.out_value_str;
                                        //    string tokenString = "";
                                        //    // Verschlüsseln
                                        //    if (_user.EncryptDecrypt == "1")
                                        //    {
                                        //        using (Security aes = new())
                                        //        {
                                        //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                                        //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                                        //        }
                                        //    }
                                        //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                                        //}
                                        //else
                                        //    return Results.Unauthorized();
                                    }
                                }
                                else
                                {
                                    return ResponseFactory.CreateEncryptedResponse(
                                        sendResponse: err,
                                        message: ((int)MSG_CODES.no_userid).ToString(),
                                        token: "",
                                        encryptFlag: _user.EncryptDecrypt
                                    );
                                    //string message = "no_userid";
                                    //string tokenString = "";
                                    //// Verschlüsseln
                                    //if (_user.EncryptDecrypt == "1")
                                    //{
                                    //    using (Security aes = new())
                                    //    {
                                    //        message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                                    //        tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                                    //    }
                                    //}
                                    //return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                                }
                            }
                        }
                        else
                        {
                            if (err)
                            {
                                return ResponseFactory.CreateEncryptedResponse(
                                    sendResponse: err,
                                    message: ((int)MSG_CODES.empty_mssql_result).ToString(), 
                                    token: "",
                                    encryptFlag: _user.EncryptDecrypt
                                );
                                //string message = "empty_mssql_result";
                                //string tokenString = "";
                                //// Verschlüsseln
                                //if (_user.EncryptDecrypt == "1")
                                //{
                                //    using (Security aes = new())
                                //    {
                                //        message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                                //        tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                                //    }
                                //}
                                //return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                            }
                            else
                                return Results.Unauthorized();
                        }
                    }
                    else
                    {
                        return ResponseFactory.CreateEncryptedResponse(
                            sendResponse: err,
                            message: $"TokenIDPUser -> _sqlClient.Scalar : {result.out_err}",
                            token: "",
                            encryptFlag: _user.EncryptDecrypt
                        );
                        //if (err)
                        //{
                        //    string message = WebapiExceptionError + " , " + res.out_err;
                        //    string tokenString = "";
                        //    // Verschlüsseln
                        //    if (_user.EncryptDecrypt == "1")
                        //    {
                        //        using (Security aes = new())
                        //        {
                        //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                        //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                        //        }
                        //    }
                        //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                        //}
                        //else
                        //    return Results.Unauthorized();
                    }
                }
                else
                {
                    return ResponseFactory.CreateEncryptedResponse(
                        sendResponse: err,
                        message: ((int)MSG_CODES.empty_json).ToString(),
                        token: "",
                        encryptFlag: _user.EncryptDecrypt
                    );
                    //if (err)
                    //{
                    //    string message = "empty_json";
                    //    string tokenString = "";
                    //    // Verschlüsseln
                    //    if (_user.EncryptDecrypt == "1")
                    //    {
                    //        using (Security aes = new())
                    //        {
                    //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                    //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                    //        }
                    //    }
                    //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                    //}
                    //else
                    //    return Results.Unauthorized();
                }
            }
            catch (Exception ex)
            {
                return ResponseFactory.CreateEncryptedResponse(
                    sendResponse: err,
                    message: $"TokenIDPUser -> try catch : {ex.Message}",
                    token: "",
                    encryptFlag: _user.EncryptDecrypt
                );
                //if (err)
                //{
                //    string message = WebapiExceptionError + " , First TRY..CATCH: " + ex.Message;
                //    string tokenString = "";
                //    // Verschlüsseln
                //    if (_user.EncryptDecrypt == "1")
                //    {
                //        using (Security aes = new())
                //        {
                //            message = string.IsNullOrWhiteSpace(message) ? "" : aes.Encrypt(message);
                //            tokenString = string.IsNullOrWhiteSpace(tokenString) ? "" : aes.Encrypt(tokenString);
                //        }
                //    }
                //    return await Task.FromResult(Results.Ok(new { msg = message, token = tokenString }));
                //}
                //else
                //    return Results.Unauthorized();
            }
        }


        public static class ResponseFactory
        {
            public static IResult CreateEncryptedResponse(
                bool sendResponse,
                string message,
                string token,
                string encryptFlag)
            {
                if (!sendResponse)
                    return Results.Unauthorized();

                string msg = message ?? "";
                string tok = token ?? "";

                // Verschlüsseln
                if (encryptFlag == "1")
                {
                    using var aes = new Security(TestSolution4.Shared.Global.Configuration.ConfigGeneral.ApplicationName, TestSolution4.Shared.Global.Configuration.ConfigGeneral.TableSchema);
                    msg = string.IsNullOrWhiteSpace(msg) ? "" : aes.Encrypt(msg);
                    tok = string.IsNullOrWhiteSpace(tok) ? "" : aes.Encrypt(tok);
                }

                return Results.Ok(new { msg, token = tok });
            }
        }
    }

    public static class JwtHelper
    {
        public static string CreateTokenFromExternalAuth(IConfiguration config, string unixTimestamp)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim("unix_ts", unixTimestamp),
                //new Claim("authusers_id", authUsers_ID),
                //new Claim("email", email)
            };

            var secret = config["JwtSettings:SymmetricSecurityKey"]; // siehe appsettings.json    //"eE3@eE3!E3ee*B@R@KUd@2023!wwWeE?3superSecretKey@345"; // 
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["JwtSettings:Issuer"],
                audience: config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
#pragma warning restore CA1416