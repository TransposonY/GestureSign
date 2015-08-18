using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GestureSign.Common.Applications;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(MatchUsing), typeof(Visibility))]
    public class MatchUsing2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return (MatchUsing)value == MatchUsing.ExecutableFilename ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}