using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using MahApps.Metro.Controls.Dialogs;


namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// AvailableActions.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableActions : UserControl
    {
        // public static event EventHandler StartCapture;
        public AvailableActions()
        {
            InitializeComponent();

            var actionsSourceView = new ListCollectionView(ActionInfos);//创建数据源的视图
            actionsSourceView.GroupDescriptions.Add(new PropertyGroupDescription("GestureName"));//在图中添加分组
            actionsSourceView.SortDescriptions.Add(new SortDescription("GestureName", ListSortDirection.Ascending));
            lstAvailableActions.ItemsSource = actionsSourceView;//绑定数据源

            var applicationSourceView = new ListCollectionView(_applications);
            var applicationGroupDesctrption = new PropertyGroupDescription("Group");//设置分组列
            applicationSourceView.GroupDescriptions.Add(applicationGroupDesctrption);//在图中添加分组
            lstAvailableApplication.ItemsSource = applicationSourceView;

            ActionDialog.ActionsChanged += (o, e) =>
            {
                if (lstAvailableApplication.SelectedItem == e.Application)
                {
                    var oldActionInfo =
                          ActionInfos.FirstOrDefault(ai => ai.ActionName.Equals(e.Action.Name, StringComparison.Ordinal));
                    if (oldActionInfo != null)
                    {
                        int index = ActionInfos.IndexOf(oldActionInfo);
                        var newActionInfo = Action2ActionInfo(e.Action);
                        ActionInfos[index] = newActionInfo;
                        RefreshGroup(e.Action.GestureName);
                        SelectAction(newActionInfo);
                    }
                    else RefreshActions(false);
                }
                else
                {
                    BindApplications();
                    _selecteNewestItem = true;
                    lstAvailableApplication.SelectedItem = e.Application;
                    lstAvailableApplication.ScrollIntoView(e.Application);
                }
            };
            AvailableGestures.GestureChanged += (o, e) => { RefreshActions(true); };
            GestureDefinition.GesturesChanged += (o, e) => { RefreshActions(true); };
            ApplicationDialog.UserApplicationChanged += (o, e) =>
            {
                BindApplications();
                lstAvailableApplication.SelectedItem = e.Application;
                lstAvailableApplication.ScrollIntoView(e.Application);
            };

            ApplicationManager.OnLoadApplicationsCompleted += (o, e) => { this.Dispatcher.Invoke(BindApplications); };

            if (ApplicationManager.FinishedLoading) BindApplications();
        }


        ObservableCollection<ActionInfo> ActionInfos = new ObservableCollection<ActionInfo>();
        private ObservableCollection<IApplication> _applications = new ObservableCollection<IApplication>();
        private Task<ActionInfo> _task;
        CancellationTokenSource _cancelTokenSource;
        private bool _selecteNewestItem;
        private IApplication _cutActionSource;
        private IAction _actionClipboard;

        private void cmdEditAction_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableActions.SelectedItems.Count == 0) return;

            // Get first item selected, associated action, and selected application
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            IAction selectedAction = null;
            IApplication selectedApplication = null;
            string selectedGesture = null;

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

            ActionDialog actionDialog = new ActionDialog(selectedAction, selectedApplication);
            actionDialog.ShowDialog();
        }

        private async void cmdDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0) return;

            // Confirm user really wants to delete selected items
            if (await
                UIHelper.GetParentWindow(this)
                    .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteConfirmTitle"),
                        LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteActionConfirm"),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                            ColorScheme = MetroDialogColorScheme.Accented,
                            AnimateHide = false,
                            AnimateShow = false
                        }) != MessageDialogResult.Affirmative)
                return;


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
            ActionInfo actionInfo = UIHelper.GetParentDependencyObject<ListBoxItem>(sender as CheckBox).Content as ActionInfo;
            if (actionInfo == null) return;
            IApplication app = lstAvailableApplication.SelectedItem as IApplication;
            if (app == null) return;
            ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, app.Name).IsEnabled = (sender as CheckBox).IsChecked.Value;
            ApplicationManager.Instance.SaveApplications();
        }





        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (GestureManager.Instance.Gestures.Length == 0)
            {
                UIHelper.GetParentWindow(this)
                    .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Action.Messages.NoGestureTitle"),
                        LocalizationProvider.Instance.GetTextValue("Action.Messages.NoGesture"), MessageDialogStyle.Affirmative,
                        new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            ColorScheme = MetroDialogColorScheme.Accented,
                            AnimateHide = false,
                            AnimateShow = false
                        });
                return;
            }

            var ai = lstAvailableActions.SelectedItem as ActionInfo;
            string gestureName = ai?.GestureName;
            ActionDialog actionDialog = new ActionDialog(gestureName, lstAvailableApplication.SelectedItem as IApplication);
            actionDialog.Show();
        }



        private void BindApplications()
        {
            _applications.Clear();

            // Add global actions to global applications group
            var userApplications =
                ApplicationManager.Instance.Applications.Where(app => (app is UserApplication)).OrderBy(app => app.Name);
            var globalApplication = ApplicationManager.Instance.GetAllGlobalApplication();

            foreach (var app in globalApplication.Union(userApplications))
            {
                _applications.Add(app);
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
                    _task.Wait(500);
                }
                ActionInfos.Clear();
                AddActionsToGroup(selectedApplication.Actions);
            }
            else
            {
                _selecteNewestItem = true;
                var newApp =
                    selectedApplication.Actions.Where(
                        a => !ActionInfos.Any
                            (ai => ai.ActionName.Equals(a.Name, StringComparison.Ordinal))).ToList();
                var deletedApp =
                    ActionInfos.Where(
                        ai =>
                            !selectedApplication.Actions.Any(a => a.Name.Equals(ai.ActionName, StringComparison.Ordinal)))
                        .ToList();

                if (newApp.Count == 1 && deletedApp.Count == 1)
                {
                    var newActionInfo = Action2ActionInfo(newApp[0]);
                    ActionInfos[ActionInfos.IndexOf(deletedApp[0])] = newActionInfo;
                    RefreshGroup(newApp[0].GestureName);
                    SelectAction(newActionInfo);
                }
                else
                {
                    foreach (ActionInfo ai in deletedApp)
                        ActionInfos.Remove(ai);
                    AddActionsToGroup(newApp);
                }
            }
        }
        private void AddActionsToGroup(List<IAction> actions)
        {
            _cancelTokenSource = new CancellationTokenSource();

            _task = new Task<ActionInfo>(() =>
            {
                ActionInfo actionInfo = null;
                foreach (Applications.Action currentAction in actions)
                {
                    actionInfo = Action2ActionInfo(currentAction);

                    if (_cancelTokenSource.IsCancellationRequested) break;

                    lstAvailableApplication.Dispatcher.Invoke(() =>
                    {
                        ActionInfos.Add(actionInfo);
                    });
                }
                return actionInfo;
            }, _cancelTokenSource.Token);
            _task.ContinueWith((t) =>
            {
                ActionInfo ai = t.Result;
                if (ai != null && _selecteNewestItem)
                    lstAvailableActions.Dispatcher.Invoke(() =>
                    {
                        _selecteNewestItem = false;
                        SelectAction(ai);
                    });

            });
            _task.Start();
        }

        void RefreshGroup(string gestureName)
        {
            lstAvailableActions.SelectedItem = null;
            var temp = ActionInfos.Where(ai => ai.GestureName.Equals(gestureName, StringComparison.Ordinal)).ToList();
            foreach (ActionInfo ai in temp)
            {
                int i = ActionInfos.IndexOf(ai);
                ActionInfos.Remove(ai);
                ActionInfos.Insert(i, ai);
            }
        }

        void SelectAction(ActionInfo actionInfo)
        {
            lstAvailableActions.SelectedItem = actionInfo;
            lstAvailableActions.UpdateLayout();
            lstAvailableActions.ScrollIntoView(actionInfo);
            EnableRelevantButtons();
        }

        private ActionInfo Action2ActionInfo(IAction action)
        {
            string description;
            string pluginName;
            // Ensure this action has a plugin
            if (PluginManager.Instance.PluginExists(action.PluginClass, action.PluginFilename))
            {
                try
                {
                    // Get plugin for this action
                    IPluginInfo pluginInfo =
                        PluginManager.Instance.FindPluginByClassAndFilename(action.PluginClass,
                            action.PluginFilename);

                    // Feed settings to plugin
                    if (!pluginInfo.Plugin.Deserialize(action.ActionSettings))
                        action.ActionSettings = pluginInfo.Plugin.Serialize();

                    pluginName = pluginInfo.Plugin.Name;
                    description = pluginInfo.Plugin.Description;
                }
                catch
                {
                    pluginName = string.Empty;
                    description = LocalizationProvider.Instance.GetTextValue("Action.Messages.NoAssociationAction");
                }
            }
            else
            {
                pluginName = String.Empty;
                description = LocalizationProvider.Instance.GetTextValue("Action.Messages.NoAssociationAction");
            }

            return new ActionInfo(
                !String.IsNullOrEmpty(action.Name) ? action.Name : pluginName,
                description,
                action.GestureName,
                action.IsEnabled);
        }

        private void EnableRelevantButtons()
        {
            cmdDelete.IsEnabled = cmdEdit.IsEnabled = lstAvailableActions.SelectedItems.Count == 1;

            var selectedActionInfo = (lstAvailableActions.SelectedItem as ActionInfo);
            if (selectedActionInfo == null)
                MoveUpButton.IsEnabled = MoveDownButton.IsEnabled = false;
            else
            {
                var actionInfoGroup = ActionInfos.Where(ai => ai.GestureName.Equals(selectedActionInfo.GestureName, StringComparison.Ordinal)).ToList();
                int index = actionInfoGroup.IndexOf(selectedActionInfo);

                MoveUpButton.IsEnabled = index != 0;
                MoveDownButton.IsEnabled = index != actionInfoGroup.Count - 1;
            }
        }

        private bool SetClipboardAction()
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return false;

            IApplication currentApp = lstAvailableApplication.SelectedItem as IApplication;

            IAction selectedAction = currentApp?.Actions.Find(a => a.Name.Equals(selectedItem.ActionName, StringComparison.Ordinal));

            if (selectedAction != null)
            {
                _actionClipboard = selectedAction;
                return true;
            }
            return false;
        }

        private void availableGesturesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox != null)
            {
                var stackPanel = VisualTreeHelper.GetParent(comboBox);
                string groupName = (VisualTreeHelper.GetChild(stackPanel, 1) as TextBlock)?.Text;
                if (groupName == null) return;

                foreach (GestureItem item in comboBox.Items)
                {
                    if (item.Name == groupName)
                    {
                        comboBox.SelectedIndex = comboBox.Items.IndexOf(item);
                        return;
                    }
                }
            }
        }


        private void availableGesturesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            ComboBox availableGesturesComboBox = sender as ComboBox;
            if (availableGesturesComboBox != null &&
                (!availableGesturesComboBox.IsDropDownOpen || e.AddedItems.Count == 0))
                return;

            Expander expander = UIHelper.GetParentDependencyObject<Expander>(availableGesturesComboBox);
            if (expander == null) return;

            var firstListBoxItem = UIHelper.FindVisualChild<ListBoxItem>(expander);
            if (firstListBoxItem == null) return;
            var listBoxItemParent = UIHelper.GetParentDependencyObject<StackPanel>(firstListBoxItem);
            if (listBoxItemParent == null) return;
            var listBoxItems = listBoxItemParent.Children;
            foreach (ListBoxItem listBoxItem in listBoxItems)
            {
                ActionInfo ai = listBoxItem.Content as ActionInfo;
                IApplication app = lstAvailableApplication.SelectedItem as IApplication;
                if (ai != null && ((GestureItem)e.AddedItems[0]).Name != ai.GestureName)
                {
                    if (app != null)
                    {
                        IAction action = app.Actions.First(a => a.Name.Equals(ai.ActionName, StringComparison.Ordinal));
                        ai.GestureName = action.GestureName = ((GestureItem)e.AddedItems[0]).Name;

                        ApplicationManager.Instance.SaveApplications();
                    }
                }
                else return;
            }
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.json;*.act",
                Title = LocalizationProvider.Instance.GetTextValue("Action.ImportActions"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                var newApps = System.IO.Path.GetExtension(ofdApplications.FileName)
                    .Equals(".act", StringComparison.OrdinalIgnoreCase)
                    ? FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true)
                    : FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName,
                        new Type[]
                        {
                            typeof (GlobalApplication), typeof (UserApplication), typeof (IgnoredApplication),
                            typeof (Applications.Action)
                        }, false);

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
                                    var result =
                                        MessageBox.Show(
                                            String.Format(
                                                LocalizationProvider.Instance.GetTextValue("Action.Messages.ReplaceConfirm"),
                                                newAction.Name, existingApp.Name),
                                            LocalizationProvider.Instance.GetTextValue("Action.Messages.ReplaceConfirmTitle"),
                                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
                            addcount += newApp.Actions.Count;
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
                MessageBox.Show(
                    String.Format(LocalizationProvider.Instance.GetTextValue("Action.Messages.ImportComplete"), addcount),
                    LocalizationProvider.Instance.GetTextValue("Action.Messages.ImportCompleteTitle"));
            }
        }

        private void ExportAllActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.act",
                FileName = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + ".act",
                Title = LocalizationProvider.Instance.GetTextValue("Action.ExportActions"),
                AddExtension = true,
                DefaultExt = "act",
                ValidateNames = true
            };
            if (sfdApplications.ShowDialog().Value)
            {
                FileManager.SaveObject(ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApplication)).ToList(), sfdApplications.FileName, true);
            }
        }
        private void ExportEnableActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.act",
                FileName = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + ".act",
                Title = LocalizationProvider.Instance.GetTextValue("Action.ExportSpecificActions"),
                AddExtension = true,
                DefaultExt = "act",
                ValidateNames = true
            };
            if (sfdApplications.ShowDialog().Value)
            {
                IApplication currentApp = lstAvailableApplication.SelectedItem as IApplication;
                if (currentApp != null)
                {
                    List<IApplication> exportedApp = new List<IApplication>(1);
                    if (currentApp is GlobalApplication)
                    {
                        exportedApp.Add(new GlobalApplication()
                        {
                            Actions = currentApp.Actions.Where(a => a.IsEnabled).ToList(),
                            Group = currentApp.Group,
                            IsRegEx = currentApp.IsRegEx,
                            MatchString = currentApp.MatchString,
                            MatchUsing = currentApp.MatchUsing
                        });
                    }
                    else
                    {
                        UserApplication userApplication = currentApp as UserApplication;
                        if (userApplication != null)
                            exportedApp.Add(new UserApplication()
                            {
                                Actions = userApplication.Actions.Where(a => a.IsEnabled).ToList(),
                                Group = userApplication.Group,
                                IsRegEx = userApplication.IsRegEx,
                                MatchString = userApplication.MatchString,
                                MatchUsing = userApplication.MatchUsing,
                                AllowSingleStroke = userApplication.AllowSingleStroke,
                                InterceptTouchInput = userApplication.InterceptTouchInput,
                                Name = userApplication.Name
                            });
                    }
                    FileManager.SaveObject(exportedApp, sfdApplications.FileName, true);
                }
            }
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
                AllowSingleMenuItem.IsEnabled = true;
                InterceptTouchInputMenuItem.IsEnabled = AppConfig.UiAccess;
                InterceptTouchInputMenuItem.IsChecked = userApplication.InterceptTouchInput;
                AllowSingleMenuItem.IsChecked = userApplication.AllowSingleStroke;
            }

            PasteActionMenuItem2.IsEnabled = _actionClipboard != null;
        }

        private void LstAvailableActions_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteActionMenuItem.IsEnabled = _actionClipboard != null;
            CopyActionMenuItem.IsEnabled = CutActionMenuItem.IsEnabled = lstAvailableActions.SelectedIndex != -1;
        }

        private void CutActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SetClipboardAction())
                _cutActionSource = (IApplication)lstAvailableApplication.SelectedItem;
        }

        private void CopyActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetClipboardAction();
            _cutActionSource = null;
        }

        private void PasteActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_actionClipboard == null) return;

            var targetApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (targetApplication == null) return;

            if (targetApplication.Actions.Exists(a => a.Name.Equals(_actionClipboard.Name, StringComparison.Ordinal)))
            {
                UIHelper.GetParentWindow(this)
                    .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExistsTitle"),
                        string.Format(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ActionExists"),
                            _actionClipboard.Name, targetApplication.Name),
                        MessageDialogStyle.Affirmative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            ColorScheme = MetroDialogColorScheme.Accented,
                            AnimateShow = false,
                            AnimateHide = false
                        });
                return;
            }

            Applications.Action newAction = new Applications.Action()
            {
                ActionSettings = _actionClipboard.ActionSettings,
                GestureName = _actionClipboard.GestureName,
                IsEnabled = _actionClipboard.IsEnabled,
                Name = _actionClipboard.Name,
                PluginClass = _actionClipboard.PluginClass,
                PluginFilename = _actionClipboard.PluginFilename
            };
            targetApplication.AddAction(newAction);

            if (_cutActionSource != null)
            {
                _cutActionSource.RemoveAction(_actionClipboard);
                _cutActionSource = null;
                _actionClipboard = null;
            }

            RefreshActions(false);

            ApplicationManager.Instance.SaveApplications();
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

        private void lstAvailableApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            RefreshActions(true);
            IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApp == null)
            {
                ToggleAllActionsCheckBox.IsEnabled = false;
                return;
            }
            ToggleAllActionsCheckBox.IsEnabled = true;
            ToggleAllActionsCheckBox.IsChecked = selectedApp.Actions.All(a => a.IsEnabled);

            EditAppButton.Visibility = DeleteAppButton.Visibility =
                selectedApp is UserApplication ? Visibility.Visible : Visibility.Hidden;
        }

        private void NewApplicationButton_OnClick(object sender, RoutedEventArgs e)
        {
            ApplicationDialog applicationDialog = new ApplicationDialog(true);
            applicationDialog.ShowDialog();
        }

        private void EditAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstAvailableApplication.SelectedItem != null)
            {
                ApplicationDialog applicationDialog = new ApplicationDialog(lstAvailableApplication.SelectedItem as IApplication);
                applicationDialog.ShowDialog();
            }
        }

        private async void DeleteAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (await
                UIHelper.GetParentWindow(this)
                    .ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteConfirmTitle"),
                        LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteAppConfirm"),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                            ColorScheme = MetroDialogColorScheme.Accented,
                            AnimateHide = false,
                            AnimateShow = false
                        }) == MessageDialogResult.Affirmative)
            {
                ApplicationManager.Instance.RemoveApplication((IApplication)lstAvailableApplication.SelectedItem);

                BindApplications();
                lstAvailableApplication.SelectedIndex = 0;
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (lstAvailableActions.SelectedItem as ActionInfo);
            var actionInfoGroup =
                  ActionInfos.Where(ai => ai.GestureName.Equals(selected.GestureName, StringComparison.Ordinal)).ToList();
            int index = actionInfoGroup.IndexOf(selected);
            if (index > 0)
            {
                ActionInfos.Move(ActionInfos.IndexOf(selected), ActionInfos.IndexOf(actionInfoGroup[index - 1]));
                RefreshGroup(selected.GestureName);
                lstAvailableActions.SelectedItem = selected;


                IApplication selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;

                int selectedIndex = selectedApplication.Actions.FindIndex(a => a.Name.Equals(selected.ActionName, StringComparison.Ordinal));
                int lastIndex = selectedApplication.Actions.FindLastIndex(selectedIndex - 1,
                    a => a.GestureName.Equals(selected.GestureName, StringComparison.Ordinal));

                var temp = selectedApplication.Actions[lastIndex];
                selectedApplication.Actions[lastIndex] = selectedApplication.Actions[selectedIndex];
                selectedApplication.Actions[selectedIndex] = temp;

                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (lstAvailableActions.SelectedItem as ActionInfo);
            var actionInfoGroup =
                  ActionInfos.Where(ai => ai.GestureName.Equals(selected.GestureName, StringComparison.Ordinal)).ToList();
            int index = actionInfoGroup.IndexOf(selected);
            if (index + 1 < actionInfoGroup.Count)
            {

                ActionInfos.Move(ActionInfos.IndexOf(selected), ActionInfos.IndexOf(actionInfoGroup[index + 1]));
                RefreshGroup(selected.GestureName);
                lstAvailableActions.SelectedItem = selected;


                IApplication selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;

                int selectedIndex = selectedApplication.Actions.FindIndex(a => a.Name.Equals(selected.ActionName, StringComparison.Ordinal));
                int nextIndex = selectedApplication.Actions.FindIndex(selectedIndex + 1,
                    a => a.GestureName.Equals(selected.GestureName, StringComparison.Ordinal));

                var temp = selectedApplication.Actions[nextIndex];
                selectedApplication.Actions[nextIndex] = selectedApplication.Actions[selectedIndex];
                selectedApplication.Actions[selectedIndex] = temp;

                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void ToggleAllActionsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var toggleButton = ((ToggleButton)sender);

                IApplication app = lstAvailableApplication.SelectedItem as IApplication;
                if (app == null) return;

                app.Actions.ForEach(a => a.IsEnabled = toggleButton.IsChecked.Value);
                ApplicationManager.Instance.SaveApplications();

                foreach (ActionInfo ai in ActionInfos)
                {
                    ai.IsEnabled = toggleButton.IsChecked.Value;
                }
            }
            catch { }
        }
    }
}
