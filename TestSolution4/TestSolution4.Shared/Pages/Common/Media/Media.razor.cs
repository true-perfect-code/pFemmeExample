using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorCore.Services.AppState;
using BlazorCore.Services.ImageOptimizer;
using BlazorCore.Services.Media;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using System.Text;

namespace TestSolution4.Shared.Pages.Common.Media
{
    public partial class Media : ComponentBase, IMediaBase, IDisposable
    {
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public string? ImageDataUrl { get; set; }
        [Parameter] public EventCallback<string?> ImageDataUrlChanged { get; set; }

        // NEUE BINDING VARIABLE für Thumbnail
        [Parameter] public string? ThumbnailDataUrl { get; set; }
        [Parameter] public EventCallback<string?> ThumbnailDataUrlChanged { get; set; }

        [Parameter] public EventCallback OnDelete { get; set; }

        //[Parameter] public bool UseNativeFeatures { get; set; } = false;

        [Parameter] public p11.UI.ButtonOrientation ButtonOrientation { get; set; } = p11.UI.ButtonOrientation.Horizontal;
        [Parameter] public bool ShowFilePickerButton { get; set; } = true;
        [Parameter] public bool ShowCameraButton { get; set; } = true;

        [Parameter] public string FilePickerText { get; set; } = "";
        [Parameter] public string CameraText { get; set; } = "";
        [Parameter] public string FilePickerIconClass { get; set; } = "bi bi-folder";
        [Parameter] public string CameraButtonIconClass { get; set; } = "bi bi-camera";
        [Parameter] public p11.UI.Size Size { get; set; } = p11.UI.Size.Medium;
        [Parameter] public string? ButtonMinWidth { get; set; }

        [Parameter] public bool ShowInlinePreview { get; set; } = true;
        [Parameter] public p11.UI.PreviewImagePosition PreviewImagePosition { get; set; } = p11.UI.PreviewImagePosition.Top;
        [Parameter] public bool PreventPhonePreviewOpen { get; set; } = false;

        [Parameter] public int ImageSize { get; set; } = 500;
        [Parameter] public int ImageQuality { get; set; } = 75;
        [Parameter] public bool CropToSquare { get; set; } = true;

        [Parameter] public int ImageDisplayedMaxHeight { get; set; } = 250;
        [Parameter] public int ImageDisplayedWidth { get; set; } = 120;

        // NEUE PARAMETER für Thumbnail
        [Parameter] public int? ThumbnailSize { get; set; } = null;
        [Parameter] public int? ThumbnailQuality { get; set; } = 45;
        [Parameter] public bool ThumbnailCropToSquare { get; set; } = true;


        [Parameter] public string PreviewModalTitle { get; set; } = string.Empty;
        [Parameter] public string CameraModalTitle { get; set; } = string.Empty;
        [Parameter] public string CancelText { get; set; } = string.Empty;
        [Parameter] public string AcceptText { get; set; } = string.Empty;
        [Parameter] public string CaptureText { get; set; } = string.Empty;

        [Parameter] public bool ShowAllButtonIcons { get; set; } = true;
        [Parameter] public bool SkipValidation { get; set; } = false;

        [Inject] protected IJSRuntime _js { get; set; }
        [Inject] protected IPlatformBase? _platform { get; set; }
        [Inject] protected IAppStateBase? _appState { get; set; }
        [Inject] private IImageOptimizer? _imageResizer { get; set; }

        protected const string JsCapPrefix = "pE_Capacitor.media";
        protected const string JsWebPrefix = "pE_Web.media"; // Angepasst an pE_Web Struktur

        protected const int DefaultImageSize = 1200;
        protected const int DefaultQuality = 85;

        //protected bool _usePhoneFeatures = false;

        private ElementReference _fileInput;
        private bool _showCameraDialog = false;
        private bool IsPreviewOpen = false;
        private bool _cameraStarted = false;
        private bool _isProcessing = false;
        private bool _isFileProcessing = false;
        private bool _isCameraProcessing = false;
        private bool _DisableWebCameraButton = true;

        private bool _hasConfigurationError = false;
        private string? _configurationErrorMessage;
        private string? _userErrorMessage;

        private Guid _idGuid = Guid.NewGuid();
        private Guid _idGuidNative = Guid.NewGuid();

