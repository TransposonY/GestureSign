using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.Converters
{
    public class GestureButtonImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var gestureMap = values[0] as Dictionary<string, GestureItem>;
            string gestureName = values[1] as string;
            if (gestureName == null || gestureMap == null)
                return null;

            GestureItem gi = null;
            if (gestureMap.TryGetValue(gestureName, out gi))
                return gi?.GestureImage;
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}