using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.UI.Common;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using Point = System.Drawing.Point;

namespace GestureSign.ControlPanel.UI
{
    public partial class EditApplicationFlyout : Flyout
    {
        public static event EventHandler OpenIgnoredRuningFlyout;
        public static event EventHandler RefreshIgnoredApplications;
        public static event ApplicationChangedEventHandler ApplicationChanged;

        private bool _editMode;
        private IApplication _currentApplication;
        private bool _isUserApp;

        public ApplicationListViewItem ApplicationListViewItem
        {
            get { return (ApplicationListViewItem)GetValue(ApplicationListViewItemProperty); }
            set { SetValue(ApplicationListViewItemProperty, value); }

        }
        public static readonly DependencyProperty ApplicationListViewItemProperty =
            DependencyProperty.Register("ApplicationListViewItem", typeof(ApplicationListViewItem), typeof(EditApplicationFlyout), new FrameworkPropertyMetadata(null));


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out Point lpPoint);


        public EditApplicationFlyout()
        {
            InitializeComponent();
            crosshairMain.CrosshairDragging += crosshairMain_CrosshairDragging;
            IgnoredApplications.ShowIgnoredCustomFlyout += ShowEditApplicationFlyout;
            AvailableAction.ShowEditApplicationFlyout += ShowEditApplicationFlyout;
            RuningApplicationsFlyout.RuningAppSelectionChanged += RuningApplicationsFlyout_RuningAppSelectionChanged;

        }


