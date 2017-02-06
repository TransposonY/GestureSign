using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GestureSign.Common.Gestures;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(PointPattern[]), typeof(ImageSource))]
    public class GestureImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var pattern = (PointPattern[])value;
            int height = int.Parse((string)parameter);
            var color = (Color)Application.Current.Resources["HighlightColor"];
            return GestureImage.CreateImage(pattern, new Size(height, height), color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}