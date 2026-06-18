using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Media
{
    /// <summary>
    /// Provides a cross-platform API for interacting with the device's media capabilities.
    /// Returns a <see cref="ScalarModel"/> containing the stream or error information.
    /// </summary>
    public interface IMediaBase
    {
        /// <summary>
        /// Prompts the user to select a photo from the device's gallery or file system.
        /// 
        /// Platform Implementation:
        /// - MAUI: Uses MediaPicker to open the native file picker.
        /// - Blazor Server: Uses a JavaScript file input dialog. (To be implemented or throws NotSupportedException)
        /// </summary>
        //Task<ScalarModel> PickPhotoAsync();
        Task<ScalarModel> PickPhotoAsync(
            Microsoft.AspNetCore.Components.ElementReference? fileInput = null,
            int? imageSize = null,
            int? quality = null,
            bool crop = false,
            int? thumbSize = null
        );

        /// <summary>
        /// Prompts the user to select a video from the device's gallery or file system.
        /// 
        /// Platform Implementation:
        /// - MAUI: Uses MediaPicker to open the native file picker.
        /// - Blazor Server: Not typically supported via this API. (Throws NotSupportedException)
        /// </summary>
        Task<ScalarModel> PickVideoAsync();

        ///// <summary>
        ///// Captures a new photo.
        ///// 
        ///// Platform Implementation:
        ///// - MAUI: Opens the device's native camera app. Returns after the user confirms the photo.
        ///// - Blazor Server: Uses the browser's MediaDevices API to capture a frame from the live camera stream. Requires prior calling of <see cref="StartCameraAsync"/>.
        ///// </summary>
        //Task<ScalarModel> CapturePhotoAsync();
        /// <summary>
        /// Captures a new photo using the device's camera.
        /// 
        /// Platform Implementation:
        /// - Capacitor (Native): Opens the device's native camera app.
        /// - Blazor WASM (Web): Captures a frame from the live camera stream (requires video element).
        /// </summary>
        /// <param name="imageSize">The desired width/height of the resulting image.</param>
        /// <param name="quality">JPEG compression quality (1-100).</param>
        /// <param name="crop">If true, the image will be cropped to a square aspect ratio.</param>
        /// <param name="thumbSize">The size for a thumbnail generation (if supported).</param>
        /// <param name="videoElementId">The ID of the HTML video element used for the Web stream (Default: cameraFeed).</param>
        /// <returns>A ScalarModel containing the image bytes in out_bytes or an error in out_err.</returns>
        Task<ScalarModel> CapturePhotoAsync(
            int? imageSize = null,
            int? quality = null,
            bool crop = false,
            int? thumbSize = null,
            string videoElementId = "cameraFeed"); // Konsistenter Default für das ganze Projekt

        /// <summary>
        /// Captures a new video.
        /// 
        /// Platform Implementation:
        /// - MAUI: Opens the device's native camera app for video recording. Returns after the user stops recording.
        /// - Blazor Server: Not supported in this implementation. (Throws NotSupportedException)
        /// </summary>
        Task<ScalarModel> CaptureVideoAsync();

        // --- Blazor Server Specific Methods ---

        /// <summary>
        /// (Blazor Server only) Starts the camera stream and displays it in the specified video element.
        /// This must be called before <see cref="CapturePhotoAsync"/> on Blazor Server.
        /// 
        /// Platform Implementation:
        /// - MAUI: Not supported. (Throws NotSupportedException)
        /// - Blazor Server: Initializes the camera via JavaScript Interop.
        /// </summary>
        /// <param name="videoElementId">The HTML ID of the video element. Default is "camera".</param>
        Task StartCameraAsync(string videoElementId = "cameraFeed");

        /// <summary>
        /// (Blazor Server only) Stops the camera stream and releases resources.
        /// 
        /// Platform Implementation:
        /// - MAUI: Not supported. (Throws NotSupportedException)
        /// - Blazor Server: Stops the camera tracks via JavaScript Interop.
        /// </summary>
        Task StopCameraAsync(string videoElementId = "cameraFeed");
    }
}
