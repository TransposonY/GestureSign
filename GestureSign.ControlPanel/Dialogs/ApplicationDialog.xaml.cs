using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Flyouts;
using MahApps.Metro.Controls;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using Point = System.Drawing.Point;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for ApplicationDialog.xaml
    /// </summary>
    public partial class ApplicationDialog : MetroWindow
    {
        public static event ApplicationChangedEventHandler UserApplicationChanged;
        public static event EventHandler IgnoredApplicationsChanged;

        private IApplication _currentApplication;
        private bool _isUserApp;

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

            _isUserApp = _currentApplication is UserApplication;
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
                var currentApplication = _currentApplication as UserApplication;
                if (currentApplication != null)
                {
                    GroupComboBox.Text = _currentApplication.Group;
                    ApplicationNameTextBox.Text = _currentApplication.Name;

                    AllowSingleCheckBox.IsChecked = currentApplication.AllowSingleStroke;
                    InterceptTouchInputCheckBox.IsChecked = currentApplication.InterceptTouchInput;
                }
                matchUsingRadio.MatchUsing = _currentApplication.MatchUsing;
                RegexCheckBox.IsChecked = _currentApplication.IsRegEx;
                MatchStringTextBox.Text = _currentApplication.MatchString;
            }
            GroupComboBox.ItemsSource =
                ApplicationManager.Instance.Applications.Where(app => !string.IsNullOrEmpty(app.Group))
                    .Select(app => app.Group)
                    .Distinct()
                    .OrderBy(g => g);

            InterceptTouchInputCheckBox.IsEnabled = AppConfig.UiAccess;

            AllowSingleCheckBox.Visibility = InterceptTouchInputCheckBox.Visibility =
                ApplicationNameTextBlock.Visibility = ApplicationNameTextBox.Visibility =
                GroupNameTextBlock.Visibility = GroupComboBox.Visibility =
                        _isUserApp ? Visibility.Visible : Visibility.Collapsed;
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
                ApplicationNameTextBox.Text = MatchStringTextBox.Text = ofdExecutable.SafeFileName;
            }
        }

        private void ShowRunningButton_Click(object sender, RoutedEventArgs e)
        {
            RuningAppFlyout.IsOpen = !RuningAppFlyout.IsOpen;
        }

        private void ChCrosshair_OnCrosshairDragging(object sender, MouseEventArgs e)
        {
            Point cursorPosition; //(e.OriginalSource as Image).PointToScreen(e.GetPosition(null));
            GetCursorPos(out cursorPosition);
            SystemWindow window = SystemWindow.FromPointEx(cursorPosition.X, cursorPosition.Y, true, true);

            // Set MatchUsings
            MatchUsing muCustom = matchUsingRadio.MatchUsing;
            // Which screen are we changing
            try
            {
                switch (muCustom)
                {
                    case MatchUsing.WindowClass:
                        MatchStringTextBox.Text = window.ClassName;

                        break;
                    case MatchUsing.WindowTitle:
                        MatchStringTextBox.Text = window.Title;

                        break;
                    case MatchUsing.ExecutableFilename:
                        MatchStringTextBox.Text = window.Process.MainModule.ModuleName;//.FileName;
                        MatchStringTextBox.SelectionStart = MatchStringTextBox.Text.Length;
                        break;
                }
                // Set application name from filename
                ApplicationNameTextBox.Text = window.Process.MainModule.FileVersionInfo.FileDescription;
            }
            catch (Exception ex)
            {
                MatchStringTextBox.Text = LocalizationProvider.Instance.GetTextValue("Messages.Error") + "：" + ex.Message;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveApplication())
            {
                Close();
            }
        }

        #endregion

        #region Private Methods

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

            string name;
            if (_isUserApp)
            {
                string groupName = GroupComboBox.Text ?? String.Empty;
                groupName = groupName.Trim();

                name = ApplicationNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.NoApplicationNameTitle"),
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.NoApplicationName"));
                }
                if (_currentApplication == null)
                {
                    //Add new UserApplication
                    var sameMatchApplications = ApplicationManager.Instance.FindMatchApplications<UserApplication>(matchUsingRadio.MatchUsing, matchString);
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

                    var newApplication = new UserApplication
                    {
                        InterceptTouchInput = InterceptTouchInputCheckBox.IsChecked.Value,
                        AllowSingleStroke = AllowSingleCheckBox.IsChecked.Value,
                        Name = name,
                        Group = groupName,
                        MatchString = matchString,
                        MatchUsing = matchUsingRadio.MatchUsing,
                        IsRegEx = RegexCheckBox.IsChecked.Value
                    };
                    ApplicationManager.Instance.AddApplication(newApplication);

                    UserApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(newApplication));
                }
                else
                {
                    var sameMatchApplications = ApplicationManager.Instance.FindMatchApplications<UserApplication>(matchUsingRadio.MatchUsing, matchString, _currentApplication.Name);
                    if (sameMatchApplications.Length != 0)
                    {
                        string sameApp = sameMatchApplications.Aggregate<IApplication, string>(null, (current, app) => current + (app.Name + " "));
                        return ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.StringConflictTitle"),
                            string.Format(LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.StringConflict"), matchString, sameApp));
                    }

                    if (!name.Equals(_currentApplication.Name) && ApplicationManager.Instance.ApplicationExists(name))
                    {
                        return ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.AppExistsTitle"),
                            LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.AppExists"));
                    }
                    _currentApplication.Name = name;
                    _currentApplication.Group = groupName;
                    _currentApplication.MatchUsing = matchUsingRadio.MatchUsing;
                    _currentApplication.MatchString = matchString;
                    _currentApplication.IsRegEx = RegexCheckBox.IsChecked.Value;
                    ((UserApplication)_currentApplication).AllowSingleStroke = AllowSingleCheckBox.IsChecked.Value;
                    ((UserApplication)_currentApplication).InterceptTouchInput = InterceptTouchInputCheckBox.IsChecked.Value;

                    UserApplicationChanged?.Invoke(this, new ApplicationChangedEventArgs(_currentApplication));
                }
            }
            else
            {
                name = matchUsingRadio.MatchUsing + "$" + matchString;

                if (_currentApplication != null)
                {
                    if (!name.Equals(_currentApplication.Name) && ApplicationManager.Instance.GetIgnoredApplications().Any(app => app.Name.Equals(name)))
                    {
                        return ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExistsTitle"),
                                LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExists"));
                    }
                    ApplicationManager.Instance.RemoveApplication(_currentApplication);
                }
                else if (ApplicationManager.Instance.GetIgnoredApplications().Any(app => app.Name.Equals(name)))
                {
                    return ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExistsTitle"),
                        LocalizationProvider.Instance.GetTextValue("ApplicationDialog.Messages.IgnoredAppExists"));
                }

                ApplicationManager.Instance.AddApplication(new IgnoredApplication(name, matchUsingRadio.MatchUsing, matchString, RegexCheckBox.IsChecked.Value, true));
                IgnoredApplicationsChanged?.Invoke(this, EventArgs.Empty);
            }
            ApplicationManager.Instance.SaveApplications();
            return true;
        }

        #endregion
    }
}
