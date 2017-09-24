using GestureSign.Common.Configuration;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using IWshRuntimeLibrary;
using ManagedWinapi.Hooks;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
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

                _VisualFeedbackColor = AppConfig.VisualFeedbackColor;
                VisualFeedbackWidthSlider.Value = AppConfig.VisualFeedbackWidth;
                MinimumPointDistanceSlider.Value = AppConfig.MinimumPointDistance;
                OpacitySlider.Value = AppConfig.Opacity;
                chkOrderByLocation.IsChecked = AppConfig.IsOrderByLocation;
                ShowBalloonTipSwitch.IsChecked = AppConfig.ShowBalloonTip;
                ShowTrayIconSwitch.IsChecked = AppConfig.ShowTrayIcon;
                SendLogToggleSwitch.IsChecked = AppConfig.SendErrorReport;
                TouchPadSwitch.IsChecked = AppConfig.RegisterTouchPad;
                if (AppConfig.DrawingButton != MouseActions.None)
                {
                    MouseSwitch.IsChecked = true;
                    DrawingButtonComboBox.SelectedValue = AppConfig.DrawingButton;
                }

                LanguageComboBox.ItemsSource = LocalizationProvider.Instance.GetLanguageList("ControlPanel");
                LanguageComboBox.SelectedValue = AppConfig.CultureName;
                InitialTimeoutTextBox.Text = (AppConfig.InitialTimeout / 1000f).ToString();
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

        private void chkOrderByLocation_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig.IsOrderByLocation = true;
        }

        private void chkOrderByLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig.IsOrderByLocation = false;
        }

        private void btnOpenApplicationData_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppConfig.ApplicationDataPath);
        }

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

        private void ShowBalloonTipSwitch_Checked(object sender, RoutedEventArgs e)
        {
            AppConfig.ShowBalloonTip = true;
        }

        private void ShowBalloonTipSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            AppConfig.ShowBalloonTip = false;
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

        private void TimeoutTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            const int min = 0;
            const float max = 2f;
            var textBox = sender as System.Windows.Controls.TextBox;
            float num = 0;
            if (textBox == null || float.TryParse(textBox.Text, out num))
            {
                if (num < min)
                {
                    num = min;
                    if (textBox != null) textBox.Text = num.ToString();
                }
                else if (num > max)
                {
                    num = max;
                    if (textBox != null) textBox.Text = num.ToString();
                }
                AppConfig.InitialTimeout = (int)(num * 1000);
            }
        }

        private void TimeoutTextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var txt = sender as System.Windows.Controls.TextBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal)
            {
                if (txt != null && (txt.Text.Contains(".") && e.Key == Key.Decimal))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else if (((e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemPeriod) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (txt != null && (txt.Text.Contains(".") && e.Key == Key.OemPeriod))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }
    }
}
