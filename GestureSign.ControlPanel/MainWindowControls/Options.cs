using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using IWshRuntimeLibrary;
using MahApps.Metro.Controls.Dialogs;
using ManagedWinapi.Hooks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
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
        }

        private void LoadSettings()
        {
            try
            {
                // Try to load saved settings
                //  Common.Configuration.AppConfig.Reload();
                CheckStartupStatus();

                GestureTrailSwitch.IsChecked = AppConfig.VisualFeedbackWidth > 0;
                _VisualFeedbackColor = AppConfig.VisualFeedbackColor;
                VisualFeedbackWidthSlider.Value = AppConfig.VisualFeedbackWidth;
                MinimumPointDistanceSlider.Value = AppConfig.MinimumPointDistance;
                OpacitySlider.Value = AppConfig.Opacity;
                ShowTrayIconSwitch.IsChecked = AppConfig.ShowTrayIcon;
                SendLogToggleSwitch.IsChecked = AppConfig.SendErrorReport;
                TouchPadSwitch.IsChecked = AppConfig.RegisterTouchPad;
                IgnoreFullScreenSwitch.IsChecked = AppConfig.IgnoreFullScreen;
                IgnoreTouchInputWhenUsingPenSwitch.IsChecked = AppConfig.IgnoreTouchInputWhenUsingPen;
                if (AppConfig.DrawingButton != MouseActions.None)
                {
                    MouseSwitch.IsChecked = true;
                    DrawingButtonComboBox.SelectedValue = AppConfig.DrawingButton;
                }

                LanguageComboBox.ItemsSource = LocalizationProvider.Instance.GetLanguageList("ControlPanel");
                LanguageComboBox.SelectedValue = AppConfig.CultureName;
                if (AppConfig.InitialTimeout > 0)
                {
                    InitialTimeoutSwitch.IsChecked = true;
                    InitialTimeoutSlider.Value = AppConfig.InitialTimeout / 1000f;
                }

                var penState = AppConfig.PenGestureButton;
                if ((penState & (DeviceStates.InRange | DeviceStates.Tip)) != 0 && (penState & (DeviceStates.RightClickButton | DeviceStates.Invert)) != 0)
                {
                    PenGestureSwitch.IsChecked = true;
                    TipCheckBox.IsChecked = penState.HasFlag(DeviceStates.Tip);
                    HoverCheckBox.IsChecked = penState.HasFlag(DeviceStates.InRange);
                    RightClickButtonCheckBox.IsChecked = penState.HasFlag(DeviceStates.RightClickButton);
                    EraserCheckBox.IsChecked = penState.HasFlag(DeviceStates.Invert);
                }
                else
                {
                    PenGestureSwitch.IsChecked = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Options.Messages.LoadSettingError"),
                    LocalizationProvider.Instance.GetTextValue("Options.Messages.LoadSettingErrorTitle"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            LoadSettings();

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
        }

        private void VisualFeedbackWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVisualFeedbackExample();

            var newValue = (int)Math.Round(e.NewValue);
            if (newValue == AppConfig.VisualFeedbackWidth) return;

            AppConfig.VisualFeedbackWidth = newValue;
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Change opacity display text with new value
            OpacityText.Text = LocalizationProvider.Instance.GetTextValue("Options.Opacity") + GetAlphaPercentage(OpacitySlider.Value) + "%";
            if (Math.Abs(e.NewValue - AppConfig.Opacity) < 0.001) return;

            AppConfig.Opacity = OpacitySlider.Value;
        }

        private void MinimumPointDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var newValue = (int)Math.Round(e.NewValue);
            if (newValue == AppConfig.MinimumPointDistance || (int)e.OldValue == 0) return;
            AppConfig.MinimumPointDistance = newValue;
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

#if ConvertedDesktopApp

        private async void CheckStartupStatus()
        {
            try
            {
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
                switch (startupTask.State)
                {
                    case Windows.ApplicationModel.StartupTaskState.Disabled:
                        StartupSwitch.IsChecked = false;
                        break;
                    case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                        StartupSwitch.IsChecked = false;
                        break;
                    case Windows.ApplicationModel.StartupTaskState.Enabled:
                        StartupSwitch.IsChecked = true;
                        break;
                }
#else
        private void CheckStartupStatus()
        {
            try
            {
                string lnkPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                                 "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";
                StartupSwitch.IsChecked = File.Exists(lnkPath);
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

#if ConvertedDesktopApp

        private async void StartupSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StartupSwitch.IsChecked != null && StartupSwitch.IsChecked.Value)
                {
                    var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
                    if (startupTask.State != Windows.ApplicationModel.StartupTaskState.Enabled)
                    {
                        var state = await startupTask.RequestEnableAsync();
                        if (state == Windows.ApplicationModel.StartupTaskState.DisabledByUser)
                        {
                            MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Options.Messages.TaskUserDisabled"), LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                            StartupSwitch.IsChecked = false;
                        }
                    }
                }
                else
                {
                    var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("GestureSignTask");
                    if (startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled)
                    {
                        startupTask.Disable();
                    }
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error); }
        }

#else

        private void StartupSwitch_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string lnkPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup) +
                                 "\\" + Application.ResourceAssembly.GetName().Name + ".lnk";

                if (StartupSwitch.IsChecked != null && StartupSwitch.IsChecked.Value)
                {
                    CreateLnk(lnkPath);
                }
                else
                {
                    if (File.Exists(lnkPath))
                        File.Delete(lnkPath);
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error); }
        }

