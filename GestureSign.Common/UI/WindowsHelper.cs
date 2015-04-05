using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media;

using GestureSign;

namespace GestureSign.Common.UI
{
    public class WindowsHelper
    {
      
        public static ResourceDictionary GetSystemAccent()
        {
            SolidColorBrush AccentColorBrush = (SolidColorBrush)SystemParameters.WindowGlassBrush;
            Double dY = AccentColorBrush.Color.R * 0.299 + AccentColorBrush.Color.G * 0.587 + AccentColorBrush.Color.B * 0.114;
            if (dY >= 192 || dY < 50)
            {
                return null;
            }
            ResourceDictionary rd = new ResourceDictionary();

            Color AccentColor = AccentColorBrush.Color;

            Color HighlightColor = Color.Multiply(AccentColor, 0.73f);
            HighlightColor.A = 0xFF;
            SolidColorBrush HighlightBrush = new SolidColorBrush(HighlightColor);

            Color AccentColor2 = AccentColor;
            AccentColor2.A = 0xCC;
            SolidColorBrush AccentColorBrush2 = new SolidColorBrush(AccentColor2);

            Color AccentColor3 = AccentColor;
            AccentColor3.A = 0x99;
            SolidColorBrush AccentColorBrush3 = new SolidColorBrush(AccentColor3);

            Color AccentColor4 = AccentColor;
            AccentColor4.A = 0x66;
            SolidColorBrush AccentColorBrush4 = new SolidColorBrush(AccentColor4);
            //Color AccentColor= HighlightColor,
            rd.Add("HighlightColor", HighlightColor);
            rd.Add("AccentColor", AccentColor);
            rd.Add("AccentColor2", AccentColor2);
            rd.Add("AccentColor3", AccentColor3);
            rd.Add("AccentColor4", AccentColor4);

            rd.Add("HighlightBrush", HighlightBrush);
            rd.Add("AccentColorBrush", AccentColorBrush);
            rd.Add("AccentColorBrush2", AccentColorBrush2);
            rd.Add("AccentColorBrush3", AccentColorBrush3);
            rd.Add("AccentColorBrush4", AccentColorBrush4);

            rd.Add("WindowTitleColorBrush", AccentColorBrush);
            rd.Add("AccentSelectedColorBrush", Brushes.White);

            rd.Add("IdealForegroundColor", Colors.White);
            rd.Add("IdealForegroundColorBrush", Brushes.White);

            rd.Add("CheckmarkFill", AccentColorBrush);
            rd.Add("RightArrowFill", AccentColorBrush);
            return rd;
        }

        public static T GetParentDependencyObject<T>(DependencyObject dependencyObject) where T : DependencyObject
        {

            DependencyObject parent = VisualTreeHelper.GetParent(dependencyObject);
            while (parent != null)
            {
                if (parent is T)
                {
                    return (T)parent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
