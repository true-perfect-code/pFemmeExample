using Microsoft.JSInterop;
using BlazorCore.Services.ImageOptimizer;
using BlazorCore.Services.Platform;

namespace TestSolution4.Shared.Services.ImageOptimizer
{
    public class ImageOptimizer : IImageOptimizer
    {
        private readonly IJSRuntime _js;
        private readonly IPlatformBase _platform;

        private const string JsCapPrefix = "pE_Capacitor.media";
        private const string JsWebPrefix = "pE_Web.media"; // Angepasst an pE_Web Struktur

        public ImageOptimizer(IJSRuntime js, IPlatformBase platform)
        {
            _js = js;
            _platform = platform;
        }

        private bool IsNative => _platform.GetCurrPlatform() != PLATFORMS.WASM;

        //public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxWidth, int maxHeight)
        //{
        //    var prefix = IsNative ? JsCapPrefix : JsWebPrefix;

        //    var base64 = await StreamToBase64Async(imageStream);
        //    var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
        //        new { maxWidth, maxHeight, cropToSquare = false });
        //    return Base64ToStream(result);
        //}
        public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxWidth, int maxHeight)
        {
            var prefix = IsNative ? JsCapPrefix : JsWebPrefix;
            var base64 = await StreamToBase64Async(imageStream);

            var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                new
                {
                    maxWidth,
                    maxHeight,
                    maxSize = Math.Max(maxWidth, maxHeight), // Wichtig für Web-JS Fallback
                    quality = 80,
                    cropToSquare = false
                });
            return Base64ToStream(result);
        }

        public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxSize)
        {
            var prefix = IsNative ? JsCapPrefix : JsWebPrefix;

            var base64 = await StreamToBase64Async(imageStream);
            // Übergabe von maxSize; JS berechnet das Seitenverhältnis automatisch
            var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                new { maxSize, cropToSquare = false });
            return Base64ToStream(result);
        }

        public async Task<Stream> CropToSquareAsync(Stream imageStream, int size)
        {
            var prefix = IsNative ? JsCapPrefix : JsWebPrefix;

            var base64 = await StreamToBase64Async(imageStream);
            var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                new { maxSize = size, cropToSquare = true });
            return Base64ToStream(result);
        }

        public async Task<Stream> OptimizeImageAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true)
        {
            var prefix = IsNative ? JsCapPrefix : JsWebPrefix;

            var base64 = await StreamToBase64Async(imageStream);
            var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                new { maxSize, quality, cropToSquare = crop });
            return Base64ToStream(result);
        }

        public async Task<byte[]> OptimizeImageToBytesAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg)
        {
            var prefix = IsNative ? JsCapPrefix : JsWebPrefix;
            var base64 = await StreamToBase64Async(imageStream);
            var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                new { maxSize, quality, cropToSquare = crop, format = (int)format });

            if (string.IsNullOrEmpty(result)) return Array.Empty<byte>();

            // Header-Check für Stabilität hinzufügen
            var pureBase64 = result.Contains(",") ? result.Split(',')[1] : result;
            return Convert.FromBase64String(pureBase64);
        }

        public async Task<Stream> CompressImageAsync(Stream imageStream, long quality, ImageOutputFormat format = ImageOutputFormat.Jpeg)
        {
            var prefix = IsNative ? JsCapPrefix : JsWebPrefix;

            var base64 = await StreamToBase64Async(imageStream);
            var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                new { quality, format = (int)format, cropToSquare = false });
            return Base64ToStream(result);
        }

        public async Task<Stream> CropImageAsync(Stream imageStream, int x, int y, int width, int height)
        {
            // Hinweis: Falls du spezielles freies Cropping (X, Y) oft brauchst, 
            // müssten wir die JS processImage Funktion noch um sx/sy erweitern.
            // Aktuell nutzen wir als Fallback die Resize-Logik.
            return await ResizeImageAsync(imageStream, width, height);
        }

        //public async Task<byte[]> OptimizeBytesToBytesAsync(byte[] imageBytes, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg, bool isAlreadyOptimized = false)
        //{
        //    if (imageBytes == null || imageBytes.Length == 0)
        //        return Array.Empty<byte>();

        //    // --- BYPASS LOGIK ---
        //    if (isAlreadyOptimized)
        //    {
        //        // Wir geben die Bytes zurück, die wir erhalten haben.
        //        // WICHTIG: Da diese Bytes von JS kommen, müssen wir sicher sein, 
        //        // dass sie kein 'data:image...'-Präfix als Text-Bytes enthalten.
        //        // Falls dein JS bereits reine Bytes liefert, ist das hier perfekt:
        //        return imageBytes;
        //    }

        //    try
        //    {
        //        var prefix = IsNative ? JsCapPrefix : JsWebPrefix;
        //        var base64 = Convert.ToBase64String(imageBytes);

        //        var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
        //            new
        //            {
        //                maxSize,
        //                quality,
        //                cropToSquare = crop,
        //                format = (int)format
        //            });

        //        if (string.IsNullOrEmpty(result)) return imageBytes;

        //        // Falls das JS einen Header mitschickt, schneiden wir ihn hier ab,
        //        // damit FromBase64String nicht abstürzt.
        //        var pureBase64 = result.Contains(",") ? result.Split(',')[1] : result;

        //        return Convert.FromBase64String(pureBase64);
        //    }
        //    catch (Exception ex)
        //    {
        //        return imageBytes;
        //    }
        //}
        public async Task<byte[]> OptimizeBytesToBytesAsync(byte[] imageBytes, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg, bool isAlreadyOptimized = false)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return Array.Empty<byte>();

            // --- BYPASS LOGIK (C# Ebene) ---
            // Wenn wir bereits optimierte Bytes haben und KEIN Crop (Zuschnitt) angefordert ist,
            // können wir uns den JS-Invoke komplett sparen.
            if (isAlreadyOptimized && !crop)
            {
                return imageBytes;
            }

            try
            {
                var prefix = IsNative ? JsCapPrefix : JsWebPrefix;

                // Umwandlung der Bytes in Base64 für den JS-Transfer
                var base64 = Convert.ToBase64String(imageBytes);

                // Aufruf der harmonisierten JS-Funktion mit allen Parametern
                var result = await _js.InvokeAsync<string>($"{prefix}.processImage", base64,
                    new
                    {
                        maxSize,
                        maxWidth = maxSize,  // Für Kompatibilität mit der neuen JS-Logik
                        maxHeight = maxSize, // Für Kompatibilität mit der neuen JS-Logik
                        quality,
                        cropToSquare = crop,
                        format = (int)format,
                        isAlreadyOptimized = isAlreadyOptimized // Reicht das Flag an Capacitor JS weiter
                    });

                if (string.IsNullOrEmpty(result)) return imageBytes;

                // Falls das JS einen Data-URL Header mitschickt (data:image/jpeg;base64,...),
                // schneiden wir ihn hier ab, damit Convert.FromBase64String nicht abstürzt.
                var pureBase64 = result.Contains(",") ? result.Split(',')[1] : result;

                return Convert.FromBase64String(pureBase64);
            }
            catch (Exception ex)
            {
                // Im Fehlerfall loggen wir intern (optional) und geben die Originaldaten zurück
                return imageBytes;
            }
        }

        // ---- Private Helpers ----

        private async Task<string> StreamToBase64Async(Stream stream)
        {
            if (stream == null) return string.Empty;
            using var ms = new MemoryStream();
            if (stream.CanSeek) stream.Position = 0;
            await stream.CopyToAsync(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        private Stream Base64ToStream(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return new MemoryStream();

            // WICHTIG: Auch hier Header entfernen, falls vorhanden!
            var pureBase64 = base64.Contains(",") ? base64.Split(',')[1] : base64;
            return new MemoryStream(Convert.FromBase64String(pureBase64));
        }

        // Hilfsklasse für die JS-Rückgabe der Dimensionen
        private class ImageSize { public int Width { get; set; } public int Height { get; set; } }
    }

}