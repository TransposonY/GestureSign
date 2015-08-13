using System;
using System.Globalization;
using System.Windows.Data;
using GestureSign.Common.Configuration;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InterceptTouchInputCheckBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value && AppConfig.UiAccess;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}