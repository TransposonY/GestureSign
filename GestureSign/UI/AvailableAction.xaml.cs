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

using GestureSign.Common.Applications;
using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.Common.Drawing;

using MahApps.Metro.Controls.Dialogs;
//using MahApps.Metro.Controls;

namespace GestureSign.UI
{
    /// <summary>
    /// AvailableAction.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableAction : UserControl, IDisposable
    {
        // public static event EventHandler StartCapture;
        public AvailableAction()
        {
            InitializeComponent();
            var sourceView = new ListCollectionView(ActionInfos);//创建数据源的视图
            var groupDesctrption = new PropertyGroupDescription("ApplicationName");//设置分组列
            sourceView.GroupDescriptions.Add(groupDesctrption);//在图中添加分组
            lstAvailableActions.ItemsSource = sourceView;//绑定数据源

            ApplicationDialog.ActionsChanged += ActionDefinition_ActionsChanged;

            AvailableGestures.GestureChanged += ActionDefinition_ActionsChanged;
        }




        Size sizThumbSize = new Size(65, 65);
        ObservableCollection<ActionInfo> ActionInfos = new ObservableCollection<ActionInfo>();
        //  List<ActionInfo> ActionInfos = new List<ActionInfo>(5);

        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.

            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

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

        void ActionDefinition_ActionsChanged(object sender, EventArgs e)
        {
            BindActions();
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

                MessageDialogResult result = await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("请选择", "编辑前需要先选择一项动作 ", MessageDialogStyle.Affirmative, mySettings);
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

            if (strApplicationHeader != Applications.ApplicationManager.Instance.GetGlobalApplication().Name)
                selectedApplication = Applications.ApplicationManager.Instance.GetExistingUserApplication(strApplicationHeader);
            else
                selectedApplication = Applications.ApplicationManager.Instance.GetGlobalApplication();

            if (selectedApplication == null)
                // Select action from global application list
                selectedAction = Applications.ApplicationManager.Instance.GetGlobalApplication().Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            else
                // Select action from selected application list
                selectedAction = selectedApplication.Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);

            // Get currently assigned gesture
            selectedGesture = selectedAction.GestureName;

            // Set current application, current action, and current gestures
            Applications.ApplicationManager.Instance.CurrentApplication = selectedApplication;
            Gestures.GestureManager.Instance.GestureName = selectedGesture;

            ApplicationDialog applicationDialog = new ApplicationDialog(this, selectedAction);
            applicationDialog.ShowDialog();
            SelectAction(strApplicationHeader, selectedItem.ActionName, false);
        }

        private void SelectAction(string applicationName, string actionName, bool scrollIntoView)
        {
            foreach (ActionInfo ai in lstAvailableActions.Items)
            {
                if (ai.ApplicationName.Equals(applicationName) && ai.ActionName.Equals(actionName))
                {
                    lstAvailableActions.SelectedItem = ai;
                    if (scrollIntoView)
                    {
                        lstAvailableActions.UpdateLayout();
                        lstAvailableActions.ScrollIntoView(ai);
                    }
                    return;
                }
            }
        }

