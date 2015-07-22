using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.UI.Common;
using MahApps.Metro.Controls;
using ManagedWinapi.Windows;
using Microsoft.Win32;
using Point = System.Drawing.Point;

namespace GestureSign.UI
{
    public partial class ActionDialog : MetroWindow
    {
        public ActionDialog()
        {
            InitializeComponent();
        }

        //Add action by new gesture
        public ActionDialog(string newGestureName)
            : this()
        {
            _gestureName = newGestureName;
        }
        //Add action by existing gesture
        public ActionDialog(AvailableAction source, IApplication selectedApplication)
            : this()
        {
            _selectedApplication = selectedApplication;
            _availableAction = source;
        }
        //Edit action
        public ActionDialog(AvailableAction source, IAction selectedAction, IApplication selectedApplication)
            : this(source, selectedApplication)
        {
            Title = LocalizationProvider.Instance.GetTextValue("ActionDialog.EditActionTitle");
            _currentAction = selectedAction;
        }

        #region Private Instance Fields

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out Point lpPoint);

        private readonly IApplication _selectedApplication;
        IApplication _newApplication;
        readonly AvailableAction _availableAction;
        // Create variable to hold current selected plugin
        IPluginInfo _pluginInfo;
        IAction _currentAction;
        string _gestureName;
        #endregion

        #region Public Instance Properties

        public static event EventHandler<ActionChangedEventArgs> ActionsChanged;
        #endregion


        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            BindGroupComboBox();
            BindExistingApplications();
            BindExistingGestures();

            chCrosshair.CrosshairDragging += chCrosshair_CrosshairDragging;
            BindPlugins();
            if (_currentAction != null)
            {
                //cmbExistingApplication.SelectedItem = ApplicationManager.Instance.CurrentApplication;
                foreach (object comboItem in cmbPlugins.Items)
                {
                    IPluginInfo pluginInfo = (IPluginInfo)comboItem;

                    if (pluginInfo.Class == _currentAction.PluginClass && pluginInfo.Filename == _currentAction.PluginFilename)
                    {
                        cmbPlugins.SelectedIndex = cmbPlugins.Items.IndexOf(comboItem);
                        return;
                    }
                }
            }

