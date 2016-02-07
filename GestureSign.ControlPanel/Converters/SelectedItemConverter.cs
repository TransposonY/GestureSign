using System;
using System.Globalization;
using System.Windows.Data;
using GestureSign.Common.Applications;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.Converters
{
    public class SelectedItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int selectedIndex = (int)values[0];
            IApplication existingApplication = values[1] as IApplication;
            ApplicationListViewItem applicationListViewItem = values[2] as ApplicationListViewItem;
            if (selectedIndex == 0)
                return applicationListViewItem == null
                    ? string.Empty : applicationListViewItem.WindowTitle;
            return existingApplication != null ? existingApplication.Name : Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing, Binding.DoNothing };
        }
    }
}