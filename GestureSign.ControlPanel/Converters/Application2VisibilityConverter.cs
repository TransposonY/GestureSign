using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GestureSign.Common.Applications;

namespace GestureSign.ControlPanel.Converters
{
    public class Application2VisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var userApplication = values[0] as UserApp;
            bool selected = (bool)values[1];
            return userApplication != null && selected ? Visibility.Visible : Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}