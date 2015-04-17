using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using GestureSign.Common.Applications;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;
using System.Windows.Interop;
using System.Diagnostics;
using System.Collections.ObjectModel;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using GestureSign.Common.Gestures;
using GestureSign.Common.Drawing;

namespace GestureSign.UI
{
    /// <summary>
    /// ApplicationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ApplicationDialog : MetroWindow
    {
        public ApplicationDialog()
        {
            InitializeComponent();
        }

        //Add action by new gesture
        public ApplicationDialog(string newGestureName)
            : this()
        {
            gestureName = newGestureName;
        }
        //Add action by existing gesture
        public ApplicationDialog(AvailableAction source, IApplication selectedApplication)
            : this()
        {
            _selectedApplication = selectedApplication;
            _availableAction = source;
        }
        //Edit action
        public ApplicationDialog(AvailableAction source, IAction selectedAction, IApplication selectedApplication)
            : this(source, selectedApplication)
        {
            this.Title = "编辑动作";
            _CurrentAction = selectedAction;
        }

        #region Private Instance Fields

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        private readonly IApplication _selectedApplication;
        IApplication _newApplication;
        readonly AvailableAction _availableAction;
        // Create variable to hold current selected plugin
        IPluginInfo _PluginInfo = null;
        IAction _CurrentAction = null;
        string gestureName;
        #endregion

        #region Public Instance Properties

        public static event EventHandler ActionsChanged;
        #endregion


        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            BindGroupComboBox();
            BindExistingApplications();
            BindExistingGestures();

            this.chCrosshair.CrosshairDragging += new EventHandler<MouseEventArgs>(chCrosshair_CrosshairDragging);
            BindPlugins();
            if (_CurrentAction != null)
            {
                //cmbExistingApplication.SelectedItem = ApplicationManager.Instance.CurrentApplication;
                foreach (object comboItem in cmbPlugins.Items)
                {
                    IPluginInfo pluginInfo = (IPluginInfo)comboItem;

                    if (pluginInfo.Class == _CurrentAction.PluginClass && pluginInfo.Filename == _CurrentAction.PluginFilename)
                    {
                        cmbPlugins.SelectedIndex = cmbPlugins.Items.IndexOf(comboItem);
                        return;
                    }
                }
            }

            GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("DisableTouchCapture", "GestureSignDaemon");
        }

