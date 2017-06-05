using System;
using System.Windows;
using System.Windows.Data;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Converters
{
    public class ApplicationListHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = values[0] as string;
            int count = values[1] is int ? (int)values[1] : 0;

            if (String.IsNullOrEmpty(name))
                name = LocalizationProvider.Instance.GetTextValue("Action.DefaultGroup");
            return String.Format(LocalizationProvider.Instance.GetTextValue("Action.AppCount"), name, count);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }
}