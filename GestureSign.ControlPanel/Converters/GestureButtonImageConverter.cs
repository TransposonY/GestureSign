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
            var gestureMap = values[0] as Dictionary<string, IGesture>;
            string gestureName = values[1] as string;
            if (gestureName == null || gestureMap == null)
                return null;

            IGesture gesture = null;
            if (gestureMap.TryGetValue(gestureName, out gesture))
                return GestureImageConverter.Convert(gesture.PointPatterns, targetType, parameter, culture);
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}