using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : TouchWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (CheckIfApplicationRunAsAdmin())
            {
                var result = MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CompatWarning"),
                 LocalizationProvider.Instance.GetTextValue("Messages.CompatWarningTitle"), MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
            StartDaemon();
            SetAboutInfo();

            if (ExistsNewerErrorLog() && AppConfig.SendErrorReport)
            {
                this.Dispatcher.InvokeAsync(SendLog, DispatcherPriority.Input);
            }

            Activate();
        }

        private void SetAboutInfo()
        {
            string version = LocalizationProvider.Instance.GetTextValue("About.Version") +
                             FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location)
                                 .FileVersion;
            string releaseDate = LocalizationProvider.Instance.GetTextValue("About.ReleaseDate") +
                                 new DateTime(2000, 1, 1).AddDays(Application.ResourceAssembly.GetName().Version.Build)
                                     .AddSeconds(Application.ResourceAssembly.GetName().Version.Revision * 2);
            this.AboutTextBox.Text = this.AboutTextBox.Text.Insert(0, version + "\r\n" + releaseDate + "\r\n");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var commandSource = sender as ICommandSource;
                var uri = commandSource?.CommandParameter as string;
                if (uri != null)
                    Process.Start(uri);
            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
                MessageBox.Show(exception.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"));
            }
        }

        private void SendFeedback_Click(object sender, RoutedEventArgs e)
        {
            SendFeedback();
        }

        private bool ExistsNewerErrorLog()
        {
            EventLog logs = new EventLog { Log = "Application" };
            var now = DateTime.Now;
            var entryCollection = logs.Entries;
            int logCount = entryCollection.Count;
            for (int i = logCount - 1; i > logCount - 1000 && i >= 0; i--)
            {
                var entry = entryCollection[i];
                if (now.Subtract(entry.TimeWritten).TotalHours > 1)
                    break;

                if (entry.EntryType == EventLogEntryType.Error && ".NET Runtime".Equals(entry.Source) &&
                    entry.Message.IndexOf("GestureSign", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool hasNewLog = AppConfig.LastErrorTime.CompareTo(entry.TimeWritten) < 0;
                    if (hasNewLog)
                    {
                        AppConfig.LastErrorTime = entry.TimeWritten;
                    }

                    return hasNewLog;
                }
            }
            return false;
        }

        private void SendLog()
        {
            var dialogResult = this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("About.SendLogTitle"),
            LocalizationProvider.Instance.GetTextValue("Messages.FindNewErrorLog"),
            MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
            {
                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("About.SendButton"),
                NegativeButtonText = LocalizationProvider.Instance.GetTextValue("About.DontSendButton"),
            });
            if (dialogResult == MessageDialogResult.Negative) return;
            SendFeedback();
        }

        private async void SendFeedback()
        {
            var controller =
                await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("About.Waiting"),
                            LocalizationProvider.Instance.GetTextValue("About.Exporting"));
            controller.SetIndeterminate();

            string result = await Task.Factory.StartNew(() =>
            {
                return Feedback.OutputLog();
            });
            await controller.CloseAsync();

            LogWindow logWin = new LogWindow(result);
            var dialogResult = logWin.ShowDialog();
            string msg = logWin.Message;

            while (dialogResult != null && dialogResult.Value)
            {
                var sendReportTask = Task.Factory.StartNew(() => Feedback.Send(result, msg));

                controller = await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("About.Waiting"),
                        LocalizationProvider.Instance.GetTextValue("About.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await sendReportTask;

                await controller.CloseAsync();

                if (exceptionMessage == null)
                {
                    this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("About.SendSuccessTitle"),
                            LocalizationProvider.Instance.GetTextValue("About.SendSuccess"));
                    break;
                }
                else
                {
                    dialogResult =
                        this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("About.SendFailed"),
                                exceptionMessage + Environment.NewLine + LocalizationProvider.Instance.GetTextValue("About.Mail"),
                                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("About.Retry"),
                                    NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                                }) == MessageDialogResult.Affirmative;
                }
            }
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

        private void StartDaemon()
        {
            string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
            if (!File.Exists(daemonPath))
            {
                MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CannotFindDaemonMessage"),
                    LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            bool createdNewDaemon;
            using (new Mutex(false, "GestureSignDaemon", out createdNewDaemon))
            {
            }
            if (createdNewDaemon)
            {
                try
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
                catch (Exception e)
                {
                    Logging.LogException(e);
                    MessageBox.Show(string.Format(e.Message + Environment.NewLine + LocalizationProvider.Instance.GetTextValue("Messages.StartupError"), daemonPath),
                        LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
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
