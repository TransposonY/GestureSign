using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using GestureSign.Common.Gestures;

namespace GestureSign.ControlPanel.Converters
{
    public class GestureButtonImageConverter : IMultiValueConverter
    {
        public GestureImageConverter GestureImageConverter { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var patternMap = values[0] as Dictionary<string, PointPattern[]>;
            string gestureName = values[1] as string;
            if (gestureName == null || patternMap == null)
                return null;

            PointPattern[] pattern = null;
            if (patternMap.ContainsKey(gestureName))
                pattern = patternMap[gestureName];
            return GestureImageConverter.Convert(pattern, targetType, parameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}