            NamedPipe.SendMessageAsync("DisableTouchCapture", "GestureSignDaemon");
        }

        protected void chCrosshair_CrosshairDragging(object sender, MouseEventArgs e)
        {
            Point cursorPosition;
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

                // Set application name from filename
                txtApplicationName.Text = window.Process.MainModule.FileVersionInfo.FileDescription;
            }
            catch (Exception ex) { txtApplicationName.Text = ex.Message; }

        }


        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveApplication())
            {
                if (SaveAction())
                {
                    Close();
                }
            }
        }

        private void cmdBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdExecutable = new OpenFileDialog
            {
                Filter = LocalizationProvider.Instance.GetTextValue("ActionDialog.ExecutableFile") + "|*.exe"
            };
            if (ValidateFilepath(txtMatchString.Text.Trim()))
            {
                ofdExecutable.InitialDirectory = Path.GetDirectoryName(txtMatchString.Text);
                ofdExecutable.FileName = Path.GetFileName(txtMatchString.Text);
            }
            if (ofdExecutable.ShowDialog().Value)
            {
                txtApplicationName.Text = txtMatchString.Text = Path.GetFileName(ofdExecutable.FileName);

            }
        }

        private async void MetroWindow_Closed(object sender, EventArgs e)
        {
            // User canceled dialog, re-enable gestures and hide form

            await NamedPipe.SendMessageAsync("EnableTouchCapture", "GestureSignDaemon");
        }

        private void cmbPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cmbPlugins.SelectedItem == null) return;
            LoadPlugin((IPluginInfo)cmbPlugins.SelectedItem);
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }




        #endregion


        #region Private Instance Methods

        private bool SaveApplication()
        {

            if (ExistingApplicationRadioButton.IsChecked.Value)
            {
                if (_currentAction != null && cmbExistingApplication.SelectedItem as IApplication != _selectedApplication)
                {
                    if (((IApplication)cmbExistingApplication.SelectedItem).Actions.Any(a => a.Name.Equals(TxtActionName.Text.Trim())))
                    {
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                                    TxtActionName.Text.Trim(), ((IApplication)cmbExistingApplication.SelectedItem).Name));
                    }
                }
                ApplicationManager.Instance.CurrentApplication = (IApplication)cmbExistingApplication.SelectedItem;
                return true;
            }
            if (NewApplicationRadioButton.IsChecked.Value)
            {
                string appName = txtApplicationName.Text.Trim();

                // Make sure we have a valid application name
                if (String.IsNullOrWhiteSpace(appName))
                    return
                        ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoApplicationNameTitle"),
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoApplicationName"));

                if (_currentAction == null && ApplicationManager.Instance.ApplicationExists(appName))
                    return ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.AppExistsTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.AppExists"));


                string matchString = txtMatchString.Text.Trim();
                // Make sure the user entered a match string
                if (String.IsNullOrEmpty(matchString))
                    return
                        ShowErrorMessage(
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoMatchStringTitle"),
                            LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoMatchString"));

                _newApplication = new UserApplication
                {
                    InterceptTouchInput = InterceptTouchInputCheckBox.IsChecked.Value,
                    AllowSingleStroke = AllowSingleCheckBox.IsChecked.Value,
                    Name = appName,
                    Group = GroupComboBox.Text.Trim(),
                    MatchString = matchString,
                    MatchUsing = matchUsingRadio.MatchUsing,
                    IsRegEx = chkRegex.IsChecked.Value
                };


                ApplicationManager.Instance.CurrentApplication = _newApplication;
                return true;
            }
            return false;
        }


        private bool ShowErrorMessage(string title, string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = title;
            MessageFlyout.IsOpen = true;
            return false;
        }


        private void BindExistingApplications()
        {
            // Clear existing items
            cmbExistingApplication.Items.Clear();

            // Add generic application listview item
            IApplication allApplicationsItem = ApplicationManager.Instance.GetGlobalApplication();

            IApplication[] existingApplications = ApplicationManager.Instance.GetAvailableUserApplications();

            // Add application items to the combobox
            cmbExistingApplication.Items.Add(allApplicationsItem);
            foreach (IApplication app in existingApplications)
                cmbExistingApplication.Items.Add(app);

            // Select new applications
            cmbExistingApplication.SelectedItem = _selectedApplication ?? allApplicationsItem;
        }
        private void BindExistingGestures()
        {

            Binding bind = new Binding();
            //no GestureItem source
            if (_availableAction == null)
            {
                IEnumerable<IGesture> results = GestureManager.Instance.Gestures.OrderBy(g => g.Name);//.GroupBy(g => g.Name).Select(g => g.First().Name);
                List<GestureItem> gestureItems = new List<GestureItem>(results.Count());
                var brush = Application.Current.Resources["HighlightBrush"] as Brush ?? Brushes.RoyalBlue;
                gestureItems.AddRange(results.Select(gesture => new GestureItem
                {
                    Image = GestureImage.CreateImage(gesture.Points, new Size(65, 65), brush),
                    Name = gesture.Name
                }));
                bind.Source = gestureItems;
            }
            else
            {
                bind.Source = ((UIHelper.GetParentDependencyObject<TabControl>(_availableAction)).FindName("availableGestures") as AvailableGestures).lstAvailableGestures;
                bind.Path = new PropertyPath("Items");

                var ai = _availableAction.lstAvailableActions.SelectedItem as AvailableAction.ActionInfo;
                if (ai != null)
                    _gestureName = ai.GestureName;
            }
            bind.Mode = BindingMode.OneWay;
            availableGesturesComboBox.SetBinding(ComboBox.ItemsSourceProperty, bind);
            if (_currentAction != null)
            {
                _gestureName = _currentAction.GestureName;
            }
            if (_gestureName != null)
            {
                foreach (GestureItem item in availableGesturesComboBox.Items)
                {
                    if (item.Name == _gestureName)
                        availableGesturesComboBox.SelectedIndex = availableGesturesComboBox.Items.IndexOf(item);
                }
            }
        }
        private void BindGroupComboBox()
        {
            GroupComboBox.ItemsSource = ApplicationManager.Instance.Applications.Select(app => app.Group).Distinct();
        }
        #endregion

        #region Type Methods

        public static bool ValidateFilepath(string path)
        {
            if (path.Trim() == String.Empty)
                return false;

            string pathname;
            string filename;

            try
            {
                pathname = Path.GetPathRoot(path);
                filename = Path.GetFileName(path);
            }
            catch (ArgumentException)
            {
                // GetPathRoot() and GetFileName() above will throw exceptions
                // if pathname/filename could not be parsed.

                return false;
            }

            // Make sure the filename part was actually specified
            if (filename.Trim() == String.Empty)
                return false;

            // Not sure if additional checking below is needed, but no harm done
            if (pathname.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            return true;
        }

        #endregion






        public void RefreshApplications()
        {
            alvRunningApplications.Items.Clear();
            ThreadPool.QueueUserWorkItem(GetValidWindows);
            //  this.lstRunningApplications.Items.Clear();
        }
        private void GetValidWindows(object s)
        {
            // Get valid running windows
            var windows = SystemWindow.AllToplevelWindows.Where
                     (
                         w => w.Visible &&	// Must be a visible windows
                         w.Title != "" &&	// Must have a window title
                         IsProcessAccessible(w.Process) &&
                        Path.GetDirectoryName(w.Process.ProcessName) != Process.GetCurrentProcess().ProcessName &&	// Must not be a GestureSign window
                         (w.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) != WindowExStyleFlags.TOOLWINDOW	// Must not be a tool window
                     );
            //System.Threading.Thread.Sleep(500);
            foreach (SystemWindow sWind in windows)
            {
                alvRunningApplications.Dispatcher.BeginInvoke(new Action(() =>
               {
                   ApplicationListViewItem lItem = new ApplicationListViewItem();

                   try
                   {
                       // Store identifying information
                       lItem.WindowClass = sWind.ClassName;
                       lItem.WindowTitle = sWind.Title;
                       lItem.WindowFilename = Path.GetFileName(sWind.Process.MainModule.FileName);
                       lItem.ApplicationIcon = Imaging.CreateBitmapSourceFromHIcon(sWind.Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                       alvRunningApplications.Items.Add(lItem);
                   }
                   catch
                   {
                   }
               }));
            }
        }


        private bool IsProcessAccessible(Process Process)
        {
            try
            {
                ProcessModule module = Process.MainModule;
                return true;
            }
            catch { return false; }
        }




        private bool SaveAction()
        {
            if (availableGesturesComboBox.SelectedItem == null)
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoSelectedGestureTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoSelectedGesture"));
            // Check if we're creating a new action
            bool isNew = false;
            if (_currentAction == null)
            {
                _currentAction = new Applications.Action();
                isNew = true;
            }
            string newActionName = TxtActionName.Text.Trim();

            if (String.IsNullOrEmpty(newActionName))
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoActionNameTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoActionName"));

            if (isNew)
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(newActionName))
                    {
                        _currentAction = null;
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsInGlobal"),
                                    newActionName));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.CurrentApplication.Actions.Any(a => a.Name.Equals(newActionName)))
                    {
                        _currentAction = null;
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                                    newActionName, ApplicationManager.Instance.CurrentApplication.Name));
                    }
                }
            }
            else
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(newActionName) && newActionName != _currentAction.Name)
                    {
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsInGlobal"),
                                    newActionName));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.CurrentApplication.Actions.Any(a => a.Name.Equals(newActionName)) && newActionName != _currentAction.Name)
                    {
                        return
                            ShowErrorMessage(
                                LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                                String.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                                    newActionName, ApplicationManager.Instance.CurrentApplication.Name));
                    }
                }
            }

            // Store new values
            _currentAction.GestureName = (availableGesturesComboBox.SelectedItem as GestureItem).Name;
            _currentAction.Name = newActionName;
            _currentAction.PluginClass = _pluginInfo.Class;
            _currentAction.PluginFilename = _pluginInfo.Filename;
            _currentAction.ActionSettings = _pluginInfo.Plugin.Serialize();
            _currentAction.IsEnabled = true;

            if (isNew)
            {
                if (_newApplication is UserApplication)
                {
                    ApplicationManager.Instance.AddApplication(_newApplication);
                }
                // Save new action to specific application
                ApplicationManager.Instance.CurrentApplication.AddAction(_currentAction);

            }
            else
            {
                if (ExistingApplicationRadioButton.IsChecked.Value)
                {
                    if (cmbExistingApplication.SelectedItem as IApplication != _selectedApplication)
                    {
                        _selectedApplication.RemoveAction(_currentAction);
                        ((IApplication)cmbExistingApplication.SelectedItem).AddAction(_currentAction);
                    }
                }
                if (NewApplicationRadioButton.IsChecked.Value)
                {
                    _selectedApplication.RemoveAction(_currentAction);
                    _newApplication.AddAction(_currentAction);
                    ApplicationManager.Instance.AddApplication(_newApplication);
                }

            }
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
            if (ActionsChanged != null)
                ActionsChanged(this, new ActionChangedEventArgs(ApplicationManager.Instance.CurrentApplication, _currentAction));

            return true;
        }

        private void BindPlugins()
        {
            // Compile list of PluginInfo objects to bind to drop down
            List<IPluginInfo> availablePlugins = new List<IPluginInfo>();
            availablePlugins.AddRange(PluginManager.Instance.Plugins.Where(pi => pi.Plugin.IsAction).OrderBy(pi => pi.ToString()));

            // Bind available plugin list to combo box
            // cmbPlugins.DisplayMemberPath = "DisplayText";
            cmbPlugins.ItemsSource = availablePlugins;
            cmbPlugins.SelectedIndex = 0;
        }

        private void LoadPlugin(IPluginInfo selectedPlugin)
        {
            // Try to load plugin, and set current plugin to newly selected plugin
            _pluginInfo = selectedPlugin;

            // Set action name
            if (IsPluginMatch(_currentAction, selectedPlugin.Class, selectedPlugin.Filename))
            {
                TxtActionName.Text = _currentAction.Name;
                // Load action settings or no settings
                _pluginInfo.Plugin.Deserialize(_currentAction.ActionSettings);
            }
            else
            {
                TxtActionName.Text = GetNextActionName(_pluginInfo.Plugin.Name);
                _pluginInfo.Plugin.Deserialize("");
            }
            // Does the plugin have a graphical interface
            if (_pluginInfo.Plugin.GUI != null)
                // Show plugins graphical interface
                ShowSettings(_pluginInfo);
            else
                // There is no interface for this plugin, hide settings but leave action name input box
                HideSettings();
        }

        private string GetNextActionName(string name, int i = 1)
        {
            var actionName = i == 1 ? name : String.Format("{0}({1})", name, i);
            var selectedItem = cmbExistingApplication.SelectedItem;
            if (selectedItem != null && (ExistingApplicationRadioButton.IsChecked.Value && ((IApplication)selectedItem).Actions.Exists(a => a.Name.Equals(actionName))))
                return GetNextActionName(name, ++i);
            return actionName;
        }

        private void ShowSettings(IPluginInfo pluginInfo)
        {
            var PluginGUI = pluginInfo.Plugin.GUI;
            if (PluginGUI.Parent != null)
            {
                SettingsContent.Content = null;
            }
            // Add settings to settings panel
            SettingsContent.Content = PluginGUI;

            SettingsContent.Height = PluginGUI.Height;
            SettingsContent.Visibility = Visibility.Visible;
        }
        private void HideSettings()
        {
            SettingsContent.Height = 0;
            SettingsContent.Visibility = Visibility.Collapsed;
        }

        private bool IsPluginMatch(IAction action, string PluginClass, string PluginFilename)
        {
            return (action != null && action.PluginClass == PluginClass && action.PluginFilename == PluginFilename);
        }

        private void RunningApplicationsPopup_Opened(object sender, EventArgs e)
        {
            RunningApplicationsPopup.PlacementRectangle = SystemParameters.MenuDropAlignment ? new Rect(Width, 0, 0, 0) : new Rect(Width / 2, 0, 0, 0);
            RefreshApplications();
        }




















    }
    // Converter
    public class SelectedItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isExisting = values != null && (bool)values[0];
            IApplication existingApplication = values[1] as IApplication;
            ApplicationListViewItem applicationListViewItem = values[2] as ApplicationListViewItem;
            if (!isExisting)
                return applicationListViewItem == null
                    ? Binding.DoNothing
                    : applicationListViewItem.WindowTitle;
            return existingApplication != null ? existingApplication.Name : Binding.DoNothing;
        }
        // 因为是只从数据源到目标的意向Binding，所以，这个函数永远也不会被调到
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing, Binding.DoNothing };
        }
    }
    public class ListviewItem2TextBoxConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            if (values[0] == null || values[1] == null) return Binding.DoNothing;
            var matchUsing = (MatchUsing)values[0];
            ApplicationListViewItem applicationListViewItem = values[1] as ApplicationListViewItem;
            switch (matchUsing)
            {
                case MatchUsing.WindowClass:
                    return applicationListViewItem.WindowClass;

                case MatchUsing.WindowTitle:
                    return applicationListViewItem.WindowTitle;

                case MatchUsing.ExecutableFilename:
                    return applicationListViewItem.WindowFilename;
            }
            return Binding.DoNothing;
        }
        // 因为是只从数据源到目标的意向Binding，所以，这个函数永远也不会被调到
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
    [ValueConversion(typeof(MatchUsing), typeof(string))]
    public class MatchUsingToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MatchUsing mu = (MatchUsing)value;
            switch (mu)
            {
                case MatchUsing.All:
                    return LocalizationProvider.Instance.GetTextValue("Common.AllApplications");
                case MatchUsing.ExecutableFilename:
                    return LocalizationProvider.Instance.GetTextValue("Common.FileName");
                case MatchUsing.WindowClass:
                    return LocalizationProvider.Instance.GetTextValue("Common.WindowClass");
                case MatchUsing.WindowTitle:
                    return LocalizationProvider.Instance.GetTextValue("Common.WindowTitle");
                default: return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class InterceptTouchInputBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var userApplication = values[0] as UserApplication;
            bool existingApp = (bool)values[1];
            if (userApplication != null && existingApp)
            {
                return userApplication.InterceptTouchInput;
            }
            return false;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

    public class AllowSingleBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var userApplication = values[0] as UserApplication;
            bool existingApp = (bool)values[1];
            if (userApplication != null && existingApp)
            {
                return userApplication.AllowSingleStroke;
            }
            return false;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class Bool2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    [ValueConversion(typeof(MatchUsing), typeof(Visibility))]
    public class MatchUsing2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return (MatchUsing)value == MatchUsing.ExecutableFilename ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InterceptTouchInputCheckBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value && AppConfig.UiAccess;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
