using System;
using System.Globalization;
using System.Windows.Data;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(int), typeof(bool))]
    public class SelectedIndex2BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value == 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}