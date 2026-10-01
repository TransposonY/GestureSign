using GestureSign.Common.Applications;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestureSign.ControlPanel.Converters
{
    /// <summary>
    /// Glyph shown in the application list when an application has no icon of its own.
    /// </summary>
    [ValueConversion(typeof(IApplication), typeof(object))]
    public class ApplicationFallbackIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value is GlobalApp ? "Icon.Globe" : "Icon.Window";
            return Application.Current?.TryFindResource(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
