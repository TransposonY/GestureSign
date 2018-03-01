using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GestureSign.Common.Gestures;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(PointPattern[]), typeof(int))]
    public class GestureGroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var gesture = (IGesture)value;
            int strokeCount = gesture?.PointPatterns.Max(p => p.Points.Count) ?? 0;
            return strokeCount;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}