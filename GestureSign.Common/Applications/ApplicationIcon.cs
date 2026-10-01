using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GestureSign.Common.Applications
{
    public static class ApplicationIcon
    {
        public static byte[] FromAssociatedIcon(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            try
            {
                using (var icon = Icon.ExtractAssociatedIcon(path))
                {
                    if (icon == null)
                        return null;
                    using (var bitmap = icon.ToBitmap())
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool TryApplyImported(IApplication destination, IApplication source)
        {
            if (destination == null || source == null)
                return false;
            if (destination.Icon != null || source.Icon == null)
                return false;
            destination.Icon = source.Icon;
            return true;
        }

    }
}
