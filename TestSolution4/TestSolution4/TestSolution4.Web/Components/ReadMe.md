If you are creating a new web project, then:
- Delete the 'Page' and 'Layout' folders from the 'Web' project.
- Delete the file Routes.razor from the 'Web' project.
- In the file _Imports.razor, remove the 'using' reference to the layout (@using YOUR_PROJECT.Components.Layout)
- Add the following new using statement to the file '_Imports.razor': '@using Shared'
- Add the following new using statement to the file '_Imports.razor': '@using Shared.Layout'

Change (if needed) in App.razor:
- Text of: Primary SEO, Open Graph, Twitter Card 
- Set 'Shared.Global.Configuration.ConfigGeneral.ApplicationName' and 'Shared.Global...ApplicationDomain'
- Save image 'opengraphimg.jpg' in Shared/img/opengraphimg.jpg 

Examle App.razor:
```
<!DOCTYPE html>
<html lang="en">
    <head>
        <!-- Basic -->
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <base href="/" />

        <!-- Primary SEO -->
        <title>@Shared.Global.Configuration.ConfigGeneral.ApplicationName - Privacy First App</title>
        <meta name="description" content="@($"{@Shared.Global.Configuration.ConfigGeneral.ApplicationName} is a privacy-first, anonymous and cross-platform app. Full data sovereignty. Local, Cloud or Hybrid storage. 100% free.")" />
        <meta name="robots" content="index, follow" />
        <link rel="canonical" href="@($"https://{Shared.Global.Configuration.ConfigGeneral.ApplicationDomain}/")" />

        <!-- Open Graph -->
        <meta property="og:type" content="website" />
        <meta property="og:title" content="@($"{@Shared.Global.Configuration.ConfigGeneral.ApplicationName} - The Privacy First App")" />
        <meta property="og:description" content="Anonymous and full data ownership. Cross-platform app for your digital sovereignty." />
        <meta property="og:url" content="@($"https://{Shared.Global.Configuration.ConfigGeneral.ApplicationDomain}/")" />
        <meta property="og:image" content="@($"https://{Shared.Global.Configuration.ConfigGeneral.ApplicationDomain}/_content/Shared/img/opengraphimg.jpg")" />

        <!-- Twitter Card -->
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="@($"{@Shared.Global.Configuration.ConfigGeneral.ApplicationName} - Privacy First App")" />
        <meta name="twitter:description" content="Full control and anonymous. Sovereign. Cross-platform." />
        <meta name="twitter:image" content="@($"https://{Shared.Global.Configuration.ConfigGeneral.ApplicationDomain}/_content/Shared/img/opengraphimg.jpg")" />

        <!-- Theme -->
        <meta name="theme-color" content="#ffffff" />

        <!-- Styles -->
        <link rel="stylesheet" href="_content/Shared/fonts.css" />
        <link rel="stylesheet" href="@Assets["_content/p11/p11.css"]" />
        <link rel="stylesheet" href="TestSolution4.Web.styles.css" />
        <style>
                footer,
                .footer,
                .fixed-bottom {
                    position: fixed;
                    left: 0;
                    right: 0;
                    z-index: 1000;
                    bottom: var(--p11-bottom-fix);
                    min-height: calc(var(--p11-safe-area-bottom) + var(--p11-height-footer-fix));
                }
        </style>

        <!-- Favicon -->
        <link rel="icon" type="image/png" href="_content/Shared/img/logo_web.png" />

        <!-- Structured Data -->
        <script type="application/ld+json">
            @(
                $@"{{
                  ""@context"": ""https://schema.org"",
                  ""@type"": ""SoftwareApplication"",
                  ""name"": ""{Shared.Global.Configuration.ConfigGeneral.ApplicationName}"",
                  ""applicationCategory"": ""ProductivityApplication"",
                  ""operatingSystem"": ""Web, Windows, macOS, iOS, Android, Linux (PWA)"",
                  ""offers"": {{
                    ""@type"": ""Offer"",
                    ""price"": ""0"",
                    ""priceCurrency"": ""EUR""
                  }},
                  ""description"": ""A privacy-first and anonymous application focused on digital sovereignty and full data ownership."",
                  ""url"": ""https://{Shared.Global.Configuration.ConfigGeneral.ApplicationDomain}/""
                }}"
            )
        </script>

        <HeadOutlet @rendermode="InteractiveServer" />
    </head>

    <body>
        <Routes @rendermode="InteractiveServer" />
        <ReconnectModal />

        <script src="_content/pE/app.js"></script>

        <script src="_content/P11/p11.js" type="module"></script>

        <script src="_framework/blazor.web.js"></script>

        <script>
            window.pE_Web = window.pE_Web || {};

            window.pE_Web.registerWebViewReadyCallback = function (dotNetRef) {
                dotNetRef?.invokeMethodAsync("OnWebViewReady");
            };

            window.pE_Web.disposeWebViewReadyCallback = function () {
            };
        </script>

    </body>

</html>
```