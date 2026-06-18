If you are creating a new pwa project, then:
- Delete the 'css', 'lib' and 'sample-data' folders.
- Add file 'service-worker.js' if not exists

- Update 'manifest.webmanifest'
```
{
  "name": "TestSolution4",
  "short_name": "TestSolution4",
  "description": "TestSolution4 Productivity Programming",
  "id": "testsolution4-pwa",
  "start_url": "./",
  "display": "standalone",
  "background_color": "#f4eccf",
  "theme_color": "#03173d",
  "prefer_related_applications": false,
  "icons": [
    {
      "src": "icon-192.png",
      "type": "image/png",
      "sizes": "192x192",
      "purpose": "any"
    },
    {
      "src": "icon-512.png",
      "type": "image/png",
      "sizes": "512x512",
      "purpose": "any"
    },
    {
      "src": "icon-512.png",
      "type": "image/png",
      "sizes": "512x512",
      "purpose": "maskable"
    }
  ]
}
```

- Update 'index.html'
```
<!DOCTYPE html>
<html lang="en">
<head>
    <title>TestSolution4</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, minimum-scale=1, maximum-scale=1, user-scalable=no">
    <meta name="apple-mobile-web-app-title" content="TestSolution4">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="theme-color" content="#f5f5dc">
    <base href="/" />

    <link href="manifest.webmanifest" rel="manifest" />
    <link rel="apple-touch-icon" sizes="512x512" href="icon-512.png" />

    <link rel="stylesheet" href="_content/P11/p11.css" />
    <link rel="stylesheet" href="_content/Shared/fonts.css" />

    <link rel="stylesheet" href="TestSolution4.Pwa.styles.css" />
    <link rel="icon" type="image/png" href="icon-512.png" />

    <link rel="stylesheet" href="_content/pE/nativewebview.css" />
</head>

<body class="lock-body-scroll">

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

    <script>navigator.serviceWorker.register('service-worker.js');</script>

    <script>
        window.pE_Web = window.pE_Web || {};

        /* Logik vereinfacht für PWA (kein Capacitor-Check-Loop nötig) */
        window.pE_Web.registerWebViewReadyCallback = function (dotNetRef) {
            window.pE_Web._readyDotNetRef = dotNetRef;
            // In der PWA sind wir sofort "Ready", da keine native Bridge geladen werden muss
            dotNetRef.invokeMethodAsync("OnWebViewReady");
        };

        window.pE_Web.disposeWebViewReadyCallback = function () {
            window.pE_Web._readyDotNetRef = null;
        };

        window.pE_Web.registerWebNavigationHelper = function (dotNetRef) {
            window.pE_Web._navigationHelperRef = dotNetRef;
        };

        let deferredPrompt;

        window.addEventListener('beforeinstallprompt', (e) => {
            // Verhindert, dass der Browser den Standard-Banner zeigt
            e.preventDefault();
            // Speichert das Event, damit wir es später auslösen können
            deferredPrompt = e;
            console.log('TestSolution4: Install prompt ready to be triggered.');
        });

        window.triggerPwaInstall = async () => {
            if (!deferredPrompt) {
                console.warn('Install prompt not available yet.');
                return;
            }
            // Zeigt den Browser-Dialog an
            deferredPrompt.prompt();
            // Wartet auf die Entscheidung des Nutzers
            const { outcome } = await deferredPrompt.userChoice;
            console.log(`User response to install prompt: ${outcome}`);
            // Wir löschen den Prompt, da er nur einmal verwendet werden kann
            deferredPrompt = null;
        };

        window.checkIfAppInstalled = () => {
            // 1. Check ob bereits im Standalone-Modus geöffnet
            if (window.matchMedia('(display-mode: standalone)').matches) {
                return true;
            }
            // 2. Für Chrome/Edge: Prüfen, ob die Installation bereits abgeschlossen wurde
            // (Hinweis: Das funktioniert nicht in allen Browsern perfekt, ist aber ein guter Indikator)
            if (navigator.scheduling && navigator.scheduling.isInputPending) {
                // Zusätzliche Heuristik falls nötig
            }
            return false;
        };

        // Ergänze dies in deinem <script> Block in der index.html
        let dotNetHelper;

        window.registerPwaHelper = (dotNetObj) => {
            dotNetHelper = dotNetObj;
        };

        window.addEventListener('appinstalled', (event) => {
            console.log('TestSolution4: Installation abgeschlossen!');
            if (dotNetHelper) {
                // Ruft die C#-Methode auf, um den Button sofort zu verstecken
                dotNetHelper.invokeMethodAsync('OnAppInstalledSucceeded');
            }
        });
    </script>
</body>
</html>
```