        protected void chCrosshair_CrosshairDragging(object sender, MouseEventArgs e)
        {
            System.Drawing.Point cursorPosition;
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
                    if (ActionsChanged != null)
                        ActionsChanged(this, new EventArgs());
                    this.Close();
                }
            }
        }

        private void cmdBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdExecutable = new Microsoft.Win32.OpenFileDialog() { Filter = "可执行文件|*.exe" };
            if (ValidateFilepath(txtMatchString.Text.Trim()))
            {
                ofdExecutable.InitialDirectory = System.IO.Path.GetDirectoryName(txtMatchString.Text);
                ofdExecutable.FileName = System.IO.Path.GetFileName(txtMatchString.Text);
            }
            if (ofdExecutable.ShowDialog().Value)
            {
                this.txtApplicationName.Text = txtMatchString.Text = System.IO.Path.GetFileName(ofdExecutable.FileName);

            }
        }

        private async void MetroWindow_Closed(object sender, EventArgs e)
        {
            // User canceled dialog, re-enable gestures and hide form

            await GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("EnableTouchCapture", "GestureSignDaemon");
        }

        private void cmbPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cmbPlugins.SelectedItem == null) return;
            LoadPlugin((IPluginInfo)cmbPlugins.SelectedItem);
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }




        #endregion


        #region Private Instance Methods

        private bool SaveApplication()
        {

            if (ExistingApplicationRadioButton.IsChecked.Value)
            {
                if (_CurrentAction != null && cmbExistingApplication.SelectedItem as IApplication != ApplicationManager.Instance.CurrentApplication)
                {
                    if (((IApplication)cmbExistingApplication.SelectedItem).Actions.Any(a => a.Name.Equals(TxtActionName.Text.Trim())))
                    {
                        return ShowErrorMessage("此动作已存在", String.Format("动作 “{0}”已经定义给 {1}", TxtActionName.Text.Trim(), ((IApplication)cmbExistingApplication.SelectedItem).Name));
                    }
                    ApplicationManager.Instance.CurrentApplication.RemoveAction(_CurrentAction);
                    ((IApplication)cmbExistingApplication.SelectedItem).AddAction(_CurrentAction);
                }
                ApplicationManager.Instance.CurrentApplication = (IApplication)cmbExistingApplication.SelectedItem;
                return true;
            }
            else if (NewApplicationRadioButton.IsChecked.Value)
            {
                _newApplication = new UserApplication
                {
                    InterceptTouchInput = this.InterceptTouchInputCheckBox.IsChecked.Value,
                    AllowSingleStroke = this.AllowSingleCheckBox.IsChecked.Value,
                    Name = txtApplicationName.Text.Trim(),
                    Group = GroupComboBox.Text.Trim()
                };
                // Store application name
                // Make sure we have a valid application name
                if (_newApplication.Name == "")
                    return ShowErrorMessage("无程序名", "请定义程序名");

                if (_CurrentAction == null && ApplicationManager.Instance.ApplicationExists(_newApplication.Name))
                    return ShowErrorMessage("该程序名已经存在", "程序名称已经存在，请输入其他名字");

                string matchString = txtMatchString.Text.Trim();
                // Make sure the user entered a match string
                if (String.IsNullOrEmpty(matchString))
                    return ShowErrorMessage("无匹配字段", "请先定义一个匹配字段");
                try
                {
                    if (this.chkRegex.IsChecked.Value)
                        System.Text.RegularExpressions.Regex.IsMatch(matchString, "teststring");
                }
                catch
                {
                    return ShowErrorMessage("格式错误", "正则表达式格式错误，请重新检查");
                }

                _newApplication.MatchString = matchString;
                _newApplication.MatchUsing = matchUsingRadio.MatchUsing;
                _newApplication.IsRegEx = chkRegex.IsChecked.Value;


                ApplicationManager.Instance.CurrentApplication = _newApplication;
                return true;
            }
            return false;
        }


        private bool ShowErrorMessage(string title, string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = "错误： " + title;
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
                List<GestureItem> GestureItems = new List<GestureItem>(results.Count());
                var brush = Application.Current.Resources["HighlightBrush"] as Brush ?? Brushes.RoyalBlue;
                foreach (IGesture gesture in results)
                {
                    // Create new listviewitem to represent gestures, create a thumbnail of the latest version of each gesture
                    // and add it to image list, then to the output list      gestureName
                    GestureItem newItem = new GestureItem()
                    {
                        Image = GestureImage.CreateImage(gesture.Points, new Size(65, 65), brush),
                        Name = gesture.Name
                    };
                    GestureItems.Add(newItem);
                }
                bind.Source = GestureItems;
            }
            else
            {
                bind.Source = ((GestureSign.Common.UI.WindowsHelper.GetParentDependencyObject<TabControl>(_availableAction)).FindName("availableGestures") as AvailableGestures).lstAvailableGestures;
                bind.Path = new PropertyPath("Items");
            }
            bind.Mode = BindingMode.OneWay;
            this.availableGesturesComboBox.SetBinding(ComboBox.ItemsSourceProperty, bind);
            if (_CurrentAction != null)
            {
                gestureName = _CurrentAction.GestureName;
            }
            if (gestureName != null)
            {
                foreach (GestureItem item in availableGesturesComboBox.Items)
                {
                    if (item.Name == gestureName)
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
                pathname = System.IO.Path.GetPathRoot(path);
                filename = System.IO.Path.GetFileName(path);
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
            if (pathname.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                return false;

            if (filename.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                return false;

            return true;
        }

        #endregion






        public void RefreshApplications()
        {
            this.alvRunningApplications.Items.Clear();
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(GetValidWindows));
            //  this.lstRunningApplications.Items.Clear();
        }
        private void GetValidWindows(object s)
        {
            // Get valid running windows
            var Windows = SystemWindow.AllToplevelWindows.Where
                     (
                         w => w.Visible &&	// Must be a visible windows
                         w.Title != "" &&	// Must have a window title
                         IsProcessAccessible(w.Process) &&
                        System.IO.Path.GetDirectoryName(w.Process.ProcessName) != Process.GetCurrentProcess().ProcessName &&	// Must not be a GestureSign window
                         (w.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) != WindowExStyleFlags.TOOLWINDOW	// Must not be a tool window
                     );
            //System.Threading.Thread.Sleep(500);
            foreach (SystemWindow sWind in Windows)
            {
                this.alvRunningApplications.Dispatcher.BeginInvoke(new System.Action(() =>
               {
                   ApplicationListViewItem lItem = new ApplicationListViewItem();

                   try
                   {
                       // Store identifying information
                       lItem.WindowClass = sWind.ClassName;
                       lItem.WindowTitle = sWind.Title;
                       lItem.WindowFilename = System.IO.Path.GetFileName(sWind.Process.MainModule.FileName);
                       lItem.ApplicationIcon = Imaging.CreateBitmapSourceFromHIcon(sWind.Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                       this.alvRunningApplications.Items.Add(lItem);
                   }
                   catch { return; }
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
                return ShowErrorMessage("手势未定义", "未选择手势，请先选择一个手势。");
            // Check if we're creating a new action
            bool _IsNew = false;
            if (_CurrentAction == null)
            {
                _CurrentAction = new GestureSign.Applications.Action();
                _IsNew = true;
            }
            string newActionName = TxtActionName.Text.Trim();

            if (String.IsNullOrEmpty(newActionName))
                return ShowErrorMessage("无动作名", "未填写动作名称，请先为该动作命名。");

            if (_IsNew)
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(newActionName))
                    {
                        _CurrentAction = null;
                        return ShowErrorMessage("此动作已存在", String.Format("在全局动作中已存在 “{0}” ", newActionName));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.CurrentApplication.Actions.Any(a => a.Name.Equals(newActionName)))
                    {
                        _CurrentAction = null;
                        return ShowErrorMessage("此动作已存在", String.Format("动作 “{0}” 已经定义给 {1}", newActionName, ApplicationManager.Instance.CurrentApplication.Name));
                    }
                }
            }
            else
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(newActionName) && newActionName != _CurrentAction.Name)
                    {
                        return ShowErrorMessage("此动作已存在", String.Format("在全局动作中已存在 “{0}” ", newActionName));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.CurrentApplication.Actions.Any(a => a.Name.Equals(newActionName)) && newActionName != _CurrentAction.Name)
                    {
                        return ShowErrorMessage("此动作已存在", String.Format("动作 “{0}” 已经定义给 {1}", newActionName, ApplicationManager.Instance.CurrentApplication.Name));

                    }
                }
            }

            // Store new values
            _CurrentAction.GestureName = (availableGesturesComboBox.SelectedItem as GestureItem).Name;
            _CurrentAction.Name = newActionName;
            _CurrentAction.PluginClass = _PluginInfo.Class;
            _CurrentAction.PluginFilename = _PluginInfo.Filename;
            _CurrentAction.ActionSettings = _PluginInfo.Plugin.Serialize();
            _CurrentAction.IsEnabled = true;
            // Check if we already have this action somewhere
            if (_IsNew)
            {
                if (_newApplication != null || _newApplication is UserApplication)
                    ApplicationManager.Instance.AddApplication(_newApplication);
                // Save new action to specific application
                ApplicationManager.Instance.CurrentApplication.AddAction(_CurrentAction);
            }
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();

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
            _PluginInfo = selectedPlugin;

            // Set action name
            if (IsPluginMatch(_CurrentAction, selectedPlugin.Class, selectedPlugin.Filename))
            {
                TxtActionName.Text = _CurrentAction.Name;
                // Load action settings or no settings
                _PluginInfo.Plugin.Deserialize(_CurrentAction.ActionSettings);
            }
            else
            {
                TxtActionName.Text = GetNextActionName(_PluginInfo.Plugin.Name);
                _PluginInfo.Plugin.Deserialize("");
            }
            // Does the plugin have a graphical interface
            if (_PluginInfo.Plugin.GUI != null)
                // Show plugins graphical interface
                ShowSettings(_PluginInfo);
            else
                // There is no interface for this plugin, hide settings but leave action name input box
                HideSettings();
        }

        private string GetNextActionName(string name, int i = 1)
        {
            var actionName = i == 1 ? name : String.Format("{0}({1})", name, i);
            var selectedItem = this.cmbExistingApplication.SelectedItem;
            if (selectedItem != null && (ExistingApplicationRadioButton.IsChecked.Value && ((IApplication)selectedItem).Actions.Exists(a => a.Name.Equals(actionName))))
                return GetNextActionName(name, ++i);
            return actionName;
        }

        private void ShowSettings(IPluginInfo PluginInfo)
        {
            var PluginGUI = PluginInfo.Plugin.GUI;
            if (PluginGUI.Parent != null)
            {
                this.SettingsContent.Content = null;
            }
            // Add settings to settings panel
            this.SettingsContent.Content = PluginGUI;

            this.SettingsContent.Height = PluginGUI.Height;
            this.SettingsContent.Visibility = Visibility.Visible;
        }
        private void HideSettings()
        {
            this.SettingsContent.Height = 0;
            this.SettingsContent.Visibility = Visibility.Collapsed;
        }

        private bool IsPluginMatch(IAction Action, string PluginClass, string PluginFilename)
        {
            return (Action != null && Action.PluginClass == PluginClass && Action.PluginFilename == PluginFilename);
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
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[3] { Binding.DoNothing, Binding.DoNothing, Binding.DoNothing };
        }
    }
    public class ListviewItem2TextBoxConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
    [ValueConversion(typeof(MatchUsing), typeof(string))]
    public class MatchUsingToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MatchUsing mu = (MatchUsing)value;
            switch (mu)
            {
                case MatchUsing.All:
                    return "所有程序";
                case MatchUsing.ExecutableFilename:
                    return "文件名";
                case MatchUsing.WindowClass:
                    return "窗口类名称";
                case MatchUsing.WindowTitle:
                    return "窗口标题";
                default: return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    [ValueConversion(typeof(IApplication), typeof(bool))]
    public class InterceptTouchInputBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var app = value as UserApplication;
                if (app != null)
                    return app.InterceptTouchInput;
                else return false;
            }
            else return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    [ValueConversion(typeof(IApplication), typeof(bool))]
    public class AllowSingleBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var app = value as UserApplication;
                if (app != null)
                    return app.AllowSingleStroke;
                else return false;
            }
            else return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class Bool2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    [ValueConversion(typeof(MatchUsing), typeof(Visibility))]
    public class MatchUsing2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                return (MatchUsing)value == MatchUsing.ExecutableFilename ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
