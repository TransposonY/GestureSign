using System;
using System.Globalization;
using System.Windows.Data;
using GestureSign.Common.Applications;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.Converters
{
    public class MatchStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ApplicationListViewItem applicationListViewItem = values[0] as ApplicationListViewItem;
            MatchUsing matchUsing = (MatchUsing)values[1];
            if (applicationListViewItem == null) return null;
            return matchUsing == MatchUsing.ExecutableFilename
                ? applicationListViewItem.WindowFilename
                : matchUsing == MatchUsing.WindowTitle
                    ? applicationListViewItem.WindowTitle
                    : applicationListViewItem.WindowClass;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}