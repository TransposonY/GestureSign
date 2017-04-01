using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using GestureSign.Common.Log;

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
            SetAboutInfo();

            if (ExistsNewerErrorLog() && AppConfig.SendErrorReport)
            {
                SendLog();
            }
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
                ErrorReport.OutputLog(ref result);
            });
            await controller.CloseAsync();

            if (dialogResult == MessageDialogResult.FirstAuxiliary)
            {
                string logPath = Path.Combine(AppConfig.ApplicationDataPath, "GestureSign" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".log");

                File.WriteAllText(logPath, result.ToString());
                Process.Start("notepad.exe", logPath)?.WaitForInputIdle();
                File.Delete(logPath);

                dialogResult = this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
                    LocalizationProvider.Instance.GetTextValue("Options.SendLog"),
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.SendButton"),
                        NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.DontSendButton"),
                    });
            }

            while (dialogResult == MessageDialogResult.Affirmative)
            {
                controller = await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                    LocalizationProvider.Instance.GetTextValue("Options.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await Task.Run(() => ErrorReport.SendReports(result.ToString()));

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
    }
}