        private DotNetObjectReference<Media>? _dotNetRef;

        protected override void OnInitialized()
        {
            //SetPlattformForModalNative();

            // Initialisierung im Codebehind
            _dotNetRef = DotNetObjectReference.Create(this);

            if (string.IsNullOrEmpty(FilePickerText)) FilePickerIconClass = "bi bi-folder";
            if (string.IsNullOrEmpty(CameraText)) CameraButtonIconClass = "bi bi-camera";

            PreviewModalTitle = _appState!.T("Img thumbnail");

            base.OnInitialized();
        }

        protected override void OnParametersSet()
        {
            _hasConfigurationError = false;
            _configurationErrorMessage = null;
            var messagesBuilder = new StringBuilder();

            if (!SkipValidation)
            {
                if (ImageQuality < 0 || ImageQuality > 100)
                    messagesBuilder.AppendLine($"Configuration Error: {nameof(ImageQuality)} must be a value between 0 and 100. Current value is '{ImageQuality}'.");

                if (ImageSize <= 0)
                    messagesBuilder.AppendLine($"Configuration Error: {nameof(ImageSize)} must be a positive integer. Current value is '{ImageSize}'.");

                // Überprüfe ThumbnailSize und ThumbnailQuality falls angegeben
                if (ThumbnailSize.HasValue && ThumbnailSize.Value <= 0)
                    messagesBuilder.AppendLine($"Configuration Error: {nameof(ThumbnailSize)} must be a positive integer. Current value is '{ThumbnailSize.Value}'.");
                if (ThumbnailQuality.HasValue && ThumbnailQuality.Value <= 0)
                    messagesBuilder.AppendLine($"Configuration Error: {nameof(ThumbnailQuality)} must be a positive integer. Current value is '{ThumbnailQuality.Value}'.");

                if (!string.IsNullOrWhiteSpace(ButtonMinWidth))
                {
                    if (!ButtonMinWidth.EndsWith("px") && !ButtonMinWidth.EndsWith("rem") &&
                      !ButtonMinWidth.EndsWith("em") && !ButtonMinWidth.EndsWith("%") && !ButtonMinWidth.EndsWith("vw"))
                    {
                        messagesBuilder.AppendLine($"Configuration Warning: {nameof(ButtonMinWidth)} ('{ButtonMinWidth}') does not appear to be a valid CSS length unit (e.g., '150px', '10rem').");
                    }
                }
            }

            if (messagesBuilder.Length > 0)
            {
                _hasConfigurationError = true;
                _configurationErrorMessage = messagesBuilder.ToString().Trim();
            }

            base.OnParametersSet();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_appState!.IsPhone && _showCameraDialog && !_cameraStarted)
            {
                if (_appState != null && !string.IsNullOrEmpty(_configurationErrorMessage))
                    await _appState.Log($"Kamera-Zugriff verweigert oder nicht verfügbar. Grund: {_configurationErrorMessage}");

                try
                {
                    ////await JSRuntime.InvokeVoidAsync("startCamera", "cameraFeed");
                    ////await _jsRuntime.InvokeVoidAsync($"{JsWebPrefix}.startCamera", "cameraFeed");
                    //await _media!.StartCameraAsync("cameraFeed");
                    await StartCameraAsync("cameraFeed");
                    //await _jsRuntime.InvokeVoidAsync("pE_Web.media.startCamera", "cameraFeed");
                    _cameraStarted = true;
                }
                catch (JSException ex)
                {
                    _showCameraDialog = false;
                    _cameraStarted = false;
                    if (_appState != null)
                        await _appState.Log($"Kamera-Zugriff verweigert oder nicht verfügbar. Grund: {ex.Message}");
                    StateHasChanged();
                }
            }
        }

        [JSInvokable]
        public async Task SetOptimizedImageData(string base64Data, string? thumbBase64 = null)
        {
            try
            {
                await ProcessImageBytes(Convert.FromBase64String(NormalizeBase64(base64Data)), isAlreadyOptimized: true);

                if (!string.IsNullOrEmpty(thumbBase64))
                {
                    ThumbnailDataUrl = NormalizeBase64(thumbBase64);
                    await ThumbnailDataUrlChanged.InvokeAsync(ThumbnailDataUrl);
                }
            }
            finally
            {
                _isCameraProcessing = false; // Hier sicherstellen, dass der Spinner stoppt
                _isProcessing = false;
                _showCameraDialog = false;
                await OnCancelCamera(false);
                StateHasChanged();
            }
        }

