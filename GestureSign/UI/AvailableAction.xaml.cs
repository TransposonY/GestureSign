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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.Common.Drawing;
using MahApps.Metro.Controls.Dialogs;


namespace GestureSign.UI
{
    /// <summary>
    /// AvailableAction.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableAction : UserControl
    {
        // public static event EventHandler StartCapture;
        public AvailableAction()
        {
            InitializeComponent();

            var actionsSourceView = new ListCollectionView(ActionInfos);//创建数据源的视图
            var actionsGroupDesctrption = new PropertyGroupDescription("ApplicationName");//设置分组列
            actionsSourceView.GroupDescriptions.Add(actionsGroupDesctrption);//在图中添加分组
            actionsSourceView.SortDescriptions.Add(new SortDescription("ActionName", ListSortDirection.Ascending));
            lstAvailableActions.ItemsSource = actionsSourceView;//绑定数据源

            var applicationSourceView = new ListCollectionView(_applications);
            var applicationGroupDesctrption = new PropertyGroupDescription("Group");//设置分组列
            applicationSourceView.GroupDescriptions.Add(applicationGroupDesctrption);//在图中添加分组
            lstAvailableApplication.ItemsSource = applicationSourceView;

            ApplicationDialog.ActionsChanged += (o, e) =>
            {
                if (lstAvailableApplication.SelectedItem == e.Application)
                {
                    ActionInfos.Remove(ActionInfos.FirstOrDefault(ai => ai.ActionName.Equals(e.Action.Name, StringComparison.Ordinal)));
                    RefreshActions(false);
                }
                else
                {
                    BindApplications();
                    _selecteNewdItem = true;
                    lstAvailableApplication.SelectedItem = e.Application;
                }
            };
            AvailableGestures.GestureChanged += (o, e) => { RefreshActions(true); };
            GestureDefinition.GesturesChanged += (o, e) => { RefreshActions(true); };
            CustomApplicationsFlyout.ApplicationChanged += (o, e) => { BindApplications(); lstAvailableApplication.SelectedItem = e.Application; };

            if (ApplicationManager.Instance.FinishedLoading) { BindApplications(); }
            ApplicationManager.Instance.OnLoadApplicationsCompleted += (o, e) => { this.Dispatcher.Invoke(BindApplications); };
        }


        readonly Size sizThumbSize = new Size(65, 65);
        ObservableCollection<ActionInfo> ActionInfos = new ObservableCollection<ActionInfo>();
        private ObservableCollection<IApplication> _applications = new ObservableCollection<IApplication>();
        public static event ApplicationChangedEventHandler ShowEditApplicationFlyout;
        private Task<ActionInfo> _task;
        CancellationTokenSource _cancelTokenSource;
        private bool _selecteNewdItem;

        public class ActionInfo : INotifyPropertyChanged
        {

            public ActionInfo(string actionName, string applicationName, string description, ImageSource gestureThumbnail, string gestureName, bool isEnabled)
            {
                IsEnabled = isEnabled;
                GestureThumbnail = gestureThumbnail;
                ApplicationName = applicationName;
                ActionName = actionName;
                Description = description;
                GestureName = gestureName;
            }
            private bool isEnabled;
            public bool IsEnabled
            {
                get
                {
                    return isEnabled;
                }

                set { SetProperty(ref isEnabled, value); }
            }
            public string GestureName { get; set; }
            public ImageSource GestureThumbnail { get; set; }
            public string ApplicationName { get; set; }
            public string ActionName { get; set; }

            public string Description { get; set; }


            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
            {
                if (object.Equals(storage, value)) return;

                storage = value;
                this.OnPropertyChanged(propertyName);
            }
        }
        private async void cmdEditAction_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                var mySettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    // NegativeButtonText = "Go away!",
                    // FirstAuxiliaryButtonText = "Cancel",
                    ColorScheme = MetroDialogColorScheme.Accented //: MetroDialogColorScheme.Theme
                };

                MessageDialogResult result = await UIHelper.GetParentWindow(this).ShowMessageAsync("请选择", "编辑前需要先选择一项动作 ", MessageDialogStyle.Affirmative, mySettings);
                // MessageBox.Show("You must select an item before editing", "Please Select an Item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Get first item selected, associated action, and selected application
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            IAction selectedAction = null;
            IApplication selectedApplication = null;
            string selectedGesture = null;

