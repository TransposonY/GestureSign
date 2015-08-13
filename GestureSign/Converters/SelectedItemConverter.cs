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
            bool isExisting = values != null && (bool)values[0];
            IApplication existingApplication = values[1] as IApplication;
            ApplicationListViewItem applicationListViewItem = values[2] as ApplicationListViewItem;
            if (!isExisting)
                return applicationListViewItem == null
                    ? Binding.DoNothing
                    : applicationListViewItem.WindowTitle;
            return existingApplication != null ? existingApplication.Name : Binding.DoNothing;
        }
        // 因为是只从数据源到目标的意向Binding，所以，这个函数永远也不会被调到
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing, Binding.DoNothing };
        }
    }
}