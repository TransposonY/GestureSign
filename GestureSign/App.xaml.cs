using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
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
                LoadLanguageData();

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                if (!File.Exists(path))
                {
                    MessageBox.Show(LanguageDataManager.Instance.GetTextValue("Messages.CannotFindDaemonMessage"),
                        LanguageDataManager.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            mutex = new Mutex(true, "GestureSignControlPanel", out createdNew);
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

                NamedPipe.Instance.RunNamedPipeServer("GestureSignControlPanel", new MessageProcessor());
            }
            else
            {
                MessageBox.Show(LanguageDataManager.Instance.GetTextValue("Messages.AlreadyRunning"),
                    LanguageDataManager.Instance.GetTextValue("Messages.AlreadyRunningTitle"));
                Current.Shutdown();
            }
        }

        private void LoadLanguageData()
        {
            if ("Built-in".Equals(AppConfig.CultureName) || !LanguageDataManager.Instance.LoadFromFile("ControlPanel", GestureSign.Properties.Resources.en))
            {
                LanguageDataManager.Instance.LoadFromResource(GestureSign.Properties.Resources.en);
            }

            Application.Current.Resources.Remove("DefaultFlowDirection");
            Current.Resources.Add("DefaultFlowDirection", LanguageDataManager.Instance.FlowDirection);
            Application.Current.Resources.Remove("DefaultFont");
            Current.Resources.Add("DefaultFont", LanguageDataManager.Instance.Font);
            Application.Current.Resources.Remove("HeaderFontFamily");
            Current.Resources.Add("HeaderFontFamily", LanguageDataManager.Instance.Font);
            Application.Current.Resources.Remove("ContentFontFamily");
            Current.Resources.Add("ContentFontFamily", LanguageDataManager.Instance.Font);
        }
    }
}