            // Store selected item group header for later use
            string strApplicationHeader = selectedItem.ApplicationName;

            selectedApplication = lstAvailableApplication.SelectedItem as IApplication;

            if (selectedApplication == null)
                // Select action from global application list
                selectedAction = ApplicationManager.Instance.GetGlobalApplication().Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            else
                // Select action from selected application list
                selectedAction = selectedApplication.Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            if (selectedAction == null) return;
            // Get currently assigned gesture
            selectedGesture = selectedAction.GestureName;

            // Set current application, current action, and current gestures
            ApplicationManager.Instance.CurrentApplication = selectedApplication;
            GestureManager.Instance.GestureName = selectedGesture;

            ApplicationDialog applicationDialog = new ApplicationDialog(this, selectedAction, selectedApplication);
            applicationDialog.ShowDialog();
        }

        private async void cmdDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                await UIHelper.GetParentWindow(this).ShowMessageAsync("未选择项目", "删除前需要先选择一项 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                // MessageBox.Show("Please select and item before trying to delete.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Confirm user really wants to delete selected items
            if (await UIHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定要删除这个动作吗？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
            {
                AffirmativeButtonText = "确定",
                NegativeButtonText = "取消",
                ColorScheme = MetroDialogColorScheme.Accented
            }) != MessageDialogResult.Affirmative) return;


            // Loop through selected actions
            for (int i = lstAvailableActions.SelectedItems.Count - 1; i >= 0; i--)
            {
                // Grab selected item
                ActionInfo selectedAction = lstAvailableActions.SelectedItems[i] as ActionInfo;

                // Get the name of the action
                string strActionName = selectedAction.ActionName;

                IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;

                selectedApp.RemoveAction(selectedApp.Actions.FirstOrDefault(a => a.Name.Equals(strActionName, StringComparison.Ordinal)));

            }
            RefreshActions(false);
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableRelevantButtons();
        }
        //如果有全选需求，再分别选择：界面+保存的数据
        private void ActionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo actionInfo = Common.UI.WindowsHelper.GetParentDependencyObject<ListBoxItem>(sender as CheckBox).Content as ActionInfo;
            if (actionInfo == null) return;
            ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, actionInfo.ApplicationName).IsEnabled = (sender as CheckBox).IsChecked.Value;
            ApplicationManager.Instance.SaveApplications();
        }





        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (GestureManager.Instance.Gestures.Length == 0)
            {
                UIHelper.GetParentWindow(this).ShowMessageAsync("无可用手势", "添加动作前需要先添加至少一项手势 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                return;
            }
            ApplicationDialog applicationDialog = new ApplicationDialog(this, lstAvailableApplication.SelectedItem as IApplication);
            applicationDialog.Show();
        }



        private void BindApplications()
        {
            CopyActionMenuItem.Items.Clear();
            _applications.Clear();

            // Add global actions to global applications group
            var userApplications =
                ApplicationManager.Instance.Applications.Where(app => (app is UserApplication)).OrderBy(app => app.Name);
            var globalApplication = ApplicationManager.Instance.GetAllGlobalApplication();

            foreach (var app in globalApplication.Union(userApplications))
            {
                _applications.Add(app);

                MenuItem menuItem = new MenuItem() { Header = app.Name };
                menuItem.Click += CopyActionMenuItem_Click;
                CopyActionMenuItem.Items.Add(menuItem);
            }
        }

        private void RefreshActions(bool refreshAll)
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null) return;
            if (refreshAll)
            {
                if (_task != null && _task.Status.HasFlag(TaskStatus.Running))
                {
                    _cancelTokenSource.Cancel();
                    _task.Wait(1000);
                }
                ActionInfos.Clear();
                AddActionsToGroup(selectedApplication.Name, selectedApplication.Actions);
            }
            else
            {
                _selecteNewdItem = true;
                var newApp =
                    selectedApplication.Actions.Where(
                        a => !ActionInfos.Any
                            (ai => ai.ActionName.Equals(a.Name, StringComparison.Ordinal) && ai.ApplicationName.Equals(selectedApplication.Name))).ToList();
                var deletedApp =
                    ActionInfos.Where(
                        ai =>
                            !selectedApplication.Actions.Any(a => a.Name.Equals(ai.ActionName, StringComparison.Ordinal)) || !ai.ApplicationName.Equals(selectedApplication.Name, StringComparison.Ordinal))
                        .ToList();

                foreach (ActionInfo ai in deletedApp)
                    ActionInfos.Remove(ai);
                AddActionsToGroup(selectedApplication.Name, newApp);
            }
        }
        private void AddActionsToGroup(string applicationName, List<IAction> actions)
        {

            string description;
            DrawingImage Thumb = null;
            string gestureName;
            string pluginName;
            var brush = Application.Current.Resources["HighlightBrush"] as Brush ?? Brushes.RoyalBlue;
            // Loop through each global action  
            _cancelTokenSource = new CancellationTokenSource();

            _task = new Task<ActionInfo>(() =>
            {
                Thread.Sleep(100);
                ActionInfo actionInfo = null;
                foreach (Applications.Action currentAction in actions)
                {
                    // Ensure this action has a plugin
                    if (PluginManager.Instance.PluginExists(currentAction.PluginClass, currentAction.PluginFilename))
                    {

                        // Get plugin for this action
                        IPluginInfo pluginInfo =
                            PluginManager.Instance.FindPluginByClassAndFilename(currentAction.PluginClass,
                                currentAction.PluginFilename);

                        // Feed settings to plugin
                        if (!pluginInfo.Plugin.Deserialize(currentAction.ActionSettings))
                            currentAction.ActionSettings = pluginInfo.Plugin.Serialize();

                        pluginName = pluginInfo.Plugin.Name;
                        description = pluginInfo.Plugin.Description;
                    }
                    else
                    {
                        pluginName = String.Empty;
                        description = "无关联动作";
                    }
                    // Get handle of action gesture
                    IGesture actionGesture = GestureManager.Instance.GetNewestGestureSample(currentAction.GestureName);

                    if (actionGesture == null)
                    {
                        Thumb = null;
                        gestureName = String.Empty;
                    }
                    else
                    {
                        Thumb = GestureImage.CreateImage(actionGesture.Points, sizThumbSize, brush);
                        gestureName = actionGesture.Name;
                    }
                    if (_cancelTokenSource.IsCancellationRequested) break;
                    lstAvailableApplication.Dispatcher.Invoke(() =>
                    {

                        actionInfo = new ActionInfo(
                            !String.IsNullOrEmpty(currentAction.Name) ? currentAction.Name : pluginName,
                            applicationName,
                            description,
                            Thumb,
                            gestureName,
                            currentAction.IsEnabled);
                        ActionInfos.Add(actionInfo);
                    });
                }
                return actionInfo;
            }, _cancelTokenSource.Token);
            _task.ContinueWith((t) =>
            {
                ActionInfo ai = t.Result;
                if (ai != null && _selecteNewdItem)
                    lstAvailableActions.Dispatcher.Invoke(() =>
                    {
                        _selecteNewdItem = false;
                        lstAvailableActions.SelectedItem = ai;
                        lstAvailableActions.UpdateLayout();
                        lstAvailableActions.ScrollIntoView(ai);
                        EnableRelevantButtons();
                    });

            });
            _task.Start();
        }


        private void EnableRelevantButtons()
        {
            cmdEdit.IsEnabled = (lstAvailableActions.SelectedItems.Count == 1);
            cmdDelete.IsEnabled = (lstAvailableActions.SelectedItems.Count > 0);
        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            ActionInfo ai = (sender as ListBoxItem).Content as ActionInfo;
            if (ai != null)
            {
                // Getting the ContentPresenter of myListBoxItem
                ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(sender as ListBoxItem);
                if (myContentPresenter == null) return;
                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                // DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                ComboBox comboBox = (myContentPresenter.ContentTemplate.FindName("availableGesturesComboBox", myContentPresenter)) as ComboBox;
                comboBox.Visibility = Visibility.Visible;
                ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Visibility = Visibility.Collapsed;

                Binding bind = new Binding();
                bind.Source = ((GestureSign.Common.UI.WindowsHelper.GetParentDependencyObject<TabControl>(this)).FindName("availableGestures") as AvailableGestures).lstAvailableGestures;
                bind.Mode = BindingMode.OneWay;
                bind.Path = new PropertyPath("Items");
                comboBox.SetBinding(ComboBox.ItemsSourceProperty, bind);

                foreach (GestureItem item in comboBox.Items)
                {
                    if (item.Name == ai.GestureName)
                        comboBox.SelectedIndex = comboBox.Items.IndexOf(item);
                }
            }
        }
        private void ListBoxItem_Unselected(object sender, RoutedEventArgs e)
        {
            if ((sender as ListBoxItem).Content is ActionInfo)
            {
                // Getting the ContentPresenter of myListBoxItem
                ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(sender as ListBoxItem);
                if (myContentPresenter.ContentTemplate == null) return;
                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                // DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                ((myContentPresenter.ContentTemplate.FindName("availableGesturesComboBox", myContentPresenter)) as ComboBox).Visibility = Visibility.Collapsed;
                ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Visibility = Visibility.Visible;
            }
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void availableGesturesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            ContentPresenter myContentPresenter = Common.UI.WindowsHelper.GetParentDependencyObject<ContentPresenter>(sender as ComboBox);
            ActionInfo actionInfo = Common.UI.WindowsHelper.GetParentDependencyObject<ListBoxItem>(sender as ComboBox).Content as ActionInfo;
            IApplication app = lstAvailableApplication.SelectedItem as IApplication;
            if (actionInfo != null && ((GestureItem)e.AddedItems[0]).Name != actionInfo.GestureName)
            {
                if (app != null)
                {
                    IAction action = app.Actions.FirstOrDefault(a => a.Name.Equals(actionInfo.ActionName));
                    actionInfo.GestureName = action.GestureName = ((GestureItem)e.AddedItems[0]).Name;

                    ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Source = ((sender as ComboBox).SelectedItem as GestureItem).Image;
                    ApplicationManager.Instance.SaveApplications();
                }
            }
        }

        private void CopyActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            var targetApplication = ApplicationManager.Instance.Applications.Find(
                   a => !(a is IgnoredApplication) && a.Name == menuItem.Header.ToString().Trim());

            if (targetApplication.Actions.Exists(a => a.Name == selectedItem.ActionName))
            {
                UIHelper.GetParentWindow(this).ShowMessageAsync("此动作已存在", String.Format("在 {0} 中已经存在 {1} 动作", menuItem.Header, selectedItem.ActionName),
                    MessageDialogStyle.Affirmative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "确定",
                        ColorScheme = MetroDialogColorScheme.Accented
                    });
                return;
            }
            IAction selectedAction = ApplicationManager.Instance.GetAnyDefinedAction(selectedItem.ActionName, selectedItem.ApplicationName);
            if (selectedAction == null) return;
            Applications.Action newAction = new Applications.Action()
            {
                ActionSettings = selectedAction.ActionSettings,
                GestureName = selectedAction.GestureName,
                IsEnabled = selectedAction.IsEnabled,
                Name = selectedAction.Name,
                PluginClass = selectedAction.PluginClass,
                PluginFilename = selectedAction.PluginFilename
            };
            targetApplication.AddAction(selectedAction);

            if (targetApplication != lstAvailableApplication.SelectedItem)
            {
                _selecteNewdItem = true;
                lstAvailableApplication.SelectedItem = targetApplication;
            }
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog() { Filter = "动作文件|*.json", Title = "导入动作定义文件", CheckFileExists = true };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                List<IApplication> newApps = Common.Configuration.FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(IgnoredApplication), typeof(Applications.Action) }, false);

                if (newApps != null)
                {
                    foreach (IApplication newApp in newApps)
                    {
                        if (newApp is IgnoredApplication) continue;
                        if (ApplicationManager.Instance.ApplicationExists(newApp.Name))
                        {
                            var existingApp = ApplicationManager.Instance.Applications.Find(a => a.Name == newApp.Name);
                            foreach (IAction newAction in newApp.Actions)
                            {
                                if (existingApp.Actions.Exists(action => action.Name.Equals(newAction.Name)))
                                {
                                    var result = MessageBox.Show(String.Format("在 \"{0}\" 中已经存在 \"{1}\" 动作，是否覆盖？", existingApp.Name, newAction.Name), "已存在同名动作", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        existingApp.Actions.RemoveAll(ac => ac.Name.Equals(newAction.Name));
                                        existingApp.AddAction(newAction);
                                        addcount++;
                                    }
                                    else if (result == MessageBoxResult.Cancel) goto End;
                                }
                                else
                                {
                                    existingApp.AddAction(newAction);
                                    addcount++;
                                }
                            }
                        }
                        else
                        {
                            ApplicationManager.Instance.AddApplication(newApp);
                        }
                    }
                }
            End:
                if (addcount != 0)
                {
                    ApplicationManager.Instance.SaveApplications();
                    BindApplications();
                    lstAvailableApplication.SelectedIndex = 0;
                }
                MessageBox.Show(String.Format("已添加 {0} 个动作", addcount), "导入完成");
            }
        }

        private void ExportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog() { Filter = "动作文件|*.json", Title = "导出动作定义文件", AddExtension = true, DefaultExt = "json", ValidateNames = true };
            if (sfdApplications.ShowDialog().Value)
            {
                Common.Configuration.FileManager.SaveObject<List<IApplication>>(ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApplication)).ToList(), sfdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(IgnoredApplication), typeof(Applications.Action) });
            }
        }

        private void ActionContextMenu_Opening(object sender, RoutedEventArgs e)
        {
            CopyActionMenuItem.IsEnabled = lstAvailableActions.SelectedIndex != -1;
        }
        private void lstAvailableApplication_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            UserApplication userApplication = lstAvailableApplication.SelectedItem as UserApplication;
            if (userApplication == null)
            {
                InterceptTouchInputMenuItem.IsChecked =
                    InterceptTouchInputMenuItem.IsEnabled =
                    AllowSingleMenuItem.IsChecked =
                    AllowSingleMenuItem.IsEnabled = false;
            }
            else
            {
                AllowSingleMenuItem.IsEnabled = InterceptTouchInputMenuItem.IsEnabled = true;
                InterceptTouchInputMenuItem.IsChecked = userApplication.InterceptTouchInput;
                AllowSingleMenuItem.IsChecked = userApplication.AllowSingleStroke;
            }
        }

        private void InterceptTouchInputMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UserApplication selectedItem = lstAvailableApplication.SelectedItem as UserApplication;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            selectedItem.InterceptTouchInput = menuItem.IsChecked;

            ApplicationManager.Instance.SaveApplications();

        }
        private void AllowSingleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var userApplication = lstAvailableApplication.SelectedItem as UserApplication;
            if (userApplication != null)
            {
                userApplication.AllowSingleStroke = menuItem.IsChecked;
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void AllActionsCheckBoxs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var checkbox = ((CheckBox)sender);
                dynamic dc = checkbox.DataContext;

                ApplicationManager.Instance.Applications.Find(app => app.Name.Equals(dc.Name)).Actions.ForEach(a => a.IsEnabled = checkbox.IsChecked.Value);
                ApplicationManager.Instance.SaveApplications();

                foreach (ActionInfo ai in ActionInfos.Where(a => a.ApplicationName.Equals(dc.Name)))
                {
                    ai.IsEnabled = checkbox.IsChecked.Value;
                }
            }
            catch { }
            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            RefreshActions(true);
            EditAppButton.IsEnabled = lstAvailableApplication.SelectedItem is UserApplication;
        }

        private void EditAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShowEditApplicationFlyout != null && lstAvailableApplication.SelectedItem != null)
                ShowEditApplicationFlyout(this,
                    new ApplicationChangedEventArgs(lstAvailableApplication.SelectedItem as IApplication));
        }

        private async void DeleteAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (await UIHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定删除该程序吗，控制该程序的相关动作也将一并删除 ",
              MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
              {
                  AffirmativeButtonText = "确定",
                  NegativeButtonText = "取消",
                  ColorScheme = MetroDialogColorScheme.Accented
              }) == MessageDialogResult.Affirmative)
            {
                ApplicationManager.Instance.RemoveApplication((IApplication)lstAvailableApplication.SelectedItem);

                BindApplications();
                lstAvailableApplication.SelectedIndex = 0;
                ApplicationManager.Instance.SaveApplications();
            }
        }


    }
    [ValueConversion(typeof(IApplication), typeof(string))]
    public class HeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var app = value as IApplication;
            if (app != null)
            {
                return String.Format("{0}  ( {1}个动作 )", app.Name, app.Actions.Count);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    public class ApplicationListHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = values[0] as string;
            int count = (int)values[1];
            return String.IsNullOrEmpty(name) ? String.Format("未分组 {0}程序", count) : String.Format("{0} {1}程序", name, count);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }
}
