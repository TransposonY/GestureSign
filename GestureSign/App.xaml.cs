using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro;

namespace GestureSign
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool createdNew;
            mutex = new System.Threading.Mutex(true, Application.ResourceAssembly.GetName().FullName, out createdNew);

            if (createdNew)
            {
                string newConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                newConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                newConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                newConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                newConfigFilePath = config.FilePath;
                Input.TouchCapture.Instance.Load();
                Gestures.GestureManager.Instance.Load();
                UI.FormManager.Instance.Load();
                Applications.ApplicationManager.Instance.Load();
                Plugins.PluginManager.Instance.Load();
                UI.TrayManager.Instance.Load();

                UI.Surface surface = new UI.Surface();
                Input.TouchCapture.Instance.EnableTouchCapture();

                var systemAccent = Common.UI.WindowsHelper.GetSystemAccent();
                if (systemAccent != null)
                {
                    var accent = ThemeManager.GetAccent(systemAccent);
                    ThemeManager.ChangeAppStyle(Application.Current, accent, MahApps.Metro.ThemeManager.GetAppTheme("BaseLight"));
                }

                if (GestureSign.Input.MessageWindow.NumberOfTouchscreens == 0)
                {
                    MessageBox.Show("未检测到触摸屏设备，本软件无法正常使用！", "错误");
                }
                else if (GestureSign.Configuration.AppConfig.XRatio == 0)
                {
                    UI.Guide guide = new UI.Guide();
                    guide.Show();
                    guide.Activate();
                }
            }
            else
            {
                MessageBox.Show("本程序已经运行", "提示");
                Application.Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            GestureSign.Configuration.AppConfig.Save();
        }
    }
}