#endif

        private void ShowTrayIconSwitch_Checked(object sender, RoutedEventArgs e)
        {
            NamedPipe.SendMessageAsync("ShowTrayIcon", "GestureSignDaemon");
            AppConfig.ShowTrayIcon = true;
        }

        private void ShowTrayIconSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            NamedPipe.SendMessageAsync("HideTrayIcon", "GestureSignDaemon");
            AppConfig.ShowTrayIcon = false;
        }

        private void SendLogToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig.SendErrorReport = true;
        }

        private void SendLogToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig.SendErrorReport = false;
        }

        private void LanguageComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (LanguageComboBox.SelectedValue == null) return;
            AppConfig.CultureName = (string)LanguageComboBox.SelectedValue;
        }

        private void MouseSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (MouseSwitch.IsChecked != null && MouseSwitch.IsChecked.Value)
                DrawingButtonComboBox.SelectedValue = AppConfig.DrawingButton = MouseActions.Right;
            else AppConfig.DrawingButton = MouseActions.None;
        }

        private void DrawingButtonComboBox_DropDownClosed(object sender, EventArgs e)
        {
            AppConfig.DrawingButton = (MouseActions)DrawingButtonComboBox.SelectedValue;
        }

        private void TouchPadSwitch_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.RegisterTouchPad = TouchPadSwitch.IsChecked != null && TouchPadSwitch.IsChecked.Value;
        }

        private void IgnoreFullScreenSwitch_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.IgnoreFullScreen = IgnoreFullScreenSwitch.IsChecked.GetValueOrDefault();
        }

        private void IgnoreTouchInputWhenUsingPenSwitch_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.IgnoreTouchInputWhenUsingPen = IgnoreTouchInputWhenUsingPenSwitch.IsChecked.GetValueOrDefault();
        }

        private void InitialTimeoutSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (InitialTimeoutSwitch.IsChecked.GetValueOrDefault())
            {
                InitialTimeoutSlider.Value = 0.6;
            }
            else
            {
                InitialTimeoutSlider.Value = 0;
            }
        }

        private void InitialTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var newValue = (int)Math.Round(e.NewValue * 1000);
            if (newValue == AppConfig.InitialTimeout) return;
            AppConfig.InitialTimeout = newValue;
        }

        private void PenGestureSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (PenGestureSwitch.IsChecked.GetValueOrDefault())
            {
                AppConfig.PenGestureButton = DeviceStates.RightClickButton | DeviceStates.Tip;
                RightClickButtonCheckBox.IsChecked = TipCheckBox.IsChecked = true;
                EraserCheckBox.IsChecked = HoverCheckBox.IsChecked = false;
            }
            else
            {
                AppConfig.PenGestureButton = DeviceStates.None;
                RightClickButtonCheckBox.IsChecked = EraserCheckBox.IsChecked = HoverCheckBox.IsChecked = TipCheckBox.IsChecked = false;
            }
        }

        private void RightClickButtonCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickButtonCheckBox.IsChecked.GetValueOrDefault())
            {
                AppConfig.PenGestureButton |= DeviceStates.RightClickButton;
            }
            else
            {
                AppConfig.PenGestureButton &= ~DeviceStates.RightClickButton;
            }
        }

        private void EraserCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (EraserCheckBox.IsChecked.GetValueOrDefault())
            {
                AppConfig.PenGestureButton |= DeviceStates.Invert;
            }
            else
            {
                AppConfig.PenGestureButton &= ~DeviceStates.Invert;
            }
        }

        private void TipCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (TipCheckBox.IsChecked.GetValueOrDefault())
            {
                AppConfig.PenGestureButton |= DeviceStates.Tip;
            }
            else
            {
                AppConfig.PenGestureButton &= ~DeviceStates.Tip;
            }
        }

        private void HoverCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (HoverCheckBox.IsChecked.GetValueOrDefault())
            {
                AppConfig.PenGestureButton |= DeviceStates.InRange;
            }
            else
            {
                AppConfig.PenGestureButton &= ~DeviceStates.InRange;
            }
        }

        private void GestureTrailSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (GestureTrailSwitch.IsChecked.GetValueOrDefault())
            {
                VisualFeedbackWidthSlider.Value = 9;
            }
            else
            {
                VisualFeedbackWidthSlider.Value = 0;
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Options.BackupFile") + "|*" + GestureSign.Common.Constants.BackupFileExtension,
                FileName = LocalizationProvider.Instance.GetTextValue("Options.BackupFile") + " " + DateTime.Now.ToString("MMddHHmm"),
                Title = LocalizationProvider.Instance.GetTextValue("Options.Backup"),
                AddExtension = true,
                DefaultExt = GestureSign.Common.Constants.BackupFileExtension.Remove(0, 1),
                ValidateNames = true
            };
            if (saveFileDialog.ShowDialog().Value)
            {
                try
                {
                    Archive.CreateArchive(ApplicationManager.Instance.Applications, GestureManager.Instance.Gestures, saveFileDialog.FileName, AppConfig.ConfigPath);

                    UIHelper.GetParentWindow(this).ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Options.Messages.BackupCompleteTitle"), null);
                }
                catch (Exception exception)
                {
                    UIHelper.GetParentWindow(this).ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Messages.Error"), exception.Message);
                }
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = $"{LocalizationProvider.Instance.GetTextValue("Options.BackupFile")}|*{GestureSign.Common.Constants.BackupFileExtension}",
                Title = LocalizationProvider.Instance.GetTextValue("Common.Import"),
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog().Value)
            {
                try
                {
                    string tempArchivePath = Archive.ExtractToTempDirectory(openFileDialog.FileName);

                    string configPath = Path.Combine(tempArchivePath, Path.GetFileName(AppConfig.ConfigPath));
                    if (File.Exists(configPath))
                        File.Copy(configPath, AppConfig.ConfigPath, true);
                    AppConfig.Reload();
                    LoadSettings();

                    var applications = FileManager.LoadObject<List<IApplication>>(Path.Combine(tempArchivePath, GestureSign.Common.Constants.ActionFileName), false, true, true);
                    var gestures = FileManager.LoadObject<List<Gesture>>(Path.Combine(tempArchivePath, GestureSign.Common.Constants.GesturesFileName), false, false, true);

                    if (gestures != null)
                    {
                        var oldGestures = GestureManager.Instance.Gestures;
                        foreach (var g in oldGestures)
                        {
                            GestureManager.Instance.DeleteGesture(g.Name);
                        }
                        foreach (var g in gestures)
                        {
                            GestureManager.Instance.AddGesture(g);
                        }

                        GestureManager.Instance.SaveGestures();
                    }
                    if (applications != null)
                    {
                        ApplicationManager.Instance.RemoveAllApplication();
                        ApplicationManager.Instance.AddApplicationRange(applications);

                        ApplicationManager.Instance.SaveApplications();
                    }

                    Directory.Delete(tempArchivePath, true);
                    UIHelper.GetParentWindow(this).ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Options.Messages.RestoreCompleteTitle"), null);
                }
                catch (Exception exception)
                {
                    UIHelper.GetParentWindow(this).ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Messages.Error"), exception.Message);
                }
            }
        }
    }
}
