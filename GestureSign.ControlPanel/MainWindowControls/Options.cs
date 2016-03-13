using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using IWshRuntimeLibrary;
using ManagedWinapi.Windows;
using Microsoft.Win32.TaskScheduler;
using MahApps.Metro.Controls.Dialogs;
using Application = System.Windows.Application;
using Color = System.Drawing.Color;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// Options.xaml 的交互逻辑
    /// </summary>
    public partial class Options : UserControl
    {
        Color _VisualFeedbackColor;
        private const string TaskName = "GestureSignAutoRunTask";

        public Options()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                // Try to load saved settings
                //  Common.Configuration.AppConfig.Reload();

                _VisualFeedbackColor = AppConfig.VisualFeedbackColor;
                VisualFeedbackWidthSlider.Value = AppConfig.VisualFeedbackWidth;
                MinimumPointDistanceSlider.Value = AppConfig.MinimumPointDistance;
                chkWindowsStartup.IsChecked = GetStartupStatus();
                OpacitySlider.Value = AppConfig.Opacity;
                chkOrderByLocation.IsChecked = AppConfig.IsOrderByLocation;
                ShowBalloonTipSwitch.IsChecked = AppConfig.ShowBalloonTip;
                ShowTrayIconSwitch.IsChecked = AppConfig.ShowTrayIcon;
                SendLogToggleSwitch.IsChecked = AppConfig.SendErrorReport;

                LanguageComboBox.ItemsSource = LocalizationProvider.Instance.GetLanguageList("ControlPanel");
                LanguageComboBox.SelectedValue = AppConfig.CultureName;
            }
            catch (Exception)
            {
                MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Options.Messages.LoadSettingError"),
                    LocalizationProvider.Instance.GetTextValue("Options.Messages.LoadSettingErrorTitle"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Disable gestures while options are open
            //Input.MouseCapture.Instance.DisableMouseCapture();

            UpdateVisualFeedbackExample();
        }

        private void btnPickColor_Click(object sender, RoutedEventArgs e)
        {
            // Set color picker dialog color to current visual feedback color
            ColorDialog cdColorPicker = new ColorDialog();
            cdColorPicker.AllowFullOpen = true;
            cdColorPicker.Color = _VisualFeedbackColor;// System.Drawing.Color.FromArgb(_VisualFeedbackColor.A, _VisualFeedbackColor.R, _VisualFeedbackColor.G, _VisualFeedbackColor.B);

            // Show color picker dialog
            if (cdColorPicker.ShowDialog() != DialogResult.OK)
                return;
            // Change color of visual feedback and refresh example
            //_VisualFeedbackColor.A = cdColorPicker.Color.A;
            //_VisualFeedbackColor.R = cdColorPicker.Color.R;
            //_VisualFeedbackColor.B = cdColorPicker.Color.B;
            //_VisualFeedbackColor.G = cdColorPicker.Color.G;
            _VisualFeedbackColor = cdColorPicker.Color;
            AppConfig.VisualFeedbackColor = _VisualFeedbackColor;
            UpdateVisualFeedbackExample();

            AppConfig.Save();
        }

        private void VisualFeedbackWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVisualFeedbackExample();

            AppConfig.VisualFeedbackWidth = (int)Math.Round(VisualFeedbackWidthSlider.Value);

            AppConfig.Save();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Change opacity display text with new value
            OpacityText.Text = LocalizationProvider.Instance.GetTextValue("Options.Opacity") + GetAlphaPercentage(OpacitySlider.Value) + "%";
            AppConfig.Opacity = OpacitySlider.Value;

            AppConfig.Save();
        }

        private void MinimumPointDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.OldValue == 0) return;
            AppConfig.MinimumPointDistance = (int)Math.Round(e.NewValue);

            AppConfig.Save();
        }


        private void UpdateVisualFeedbackExample()
        {
            // Show new example graphic if visual feedback is enabled
            if (VisualFeedbackWidthSlider.Value > 0)
            {
                VisualFeedbackExample.Stroke = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(_VisualFeedbackColor.A, _VisualFeedbackColor.R, _VisualFeedbackColor.G, _VisualFeedbackColor.B));

                VisualFeedbackWidthText.Text = String.Format(LocalizationProvider.Instance.GetTextValue("Options.VisualFeedbackWidth"), VisualFeedbackWidthSlider.Value);
            }
            else
            {
                VisualFeedbackWidthText.Text = LocalizationProvider.Instance.GetTextValue("Options.Off");
            }

        }



        private bool GetStartupStatus()
        {
            string lnkPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        // Get the service on the local machine
                        using (TaskService ts = new TaskService())
                        {
                            var tasks = ts.RootFolder.GetTasks(new Regex(TaskName));
                            return tasks.Count != 0 || File.Exists(lnkPath);
                        }
                    }
                    else return File.Exists(lnkPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        private int GetAlphaPercentage(double Alpha)
        {
            return (int)Math.Round(Alpha * 100d);
        }

        private void CreateLnk(string lnkPath)
        {
            if (!File.Exists(lnkPath))
            {
                WshShell shell = new WshShell();
                IWshShortcut shortCut = (IWshShortcut)shell.CreateShortcut(lnkPath);
                shortCut.TargetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe");
                //Application.ResourceAssembly.Location;// System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                shortCut.WindowStyle = 7;
                shortCut.Arguments = "";
                shortCut.Description = Application.ResourceAssembly.GetName().Version.ToString();// Application.ProductName + Application.ProductVersion;
                shortCut.IconLocation = Application.ResourceAssembly.Location;// Application.ExecutablePath;
                shortCut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;// Application.ResourceAssembly.;
                shortCut.Save();
            }
        }

        private void chkWindowsStartup_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        // Get the service on the local machine
                        using (TaskService ts = new TaskService())
                        {
                            var tasks = ts.RootFolder.GetTasks(new Regex(TaskName));

                            if (tasks.Count == 0)
                            {
                                // Create a new task definition and assign properties
                                TaskDefinition td = ts.NewTask();
                                td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                                td.Settings.DisallowStartIfOnBatteries = false;
                                td.RegistrationInfo.Description = "Launch GestureSign when user login";

                                td.Principal.RunLevel = TaskRunLevel.Highest;

                                LogonTrigger lt = new LogonTrigger { Enabled = true };
                                td.Triggers.Add(lt);
                                // Create an action that will launch Notepad whenever the trigger fires
                                td.Actions.Add(new ExecAction(
                                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestureSignDaemon.exe"), null, AppDomain.CurrentDomain.BaseDirectory));

                                // Register the task in the root folder
                                ts.RootFolder.RegisterTaskDefinition(TaskName, td);
                            }
                        }
                    }
                    else
                    {
                        string lnkPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                            "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";

                        CreateLnk(lnkPath);
                    }
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error); }

        }

        private void chkWindowsStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {  // Get the service on the local machine
                        using (TaskService ts = new TaskService())
                        {
                            var tasks = ts.RootFolder.GetTasks(new Regex(TaskName));

                            if (tasks.Count != 0)
                            {
                                ts.RootFolder.DeleteTask(TaskName);
                            }
                        }
                    }

                    string lnkPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                      "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";

                    if (File.Exists(lnkPath))
                        File.Delete(lnkPath);

                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void chkOrderByLocation_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig.IsOrderByLocation = true;
            AppConfig.Save();
        }

        private void chkOrderByLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig.IsOrderByLocation = false;
            AppConfig.Save();
        }

        private void btnOpenApplicationData_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestureSign"));
        }

        private void ShowTrayIconSwitch_Checked(object sender, RoutedEventArgs e)
        {
            NamedPipe.SendMessageAsync("ShowTrayIcon", "GestureSignDaemon");
            AppConfig.ShowTrayIcon = true;
            AppConfig.Save();
        }

        private void ShowTrayIconSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            NamedPipe.SendMessageAsync("HideTrayIcon", "GestureSignDaemon");
            AppConfig.ShowTrayIcon = false;
            AppConfig.Save();
        }

        private void ShowBalloonTipSwitch_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig.ShowBalloonTip = true;
            AppConfig.Save();
        }

        private void ShowBalloonTipSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig.ShowBalloonTip = false;
            AppConfig.Save();
        }

        private void SendLogToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig.SendErrorReport = true;
            AppConfig.Save();
        }

        private void SendLogToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig.SendErrorReport = false;
            AppConfig.Save();
        }

        private void LanguageComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (LanguageComboBox.SelectedValue == null) return;
            AppConfig.CultureName = (string)LanguageComboBox.SelectedValue;
            AppConfig.Save();
        }

        private async void ExportLogButton_Click(object sender, RoutedEventArgs e)
        {
            string logPath = Path.Combine(Path.GetTempPath(), "GestureSign" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".log");

            StringBuilder result = new StringBuilder();

            var controller =
                await
                    UIHelper.GetParentWindow(this)
                        .ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                            LocalizationProvider.Instance.GetTextValue("Options.Exporting"));
            controller.SetIndeterminate();

            await System.Threading.Tasks.Task.Run(() =>
            {
                ErrorReport.OutputLog(ref result);

                File.WriteAllText(logPath, result.ToString());

            });
            await controller.CloseAsync();

            Process.Start("notepad.exe", logPath);

            var dialogResult =
                await
                    UIHelper.GetParentWindow(this)
                        .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendLogTitle"),
                            LocalizationProvider.Instance.GetTextValue("Options.SendLog"),
                            MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                            {
                                AnimateHide = false,
                                AnimateShow = false,
                                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.SendButton"),
                                NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Options.DontSendButton")
                            });

            while (dialogResult == MessageDialogResult.Affirmative)
            {
                controller = await UIHelper.GetParentWindow(this)
                    .ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("Options.Waiting"),
                        LocalizationProvider.Instance.GetTextValue("Options.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await System.Threading.Tasks.Task.Run(() => ErrorReport.SendMail("Error Log", result.ToString()));

                await controller.CloseAsync();

                if (exceptionMessage == null)
                {
                    await UIHelper.GetParentWindow(this)
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
                    dialogResult = await
                        UIHelper.GetParentWindow(this)
                            .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Options.SendFailed"),
                                LocalizationProvider.Instance.GetTextValue("Options.SendFailed") + ":\r\n" +
                                exceptionMessage +
                                ":\r\n" + LocalizationProvider.Instance.GetTextValue("Options.Mail"),
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
