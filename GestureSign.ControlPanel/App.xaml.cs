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
using GestureSign.Common.Log;
using GestureSign.Common.Plugins;
using GestureSign.ControlPanel.Localization;
using Microsoft.Win32;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        Mutex mutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                Logging.OpenLogFile();
                LoadLanguageData();

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                if (!File.Exists(path))
                {
                    MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CannotFindDaemonMessage"),
                        LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                    return;
                }

                if (CheckIfApplicationRunAsAdmin())
                {
                    var result = MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CompatWarning"),
                     LocalizationProvider.Instance.GetTextValue("Messages.CompatWarningTitle"), MessageBoxButton.YesNo,
                     MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        Current.Shutdown();
                        return;
                    }
                }

                StartDaemon(path);

                bool createdNew;
                mutex = new Mutex(true, "GestureSignControlPanel", out createdNew);
                if (createdNew)
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();

                    GestureManager.Instance.Load(null);
                    PluginManager.Instance.Load(null);
                    ApplicationManager.Instance.Load(null);

                    NamedPipe.Instance.RunNamedPipeServer("GestureSignControlPanel", new MessageProcessor());
                }
                else
                {
                    NamedPipe.SendMessageAsync("MainWindow", "GestureSignControlPanel").Wait();
                    Current.Shutdown();
                }

            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
                MessageBox.Show(exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

        }

        private void StartDaemon(string daemonPath)
        {
            bool createdNewDaemon;
            using (new Mutex(false, "GestureSignDaemon", out createdNewDaemon))
            {
            }
            if (createdNewDaemon)
            {
                using (Process daemon = new Process())
                {
                    daemon.StartInfo.FileName = daemonPath;

                    //daemon.StartInfo.UseShellExecute = false;
                    if (IsAdministrator())
                        daemon.StartInfo.Verb = "runas";
                    daemon.StartInfo.CreateNoWindow = false;
                    daemon.Start();
                }
            }
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void LoadLanguageData()
        {
            if ("Built-in".Equals(AppConfig.CultureName) || !LocalizationProviderEx.Instance.LoadFromFile("ControlPanel", ControlPanel.Properties.Resources.en))
            {
                LocalizationProviderEx.Instance.LoadFromResource(ControlPanel.Properties.Resources.en);
            }

            Current.Resources.Remove("DefaultFlowDirection");
            Current.Resources.Add("DefaultFlowDirection", LocalizationProviderEx.Instance.FlowDirection);
            Current.Resources.Remove("DefaultFont");
            Current.Resources.Add("DefaultFont", LocalizationProviderEx.Instance.Font);
            Current.Resources.Remove("HeaderFontFamily");
            Current.Resources.Add("HeaderFontFamily", LocalizationProviderEx.Instance.Font);
            Current.Resources.Remove("ContentFontFamily");
            Current.Resources.Add("ContentFontFamily", LocalizationProviderEx.Instance.Font);
        }

        private bool CheckIfApplicationRunAsAdmin()
        {
            string controlPanelRecord;
            string daemonRecord;
            using (RegistryKey layers = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
            {
                string controlPanelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSign.exe");
                string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");

                controlPanelRecord = layers?.GetValue(controlPanelPath) as string;
                daemonRecord = layers?.GetValue(daemonPath) as string;
            }

            return controlPanelRecord != null && controlPanelRecord.ToUpper().Contains("RUNASADMIN") ||
                   daemonRecord != null && daemonRecord.ToUpper().Contains("RUNASADMIN");
        }
    }
}
