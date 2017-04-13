using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Windows.Management.Deployment;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.Common.Plugins;
using GestureSign.ControlPanel.Localization;
using ManagedWinapi.Windows;
using Microsoft.Win32;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

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
                        LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK,
                        MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    Current.Shutdown();
                    return;
                }

                if (CheckIfApplicationRunAsAdmin())
                {
                    var result = MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CompatWarning"),
                     LocalizationProvider.Instance.GetTextValue("Messages.CompatWarningTitle"), MessageBoxButton.YesNo,
                     MessageBoxImage.Warning, MessageBoxResult.No, MessageBoxOptions.DefaultDesktopOnly);

                    if (result == MessageBoxResult.No)
                    {
                        Current.Shutdown();
                        return;
                    }
                }

                if (AppConfig.UiAccess && Environment.OSVersion.Version.Major == 10)
                {
                    using (var currentUser = WindowsIdentity.GetCurrent())
                    {
                        if (currentUser.User != null)
                        {
                            var sid = currentUser.User.ToString();
                            PackageManager packageManager = new PackageManager();
                            var storeVersion = packageManager.FindPackagesForUserWithPackageTypes(sid, "41908Transpy.GestureSign", "CN=AF41F066-0041-4D13-9D95-9DAB66112B0A", PackageTypes.Main).FirstOrDefault();
                            if (storeVersion != null)
                            {
                                using (Process explorer = new Process
                                {
                                    StartInfo =
                                    {
                                        FileName = "explorer.exe", Arguments = @"shell:AppsFolder\" + "41908Transpy.GestureSign_f441wk0cxr8zc!GestureSign"
                                    }
                                })
                                {
                                    explorer.Start();
                                }
                                Current.Shutdown();
                                return;
                            }
                        }
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
                    ShowControlPanel();
                    // use Dispatcher to resolve exception 0xc0020001
                    Current.Dispatcher.InvokeAsync(() => Current.Shutdown(), DispatcherPriority.ApplicationIdle);
                }

            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
                MessageBox.Show(exception.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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

            Current.Resources["DefaultFlowDirection"] = LocalizationProviderEx.Instance.FlowDirection;
            var font = LocalizationProviderEx.Instance.Font;
            var headerFontFamily = LocalizationProviderEx.Instance.HeaderFontFamily;
            if (font != null)
                Current.Resources["DefaultFont"] =
                    Current.Resources["ContentFontFamily"] =
                    Current.Resources["ToggleSwitchFontFamily"] =
                    Current.Resources["ToggleSwitchHeaderFontFamily"] =
                    Current.Resources["ToggleSwitchFontFamily.Win10"] =
                    Current.Resources["ToggleSwitchHeaderFontFamily.Win10"] = font;
            if (headerFontFamily != null)
                Current.Resources["HeaderFontFamily"] = headerFontFamily;
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

        private bool ShowControlPanel()
        {
            Process current = Process.GetCurrentProcess();
            var controlPanelProcesses = Process.GetProcessesByName(current.ProcessName);

            if (controlPanelProcesses.Length > 1)
            {
                foreach (Process process in controlPanelProcesses)
                {
                    if (process.Id != current.Id)
                    {
                        var window = new SystemWindow(process.MainWindowHandle);

                        if (window.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                        {
                            ShowWindowAsync(process.MainWindowHandle, SW_RESTORE);
                        }
                        SystemWindow.ForegroundWindow = window;
                        break;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
