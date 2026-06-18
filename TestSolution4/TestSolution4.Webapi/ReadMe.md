If you are creating a new webapi project, then:

- Create folders ‘Hubs’ and 'Services' (see ReadMe.md in this folders)

- Create 'Endpoints.cs' file in the root of the project and set your endpoints
```
public static void MapEndPoints(this WebApplication app)
{
    var globalState = app.Services.GetRequiredService<IGlobalStateBase>();

    ShowConnectionStringByError = globalState.ShowConnectionStringByError;
    WebapiExceptionError = globalState.WebapiExceptionError;

    // "/security/tokentpc"
    app.MapPost(globalState.tpcWebApiEndpoints.endpoint_GetTokenDataTPCuser, (UserWebApi user, ISqlClientBase _sqlClient) => TokenTPCUser(user, _sqlClient, globalState)).AllowAnonymous();

    // "/security/changepassword"
    app.MapPost(globalState.tpcWebApiEndpoints.endpoint_ChangePassword, (UserWebApi user, ISqlClientBase _sqlClient) => ChangePassword(user, _sqlClient, globalState)).RequireAuthorization();
            
    // "/api/spscalar"
    app.MapPost(globalState.tpcWebApiEndpoints.endpoint_GetScalar, (UserWebApi user, ISqlClientBase _sqlClient) => Scalar(user, _sqlClient, globalState)).RequireAuthorization();
            
    // "/api/spnonquery"
    app.MapPost(globalState.tpcWebApiEndpoints.endpoint_SetData, (UserWebApi user, ISqlClientBase _sqlClient) => NonQuery(user, _sqlClient, globalState)).RequireAuthorization();

    // "/api/spreader"
    app.MapPost(globalState.tpcWebApiEndpoints.endpoint_GetRows, (UserWebApi user, ISqlClientBase _sqlClient) => Reader(user, _sqlClient, globalState)).RequireAuthorization();
            
    // "/api/anonymous"
    app.MapPost(globalState.tpcWebApiEndpoints.endpoint_Anonymous, (UserWebApi user, ISqlClientBase _sqlClient) => AnonymousQuery(user, _sqlClient, globalState)).AllowAnonymous();

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
    app.MapGet(globalState.tpcWebApiEndpoints.endpoint_CheckToken, (ClaimsPrincipal user) =>
    {
        // Die Tatsache, dass dieser Codeblock erreicht wird, 
        // beweist, dass die Authentifizierung (RequireAuthorization) erfolgreich war.

        // Wir geben einfach den HTTP Status Code 200 (OK) zurück.
        return Results.Ok();

    }).RequireAuthorization();

}
```

- Update file 'csproj' file

Example:
```
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<Version>1.0.01</Version>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<Compile Include="..\TestSolution4.Web\Services\SqlClientInitializer.cs" Link="Services\SqlClientInitializer\SqlClientInitializer.cs">
			<CopyToOutputDirectory>Always</CopyToOutputDirectory>
		</Compile>
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="AspNet.Security.OAuth.Apple" Version="10.0.0" />
		<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="10.0.5" />
		<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.5" />
		<PackageReference Include="Microsoft.AspNetCore.Authentication.MicrosoftAccount" Version="10.0.5" />
		<PackageReference Include="Otp.NET" Version="1.4.1" />
		<PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.7" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\BlazorCore\BlazorCore.csproj" />
		<ProjectReference Include="..\TestSolution4.Shared\TestSolution4.Shared.csproj" />
	</ItemGroup>

	<ItemGroup>
	  <Folder Include="Hubs\" />
	  <Folder Include="Services\" />
	</ItemGroup>

</Project>
```

- Update 'appsettings.json' file 

