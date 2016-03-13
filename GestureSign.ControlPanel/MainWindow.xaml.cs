using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            Loaded += (e, o) =>
            {
                if (ExistsNewerErrorLog() && AppConfig.SendErrorReport)
                {
                    SendLog();
                }
            };
            InitializeComponent();
            SetAboutInfo();
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
            Process.Start(LocalizationProvider.Instance.GetTextValue("About.HelpPageUrl"));
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
            var dialogResult = await this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
            LocalizationProvider.Instance.GetTextValue("Messages.FindNewErrorLog"),
            MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings()
            {
                AnimateHide = false,
                AnimateShow = false,
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
                string logPath = Path.Combine(Path.GetTempPath(), "GestureSign" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".log");

                File.WriteAllText(logPath, result.ToString());
                Process.Start("notepad.exe", logPath);

                dialogResult = await this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
                    LocalizationProvider.Instance.GetTextValue("Options.SendLog"),
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                    {
                        AnimateHide = false,
                        AnimateShow = false,
                        AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.SendButton"),
                        NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.DontSendButton"),
                    });
            }

            while (dialogResult == MessageDialogResult.Affirmative)
            {
                controller = await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                    LocalizationProvider.Instance.GetTextValue("Options.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await Task.Run(() => ErrorReport.SendMail("Error Log", result.ToString()));

                await controller.CloseAsync();

                if (exceptionMessage == null)
                {
                    await (this)
                        .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendSuccessTitle"),
                            LocalizationProvider.Instance.GetTextValue("Options.SendSuccess"), settings: new MetroDialogSettings()
                            {
                                AnimateHide = false,
                                AnimateShow = false,
                            });
                    break;
                }
                else
                {
                    dialogResult = await this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendFailed"),
                        LocalizationProvider.Instance.GetTextValue("Options.SendFailed") + ":\r\n" + exceptionMessage,
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AnimateHide = false,
                            AnimateShow = false,
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.Retry"),
                        });
                }
            }
        }
    }
}