        private void Flyout_ClosingFinished(object sender, RoutedEventArgs e)
        {
            ClearManualFields();
        }
        void ShowEditApplicationFlyout(object sender, ApplicationChangedEventArgs e)
        {
            _currentApplication = e.Application;
            //CurrentApplication may be null or IgnoredApplication
            _isUserApp = _currentApplication is UserApplication;

            if (_isUserApp)
            {
                GroupComboBox.ItemsSource = ApplicationManager.Instance.Applications.Select(app => app.Group).Distinct();
                ApplicationNameTextBox.Text = _currentApplication.Name;
                GroupComboBox.Text = _currentApplication.Group;
                chkAllowSingleStroke.IsChecked = ((UserApplication)_currentApplication).AllowSingleStroke;
                chkInterceptTouchInput.IsChecked = ((UserApplication)_currentApplication).InterceptTouchInput;
                chkInterceptTouchInput.IsEnabled = AppConfig.UiAccess;
            }

            chkAllowSingleStroke.Visibility = chkInterceptTouchInput.Visibility = ApplicationNameTextBlock.Visibility =
                ApplicationNameTextBox.Visibility = GroupNameTextBlock.Visibility = GroupComboBox.Visibility =
                  _isUserApp ? Visibility.Visible : Visibility.Collapsed;

            Theme = _isUserApp ? FlyoutTheme.Adapt : FlyoutTheme.Inverse;
            if (_currentApplication != null)
            {
                matchUsingRadio.MatchUsing = _currentApplication.MatchUsing;
                _editMode = true;
                Header = LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.EditApplicationTitle");
                chkPattern.IsChecked = _currentApplication.IsRegEx;
                txtMatchString.Text = _currentApplication.MatchString;
            }
            else Header = LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.AddIgnoredAppTitle");
            IsOpen = true;
        }
        void RuningApplicationsFlyout_RuningAppSelectionChanged(object sender, ApplicationListViewItem e)
        {
            if (e != null)
            {
                ApplicationListViewItem = e;
            }
        }
        private void ShowRunningButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpenIgnoredRuningFlyout != null) OpenIgnoredRuningFlyout(this, EventArgs.Empty);
        }


        void crosshairMain_CrosshairDragging(object sender, MouseEventArgs e)
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
                        txtMatchString.Text = window.ClassName;

                        break;
                    case MatchUsing.WindowTitle:
                        txtMatchString.Text = window.Title;

                        break;
                    case MatchUsing.ExecutableFilename:
                        txtMatchString.Text = window.Process.MainModule.ModuleName;//.FileName;
                        txtMatchString.SelectionStart = txtMatchString.Text.Length;
                        break;
                }
            }
            catch (Exception ex)
            {
                txtMatchString.Text = LocalizationProvider.Instance.GetTextValue("Messages.Error") + "：" + ex.Message;
            }
        }



        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog
            {
                Filter = LocalizationProvider.Instance.GetTextValue("ActionDialog.ExecutableFile") + "|*.exe"
            };
            if (op.ShowDialog().Value)
            {
                txtMatchString.Text = op.SafeFileName;
            }
        }

        private void btnAddCustom_Click(object sender, RoutedEventArgs e)
        {
            string matchString = txtMatchString.Text.Trim();

            if (String.IsNullOrEmpty(matchString))
            {
                UIHelper.GetParentWindow(this)
                    .ShowMessageAsync(
                        LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.Messages.EmptyStringTitle"),
                        LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.Messages.EmptyString"),
                        settings:
                            new MetroDialogSettings
                            {
                                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK")
                            });
                return;
            }

            string name;
            if (_isUserApp)
            {
                name = ApplicationNameTextBox.Text.Trim();
                string groupName = GroupComboBox.Text ?? String.Empty;
                groupName = groupName.Trim();
                if (name.Length == 0)
                {
                    UIHelper.GetParentWindow(this)
                        .ShowMessageAsync(
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoApplicationNameTitle"),
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoApplicationName"),
                            settings:
                                new MetroDialogSettings
                                {
                                    AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK")
                                });
                    return;
                }
                if (!name.Equals(_currentApplication.Name) && ApplicationManager.Instance.ApplicationExists(name))
                {
                    UIHelper.GetParentWindow(this)
                        .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.AppExistsTitle"),
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.AppExists"),
                            settings:
                                new MetroDialogSettings
                                {
                                    AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK")
                                });
                    return;
                }
                _currentApplication.Name = name;
                _currentApplication.Group = groupName;
                _currentApplication.MatchUsing = matchUsingRadio.MatchUsing;
                _currentApplication.MatchString = matchString;
                _currentApplication.IsRegEx = chkPattern.IsChecked.Value;
                ((UserApplication)_currentApplication).AllowSingleStroke = chkAllowSingleStroke.IsChecked.Value;
                ((UserApplication)_currentApplication).InterceptTouchInput = chkInterceptTouchInput.IsChecked.Value;
                if (ApplicationChanged != null) ApplicationChanged(this, new ApplicationChangedEventArgs(_currentApplication));
            }
            else
            {
                name = matchUsingRadio.MatchUsing + "$" + matchString;

                if (_editMode)
                {
                    if (!name.Equals(_currentApplication.Name) && ApplicationManager.Instance.GetIgnoredApplications().Any(app => app.Name.Equals(name)))
                    {
                        UIHelper.GetParentWindow(this)
                            .ShowMessageAsync(
                                LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.Messages.IgnoredAppExistsTitle"),
                                LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.Messages.IgnoredAppExists"),
                                settings:
                                    new MetroDialogSettings
                                    {
                                        AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK")
                                    });
                        return;
                    }
                    ApplicationManager.Instance.RemoveApplication(_currentApplication);
                }
                else if (ApplicationManager.Instance.GetIgnoredApplications().Any(app => app.Name.Equals(name)))
                {
                    UIHelper.GetParentWindow(this).ShowMessageAsync(
                        LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.Messages.IgnoredAppExistsTitle"),
                        LocalizationProvider.Instance.GetTextValue("EditApplicationFlyout.Messages.IgnoredAppExists"),
                        settings:
                            new MetroDialogSettings
                            {
                                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK")
                            });
                    return;
                }

                ApplicationManager.Instance.AddApplication(new IgnoredApplication(name, matchUsingRadio.MatchUsing, matchString, chkPattern.IsChecked.Value, true));
                if (RefreshIgnoredApplications != null) RefreshIgnoredApplications(this, EventArgs.Empty);
            }
            ApplicationManager.Instance.SaveApplications();
            _editMode = false;
            IsOpen = false;
        }

        public void ClearManualFields()
        {
            _editMode = false;
            GroupComboBox.Text = ApplicationNameTextBox.Text = txtMatchString.Text = "";
            _currentApplication = null;
            ApplicationListViewItem = null;
            chkAllowSingleStroke.IsChecked = chkInterceptTouchInput.IsChecked = chkPattern.IsChecked = false;
        }

        private void txtMatchString_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            MatchStringPopup.IsOpen = true;
            MatchStringPopupTextBox.Focus();
        }


    }
    public class MatchStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ApplicationListViewItem applicationListViewItem = values[0] as ApplicationListViewItem;
            MatchUsing matchUsing = (MatchUsing)values[1];
            if (applicationListViewItem == null) return null;
            return matchUsing == MatchUsing.ExecutableFilename
                ? applicationListViewItem.WindowFilename
                : matchUsing == MatchUsing.WindowTitle
                    ? applicationListViewItem.WindowTitle
                    : applicationListViewItem.WindowClass;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
