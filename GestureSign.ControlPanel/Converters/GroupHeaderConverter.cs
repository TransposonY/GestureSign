using System;
using System.Windows.Data;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Converters
{
    public class GroupHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int name = (int)values[0];
            int count = (int)values[1];
            return string.Format(LocalizationProvider.Instance.GetTextValue("Gesture.GestureGroupHeader"), name, count);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}