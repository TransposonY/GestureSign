using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GestureSign.Common.Applications;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(IApplication), typeof(string))]
    public class HeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var app = value as IApplication;
            if (app != null)
            {
                return String.Format(LocalizationProvider.Instance.GetTextValue("Action.ActionCount"), app.Name, app.Actions.Count);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}