        public void Dispose()
        {
            // Verhindert Memory Leaks der .NET -> JS Referenz
            _dotNetRef?.Dispose();

            // Optional: Falls die Kamera im Web-Modus noch läuft, stoppen wir sie hart
            if (!IsCapacitorNative && _cameraStarted)
            {
                // Wir können hier InvokeVoidAsync nicht awaiten (da Dispose synchron ist),
                // aber wir können den Task abfeuern.
                _js.InvokeVoidAsync($"{JsWebPrefix}.stopCamera", "cameraFeed");
            }
        }

        //private bool IsNative => _platform.GetCurrPlatform() != PLATFORMS.WASM;
        // Die Weiche: Nur Android/iOS nutzen Capacitor. 
        // WASM, WINDOWS_SERVER und WINDOWS_CLIENT nutzen die Web-JS-Bridge (Kamera).
        protected bool IsCapacitorNative =>
            _platform!.GetCurrPlatform() == PLATFORMS.ANDROID ||
            _platform.GetCurrPlatform() == PLATFORMS.IOS;

        protected bool IsBlazorServer =>
            _platform!.GetCurrPlatform() == PLATFORMS.WINDOWS_SERVER;

        protected bool IsBlazorWebAssembly =>
            _platform!.GetCurrPlatform() == PLATFORMS.WASM;

        protected bool IsBlazorWpf =>
            _platform!.GetCurrPlatform() == PLATFORMS.WINDOWS_CLIENT;
        //protected bool SupportsWebCamera =>
        //    IsBlazorWebAssembly || IsBlazorServer; // Server kann Kamera via JS interop!

        /// <summary>
        /// Prompts the user to select a photo from the device's gallery or file system.
        /// Native: Uses Capacitor Camera. Web: Uses File Input.
        /// </summary>
        //public virtual async Task<ScalarModel> PickPhotoAsync(
        //    Microsoft.AspNetCore.Components.ElementReference? fileInput = null,
        //    int? imageSize = null,
        //    int? quality = null,
        //    bool crop = false,
        //    int? thumbSize = null)
        //{
        //    await _appState!.Log("[Media] PickPhoto started");
        //    try
        //    {
        //        var prefix = IsCapacitorNative ? JsCapPrefix : JsWebPrefix;
        //        var finalImageSize = imageSize ?? DefaultImageSize;
        //        var finalQuality = quality ?? DefaultQuality;
        //        var finalThumbSize = thumbSize ?? 128;

        //        // WEICHE FÜR BLAZOR SERVER
        //        if (IsBlazorServer && !IsCapacitorNative)
        //        {
        //            // Wir rufen die Void-Variante auf. 
        //            // Die Bilddaten kommen NUR über den JS-Callback "SetOptimizedImageData".
        //            await _js.InvokeVoidAsync(
        //                $"{prefix}.pickPhotoVoid",
        //                fileInput,
        //                _dotNetRef,
        //                finalImageSize,
        //                finalQuality,
        //                crop,
        //                finalThumbSize
        //            );

        //            // Wir geben ein "Dummy"-Erfolgsobjekt zurück, da die Daten asynchron fließen
        //            return new ScalarModel { out_value_bool = true };
        //        }

