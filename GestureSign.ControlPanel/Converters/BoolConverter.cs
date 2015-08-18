using System;
using System.Windows.Data;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool v = (bool)value;
            if (v) return LocalizationProvider.Instance.GetTextValue("Common.Yes");
            else return LocalizationProvider.Instance.GetTextValue("Common.No");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value as string;
            if (strValue == LocalizationProvider.Instance.GetTextValue("Common.Yes")) return true;
            return false;
        }
    }
}