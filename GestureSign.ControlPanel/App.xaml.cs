using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.ControlPanel.Localization;
using ManagedWinapi.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Windows.Management.Deployment;

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
            Logging.LoggedExceptionOccurred += (o, ex) => ShowException(ex);
            Logging.OpenLogFile();
            LoadLanguageData();

            bool createdNew;
            mutex = new Mutex(true, Constants.ControlPanel, out createdNew);
            if (createdNew)
            {
                if (AppConfig.UiAccess && VersionHelper.IsWindows10OrGreater())
                    if (TryLaunchStoreVersion())
                        return;

                GestureManager.Instance.Load(null);
                GestureSign.Common.Plugins.PluginManager.Instance.Load(null);
                ApplicationManager.Instance.Load(null);

                NamedPipe.Instance.RunNamedPipeServer(Constants.ControlPanel, new MessageProcessor());

                ApplicationManager.ApplicationSaved += (o, ea) => NamedPipe.SendMessageAsync(IpcCommands.LoadApplications, Constants.Daemon);
                GestureManager.GestureSaved += (o, ea) => NamedPipe.SendMessageAsync(IpcCommands.LoadGestures, Constants.Daemon);
                AppConfig.ConfigChanged += (o, ea) =>
                {
                    NamedPipe.SendMessageAsync(IpcCommands.LoadConfiguration, Constants.Daemon);
                };
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                ShowControlPanel();
                // use Dispatcher to resolve exception 0xc0020001
                Current.Dispatcher.InvokeAsync(() => Current.Shutdown(), DispatcherPriority.ApplicationIdle);
            }
        }

        private void LoadLanguageData()
        {
            if (!LocalizationProvider.Instance.LoadFromFile("ControlPanel"))
            {
                LocalizationProvider.Instance.LoadFromResource(ControlPanel.Properties.Resources.en);
            }

            Current.Resources["DefaultFlowDirection"] = LocalizationProviderEx.FlowDirection;
            var font = LocalizationProviderEx.Font;
            var headerFontFamily = LocalizationProviderEx.HeaderFontFamily;
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
                            window.RestoreWindow();
                        }
                        SystemWindow.ForegroundWindow = window;
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        private bool TryLaunchStoreVersion()
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
                        return true;
                    }
                }
            }
            return false;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                NamedPipe.Instance.Dispose();
                mutex.Dispose();
            }
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logging.LogMessage("AppDomain.CurrentDomain.UnhandledException");
                Logging.LogException((Exception)e.ExceptionObject);
                ShowException((Exception)e.ExceptionObject);
            };

            DispatcherUnhandledException += (s, e) =>
            {
                Logging.LogMessage("Application.Current.DispatcherUnhandledException");
                Logging.LogException(e.Exception);
                ShowException(e.Exception);
                e.Handled = true;
                Environment.Exit(0);
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logging.LogMessage("TaskScheduler.UnobservedTaskException");
                Logging.LogException(e.Exception);
                ShowException(e.Exception);
                e.SetObserved();
            };
        }

        private void ShowException(Exception exception)
        {
            string message = null;
            if (exception is GestureSign.Common.Exceptions.FileWriteException)
            {
                message += Environment.NewLine + Environment.NewLine + LocalizationProvider.Instance.GetTextValue("Messages.FileWriteException");
            }

            while (exception.InnerException != null)
                exception = exception.InnerException;

            MessageBox.Show(exception.Message + message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SetupExceptionHandling();
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges", false);
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
            base.OnStartup(e);
        }
    }
}
