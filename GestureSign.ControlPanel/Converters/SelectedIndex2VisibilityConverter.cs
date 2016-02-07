using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class SelectedIndex2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((string)parameter == "ExistingApplicationCanvas")
                    return (int)value == 0 ? Visibility.Collapsed : Visibility.Visible;
                return (int)value == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}