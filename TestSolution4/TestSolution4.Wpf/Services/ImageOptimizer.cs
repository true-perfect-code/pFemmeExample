using BlazorCore.Services.ImageOptimizer;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TestSolution4.Wpf.Services
{
    public class ImageOptimizer : IImageOptimizer
    {
        public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxWidth, int maxHeight)
        {
            return await Task.Run(() =>
            {
                var bitmap = DecodeStream(imageStream);
                double ratioX = (double)maxWidth / bitmap.PixelWidth;
                double ratioY = (double)maxHeight / bitmap.PixelHeight;
                double ratio = Math.Min(ratioX, ratioY);

                int newWidth = (int)(bitmap.PixelWidth * ratio);
                int newHeight = (int)(bitmap.PixelHeight * ratio);

                return EncodeToStream(ResizeBitmap(bitmap, newWidth, newHeight), ImageOutputFormat.Jpeg, 80);
            });
        }

        public async Task<Stream> ResizeImageAsync(Stream imageStream, int maxSize)
        {
            var dims = await GetImageDimensionsAsync(imageStream);
            double ratio = dims.width > dims.height
                ? (double)maxSize / dims.width
                : (double)maxSize / dims.height;

            return await ResizeImageAsync(imageStream, (int)(dims.width * ratio), (int)(dims.height * ratio));
        }

        public async Task<Stream> CropToSquareAsync(Stream imageStream, int size)
        {
            return await Task.Run(() =>
            {
                var bitmap = DecodeStream(imageStream);
                int minSide = Math.Min(bitmap.PixelWidth, bitmap.PixelHeight);
                int x = (bitmap.PixelWidth - minSide) / 2;
                int y = (bitmap.PixelHeight - minSide) / 2;

                var cropped = new CroppedBitmap(bitmap, new Int32Rect(x, y, minSide, minSide));
                var resized = ResizeBitmap(cropped, size, size);
                return EncodeToStream(resized, ImageOutputFormat.Jpeg, 80);
            });
        }

        public async Task<Stream> OptimizeImageAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true)
        {
            if (crop) return await CropToSquareAsync(imageStream, maxSize);
            return await ResizeImageAsync(imageStream, maxSize);
        }

        public async Task<byte[]> OptimizeImageToBytesAsync(Stream imageStream, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg)
        {
            using var stream = await OptimizeImageAsync(imageStream, maxSize, quality, crop);
            return ((MemoryStream)stream).ToArray();
        }

        public async Task<Stream> CompressImageAsync(Stream imageStream, long quality, ImageOutputFormat format = ImageOutputFormat.Jpeg)
        {
            return await Task.Run(() =>
            {
                var bitmap = DecodeStream(imageStream);
                return EncodeToStream(bitmap, format, (int)quality);
            });
        }

        public async Task<(int width, int height)> GetImageDimensionsAsync(Stream imageStream)
        {
            return await Task.Run(() =>
            {
                var bitmap = DecodeStream(imageStream);
                return (bitmap.PixelWidth, bitmap.PixelHeight);
            });
        }

        public async Task<Stream> CropImageAsync(Stream imageStream, int x, int y, int width, int height)
        {
            return await Task.Run(() =>
            {
                var bitmap = DecodeStream(imageStream);
                var cropped = new CroppedBitmap(bitmap, new Int32Rect(x, y, width, height));
                return EncodeToStream(cropped, ImageOutputFormat.Jpeg, 80);
            });
        }

        public async Task<byte[]> OptimizeBytesToBytesAsync(byte[] imageBytes, int maxSize, long quality = 40L, bool crop = true, ImageOutputFormat format = ImageOutputFormat.Jpeg, bool isAlreadyOptimized = false)
        {
            if (imageBytes == null || imageBytes.Length == 0) return Array.Empty<byte>();

            return await Task.Run(async () =>
            {
                using (var msInput = new MemoryStream(imageBytes))
                {
                    // Wir nutzen die bestehende OptimizeImageAsync Methode
                    using (var optimizedStream = await OptimizeImageAsync(msInput, maxSize, quality, crop))
                    {
                        // Falls ein anderes Format als Jpeg gewünscht ist, konvertieren wir hier
                        if (format != ImageOutputFormat.Jpeg)
                        {
                            var bitmap = DecodeStream(optimizedStream);
                            using (var finalStream = EncodeToStream(bitmap, format, (int)quality))
                            {
                                return ((MemoryStream)finalStream).ToArray();
                            }
                        }

                        // Da OptimizeImageAsync in WPF einen MemoryStream zurückgibt:
                        return ((MemoryStream)optimizedStream).ToArray();
                    }
                }
            });
        }

        // ---- Private Helpers (WPF Native) ----

        private BitmapSource DecodeStream(Stream stream)
        {
            if (stream.CanSeek) stream.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze(); // Wichtig für Thread-Sicherheit
            return bitmap;
        }

        private BitmapSource ResizeBitmap(BitmapSource source, int width, int height)
        {
            var target = new TransformedBitmap(source, new ScaleTransform(
                (double)width / source.PixelWidth,
                (double)height / source.PixelHeight));
            target.Freeze();
            return target;
        }

        private Stream EncodeToStream(BitmapSource source, ImageOutputFormat format, int quality)
        {
            BitmapEncoder encoder = format switch
            {
                ImageOutputFormat.Png => new PngBitmapEncoder(),
                _ => new JpegBitmapEncoder { QualityLevel = quality }
            };

            encoder.Frames.Add(BitmapFrame.Create(source));
            var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
