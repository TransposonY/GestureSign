using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Plugins;
using GestureSign.UI.Common;
using MahApps.Metro;

namespace GestureSign
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        Mutex mutex;

        private static readonly Timer Timer = new Timer(o =>
        {
            Current.Dispatcher.Invoke(
                () => { if (Current.Windows.Count == 0) Current.Shutdown(); else  Timer.Change(300000, Timeout.Infinite); });
        }, Timer, Timeout.Infinite, Timeout.Infinite);

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                if (!File.Exists(path))
                {
                    MessageBox.Show("未找到本软件组件\"GestureSignDaemon.exe\"，请重新下载或安装本软件.", "错误", MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                    Current.Shutdown();
                    return;
                }

                bool createdNewDaemon;
                using (new Mutex(false, "GestureSignDaemon", out createdNewDaemon))
                {
                }
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

                if (createdNewDaemon) Current.Shutdown();
                else
                {
                    Initialization();

                    if (e.Args.Length != 0 && e.Args[0].Equals("/L"))
                    {
                        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        Timer.Change(300000, Timeout.Infinite);
                    }
                    else
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                }

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                Current.Shutdown();
            }

        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Initialization()
        {
            bool createdNew;
            mutex = new Mutex(true, "GestureSignSetting", out createdNew);
            if (createdNew)
            {

                GestureManager.Instance.Load(null);
                ApplicationManager.Instance.Load(null);
                PluginManager.Instance.Load(null);

                var systemAccent = UIHelper.GetSystemAccent();
                if (systemAccent != null)
                {
                    var accent = ThemeManager.GetAccent(systemAccent);
                    ThemeManager.ChangeAppStyle(Current, accent, ThemeManager.GetAppTheme("BaseLight"));
                }

                NamedPipe.Instance.RunNamedPipeServer("GestureSignSetting", new MessageProcessor());
            }
            else
            {
                MessageBox.Show("本程序已经运行", "提示");
                Current.Shutdown();
            }
        }
    }
}