        //        // STANDARD-FALL (WASM / Native)
        //        // Hier ist der doppelte Payload egal (WASM) oder gewollt (Native)
        //        return await _js.InvokeAsync<ScalarModel>(
        //            $"{prefix}.pickPhoto",
        //            fileInput,
        //            _dotNetRef,
        //            finalImageSize,
        //            finalQuality,
        //            crop,
        //            finalThumbSize
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        await _appState.Error($"[Media] PickPhoto Exception: {ex.Message}");
        //        return new ScalarModel { out_err = ex.Message };
        //    }
        //}
        public virtual async Task<ScalarModel> PickPhotoAsync(
            Microsoft.AspNetCore.Components.ElementReference? fileInput = null,
            int? imageSize = null,
            int? quality = null,
            bool crop = false,
            int? thumbSize = null)
        {
            await _appState!.Log("[Media] PickPhoto started");
            try
            {
                if (_platform == null) return new ScalarModel { out_err = "_platform == null)" };

                var prefix = IsCapacitorNative ? JsCapPrefix : JsWebPrefix;
                var finalImageSize = imageSize ?? DefaultImageSize;
                var finalQuality = quality ?? DefaultQuality;
                var finalThumbSize = thumbSize ?? 128;
                var currPlatform = _platform.GetCurrPlatform();

                switch (currPlatform)
                {
                    case PLATFORMS.WINDOWS_SERVER:
                    case PLATFORMS.WASM:
                        // Wir rufen die Void-Variante auf. 
                        // Die Bilddaten kommen NUR über den JS-Callback "SetOptimizedImageData".
                        await _js.InvokeVoidAsync(
                            $"{prefix}.pickPhotoVoid",
                            fileInput,
                            _dotNetRef,
                            finalImageSize,
                            finalQuality,
                            crop,
                            finalThumbSize
                        );

                        // Wir geben ein "Dummy"-Erfolgsobjekt zurück, da die Daten asynchron fließen
                        return new ScalarModel { out_value_bool = true };
                                            
                    case PLATFORMS.ANDROID:
                    case PLATFORMS.IOS:
                    case PLATFORMS.MAC_CLIENT:
                        // STANDARD-FALL (WASM / Native)
                        // Hier ist der doppelte Payload egal (WASM) oder gewollt (Native)
                        return await _js.InvokeAsync<ScalarModel>(
                            $"{prefix}.pickPhoto",
                            fileInput,
                            _dotNetRef,
                            finalImageSize,
                            finalQuality,
                            crop,
                            finalThumbSize
                        );

                    case PLATFORMS.WINDOWS_CLIENT:
                        // Das problem ist, dass Capacitor bereits eine Optimierung vornimmt, bevor die Daten zurückkommen.
                        // wir haben deshalb bei Web gleichgezogen und machen dort auch eine Bildoptimierung.
                        // Bei Windows holen wir dann einfach Bild als Byte und verarbeiten weiter.
                        return await _platform.FilePicker("Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png", "Select image");

                    default:
                        return new ScalarModel { out_err = "no platform" };
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[Media] PickPhoto Exception: {ex.Message}");
                return new ScalarModel { out_err = ex.Message };
            }
        }

        /// <summary>
        /// Prompts the user to select a video from the device's gallery or file system.
        /// </summary>
        public virtual async Task<ScalarModel> PickVideoAsync()
        {
            await _appState!.Warn("[Media] PickVideo not fully implemented");
            return new ScalarModel { out_err = "Not implemented" };
        }

        /// <summary>
        /// Captures a new photo using the device's camera with custom parameters.
        /// </summary>
        //public async Task<ScalarModel> CapturePhotoAsync(
        //    int? imageSize = null,
        //    int? quality = null,
        //    bool crop = false,
        //    int? thumbSize = null,
        //    string videoElementId = "cameraFeed") // Genereller Standard jetzt cameraFeed
        //{
        //    try
        //    {
        //        var prefix = IsCapacitorNative ? JsCapPrefix : JsWebPrefix;

        //        return await _js.InvokeAsync<ScalarModel>(
        //            $"{prefix}.capturePhoto",
        //            _dotNetRef,
        //            videoElementId,
        //            imageSize ?? DefaultImageSize,
        //            quality ?? DefaultQuality,
        //            crop,
        //            thumbSize ?? 128
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        await _appState!.Error($"[Media] CapturePhoto Exception: {ex.Message}");
        //        return new ScalarModel { out_err = ex.Message };
        //    }
        //}
        public async Task<ScalarModel> CapturePhotoAsync(
            int? imageSize = null,
            int? quality = null,
            bool crop = false,
            int? thumbSize = null,
            string videoElementId = "cameraFeed")
        {
            try
            {
                var prefix = IsCapacitorNative ? JsCapPrefix : JsWebPrefix;

                // BLAZOR SERVER OPTIMIERUNG
                if (IsBlazorServer)
                {
                    // Wir rufen die neue Void-Methode auf
                    await _js.InvokeVoidAsync($"{prefix}.capturePhotoVoid",
                        _dotNetRef, videoElementId, imageSize ?? DefaultImageSize,
                        quality ?? DefaultQuality, crop, thumbSize ?? 128);

                    return new ScalarModel { out_value_bool = true };
                }

                // WASM / NATIVE (hier wollen wir die Daten im Rückgabewert behalten)
                return await _js.InvokeAsync<ScalarModel>(
                    $"{prefix}.capturePhoto",
                    _dotNetRef, videoElementId, imageSize ?? DefaultImageSize,
                    quality ?? DefaultQuality, crop, thumbSize ?? 128
                );
            }
            catch (Exception ex)
            {
                await _appState!.Error($"[Media] CapturePhoto Exception: {ex.Message}");
                return new ScalarModel { out_err = ex.Message };
            }
        }

        /// <summary>
        /// Captures a new video using the device's camera.
        /// </summary>
        public async Task<ScalarModel> CaptureVideoAsync()
        {
            await _appState!.Warn("[Media] CaptureVideo not fully implemented");
            return new ScalarModel { out_err = "Not implemented" };
        }

        /// <summary>
        /// (Web only) Starts the camera stream for in-browser capturing.
        /// </summary>
        public async Task StartCameraAsync(string videoElementId = "cameraFeed")
        {
            if (!IsCapacitorNative)
            {
                await _appState!.Log($"[Media] StartCamera (Web) for element: {videoElementId}");
                await _js.InvokeVoidAsync($"{JsWebPrefix}.startCamera", videoElementId);
            }
        }

        /// <summary>
        /// (Web only) Stops the camera stream and releases hardware resources.
        /// </summary>
        public async Task StopCameraAsync(string videoElementId = "cameraFeed")
        {
            if (!IsCapacitorNative)
            {
                await _appState!.Log($"[Media] StopCamera (Web) for element: {videoElementId}");
                // Wir nutzen hier die übergebene ID (Standard: cameraFeed)
                await _js.InvokeVoidAsync($"{JsWebPrefix}.stopCamera", videoElementId);
            }
        }

        /// <summary>
        /// Normalisiert den Bildstring für die Datenbank (entfernt den Data-URL Header).
        /// Erwartet: "data:image/jpeg;base64,/9j/4AAQ..."
        /// Rückgabe: "/9j/4AAQ..."
        /// </summary>
        private string NormalizeBase64(string base64Data)
        {
            if (string.IsNullOrWhiteSpace(base64Data))
                return string.Empty;

            // Wir suchen das Komma, das den Header vom Content trennt
            int commaIndex = base64Data.IndexOf(',');

            // Wenn ein Komma existiert und danach noch Daten kommen
            if (commaIndex >= 0 && commaIndex < base64Data.Length - 1)
            {
                return base64Data.Substring(commaIndex + 1);
            }

            // Falls kein Komma gefunden wurde (Daten sind evtl. schon sauber), 
            // geben wir den Originalstring zurück
            return base64Data;
        }

        private async Task OnDeletePhoto()
        {
            if (OnDelete.HasDelegate)
            {
                await OnDelete.InvokeAsync();
            }
        }

        private void OnclickPreview()
        {
            IsPreviewOpen = true;
            StateHasChanged();
        }

        private async Task OnPickPhoto()
        {
            _userErrorMessage = null;
            _isFileProcessing = true;
            StateHasChanged();

            try
            {
                // Wir gönnen der UI einen Moment Zeit für den Ladeindikator
                await Task.Delay(200);

                // Der MediaService kümmert sich nun intern um die Unterscheidung 
                // zwischen Capacitor (Native) und Web-Browser.
                // Wir übergeben bei Bedarf das _fileInput (für Web relevant).

                // Hinweis: Da dein Service PickPhotoAsync() ohne Parameter definiert hat, 
                // nutzt er intern den globalen Picker oder ein unsichtbares Element.
                ScalarModel result = await PickPhotoAsync(
                    _fileInput,
                    ImageSize,     // Nutzt jetzt den Parameter der Komponente (Default 500)
                    ImageQuality,  // Nutzt den Parameter (Default 75)
                    CropToSquare,  // Nutzt den Parameter (Default true)
                    ThumbnailSize  // Falls vorhanden
                );

                if (result != null)
                {
                    if (!string.IsNullOrEmpty(result.out_err))
                    {
                        // Fehlerbehandlung basierend auf der Antwort vom Service/JS
                        _userErrorMessage = $"Hinweis: {result.out_err}";
                    }
                    else if (result.out_bytes != null && result.out_bytes.Length > 0)
                    {
                        // Wir haben valide Bytes! 
                        // Wir übergeben die Bytes direkt an die Verarbeitungslogik
                        await ProcessImageBytes(result.out_bytes, isAlreadyOptimized: true);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_appState != null)
                    await _appState.Log($"Ein Fehler ist bei der Bildauswahl aufgetreten: {ex.Message}");
            }
            finally
            {
                _isFileProcessing = false;
                StateHasChanged();
            }
        }

        private async Task OnStartCamera()
        {
            _userErrorMessage = null;
            _isCameraProcessing = true;
            StateHasChanged();

            try
            {
                await Task.Delay(200);

                if (_appState!.IsPhone)
                {
                    // Nativer Modus über den Service - jetzt mit Parametern!
                    // var result = await _media!.CapturePhotoAsync(
                    var result = await CapturePhotoAsync(
                        ImageSize,
                        ImageQuality,
                        CropToSquare,
                        ThumbnailSize
                    );

                    if (result != null)
                    {
                        if (!string.IsNullOrEmpty(result.out_err))
                        {
                            //// "User_Cancelled" (wie im JS besprochen) fangen wir ab
                            //if (result.out_err != "User_Cancelled")
                            //{
                            //    _userErrorMessage = $"Kamera-Hinweis: {result.out_err}";
                            //}
                        }
                        else if (result.out_bytes != null && result.out_bytes.Length > 0)
                        {
                            // Erfolg: Bytes direkt verarbeiten
                            await ProcessImageBytes(result.out_bytes, isAlreadyOptimized: true);
                        }
                    }
                }
                else
                {
                    // Web Modus (Dialog-Anzeige bleibt wie gehabt)
                    _showCameraDialog = true;
                    if (_DisableWebCameraButton)
                    {
                        StateHasChanged();
                        await Task.Delay(1000);
                        _DisableWebCameraButton = false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_appState != null)
                    await _appState.Log($"Fehler bei der Kamera-Aufnahme: {ex.Message}");
            }
            finally
            {
                _isCameraProcessing = false;
                StateHasChanged();
            }
        }

        //private async Task OnCapturePhoto()
        //{
        //    _userErrorMessage = null;

        //    if (!UseNativeFeatures)
        //    {
        //        _isCameraProcessing = true;
        //        _isProcessing = true;
        //        await Task.Delay(200); // Kurze Verzögerung, um den Ladeindikator anzuzeigen
        //        _isCameraProcessing = false;
        //        _isProcessing = false;

        //        try
        //        {
        //            // Wir behalten deinen lokalen _dotNetRef bei, da die Komponente 
        //            // den Callback "SetOptimizedImageData" empfängt.
        //            _dotNetRef ??= DotNetObjectReference.Create(this);

        //            // NUR DIESE ZEILE WURDE ERSETZT:
        //            // Statt _jsRuntime.InvokeVoidAsync nutzen wir den Service
        //            // await _media!.CapturePhotoAsync(
        //            await CapturePhotoAsync(
        //                ImageSize,
        //                ImageQuality,
        //                CropToSquare,
        //                ThumbnailSize, // Falls der Service den 6. Parameter thumbSize erwartet
        //                "cameraFeed"   // Die Video-ID
        //            );
        //        }
        //        catch (Exception ex)
        //        {
        //            if (_appState != null)
        //                await _appState.Log($"Ein Fehler ist bei der Bildaufnahme aufgetreten: {ex.Message}");
        //        }
        //        finally
        //        {
        //            _isCameraProcessing = false;
        //            _isProcessing = false;
        //        }
        //    }
        //}
        private async Task<bool> WaitForVideoElementAsync(string elementId, int timeoutMs = 3000)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    // Prüfe alle Bedingungen
                    var isReady = await _js.InvokeAsync<bool>("eval",
                        $@"
                        (function() {{
                            var video = document.getElementById('{elementId}');
                            if (!video) return false;
                            if (!video.srcObject) return false;
                            if (video.readyState < 2) return false;
                            if (video.videoWidth === 0) return false;
                            return true;
                        }})()
                        ");

                    if (isReady)
                        return true;
                }
                catch
                {
                    // Ignoriere JS-Fehler während des Wartens
                }

                await Task.Delay(100);
            }

            return false;
        }

        //private async Task OnCapturePhoto()
        //{
        //    _userErrorMessage = null;
        //    _isCameraProcessing = true;
        //    _isProcessing = true;
        //    StateHasChanged();

        //    try
        //    {
        //        // DotNetRef sicherstellen
        //        _dotNetRef ??= DotNetObjectReference.Create(this);

        //        // FÜR WEB (WASM und Server): Warte auf Video-Element
        //        if (!IsCapacitorNative && !UseNativeFeatures && (IsBlazorWebAssembly || IsBlazorServer))
        //        {
        //            bool isReady = await WaitForVideoElementAsync("cameraFeed", 3000);

        //            if (!isReady)
        //            {
        //                _userErrorMessage = "Kamera ist noch nicht bereit. Bitte warten Sie einen Moment.";
        //                return;
        //            }
        //        }

        //        // GEMEINSAMER AUFRUF
        //        await CapturePhotoAsync(
        //            ImageSize,
        //            ImageQuality,
        //            CropToSquare,
        //            ThumbnailSize,
        //            "cameraFeed"
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        if (_appState != null)
        //            await _appState.Log($"Fehler bei Bildaufnahme: {ex.Message}");
        //        _userErrorMessage = $"Fehler: {ex.Message}";
        //    }
        //    finally
        //    {
        //        _isCameraProcessing = false;
        //        _isProcessing = false;
        //        StateHasChanged();
        //    }
        //}
        private async Task OnCapturePhoto()
        {
            _userErrorMessage = null;
            _isCameraProcessing = true;
            _isProcessing = true;
            StateHasChanged();

            try
            {
                _dotNetRef ??= DotNetObjectReference.Create(this);

                // --- PLATTFORM-STEUERUNG ---

                if (IsCapacitorNative)
                {
                    // CASE 1: Mobile App (Android/iOS)
                    // Nutzt native Capacitor-Kamera, kein JS-Warten nötig
                    await CapturePhotoAsync(ImageSize, ImageQuality, CropToSquare, ThumbnailSize, "cameraFeed");
                }
                else if (IsBlazorWebAssembly || IsBlazorWpf)
                {
                    // CASE 2: WASM (Lokal/Browser)
                    // Hier funktioniert dein aktueller Code perfekt.
                    // Das Polling via 'eval' ist lokal kein Problem.
                    bool isReady = await WaitForVideoElementAsync("cameraFeed", 3000);
                    if (!isReady)
                    {
                        _userErrorMessage = "Kamera (WASM) ist noch nicht bereit.";
                        return;
                    }
                    await CapturePhotoAsync(ImageSize, ImageQuality, CropToSquare, ThumbnailSize, "cameraFeed");
                }
                else if (IsBlazorServer)
                {
                    // CASE 3: BLAZOR SERVER (SignalR)
                    // Hier liegt der Fehler! Wir lassen WaitForVideoElementAsync WEG,
                    // da SignalR sonst mit 'eval'-Anfragen geflutet wird.
                    // Wir vertrauen der 'while'-Schleife in deiner app.js (capturePhoto).

                    if (_appState != null) await _appState.Log("Server-Mode: Direkter JS-Aufruf ohne C#-Polling.");

                    await CapturePhotoAsync(ImageSize, ImageQuality, CropToSquare, ThumbnailSize, "cameraFeed");
                }
                else
                {
                    // CASE 4: WPF / Sonstige
                    // Meist WebView2, verhält sich ähnlich wie WASM
                    await CapturePhotoAsync(ImageSize, ImageQuality, CropToSquare, ThumbnailSize, "cameraFeed");
                }
            }
            catch (Exception ex)
            {
                if (_appState != null)
                    await _appState.Log($"Fehler bei Bildaufnahme: {ex.Message}");
                _userErrorMessage = $"Fehler: {ex.Message}";
            }
            finally
            {
                _isCameraProcessing = false;
                _isProcessing = false;
                StateHasChanged();
            }
        }

        private async Task OnCancelCamera(bool clearImages)
        {
            if (clearImages)
            {
                await ImageDataUrlChanged.InvokeAsync(string.Empty);
                await ThumbnailDataUrlChanged.InvokeAsync(string.Empty);
            }

            _showCameraDialog = false;

            if (!_appState!.IsPhone)
            {
                // PATTERN-CLEANUP: Kein direkter JS-Aufruf mehr!
                // Wir nutzen den Service. Da der Default im Service "cameraFeed" ist,
                // müssen wir hier nicht einmal einen Parameter übergeben.
                //await _media!.StopCameraAsync(); 
                await StopCameraAsync();

                _cameraStarted = false;
            }
        }

        private async Task ProcessImageBytes(byte[]? imageData, bool isAlreadyOptimized = false)
        {
            if (imageData == null || imageData.Length == 0) return;

            try
            {
                // 1. Hauptbild generieren
                // WICHTIG: Hier geben wir 'isAlreadyOptimized' an den Service weiter!
                var mainImageBytes = await _imageResizer!.OptimizeBytesToBytesAsync(
                    imageData,
                    ImageSize,
                    ImageQuality,
                    CropToSquare,
                    format: ImageOutputFormat.Jpeg, // Standard-Format explizit angeben
                    isAlreadyOptimized: isAlreadyOptimized // <-- Hier wird die Information genutzt
                );

                //ImageDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(mainImageBytes)}";
                ImageDataUrl = NormalizeBase64(Convert.ToBase64String(mainImageBytes));
                await ImageDataUrlChanged.InvokeAsync(ImageDataUrl);

                // 2. Optional: Thumbnail generieren
                if (ThumbnailSize.HasValue || ThumbnailQuality.HasValue)
                {
                    // Bei Thumbnails setzen wir isAlreadyOptimized IMMER auf false,
                    // da das Thumbnail ja neu berechnet (verkleinert) werden muss.
                    var thumbnailBytes = await _imageResizer.OptimizeBytesToBytesAsync(
                        imageData,
                        ThumbnailSize!.Value,
                        ThumbnailQuality!.Value,
                        ThumbnailCropToSquare,
                        format: ImageOutputFormat.Jpeg,
                        isAlreadyOptimized: false
                    );

                    //ThumbnailDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(thumbnailBytes)}";
                    ThumbnailDataUrl = NormalizeBase64(Convert.ToBase64String(thumbnailBytes));
                    await ThumbnailDataUrlChanged.InvokeAsync(ThumbnailDataUrl);
                }
                else
                {
                    ThumbnailDataUrl = null;
                    await ThumbnailDataUrlChanged.InvokeAsync(ThumbnailDataUrl);
                }
            }
            catch (Exception ex)
            {
                if (_appState != null)
                    await _appState.Log($"[Component] Error processing image bytes: {ex.Message}");
            }
        }

        private string GetIconClass(string text, string defaultIconClass)
        {
            if (ShowAllButtonIcons || string.IsNullOrEmpty(text))
            {
                return defaultIconClass;
            }
            return string.Empty;
        }

        private string GetLayoutClass()
        {
            switch (PreviewImagePosition)
            {
                case p11.UI.PreviewImagePosition.Top:
                    return "d-flex flex-column";
                case p11.UI.PreviewImagePosition.Bottom:
                    return "d-flex flex-column-reverse";
                case p11.UI.PreviewImagePosition.Left:
                    return "d-flex";
                case p11.UI.PreviewImagePosition.Right:
                    return "d-flex flex-row-reverse";
                default:
                    return "d-flex flex-column";
            }
        }

        //private void SetPlattformForModalNative()
        //{
        //    var currPlatform = _platform!.GetCurrPlatform();
        //    switch (currPlatform)
        //    {
        //        case PLATFORMS.WINDOWS_CLIENT:
        //            _nativeDevice = NativeDevice.WINDOWS;
        //            break;

        //        case PLATFORMS.WASM:
        //            _nativeDevice = NativeDevice.WEB;
        //            break;

        //        case PLATFORMS.ANDROID:
        //            _nativeDevice = NativeDevice.ANDROID;
        //            _usePhoneFeatures = true;
        //            break;

        //        case PLATFORMS.IOS:
        //            _nativeDevice = NativeDevice.IPHONE;
        //            _usePhoneFeatures = true;
        //            break;

        //        case PLATFORMS.MAC_CLIENT:
        //            _nativeDevice = NativeDevice.MAC;
        //            break;
        //    }
        //}


    }
}

