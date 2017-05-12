using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.ControlPanel.Dialogs;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
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
    public partial class MainWindow : MetroWindow
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

            GestureSign.Common.Gestures.GestureManager.Instance.Load(null);
            GestureSign.Common.Plugins.PluginManager.Instance.Load(null);
            GestureSign.Common.Applications.ApplicationManager.Instance.Load(null);

            GestureSign.Common.InterProcessCommunication.NamedPipe.Instance.RunNamedPipeServer("GestureSignControlPanel", new MessageProcessor());

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

        private bool ExistsNewerErrorLog()
        {
            EventLog logs = new EventLog { Log = "Application" };
            var entryCollection = logs.Entries;
            for (int i = entryCollection.Count - 1; i > entryCollection.Count - 1000 && i >= 0; i--)
            {
                var entry = entryCollection[i];
                if (DateTime.Now.Subtract(entry.TimeWritten).TotalDays > 1)
                    break;

                if (entry.EntryType == EventLogEntryType.Error && ".NET Runtime".Equals(entry.Source) &&
                    entry.Message.IndexOf("GestureSign", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    DateTime lastTime = AppConfig.LastErrorTime;
                    AppConfig.LastErrorTime = entry.TimeWritten;
                    AppConfig.Save();

                    return lastTime.CompareTo(entry.TimeWritten) < 0;
                }
            }
            return false;
        }

        private async void SendLog()
        {
            var dialogResult = this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
            LocalizationProvider.Instance.GetTextValue("Messages.FindNewErrorLog"),
            MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings()
            {
                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.SendButton"),
                NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.DontSendButton"),
                FirstAuxiliaryButtonText = LocalizationProvider.Instance.GetTextValue("Messages.ShowLog"),
            });
            if (dialogResult == MessageDialogResult.Negative) return;

            var controller =
                await (this).ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                    LocalizationProvider.Instance.GetTextValue("Options.Exporting"));
            controller.SetIndeterminate();

            StringBuilder result = new StringBuilder();
            await Task.Run(() =>
            {
                Feedback.OutputLog(ref result);
            });
            await controller.CloseAsync();

            if (dialogResult == MessageDialogResult.FirstAuxiliary)
            {
                LogWindow logWin = new LogWindow(result.ToString());
                logWin.Show();

                dialogResult = await this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
                    LocalizationProvider.Instance.GetTextValue("Options.SendLog"),
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.SendButton"),
                        NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.DontSendButton"),
                    });
            }

            string message = null;
            while (dialogResult == MessageDialogResult.Affirmative)
            {
                var sendReportTask = Task.Run(() => Feedback.Send(result.ToString()));
                if (message == null)
                    message = this.ShowModalInputExternal(
                        LocalizationProvider.Instance.GetTextValue("Options.Feedback"),
                        LocalizationProvider.Instance.GetTextValue("Options.FeedbackTip")) ?? string.Empty;

                controller = await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                   LocalizationProvider.Instance.GetTextValue("Options.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await sendReportTask;
                if (!string.IsNullOrEmpty(message))
                {
                    var msg = message;
                    await Task.Run(() => Feedback.Send(msg, true));
                }

                await controller.CloseAsync();

                if (exceptionMessage == null)
                {
                    (this)
                       .ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Options.SendSuccessTitle"),
                           LocalizationProvider.Instance.GetTextValue("Options.SendSuccess"));
                    break;
                }
                else
                {
                    dialogResult = this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Options.SendFailed"),
                        LocalizationProvider.Instance.GetTextValue("Options.SendFailed") + ":\r\n" + exceptionMessage,
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.Retry"),
                        });
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
    }
}
