using System;
using System.Globalization;
using System.Windows.Data;
using GestureSign.Common.Applications;

namespace GestureSign.ControlPanel.Converters
{
    public class AllowSingleBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var userApplication = values[0] as UserApplication;
            bool existingApp = (bool)values[1];
            if (userApplication != null && existingApp)
            {
                return userApplication.AllowSingleStroke;
            }
            return false;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}