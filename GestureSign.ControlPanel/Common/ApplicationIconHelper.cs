using GestureSign.Common.Applications;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GestureSign.ControlPanel.Common
{
    public static class ApplicationIconHelper
    {
        public const int MaxEdge = 64;

        public static byte[] FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".exe":
                case ".dll":
                case ".ico":
                    return ApplicationIcon.FromAssociatedIcon(path);
                default:
                    return FromImageFile(path);
            }
        }

        public static byte[] FromImageFile(string path)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return FromBitmapSource(image);
            }
            catch (Exception)
            {
                return ApplicationIcon.FromAssociatedIcon(path);
            }
        }

        public static byte[] FromBitmapSource(BitmapSource source)
        {
            if (source == null)
                return null;

            BitmapSource toEncode = source;
            if (source.PixelWidth > MaxEdge || source.PixelHeight > MaxEdge)
            {
                double scale = Math.Min((double)MaxEdge / source.PixelWidth, (double)MaxEdge / source.PixelHeight);
                var scaled = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                scaled.Freeze();
                toEncode = scaled;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(toEncode));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        public static BitmapSource ToBitmapSource(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            try
            {
                var image = new BitmapImage();
                using (var stream = new MemoryStream(data))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