Example 'appsettings.json':
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "SymmetricSecurityKey": "eE3@eE3!E3ee*B@R@KUd@2023!wwWeE?3superSecretKey@345",
    "Issuer": "https://api.testsolution4.com",
    "Audience": "https://api.testsolution4.com"
  },

  "Authentication": {
    "Google": {
      "ClientId": "555775687511-20s3krg6ljl1em8s6itzgn44602a07vv.apps.googleusercontent.com",
      "ClientSecret": "555SPX-7HjCMPInD2ezHF2vNVr5WalxgwDm"
    },
    "Microsoft": {
      "ClientId": "55581d9f-2bf9-4fe3-c43e-b77b67bfe0cc",
      "ClientSecret": "5558Q~n-v1YTRhJFErcbDEX.SrySXPT4zAzuQdaO"
    },
    "Apple": {
      "ClientId": "ch.yourcompany.yourapplicationname.service",
      "TeamId": "555554NYYH",
      "KeyId": "555V42LWY6",
      "ClientSecret": "555hbGciOiJFUzI1NiIsImtpZCI6IjhROVY0M0xXWDYiLCJ0eXAiOiJKV1QifQ.eyJuYmYiOjE3NzMwNTQ4MDMsImV4cCI6MTc4ODYwNjgwMywiaXNzIjoiOTM5NTU0TllYSCIsImF1ZCI6Imh0dHBzOi8vYXBwbGVpZC5hcHBsZS5jb20iLCJzdWIiOiJjaC50cnVlcGVyZmVjdGNv1GUucE11bnVzLnNlFnZpY2UifQ.lue4B_mqoC2GsVm8jKXl1EWqG9ioP7maY79YCCso2Xcpccpe5g89ydoCsTRNtvELt4JP4O4MOgjD9beliB3555"
    }
  }
}
```

- Update file 'Program.cs'

Example 'Program.cs':
```
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // ANGEPASST
            ValidateAudience = true, // ANGEPASST
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SymmetricSecurityKey"]!))
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
```

- Update file 'launchSettings.json' in folder 'Properties'

Example 'launchSettings.json':
```
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "http://localhost:5225",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "https://localhost:7045;http://localhost:5225",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

LOCAL SETUP
-----------
- Create folder '_Connections' in 'C:\Users\YOUR_ACCOUNT\source\repos\TestSolution4\TestSolution4.Webapi\bin\Debug'

- Add file 'testsolution4.json' for MSSQL connection parameters

Examlple 'testsolution4.json':
```
{
  "Server": "DESKTOP-SERVER\\SQLEXPRESS",
  "Database": "db_testsolution4",
  "User_ID": "dbuser_db_testsolution4",
  "Password": "YOUR_SECURE_PASSWORD",
  "Integrated_Security": false,
  "Pooling": true,
  "TrustServerCertificate": true
}
```

- Add file 'testsolution4.security.config.json' for MSSQL pepper

Examlple 'testsolution4.security.config.json':
```
{
  "Pepper": "YOUR_SECURE_PEPPER"
}
```

TESTING WEBAPI
--------------
- The file Testing.md contains test examples for registration and querying via WebAPI.
- Scalar UI is integrated into the WebAPI project, allowing you to test all endpoints from the WebAPI before publishing it.


CLOUD SETUP
-----------
- Order a domain or subdomain from your hosting provider (e.g. Azure or private hosting providers like firestorm.ch).
- Create an MSSQL database with your hosting provider. Use the same parameters as when creating the database locally (e.g., Database=db_testsolution4, User_ID=dbuser_db_testsolution4 and Password=YOUR_SECURE_PASSWORD).
- Connect remotely to the database from your local Windows computer using MSSQL Management Studio.
- Run the scripts CREATE_TABLES.sql / CRUD.sql (located in the directory TestSolution4.Shared > Db)
- Upload your WebAPI (from Visual Studio direct publishing to Azure or zipping and uploading/unzipping to the hosting directory),
- Test webapi connection by calling the endpoint https://yourdomain.com/api/unauthorizedconnection (replace 'yourdomain.com' with your actual domain). You should receive the response "Successful unauthorized connection to the Webapi".