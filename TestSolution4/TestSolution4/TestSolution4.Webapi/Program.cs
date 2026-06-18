using BlazorCore.Services.GlobalState;
using BlazorCore.Services.ServerShared;
using BlazorCore.Services.SqlClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using pWebApi;
using pWebApi.Hubs;
using pWebApi.Services.GlobalState;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Wichtig für Hosting-Umgebungen:
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        //options.TokenValidationParameters = new TokenValidationParameters
        //{
        //    ValidateIssuer = true, // ANGEPASST
        //    ValidateAudience = true, // ANGEPASST
        //    ValidateLifetime = true,
        //    ValidateIssuerSigningKey = true,
        //    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SymmetricSecurityKey"]!))
        //};
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SymmetricSecurityKey"]!)
            )
        };
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthCookie";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/auth/google/callback";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.SaveTokens = true;
        AddCommonOAuthEvents(options.Events, "google");
    })
    .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
        options.CallbackPath = "/auth/microsoft/callback";
        options.AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        options.TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("https://graph.microsoft.com/User.Read");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.SaveTokens = true;
        AddCommonOAuthEvents(options.Events, "microsoft");
    })
    .AddApple("Apple", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Apple:ClientId"]!;
        options.KeyId = builder.Configuration["Authentication:Apple:KeyId"];
        options.TeamId = builder.Configuration["Authentication:Apple:TeamId"]!;
        options.CallbackPath = "/auth/apple/callback";
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.GenerateClientSecret = false;
        options.ClientSecret = builder.Configuration["Authentication:Apple:ClientSecret"]; // Created ClientSecret on 21.08.2025 
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

        AddCommonOAuthEvents(options.Events, "apple");
    });

// AUTHORIZATION hinzufügen
builder.Services.AddAuthorization();

// SignalR Service hinzufügen
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Erlaubt MAUI-Apps den Zugriff
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // ZWINGEND notwendig für SignalR
    });
});

builder.Services.AddOpenApi();

// Global State
builder.Services.AddSingleton<IGlobalState, GlobalState>();
builder.Services.AddSingleton<IGlobalStateBase>(sp => sp.GetRequiredService<IGlobalState>());

// SqlClient
builder.Services.AddScoped<ISqlClientBase, SqlClient>();

// Initialisierung von SQL Client => Mapping von allen Tabellen
builder.Services.AddHostedService<TestSolution4.Web.Services.SqlClientInitializer>();

var app = builder.Build();

