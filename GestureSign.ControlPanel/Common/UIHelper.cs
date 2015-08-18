using System;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Common
{
    class UIHelper
    {
        public static MetroWindow GetParentWindow(DependencyObject dependencyObject)
        {
            return Window.GetWindow(dependencyObject) as MetroWindow;

        }

        public static ResourceDictionary GetSystemAccent()
        {
            SolidColorBrush accentColorBrush = (SolidColorBrush)SystemParameters.WindowGlassBrush;
            Double dY = accentColorBrush.Color.R * 0.299 + accentColorBrush.Color.G * 0.587 + accentColorBrush.Color.B * 0.114;
            if (dY >= 192 || dY < 50)
            {
                return null;
            }
            ResourceDictionary rd = new ResourceDictionary();

            Color AccentColor = accentColorBrush.Color;

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
            rd.Add("AccentColorBrush", accentColorBrush);
            rd.Add("AccentColorBrush2", AccentColorBrush2);
            rd.Add("AccentColorBrush3", AccentColorBrush3);
            rd.Add("AccentColorBrush4", AccentColorBrush4);

            rd.Add("WindowTitleColorBrush", accentColorBrush);
            rd.Add("AccentSelectedColorBrush", Brushes.White);

            rd.Add("IdealForegroundColor", Colors.White);
            rd.Add("IdealForegroundColorBrush", Brushes.White);

            rd.Add("CheckmarkFill", accentColorBrush);
            rd.Add("RightArrowFill", accentColorBrush);
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

        public static TChildItem FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                var item = child as TChildItem;
                if (item != null)
                    return item;
                else
                {
                    TChildItem childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}
