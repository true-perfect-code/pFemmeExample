using BlazorCore.Services.ImageOptimizer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace TestSolution4.Web.Services
{
    public class ImageOptimizer : IImageOptimizer
    {
        // ---- Public API Methods ----

        public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxWidth, int maxHeight)
        {
            if (maxWidth <= 0 || maxHeight <= 0)
                throw new ArgumentOutOfRangeException("Width and height must be positive");

            var preparedStream = await PrepareStreamSafeAsync(imageStream);

            using var image = await Image.LoadAsync(preparedStream);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

            var output = new MemoryStream();
            var format = image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;
            await image.SaveAsync(output, format);
            output.Position = 0;
            return output;
        }

        public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxSize)
        {
            return await ResizeImageAsync(imageStream, maxSize, maxSize);
        }

        public async Task<Stream> CropToSquareAsync(Stream imageStream, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            var preparedStream = await PrepareStreamSafeAsync(imageStream);

            using var image = await Image.LoadAsync(preparedStream);

            int minDim = Math.Min(image.Width, image.Height);
            int x = (image.Width - minDim) / 2;
            int y = (image.Height - minDim) / 2;

            image.Mutate(xf => xf.Crop(new Rectangle(x, y, minDim, minDim))
                                 .Resize(size, size));

            var output = new MemoryStream();
            var format = image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;
            await image.SaveAsync(output, format);
            output.Position = 0;
            return output;
        }

        public async Task<Stream> CropImageAsync(Stream imageStream, int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Width and height must be positive");

            var preparedStream = await PrepareStreamSafeAsync(imageStream);

            using var image = await Image.LoadAsync(preparedStream);

            var rect = new Rectangle(x, y, width, height);
            image.Mutate(xf => xf.Crop(rect));

            var output = new MemoryStream();
            var format = image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;
            await image.SaveAsync(output, format);
            output.Position = 0;
            return output;
        }

        public async Task<Stream> CompressImageAsync(Stream imageStream, long quality, ImageOutputFormat format = ImageOutputFormat.Jpeg)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality));

            var preparedStream = await PrepareStreamSafeAsync(imageStream);

            using var image = await Image.LoadAsync(preparedStream);

            var output = new MemoryStream();
            IImageEncoder encoder = format switch
            {
                ImageOutputFormat.Jpeg => new JpegEncoder { Quality = (int)quality },
                ImageOutputFormat.Png => new PngEncoder(),
                ImageOutputFormat.WebP => new WebpEncoder { Quality = (int)quality },
                _ => new JpegEncoder { Quality = (int)quality }
            };

            await image.SaveAsync(output, encoder);
            output.Position = 0;
            return output;
        }

        public async Task<(int width, int height)> GetImageDimensionsAsync(Stream imageStream)
        {
            var preparedStream = await PrepareStreamSafeAsync(imageStream);
            var info = await Image.IdentifyAsync(preparedStream);
            if (info == null) throw new InvalidOperationException("Could not identify image");
            return (info.Width, info.Height);
        }

        public async Task<Stream> OptimizeImageAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true)
        {
            var preparedStream = await PrepareStreamSafeAsync(imageStream);

            // Resize
            using var resized = await ResizeImageAsync(preparedStream, maxSize, maxSize);

            // Crop optional
            Stream afterCrop = resized;
            if (crop)
            {
                afterCrop = await CropToSquareAsync(resized, maxSize);
            }

            // Compression
            var compressed = await CompressImageAsync(afterCrop, quality, ImageOutputFormat.Jpeg);

            return compressed;
        }

        public async Task<byte[]> OptimizeImageToBytesAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg)
        {
            using var optimizedStream = await OptimizeImageAsync(imageStream, maxSize, quality, crop);

            if (format != ImageOutputFormat.Jpeg)
            {
                optimizedStream.Position = 0;
                using var image = await Image.LoadAsync(optimizedStream);
                var output = new MemoryStream();
                IImageEncoder encoder = format switch
                {
                    ImageOutputFormat.Png => new PngEncoder(),
                    ImageOutputFormat.WebP => new WebpEncoder { Quality = (int)quality },
                    _ => new JpegEncoder { Quality = (int)quality }
                };
                await image.SaveAsync(output, encoder);
                return output.ToArray();
            }

            return ((MemoryStream)optimizedStream).ToArray();
        }

        public async Task<byte[]> OptimizeBytesToBytesAsync(byte[] imageBytes, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg, bool isAlreadyOptimized = false)
        {
            if (imageBytes == null || imageBytes.Length == 0) return Array.Empty<byte>();

            // Wir nutzen einen MemoryStream als Wrapper für das vorhandene Array
            using var input = new MemoryStream(imageBytes);

            // Wir rufen die bereits existierende OptimizeImageAsync Methode auf
            using var optimizedStream = await OptimizeImageAsync(input, maxSize, quality, crop);

            // Hier nutzen wir exakt deine Logik für das Encoding
            if (format != ImageOutputFormat.Jpeg)
            {
                optimizedStream.Position = 0;
                using var image = await Image.LoadAsync(optimizedStream);
                var output = new MemoryStream();

                IImageEncoder encoder = format switch
                {
                    ImageOutputFormat.Png => new PngEncoder(),
                    ImageOutputFormat.WebP => new WebpEncoder { Quality = (int)quality },
                    _ => new JpegEncoder { Quality = (int)quality }
                };

                await image.SaveAsync(output, encoder);
                return output.ToArray();
            }

            // Fallback: Wenn es Jpeg ist, ist es bereits im optimizedStream
            if (optimizedStream is MemoryStream ms)
            {
                return ms.ToArray();
            }

            using var finalMs = new MemoryStream();
            await optimizedStream.CopyToAsync(finalMs);
            return finalMs.ToArray();
        }



        // ---- Private Helper ----
        private static async Task<Stream> PrepareStreamSafeAsync(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (stream.CanSeek)
            {
                stream.Position = 0;
                return stream;
            }

            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
