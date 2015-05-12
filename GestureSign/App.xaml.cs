using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

using System.Security.Principal;
using GestureSign.Common.Configuration;

namespace GestureSign
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;

        private static Timer timer = new Timer((o) =>
        {
            Current.Dispatcher.Invoke(
                () => { if (Current.Windows.Count == 0) Current.Shutdown(); else  timer.Change(300000, Timeout.Infinite); });
        }, timer, Timeout.Infinite, Timeout.Infinite);
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool createdNew;
            mutex = new System.Threading.Mutex(true, "GestureSignSetting", out createdNew);

            if (createdNew)
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                if (!System.IO.File.Exists(path))
                {
                    MessageBox.Show("未找到本软件组件\"GestureSignDaemon.exe\"，请重新下载或安装本软件.", "错误", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Application.Current.Shutdown();
                    return;
                }
                GestureSign.Common.Gestures.GestureManager.Instance.Load(null);
                GestureSign.Common.Applications.ApplicationManager.Instance.Load(null);
                GestureSign.Common.Plugins.PluginManager.Instance.Load(null);

                var systemAccent = Common.UI.WindowsHelper.GetSystemAccent();
                if (systemAccent != null)
                {
                    var accent = ThemeManager.GetAccent(systemAccent);
                    ThemeManager.ChangeAppStyle(Application.Current, accent, MahApps.Metro.ThemeManager.GetAppTheme("BaseLight"));
                }

                GestureSign.Common.InterProcessCommunication.NamedPipe.Instance.RunNamedPipeServer("GestureSignSetting", new MessageProcessor());

                try
                {
                    bool createdNewDaemon;
                    using (new Mutex(false, "GestureSignDaemon", out createdNewDaemon)) { }
                    if (createdNewDaemon)
                    {
                        using (Process daemon = new Process())
                        {
                            daemon.StartInfo.FileName = path;

                            //daemon.StartInfo.UseShellExecute = false;
                            if (IsAdministrator())
                                daemon.StartInfo.Verb = "runas";
                            daemon.StartInfo.CreateNoWindow = false;
                            daemon.Start();
                        }

                    }

                    if (e.Args.Length != 0 && e.Args[0].Equals("/L"))
                    {
                        Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                        timer.Change(300000, Timeout.Infinite);
                    }
                    else if (createdNewDaemon)
                    { Current.Shutdown(); }
                    else
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                }
                catch (Exception exception) { MessageBox.Show(exception.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Warning); }

                AppConfig.ToggleWatcher();
#if DEBUG

                MainWindow mw = new MainWindow();
                mw.Show();
                mw.Activate();

#endif
            }
            else
            {
                MessageBox.Show("本程序已经运行", "提示");
                Application.Current.Shutdown();
            }
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
