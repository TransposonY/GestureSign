using GestureSign.ControlPanel.Common;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(byte[]), typeof(BitmapSource))]
    public class ApplicationIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ApplicationIconHelper.ToBitmapSource(value as byte[]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
