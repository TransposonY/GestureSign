using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;

namespace GestureSign.Common
{
    class UIHelper
    {
        public static MetroWindow GetParentWindow(DependencyObject dependencyObject)
        {
            return Window.GetWindow(dependencyObject) as MetroWindow;

        }
    }
}
