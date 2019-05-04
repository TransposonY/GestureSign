using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Flyouts;
using IWshRuntimeLibrary;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Point = System.Drawing.Point;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for ApplicationDialog.xaml
    /// </summary>
    public partial class ApplicationDialog : TouchWindow
    {
        private IApplication _currentApplication;
        private bool _isUserApp;
        private Dictionary<uint, string> _processInfoMap;

        public ApplicationListViewItem ApplicationListViewItem
        {
            get { return (ApplicationListViewItem)GetValue(ApplicationListViewItemProperty); }
            set { SetValue(ApplicationListViewItemProperty, value); }

        }
        public static readonly DependencyProperty ApplicationListViewItemProperty =
            DependencyProperty.Register("ApplicationListViewItem", typeof(ApplicationListViewItem), typeof(ApplicationDialog), new FrameworkPropertyMetadata(null));

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out Point lpPoint);

        public ApplicationDialog()
        {
            InitializeComponent();
            RuningApplicationsFlyout.RuningAppSelectionChanged += (o, e) => { if (e != null) ApplicationListViewItem = e; };
        }

        public ApplicationDialog(IApplication targetApplication) : this()
        {
            _currentApplication = targetApplication;

            Title = LocalizationProvider.Instance.GetTextValue("ApplicationDialog.EditApplication");

            _isUserApp = _currentApplication is UserApp;
        }

        public ApplicationDialog(bool addUserApp) : this()
        {
            _isUserApp = addUserApp;

            Title = _isUserApp
                ? LocalizationProvider.Instance.GetTextValue("ApplicationDialog.AddApplication")
                : LocalizationProvider.Instance.GetTextValue("ApplicationDialog.AddIgnoredAppTitle");
        }

        #region Events

        private void ApplicationDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_currentApplication != null)
            {
                var currentApplication = _currentApplication as UserApp;
                if (currentApplication != null)
                {
                    GroupComboBox.Text = _currentApplication.Group;

                    BlockTouchInputSlider.Value = currentApplication.BlockTouchInputThreshold;
                    LimitNumberOfFingersSlider.Value = currentApplication.LimitNumberOfFingers;
                }
                ApplicationNameTextBox.Text = _currentApplication.Name;
                matchUsingRadio.MatchUsing = _currentApplication.MatchUsing;
                RegexCheckBox.IsChecked = _currentApplication.IsRegEx;
                MatchStringTextBox.Text = _currentApplication.MatchString;
            }
            GroupComboBox.ItemsSource =
                ApplicationManager.Instance.Applications.Where(app => !string.IsNullOrEmpty(app.Group))
                    .Select(app => app.Group)
                    .Distinct()
                    .OrderBy(g => g);

            LimitNumberOfFingersSlider.Visibility = LimitNumberOfFingersInfoTextBlock.Visibility = LimitNumberOfFingersTextBlock.Visibility =
                            GroupNameTextBlock.Visibility = GroupComboBox.Visibility =
                                _isUserApp ? Visibility.Visible : Visibility.Collapsed;

            BlockTouchInputSlider.Visibility = BlockTouchInputInfoTextBlock.Visibility = BlockTouchInputTextBlock.Visibility =
                    AppConfig.UiAccess && _isUserApp ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MatchStringTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            MatchStringPopup.IsOpen = true;
            MatchStringPopupTextBox.Focus();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdExecutable = new OpenFileDialog
            {
                Filter = LocalizationProvider.Instance.GetTextValue("ApplicationDialog.ExecutableFile") + "|*.exe"
            };
            if (ofdExecutable.ShowDialog().Value)
            {
                matchUsingRadio.MatchUsing = MatchUsing.ExecutableFilename;
                ApplicationNameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(ofdExecutable.FileName);
                MatchStringTextBox.Text = ofdExecutable.SafeFileName;
            }
        }

        private void ShowRunningButton_Click(object sender, RoutedEventArgs e)
        {
            RuningAppFlyout.IsOpen = !RuningAppFlyout.IsOpen;
        }

        private void ChCrosshair_OnCrosshairDragging(object sender, MouseEventArgs e)
        {
            string className, title, fileName;
            var window = GetTargetWindow();
            var realWindow = ApplicationManager.GetWindowInfo(window, out className, out title, out fileName);
            try
            {
                // Set application name from filename
                ApplicationNameTextBox.Text = GetDescription(realWindow);
                switch (matchUsingRadio.MatchUsing)
                {
                    case MatchUsing.WindowClass:
                        MatchStringTextBox.Text = className;

                        break;
                    case MatchUsing.WindowTitle:
                        MatchStringTextBox.Text = title;

                        break;
                    case MatchUsing.ExecutableFilename:
                        MatchStringTextBox.Text = GetProcessFilename((uint)realWindow.ProcessId);
                        MatchStringTextBox.SelectionStart = MatchStringTextBox.Text.Length;
                        break;
                }
            }
            catch (Exception ex)
            {
                MatchStringTextBox.Text = LocalizationProvider.Instance.GetTextValue("Messages.Error") + "：" + ex.Message;
            }
        }

        private void chCrosshair_CrosshairDragged(object sender, MouseButtonEventArgs e)
        {
            _processInfoMap = null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveApplication())
            {
                if (!DialogResult.GetValueOrDefault())
                    DialogResult = true;
                Close();
            }
        }

        private void BlockTouchInputSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BlockTouchInputInfoTextBlock.Text = e.NewValue < 2
                ? LocalizationProvider.Instance.GetTextValue("Options.Off")
                : string.Format(LocalizationProvider.Instance.GetTextValue("ApplicationDialog.BlockTouchInputInfo"),
                    (int)e.NewValue);
        }

        private void LimitNumberOfFingersSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LimitNumberOfFingersInfoTextBlock.Text = string.Format(
                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.LimitNumberOfFingersInfo"),
                (int)e.NewValue);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files?.Length > 0)
                    {
                        string targetFile = files[0];
                        if (targetFile.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        {
                            WshShell shell = new WshShell();
                            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(targetFile);
                            targetFile = link.TargetPath;
                        }
                        if (Path.GetExtension(targetFile).ToLower() == ".exe")
                        {
                            matchUsingRadio.MatchUsing = MatchUsing.ExecutableFilename;

                            var versionInfo = FileVersionInfo.GetVersionInfo(targetFile);
                            ApplicationNameTextBox.Text = string.IsNullOrWhiteSpace(versionInfo.ProductName) ? Path.GetFileNameWithoutExtension(targetFile) : versionInfo.ProductName;

                            MatchStringTextBox.Text = Path.GetFileName(targetFile);
                        }
                    }
                }
                catch (Exception exception)
                {
                    this.ShowModalMessageExternal(exception.GetType().Name, exception.Message);
                }
            }
            e.Handled = true;
        }

        #endregion

        #region Private Methods

        private string GetDescription(SystemWindow window)
        {
            try
            {
                return window.Process.MainModule.FileVersionInfo.FileDescription;
            }
            catch (Exception)
            {
                return window.Title;
            }
        }

        private string GetProcessFilename(uint pid)
        {
            if (_processInfoMap == null)
            {
                _processInfoMap = new Dictionary<uint, string>();
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name FROM Win32_Process"))
                using (var results = searcher.Get())
                {
                    foreach (var item in results)
                    {
                        var id = item["ProcessID"];
                        var name = item["Name"] as string;

                        if (name != null)
                        {
                            _processInfoMap.Add((uint)id, name);
                        }
                    }
                }
            }

            if (_processInfoMap.ContainsKey(pid))
                return _processInfoMap[pid];
            return null;
        }

        private SystemWindow GetTargetWindow()
        {
            Point cursorPosition; //(e.OriginalSource as Image).PointToScreen(e.GetPosition(null));
            GetCursorPos(out cursorPosition);

            SystemWindow window = SystemWindow.FromPointEx(cursorPosition.X, cursorPosition.Y, true, true);
            return window;
        }

        private bool ShowErrorMessage(string title, string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = title;
            MessageFlyout.IsOpen = true;
            return false;
        }

        private bool SaveApplication()
        {
            string matchString = MatchStringTextBox.Text.Trim();

            if (string.IsNullOrEmpty(matchString))
            {
                return ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.EmptyStringTitle"),
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.EmptyString"));
            }

            string name = ApplicationNameTextBox.Text.Trim();
            if (_isUserApp)
            {
                string groupName = string.IsNullOrWhiteSpace(GroupComboBox.Text) ? null : GroupComboBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.NoApplicationNameTitle"),
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.NoApplicationName"));
                }

                var newApplication = new UserApp
                {
                    BlockTouchInputThreshold = (int)BlockTouchInputSlider.Value,
                    LimitNumberOfFingers = (int)LimitNumberOfFingersSlider.Value,
                    Name = name,
                    Group = groupName,
                    MatchString = matchString,
                    MatchUsing = matchUsingRadio.MatchUsing,
                    IsRegEx = RegexCheckBox.IsChecked.Value
                };

                if (_currentApplication == null)
                {
                    //Add new UserApplication
                    var sameMatchApplications = ApplicationManager.Instance.FindMatchApplications<UserApp>(matchUsingRadio.MatchUsing, matchString);
                    if (sameMatchApplications.Length != 0)
                    {
                        string sameApp = sameMatchApplications.Aggregate<IApplication, string>(null, (current, app) => current + (app.Name + " "));
                        return
                            ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.StringConflictTitle"),
                                string.Format(LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.StringConflict"), matchString, sameApp));
                    }

                    if (ApplicationManager.Instance.ApplicationExists(name))
                        return ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.AppExistsTitle"),
                                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.AppExists"));
                    ApplicationManager.Instance.AddApplication(newApplication);
                }
                else
                {
                    var sameMatchApplications = ApplicationManager.Instance.FindMatchApplications<UserApp>(matchUsingRadio.MatchUsing, matchString, _currentApplication.Name);
                    if (sameMatchApplications.Length != 0)
                    {
                        string sameApp = sameMatchApplications.Aggregate<IApplication, string>(null, (current, app) => current + (app.Name + " "));
                        return ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.StringConflictTitle"),
                            string.Format(LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.StringConflict"), matchString, sameApp));
                    }

                    if (name != _currentApplication.Name && ApplicationManager.Instance.ApplicationExists(name))
                    {
                        return ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.AppExistsTitle"),
                            LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.AppExists"));
                    }

                    newApplication.Actions = _currentApplication.Actions;
                    ApplicationManager.Instance.ReplaceApplication(_currentApplication, newApplication);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(name))
                    name = matchString;

                if (_currentApplication != null)
                {
                    var existingApp = ApplicationManager.Instance.FindMatchApplications<IgnoredApp>(matchUsingRadio.MatchUsing, matchString, _currentApplication.Name);
                    if (existingApp.Length != 0)
                    {
                        return ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExistsTitle"),
                                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExists"));
                    }
                    ApplicationManager.Instance.RemoveApplication(_currentApplication);
                }
                else if (ApplicationManager.Instance.GetIgnoredApplications().Any(app => app.MatchUsing == matchUsingRadio.MatchUsing && app.MatchString == matchString))
                {
                    return ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExistsTitle"),
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExists"));
                }

                ApplicationManager.Instance.AddApplication(new IgnoredApp(name, matchUsingRadio.MatchUsing, matchString, RegexCheckBox.IsChecked.Value, true));
            }
            ApplicationManager.Instance.SaveApplications();
            return true;
        }

        #endregion
    }
}
