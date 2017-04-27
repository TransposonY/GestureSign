using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GestureSign.Common.Applications;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(MatchUsing), typeof(string))]
    public class MatchUsing2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MatchUsing mu = (MatchUsing)value;
            switch (mu)
            {
                case MatchUsing.All:
                    return LocalizationProvider.Instance.GetTextValue("Common.GlobalActions");
                case MatchUsing.ExecutableFilename:
                    return LocalizationProvider.Instance.GetTextValue("Common.FileName");
                case MatchUsing.WindowClass:
                    return LocalizationProvider.Instance.GetTextValue("Common.WindowClass");
                case MatchUsing.WindowTitle:
                    return LocalizationProvider.Instance.GetTextValue("Common.WindowTitle");
                default: return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}