// Zugriff auf DI Container
using (var scope = app.Services.CreateScope())
{
    var globalState = scope.ServiceProvider.GetRequiredService<IGlobalState>();

    globalState.GlobalInit(
        TestSolution4.Shared.Global.Configuration.ConfigGeneral,
        TestSolution4.Shared.Global.Configuration.ConfigWebapi,
        TestSolution4.Shared.Global.Catalog.Sections
    );
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();

// CORS Middleware aktivieren
app.UseCors();

app.SetEndPointsConfiguration();

// Authentication & Authorization Middleware aktivieren
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapEndPoints();

app.MapHub<AuthHub>("/authHub");

app.Run();

void AddCommonOAuthEvents(OAuthEvents events, string idProvider)
{
    events.OnCreatingTicket = async context =>
    {
        var sqlClient = context.HttpContext.RequestServices.GetRequiredService<ISqlClientBase>();

        context.RunClaimActions();

        string jwt = "no_token";
        var sub = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(sub))
        {
            //int authUsers_ID = 0;
            string emailhash;

            string? pollingId = context.Properties.Items.TryGetValue("pollingId", out var p) ? p : null;
            string? idP = context.Properties.Items.TryGetValue("idP", out var idpVal) ? idpVal : null;
            string? platform = context.Properties.Items.TryGetValue("DevicePlatform", out var plat) ? plat : null;

            using (TestSolution4.Shared.Services.Security.SecurityServer aes = new())
                emailhash = aes.HashUsername(sub, Endpoints.Pepper!);

            if (!string.IsNullOrEmpty(emailhash) && !string.IsNullOrEmpty(pollingId))
            {
                string unixTS = BlazorCore.UnixTsGeneratorWebApi.Generate(TestSolution4.Shared.Global.Configuration.ConfigGeneral);

                // Benutzer auslesen
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "SelectAuthUsersEmail" },
                    { "@EmailHash", emailhash },
                    { "@PasswordHash", emailhash }
                };

                var result = await sqlClient.Scalar(db_para);
                //AuthResponse? response = null;

                if (result != null
                    && string.IsNullOrEmpty(result.out_err)
                    && !string.IsNullOrEmpty(result.out_value_str)
                    && result.out_value_str != "no_user")
                {
                    unixTS = result.out_value_str;
                }

                // Benutzer erstellen
                if (result != null && result.out_value_str == "no_user")
                {
                    db_para = new()
                    {
                        { "@Case_", "Register>>AuthUsers" },
                        { "@UnixTS", unixTS },
                        { "@EmailHash", emailhash },
                        { "@PasswordHash", emailhash },
                        { "@Int__Registration", "1" },
                        { "@Int__TwoFA", "0" },
                        { "@active", "1" },
                        { "@IdP", string.IsNullOrEmpty(idP) ? "unknown" : platform + "-" + idP }
                    };

                    result = await sqlClient.NonQuery(db_para);
                }

                if (!string.IsNullOrEmpty(emailhash) && !string.IsNullOrEmpty(unixTS))
                {
                    var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                    jwt = JwtHelper.CreateTokenFromExternalAuth(config, unixTS);

                    db_para = new Dictionary<string, string>
                    {
                        { "@Case_", "UpdateIdPToken>>AuthUsers" },
                        { "@UnixTS", unixTS },
                        { "@IdPClientIdent", pollingId },
                        { "@IdPToken", jwt }
                    };

                    await sqlClient.NonQuery(db_para);

                    // ==========================================
                    // NEU: SIGNALR PUSH AKTIVIEREN
                    // ==========================================
                    if (!string.IsNullOrEmpty(pollingId) && jwt != "no_token")
                    {
                        try
                        {
                            var hubContext = context.HttpContext.RequestServices.GetRequiredService<IHubContext<AuthHub>>();

                            await hubContext.Clients.Group(pollingId)
                                .SendAsync("ReceiveAuthTicket", new BlazorCore.Services.Dam.AuthTicketDto
                                {
                                    WebApiToken = jwt,
                                    UnixTS = unixTS
                                });
                        }
                        catch (Exception ex)
                        {
                            // Wir loggen den Fehler nur, unterbrechen aber den Flow nicht, 
                            // da das Polling (Fallback) ja noch über die DB funktioniert.
                            Console.WriteLine($"SignalR Push failed: {ex.Message}");
                        }
                    }
                    // ==========================================
                }
            }
        }

        context.Properties.Items["jwt"] = jwt;
        context.Response.Redirect("/auth/close-browser");
        await Task.CompletedTask;
    };

    events.OnTicketReceived = context =>
    {
        // Da wir Polling nutzen, schicken wir jeden User (Win/Android) auf die Erfolgsseite
        context.Response.Redirect("/auth/close-browser");

        context.HandleResponse();
        return Task.CompletedTask;
    };

    events.OnRemoteFailure = async context =>
    {
        var err = context.Failure;
        var msg = err?.Message ?? "An unknown error occurred during authentication.";
        if (err?.InnerException != null)
            msg += " (Inner Exception: " + err.InnerException.Message + ")";

        // Logging für das Server-Log (falls verfügbar)
        Console.WriteLine($"[AUTH ERROR] {idProvider}: {msg}");

        // HTML Antwort direkt an den Browser senden
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync($@"
        <html>
        <head><title>Authentication Error</title></head>
        <body style='font-family: Arial, sans-serif; padding: 40px; line-height: 1.6; background-color: #fdf2f2;'>
            <div style='max-width: 600px; margin: auto; background: white; padding: 30px; border: 1px solid #f5c6cb; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                <h1 style='color: #721c24; margin-top: 0;'>Authentication Failed</h1>
                <p>We encountered a problem while signing you in with <strong>{idProvider}</strong>.</p>
                
                <div style='background: #f8d7da; color: #721c24; padding: 15px; border-radius: 4px; font-family: monospace; font-size: 0.9em; word-break: break-all;'>
                    <strong>Error Details:</strong><br/>
                    {msg}
                </div>

                <p style='margin-top: 20px; font-size: 0.9em; color: #666;'>
                    Common reasons: The login session timed out, cookies are disabled, or the redirect configuration is incorrect.
                </p>
                
                <div style='margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px;'>
                    <button onclick='window.close()' style='background: #6c757d; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer;'>Close Window</button>
                    <p style='font-size: 0.8em; color: #999; margin-top: 10px;'>You can return to the app and try again.</p>
                </div>
            </div>
        </body>
        </html>");

        context.HandleResponse();
        await Task.CompletedTask;
    };

}