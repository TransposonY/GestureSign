using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;
using System.Globalization;

namespace GestureSign.ControlPanel.Common
{
    class UIHelper
    {
        public static CultureInfo CurrentCulture { get { return CultureInfo.CurrentCulture; } }

        public static MetroWindow GetParentWindow(DependencyObject dependencyObject)
        {
            return Window.GetWindow(dependencyObject) as MetroWindow;

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
