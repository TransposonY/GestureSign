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
        //Add action by new gesture
        public ApplicationDialog()
        {
            InitializeComponent();
        }

        public ApplicationDialog(string newGestureName)
            : this()
        {
            gestureName = newGestureName;
        }
        //Add action by existing gesture
        public ApplicationDialog(AvailableAction source)
            : this()
        {
            availableAction = source;
        }
        //Edit action
        public ApplicationDialog(AvailableAction source, IAction selectedAction)
            : this(source)
        {
            this.Title = "编辑动作";
            _CurrentAction = selectedAction;
        }

        #region Private Instance Fields

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        IApplication _SelectedApplication;
        private class ApplicationComboBoxItem
        {
            public string ApplicationName { get; set; }
            public MatchUsing MatchUsing { get; set; }
            public string MatchString { get; set; }
            public bool IsRegEx { get; set; }
        }


        private enum TabType
        {
            Existing = 0,
            Running = 1,
            Custom = 2
        }
        private TabType SelectedTab
        {
            get { return (TabType)tcApplications.SelectedIndex; }
        }

        AvailableAction availableAction;
        // Create variable to hold current selected plugin
        IPluginInfo _PluginInfo = null;
        IAction _CurrentAction = null;
        string gestureName;
        #endregion

        #region Public Instance Properties
        public MatchUsing MatchUsing { get; set; }
        public SystemWindow[] Windows { get; set; }
        public string MatchString { get; set; }


        public static event EventHandler ActionsChanged;
        #endregion


        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            BindMatchUsingComboBoxes();
            BindExistingApplications();
            BindExistingGestures();

            this.chCrosshair.CrosshairDragging += new EventHandler<MouseEventArgs>(chCrosshair_CrosshairDragging);
            BindPlugins();
            RefreshApplications();
            if (_CurrentAction != null)
            {
                cmbExistingApplication.SelectedItem = ApplicationManager.Instance.CurrentApplication;
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
            MatchUsing muCustom = ((MatchUsingComboBoxItem)cmbMatchUsingCustom.SelectedItem).MatchUsing;
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


        private void cmbMatchUsingRunning_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MatchUsing = ((MatchUsingComboBoxItem)cmbMatchUsingRunning.SelectedItem).MatchUsing;
        }

        private void cmbMatchUsingCustom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMatchUsingCustom.SelectedItem != null)
                cmdBrowse.Visibility = (((MatchUsingComboBoxItem)cmbMatchUsingCustom.SelectedItem).MatchUsing == MatchUsing.ExecutableFilename) ? Visibility.Visible : Visibility.Hidden;
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

            if (this.SelectedTab == TabType.Existing)
            {
                if (_CurrentAction != null && cmbExistingApplication.SelectedItem as IApplication != ApplicationManager.Instance.CurrentApplication)
                {
                    if (((IApplication)cmbExistingApplication.SelectedItem).Actions.Any(a => a.Name.Equals(txtActionName.Text.Trim())))
                    {
                        return ShowErrorMessage("此动作已存在", String.Format("动作 “{0}”已经定义给 {1}", txtActionName.Text.Trim(), ((IApplication)cmbExistingApplication.SelectedItem).Name));
                    }
                    ApplicationManager.Instance.CurrentApplication.RemoveAction(_CurrentAction);
                    ((IApplication)cmbExistingApplication.SelectedItem).AddAction(_CurrentAction);
                }
                ApplicationManager.Instance.CurrentApplication = (IApplication)cmbExistingApplication.SelectedItem;
                return true;
            }
            else
            {
                _SelectedApplication = new UserApplication()
                {
                    InterceptTouchInput = this.InterceptTouchInputCheckBox.IsChecked.Value,
                    AllowSingleStroke = this.AllowSingleCheckBox.IsChecked.Value
                };
                // Store application name
                _SelectedApplication.Name = txtApplicationName.Text.Trim();
                // Make sure we have a valid application name
                if (_SelectedApplication.Name == "")
                    return ShowErrorMessage("无程序名", "请定义程序名");

                if (_CurrentAction == null && ApplicationManager.Instance.ApplicationExists(_SelectedApplication.Name))
                    return ShowErrorMessage("程序名已经存在", "程序名称已经存在，请输入其他名字");


                if (SelectedTab == TabType.Running)
                {
                    // Make sure the user selected a running application
                    if (this.alvRunningApplications.SelectedIndex == -1)
                        return ShowErrorMessage("未选择程序", "请先选择一个程序");

                    ApplicationListViewItem alvi = this.alvRunningApplications.SelectedItem as ApplicationListViewItem;
                    switch (((MatchUsingComboBoxItem)this.cmbMatchUsingRunning.SelectedItem).MatchUsing)
                    {
                        case MatchUsing.WindowClass:
                            _SelectedApplication.MatchString = alvi.WindowClass;

                            break;
                        case MatchUsing.WindowTitle:
                            _SelectedApplication.MatchString = alvi.WindowTitle;

                            break;
                        case MatchUsing.ExecutableFilename:
                            _SelectedApplication.MatchString = alvi.WindowFilename;
                            break;
                    }
                    _SelectedApplication.MatchUsing = ((MatchUsingComboBoxItem)this.cmbMatchUsingRunning.SelectedItem).MatchUsing;
                    _SelectedApplication.IsRegEx = false;
                }
                else // (SelectedTab==TabType.Custom)
                {
                    // Make sure the user entered a match string
                    if (txtMatchString.Text.Trim() == "")
                        return ShowErrorMessage("无匹配字段", "请先定义一个匹配字段");
                    try
                    {
                        if (this.chkRegex.IsChecked.Value)
                            System.Text.RegularExpressions.Regex.IsMatch(txtMatchString.Text, "teststring");
                    }
                    catch
                    {
                        return ShowErrorMessage("格式错误", "正则表达式格式错误，请重新检查");
                    }

                    _SelectedApplication.MatchString = txtMatchString.Text.Trim();
                    if (cmbMatchUsingCustom != null)
                        _SelectedApplication.MatchUsing = ((MatchUsingComboBoxItem)cmbMatchUsingCustom.SelectedItem).MatchUsing;
                    _SelectedApplication.IsRegEx = chkRegex.IsChecked.Value;
                }
                if (_CurrentAction != null)
                {
                    ApplicationManager.Instance.CurrentApplication.IsRegEx = _SelectedApplication.IsRegEx;
                    ApplicationManager.Instance.CurrentApplication.MatchString = _SelectedApplication.MatchString;
                    ApplicationManager.Instance.CurrentApplication.MatchUsing = _SelectedApplication.MatchUsing;
                    ApplicationManager.Instance.CurrentApplication.Name = _SelectedApplication.Name;
                }
                else
                    ApplicationManager.Instance.CurrentApplication = _SelectedApplication;
            }
            return true;
        }


        private bool ShowErrorMessage(string title, string message)
        {
            this.ShowMessageAsync(title, message,
                MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "确定" });
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
            cmbExistingApplication.SelectedItem = allApplicationsItem;
        }
        private void BindExistingGestures()
        {

            Binding bind = new Binding();
            //no GestureItem source
            if (availableAction == null)
            {
                IEnumerable<IGesture> results = GestureManager.Instance.Gestures.OrderBy(g => g.Name);//.GroupBy(g => g.Name).Select(g => g.First().Name);
                List<GestureItem> GestureItems = new List<GestureItem>(results.Count());
                var accent = MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current);
                var brush = accent != null ? accent.Item2.Resources["HighlightBrush"] as Brush : SystemParameters.WindowGlassBrush;
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
                bind.Source = ((GestureSign.Common.UI.WindowsHelper.GetParentDependencyObject<TabControl>(availableAction)).FindName("availableGestures") as AvailableGestures).lstAvailableGestures;
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
        private void BindMatchUsingComboBoxes()
        {
            MatchUsingComboBoxItem mciWindowClass = new MatchUsingComboBoxItem() { DisplayText = "窗口类", MatchUsing = MatchUsing.WindowClass };
            MatchUsingComboBoxItem mciWindowTitle = new MatchUsingComboBoxItem() { DisplayText = "窗口标题", MatchUsing = MatchUsing.WindowTitle };
            MatchUsingComboBoxItem mciExecutableFilename = new MatchUsingComboBoxItem() { DisplayText = "文件名", MatchUsing = MatchUsing.ExecutableFilename };

            cmbMatchUsingRunning.DisplayMemberPath = cmbMatchUsingCustom.DisplayMemberPath = "DisplayText";
            cmbMatchUsingRunning.ItemsSource = (new MatchUsingComboBoxItem[] { mciExecutableFilename, mciWindowTitle, mciWindowClass });
            cmbMatchUsingCustom.ItemsSource = (new MatchUsingComboBoxItem[] { mciExecutableFilename, mciWindowTitle, mciWindowClass });

            cmbMatchUsingRunning.SelectedItem = cmbMatchUsingCustom.SelectedItem = mciExecutableFilename;
        }


        private void SelectMatchUsing(ComboBox MatchUsingList, MatchUsing Value)
        {
            MatchUsingList.SelectedItem = MatchUsingList.Items.Cast<MatchUsingComboBoxItem>().FirstOrDefault(ci => ci.MatchUsing == Value);
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
            System.Threading.Thread.Sleep(500);
            foreach (SystemWindow sWind in Windows)
            {
                this.alvRunningApplications.Dispatcher.BeginInvoke(new System.Action(() =>
               {
                   ApplicationListViewItem lItem = new ApplicationListViewItem();

                   // Todo: Add no icon found image


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

            if (_IsNew)
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(txtActionName.Text.Trim()))
                    {
                        _CurrentAction = null;
                        return ShowErrorMessage("此动作已存在", String.Format("在全局动作中已存在 “{0}” ", txtActionName.Text.Trim()));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.IsUserAction(txtActionName.Text.Trim()))
                    {
                        _CurrentAction = null;
                        return ShowErrorMessage("此动作已存在", String.Format("动作 “{0}” 已经定义给 {1}", txtActionName.Text.Trim(), ApplicationManager.Instance.CurrentApplication.Name));
                    }
                }
            }
            else
            {
                if (ApplicationManager.Instance.CurrentApplication is GlobalApplication)
                {
                    if (ApplicationManager.Instance.IsGlobalAction(txtActionName.Text.Trim()) && txtActionName.Text.Trim() != _CurrentAction.Name)
                    {
                        return ShowErrorMessage("此动作已存在", String.Format("在全局动作中已存在 “{0}” ", txtActionName.Text.Trim()));
                    }
                }
                else
                {
                    if (ApplicationManager.Instance.IsUserAction(txtActionName.Text.Trim()) && txtActionName.Text.Trim() != _CurrentAction.Name)
                    {
                        return ShowErrorMessage("此动作已存在", String.Format("动作 “{0}” 已经定义给 {1}", txtActionName.Text.Trim(), ApplicationManager.Instance.CurrentApplication.Name));

                    }
                }
            }

            // Store new values
            _CurrentAction.GestureName = (availableGesturesComboBox.SelectedItem as GestureItem).Name;
            _CurrentAction.Name = txtActionName.Text.Trim();
            _CurrentAction.PluginClass = _PluginInfo.Class;
            _CurrentAction.PluginFilename = _PluginInfo.Filename;
            _CurrentAction.ActionSettings = _PluginInfo.Plugin.Serialize();
            _CurrentAction.IsEnabled = true;
            // Check if we already have this action somewhere
            if (_IsNew)
            {
                if (_SelectedApplication != null || _SelectedApplication is UserApplication)
                    ApplicationManager.Instance.AddApplication(_SelectedApplication);
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
                txtActionName.Text = _CurrentAction.Name;
                // Load action settings or no settings
                _PluginInfo.Plugin.Deserialize(_CurrentAction.ActionSettings);
            }
            else
            {
                txtActionName.Text = _PluginInfo.Plugin.Name;
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

        private async void DelAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (await this.ShowMessageAsync("确认删除", "确定删除该程序吗，控制该程序的相关动作也将一并删除。",
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "确定", NegativeButtonText = "取消" }) == MessageDialogResult.Affirmative)
            {
                ApplicationManager.Instance.RemoveApplication((IApplication)cmbExistingApplication.SelectedItem);

                ApplicationManager.Instance.SaveApplications();
                BindExistingApplications();
            }
        }

        private void cmbExistingApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.DelAppButton.IsEnabled = !(cmbExistingApplication.SelectedItem is GlobalApplication);
        }






















    }
    public class MatchUsingComboBoxItem
    {
        public string DisplayText { get; set; }
        public MatchUsing MatchUsing { get; set; }
    }
    // Converter
    public class SelectedItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // 按对应值做决策
            //cmbExistingApplication alvRunningApplications tabControlApplications
            int index = (int)values[2];
            if (index == 0)
            {
                if (values[0] != null) return ((IApplication)values[0]).Name;
                else return DependencyProperty.UnsetValue;
            }
            else if (index == 1)
            {
                if (values[1] == null) return DependencyProperty.UnsetValue;
                else
                {
                    ApplicationListViewItem alvi = (ApplicationListViewItem)values[1];
                    return alvi.WindowTitle;
                }
            }
            return DependencyProperty.UnsetValue;
        }
        // 因为是只从数据源到目标的意向Binding，所以，这个函数永远也不会被调到
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[3] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }

    [ValueConversion(typeof(MatchUsing), typeof(int))]
    public class MatchUsingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                MatchUsing v = (MatchUsing)value;
                return (int)v;
            }
            else return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
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

    [ValueConversion(typeof(object), typeof(bool))]
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

    [ValueConversion(typeof(object), typeof(bool))]
    public class EnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                return value is UserApplication;
            }
            else return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
