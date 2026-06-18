namespace BlazorCore.Services.ImageOptimizer
{
    /// <summary>
    /// Provides functionality for optimizing images, including resizing,
    /// cropping, compression, and conversion to different output formats.
    /// </summary>
    public interface IImageOptimizer
    {
        /// <summary>
        /// Resizes an image to the specified maximum width and height while preserving aspect ratio.
        /// Optionally applies JPEG compression with the given quality.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="maxWidth">The maximum width of the output image.</param>
        /// <param name="maxHeight">The maximum height of the output image.</param>
        /// <param name="quality">The compression quality (1–100, only relevant for JPEG/WebP).</param>
        /// <returns>A stream containing the resized image.</returns>
        Task<Stream> ResizeImageAsync(Stream imageStream, int maxWidth, int maxHeight);

        /// <summary>
        /// Resizes an image to fit within the specified maximum size (applied to the larger dimension),
        /// preserving aspect ratio.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="maxSize">The maximum size for width or height, whichever is larger.</param>
        /// <returns>A stream containing the resized image.</returns>
        Task<Stream> ResizeImageAsync(Stream imageStream, int maxSize);

        /// <summary>
        /// Crops an image to a centered square of the specified size.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="size">The size (width and height) of the square crop.</param>
        /// <returns>A stream containing the cropped square image.</returns>
        Task<Stream> CropToSquareAsync(Stream imageStream, int size);

        /// <summary>
        /// Crops an image to the specified rectangle defined by position and size.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="x">The x-coordinate of the top-left corner of the crop area.</param>
        /// <param name="y">The y-coordinate of the top-left corner of the crop area.</param>
        /// <param name="width">The width of the crop area.</param>
        /// <param name="height">The height of the crop area.</param>
        /// <returns>A stream containing the cropped image.</returns>
        Task<Stream> CropImageAsync(Stream imageStream, int x, int y, int width, int height);

        /// <summary>
        /// Compresses an image using the specified quality and output format.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="quality">The compression quality (1–100, only relevant for JPEG/WebP).</param>
        /// <param name="format">The desired output format (JPEG, PNG, WebP, etc.).</param>
        /// <returns>A stream containing the compressed image.</returns>
        Task<Stream> CompressImageAsync(Stream imageStream, long quality, ImageOutputFormat format = ImageOutputFormat.Jpeg);

        /// <summary>
        /// Gets the dimensions (width and height) of an image without modifying it.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <returns>A tuple containing the width and height of the image.</returns>
        //Task<(int width, int height)> GetImageDimensionsAsync(Stream imageStream);

        /// <summary>
        /// Optimizes an image by resizing, optional cropping, and compression.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="maxSize">The maximum size for width or height.</param>
        /// <param name="quality">The compression quality (1–100, only relevant for JPEG/WebP).</param>
        /// <param name="crop">Whether to crop the image (usually to square).</param>
        /// <returns>A stream containing the optimized image.</returns>
        Task<Stream> OptimizeImageAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true);

        /// <summary>
        /// Optimizes an image and returns the result as a byte array instead of a stream.
        /// Useful for scenarios where the image needs to be encoded as Base64 (e.g., in Razor components).
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <param name="maxSize">The maximum size for width or height.</param>
        /// <param name="quality">The compression quality (1–100, only relevant for JPEG/WebP).</param>
        /// <param name="crop">Whether to crop the image (usually to square).</param>
        /// <param name="format">The desired output format (JPEG by default).</param>
        /// <returns>A byte array containing the optimized image.</returns>
        Task<byte[]> OptimizeImageToBytesAsync(
            Stream imageStream,
            int maxSize,
            long quality = 40L,
            bool crop = true,
            ImageOutputFormat format = ImageOutputFormat.Jpeg);

        // Neue Methode im Interface, um unnötige Stream-Kopien zu vermeiden
        Task<byte[]> OptimizeBytesToBytesAsync(
            byte[] imageBytes,
            int maxSize,
            long quality = 40L,
            bool crop = true,
            ImageOutputFormat format = ImageOutputFormat.Jpeg,
            bool isAlreadyOptimized = false);
    }

    /// <summary>
    /// Specifies the output format for compressed images.
    /// </summary>
    public enum ImageOutputFormat
    {
        /// <summary>
        /// JPEG format - best for photographs with good compression.
        /// </summary>
        Jpeg,
        
        /// <summary>
        /// PNG format - best for images with transparency or sharp edges.
        /// </summary>
        Png,
        
        /// <summary>
        /// WebP format - modern format with excellent compression (if supported by platform).
        /// </summary>
        WebP
    }
}
