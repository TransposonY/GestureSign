using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

using System.Security.Principal;

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

                MessageProcessor messageProcessor = new MessageProcessor();
                GestureSign.Common.InterProcessCommunication.NamedPipe.Instance.RunNamedPipeServer("GestureSignSetting", messageProcessor.ProcessMessages);
                try
                {
                    bool createdNewDaemon;
                    Mutex daemonMutex = new Mutex(false, "GestureSignDaemon", out createdNewDaemon);
                    daemonMutex.Dispose();
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

                }
                catch (Exception exception) { MessageBox.Show(exception.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Warning); }


                if (GestureSign.Common.Configuration.AppConfig.XRatio == 0 || e.Args.Length != 0 && e.Args[0].Equals("/L"))
                {
                    Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                }
                else
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    mainWindow.Activate();
                    mainWindow.availableAction.BindActions();
                }
#if DEBUG

                MainWindow mw = new MainWindow();
                mw.Show();
                mw.Activate();
                mw.availableAction.BindActions();

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
