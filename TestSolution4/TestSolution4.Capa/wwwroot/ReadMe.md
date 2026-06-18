If you are creating a new capacitor project, then:
- Delete the 'css', 'lib' and 'sample-data' folders.

Examle index.html:
```
<!DOCTYPE html>
<html lang="en" class="p11-mobile">
<head>
    <title>TestSolution4</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover">
    <base href="/" />

    <link rel="stylesheet" href="_content/P11/p11.css" />

    <link rel="stylesheet" href="_content/Shared/fonts.css" />

    <link rel="stylesheet" href="TestSolution4.Capa.styles.css" />
    <link rel="icon" href="data:,">

    <link rel="stylesheet" href="_content/pE/nativewebview.css" />

</head>

<body class="lock-body-scroll">

    <div class="status-bar-safe-area"></div>

    <div id="app">
        <div class="d-flex flex-column justify-content-center align-items-center vh-100 bg-light">
            <div class="text-center">
                <div class="spinner-border text-secondary" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div class="loading-progress-text mt-3 text-secondary" style="font-family: sans-serif; font-size: 0.85rem;"></div>
            </div>
        </div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="." class="reload">Reload</a>
        <span class="dismiss">🗙</span>
    </div>

    <script src="_content/P11/p11.js" type="module"></script>
    <script src="_content/pE/app.js"></script>
    <script src="_content/pE/cap.js"></script>

    <script src="_framework/blazor.webassembly.js"></script>

    <script>
        window.pE_Web = window.pE_Web || {};

        /* ========================================================================
           PLATFORM & READY LOGIC
           ======================================================================== */
        window.pE_Web._readyTimer = null;
        window.pE_Web._readyDotNetRef = null;

        window.pE_Web.registerWebViewReadyCallback = function (dotNetRef) {
            window.pE_Web._readyDotNetRef = dotNetRef;
            const ua = navigator.userAgent;
            const isNativeCandidate = window.location.hostname === 'localhost' &&
                (/Android/i.test(ua) || /iPhone|iPad|iPod/i.test(ua));

            if (!isNativeCandidate) {
                dotNetRef.invokeMethodAsync("OnWebViewReady");
                return;
            }

            window.pE_Web._readyTimer = setInterval(() => {
                if (window.Capacitor?.getPlatform && window.Capacitor.getPlatform() !== 'web') {
                    clearInterval(window.pE_Web._readyTimer);
                    dotNetRef.invokeMethodAsync("OnWebViewReady");
                }
            }, 50);
        };

        window.pE_Web.disposeWebViewReadyCallback = function () {
            if (window.pE_Web._readyTimer) clearInterval(window.pE_Web._readyTimer);
            window.pE_Web._readyDotNetRef = null;
        };

        window.pE_Web.registerWebNavigationHelper = function (dotNetRef) {
            window.pE_Web._navigationHelperRef = dotNetRef;
        };
    </script>

</body>
</html>
```