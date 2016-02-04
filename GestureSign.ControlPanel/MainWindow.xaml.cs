using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        TouchDevice _ellipseControlTouchDevice;
        Point _lastPoint;

        public MainWindow()
        {
            Loaded += (e, o) => { if (AppConfig.SendErrorReport) CheckAndSendLog(); };
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

        private async void CheckAndSendLog()
        {
            if (!ExistsNewerErrorLog()) return;
            var dialogResult = await this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
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
                if (File.Exists(Logging.LogFilePath))
                {
                    result.Append(File.ReadAllText(Logging.LogFilePath));
                }

                EventLog logs = new EventLog { Log = "Application" };

                foreach (EventLogEntry entry in logs.Entries)
                {
                    if (entry.EntryType == EventLogEntryType.Error && ".NET Runtime".Equals(entry.Source))
                    {
                        result.AppendLine(entry.TimeWritten.ToString(CultureInfo.InvariantCulture));
                        result.AppendLine(entry.Message.Replace("\n", "\r\n"));
                    }
                }
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
                        AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.SendButton"),
                        NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.DontSendButton"),
                    });
            }

            while (dialogResult == MessageDialogResult.Affirmative)
            {
                controller = await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                    LocalizationProvider.Instance.GetTextValue("Options.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await Task.Run(() => Net.SendMail("Error Log", result.ToString()));

                await controller.CloseAsync();

                if (exceptionMessage == null)
                {
                    await (this)
                        .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendSuccessTitle"),
                            LocalizationProvider.Instance.GetTextValue("Options.SendSuccess"));
                    break;
                }
                else
                {
                    dialogResult = await this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendFailed"),
                        LocalizationProvider.Instance.GetTextValue("Options.SendFailed") + ":\r\n" + exceptionMessage,
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.Retry"),
                        });
                }
            }
        }


        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            // Capture to the ellipse.  
            e.TouchDevice.Capture(this);

            // Remember this contact if a contact has not been remembered already.  
            // This contact is then used to move the ellipse around.
            if (_ellipseControlTouchDevice == null)
            {
                _ellipseControlTouchDevice = e.TouchDevice;

                // Remember where this contact took place.  
                _lastPoint = PointToScreen(_ellipseControlTouchDevice.GetTouchPoint(this).Position);
            }

            // Mark this event as handled.  
            e.Handled = true;
        }

        private void Window_TouchMove(object sender, TouchEventArgs e)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null && e.TouchDevice == _ellipseControlTouchDevice)
            {
                // Get the current position of the contact.  
                var currentTouchPoint = PointToScreen(_ellipseControlTouchDevice.GetTouchPoint(this).Position);
                // Get the change between the controlling contact point and
                // the changed contact point.  
                double deltaX = currentTouchPoint.X - _lastPoint.X;
                double deltaY = currentTouchPoint.Y - _lastPoint.Y;

                // Get and then set a new top position and a new left position for the ellipse.  

                Top = Top + deltaY / source.CompositionTarget.TransformToDevice.M11;
                Left = Left + deltaX / source.CompositionTarget.TransformToDevice.M22;

                // Forget the old contact point, and remember the new contact point.  
                _lastPoint = currentTouchPoint;

                // Mark this event as handled.  
                e.Handled = true;
            }
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            // If this contact is the one that was remembered  
            if (e.TouchDevice == _ellipseControlTouchDevice)
            {
                // Forget about this contact.
                _ellipseControlTouchDevice = null;
            }

            // Mark this event as handled.  
            e.Handled = true;
        }
    }
}
