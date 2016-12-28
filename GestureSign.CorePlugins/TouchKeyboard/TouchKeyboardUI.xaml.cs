using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace GestureSign.CorePlugins.TouchKeyboard
{
    /// <summary>
    /// TouchKeyboardUI.xaml 的交互逻辑
    /// </summary>
    public partial class TouchKeyboardUI : UserControl
    {
        private const string KeyPath = @"SOFTWARE\Microsoft\TabletTip\1.7";
        private const string ValueName = "EnableDesktopModeAutoInvoke";

        public TouchKeyboardUI()
        {
            InitializeComponent();
        }

        private void AutoInvokeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (AutoInvokeCheckBox.IsChecked.Value)
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(KeyPath, true);
                rk?.SetValue(ValueName, 1, RegistryValueKind.DWord);
            }
            else
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(KeyPath, true);
                rk?.DeleteValue(ValueName);
            }

            foreach (var process in Process.GetProcessesByName("TabTip"))
            {
                process.Kill();
                process.WaitForExit();
                process.Dispose();
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName =
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) +
                    @"\Microsoft Shared\ink\TabTip.exe"
            };
            Process.Start(startInfo);
        }

        private void AutoInvokeCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(KeyPath);
            if (rk != null)
            {
                var result = rk.GetValue(ValueName);
                AutoInvokeCheckBox.IsChecked = result != null && (int)result == 1;
            }
        }
    }
}
