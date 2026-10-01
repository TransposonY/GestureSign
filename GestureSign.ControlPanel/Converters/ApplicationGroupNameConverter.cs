using GestureSign.Common.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace GestureSign.ControlPanel.Converters
{
    /// <summary>
    /// Section title above the application list: the group name, or a generic "Applications" for the default group.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class ApplicationGroupNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = value as string;
            if (string.IsNullOrEmpty(name))
                name = LocalizationProvider.Instance.GetTextValue("Action.Applications");

            return name.ToUpper(culture ?? CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