        private async void cmdDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("未选择项目", "删除前需要先选择一项 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                // MessageBox.Show("Please select and item before trying to delete.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Confirm user really wants to delete selected items
            if (await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定要删除这个动作吗？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
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

                // Get name of application
                string strApplicationName = selectedAction.ApplicationName;

                // Is this a global action or application specific
                if (strApplicationName == Applications.ApplicationManager.Instance.GetGlobalApplication().Name)
                    // Delete action from global list
                    Applications.ApplicationManager.Instance.RemoveGlobalAction(strActionName);
                else
                    // Delete action from application
                    Applications.ApplicationManager.Instance.RemoveNonGlobalAction(strActionName);

            }
            BindActions();
            // Save entire list of applications
            Applications.ApplicationManager.Instance.SaveApplications();
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
            Applications.ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, actionInfo.ApplicationName).IsEnabled = (sender as CheckBox).IsChecked.Value;
            Applications.ApplicationManager.Instance.SaveApplications();
        }

        private void AllCheckBoxs_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((CheckBox)sender).IsChecked.Value;
            foreach (ActionInfo ai in ActionInfos)
            {
                ai.IsEnabled = isChecked;
            }
        }




        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (Gestures.GestureManager.Instance.Gestures.Length == 0)
            {
                Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("无可用手势", "添加动作前需要先添加至少一项手势 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                return;
            }
            ApplicationDialog applicationDialog = new ApplicationDialog(this);
            applicationDialog.Show();
        }



        public void BindActions()
        {
            ActionInfos.Clear();
            this.CopyActionMenuItem.Items.Clear();
            //Task task = new Task(() =>
            //{
            //    this.Dispatcher.BeginInvoke(new Action(() =>
            //     {
            // Add global actions to global applications group
            AddActionsToGroup(Applications.ApplicationManager.Instance.GetGlobalApplication().Name, Applications.ApplicationManager.Instance.GetGlobalApplication().Actions.OrderBy(a => a.Name));

            // Get all applications
            IApplication[] lstApplications = Applications.ApplicationManager.Instance.GetAvailableUserApplications();

            foreach (UserApplication App in lstApplications)
            {
                // Add this applications actions to applications group
                AddActionsToGroup(App.Name, App.Actions.OrderBy(a => a.Name));
            }

            EnableRelevantButtons();
            //     }));
            //});
            //task.Start();
        }
        private void AddActionsToGroup(string ApplicationName, IEnumerable<IAction> Actions)
        {

            MenuItem menuItem = new MenuItem() { Header = ApplicationName };
            menuItem.Click += CopyActionMenuItem_Click;
            this.CopyActionMenuItem.Items.Add(menuItem);



            // Loop through each global action
            foreach (Applications.Action currentAction in Actions)
            {
                // Ensure this action has a plugin
                if (!Plugins.PluginManager.Instance.PluginExists(currentAction.PluginClass, currentAction.PluginFilename))
                    continue;

                // Get plugin for this action
                IPluginInfo pluginInfo = Plugins.PluginManager.Instance.FindPluginByClassAndFilename(currentAction.PluginClass, currentAction.PluginFilename);

                // Feed settings to plugin
                if (!pluginInfo.Plugin.Deserialize(currentAction.ActionSettings))
                    currentAction.ActionSettings = pluginInfo.Plugin.Serialize();
                // Get handle of action gesture
                IGesture actionGesture = Gestures.GestureManager.Instance.GetNewestGestureSample(currentAction.GestureName);
               
                ActionInfo ai;

                if (actionGesture == null)
                {
                    ai = new ActionInfo(
                       !String.IsNullOrEmpty(currentAction.Name) ? currentAction.Name : pluginInfo.Plugin.Name,
                       ApplicationName,
                       pluginInfo.Plugin.Description,
                       null,
                       String.Empty,
                       currentAction.IsEnabled);

                }
                else
                {
                    ai = new ActionInfo(
                      !String.IsNullOrEmpty(currentAction.Name) ? currentAction.Name : pluginInfo.Plugin.Name,
                       ApplicationName,
                       pluginInfo.Plugin.Description,
                       GestureImage.CreateImage(actionGesture.Points, sizThumbSize),
                       actionGesture.Name,
                       currentAction.IsEnabled);
                }
                ActionInfos.Add(ai);

            }
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
                // if (myContentPresenter.ContentTemplate == null) return;
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
            if (((GestureItem)e.AddedItems[0]).Name != actionInfo.GestureName)
            {
                IAction action = Applications.ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, actionInfo.ApplicationName);
                actionInfo.GestureName = action.GestureName = ((GestureItem)e.AddedItems[0]).Name;
                ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Source = ((sender as ComboBox).SelectedItem as GestureItem).Image;
                Applications.ApplicationManager.Instance.SaveApplications();
            }
        }

        private void CopyActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            var targetApplication = Applications.ApplicationManager.Instance.Applications.Find(
                   a => !(a is IgnoredApplication) && a.Name == menuItem.Header.ToString().Trim());

            if (targetApplication.Actions.Exists(a => a.Name == selectedItem.ActionName))
            {
                Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("此动作已存在", String.Format("在 {0} 中已经存在 {1} 动作", menuItem.Header, selectedItem.ActionName),
                    MessageDialogStyle.Affirmative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "确定",
                        ColorScheme = MetroDialogColorScheme.Accented
                    });
                return;
            }
            IAction selectedAction = Applications.ApplicationManager.Instance.GetAnyDefinedAction(selectedItem.ActionName, selectedItem.ApplicationName);
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

            BindActions();
            SelectAction(targetApplication.Name, newAction.Name, true);
            Applications.ApplicationManager.Instance.SaveApplications();
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog() { Filter = "动作文件|*.json", Title = "导入动作定义文件", CheckFileExists = true };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                List<IApplication> newApps = Configuration.IO.FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(IgnoredApplication), typeof(Applications.Action) }, false);
                if (newApps != null)
                    foreach (IApplication newApp in newApps)
                    {
                        if (newApp is IgnoredApplication) continue;
                        if (Applications.ApplicationManager.Instance.ApplicationExists(newApp.Name))
                        {
                            var existingApp = Applications.ApplicationManager.Instance.Applications.Find(a => a.Name == newApp.Name);
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
                            Applications.ApplicationManager.Instance.AddApplication(newApp);
                        }
                    }
            End:
                if (addcount != 0)
                {
                    Applications.ApplicationManager.Instance.SaveApplications();
                    BindActions();
                }
                MessageBox.Show(String.Format("已添加 {0} 个动作", addcount), "导入完成");
            }
        }

        private void ExportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog() { Filter = "动作文件|*.json", Title = "导出动作定义文件", AddExtension = true, DefaultExt = "json", ValidateNames = true };
            if (sfdApplications.ShowDialog().Value)
            {
                Configuration.IO.FileManager.SaveObject<List<IApplication>>(Applications.ApplicationManager.Instance.Applications.Select(app => !(app is IgnoredApplication)).ToList(), sfdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(IgnoredApplication), typeof(Applications.Action) });
            }
        }
    }
}
