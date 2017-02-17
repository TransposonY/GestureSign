using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

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
            DataContext = this;
        }

        public ObservableCollection<ActionInfo> ActionInfos { get; } = new ObservableCollection<ActionInfo>();

        private Task _addActionTask;
        private IApplication _cutActionSource;
        private readonly List<IAction> _actionClipboard = new List<IAction>();

        private void UserControl_Initialized(object sender, EventArgs eArgs)
        {
            ActionDialog.ActionsChanged += (o, e) =>
            {
                var oldActionInfo =
                      ActionInfos.FirstOrDefault(ai => ai.ActionName.Equals(e.Action.Name, StringComparison.Ordinal));
                if (oldActionInfo != null)
                {
                    int index = ActionInfos.IndexOf(oldActionInfo);
                    var newActionInfo = Action2ActionInfo(e.Action);
                    ActionInfos[index] = newActionInfo;
                    RefreshGroup(e.Action.GestureName);
                    SelectAction(newActionInfo.ActionName);
                }
                else RefreshPartialActions();
            };

            ApplicationManager.ApplicationChanged += (o, e) =>
            {
                lstAvailableApplication.SelectedItem = e.Application;
                lstAvailableApplication.ScrollIntoView(e.Application);
            };
        }

        private void cmdEditAction_Click(object sender, RoutedEventArgs e)
        {
            EditAction();
        }

        private void EditAction()
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

        private void cmdDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0) return;

            // Confirm user really wants to delete selected items
            if (UIHelper.GetParentWindow(this)
                    .ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteConfirmTitle"),
                      string.Format(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteActionConfirm"), lstAvailableActions.SelectedItems.Count),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                            ColorScheme = MetroDialogColorScheme.Accented,
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
            RefreshPartialActions();
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableRelevantButtons();
        }

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
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null) return;
            var ai = lstAvailableActions.SelectedItem as ActionInfo;

            Applications.Action action = new Applications.Action()
            {
                Name = ApplicationManager.GetNextActionName(LocalizationProvider.Instance.GetTextValue("Action.NewAction"), selectedApplication)
            };

            ActionInfo actionInfo = Action2ActionInfo(action);
            if (ai == null)
            {
                selectedApplication.AddAction(action);
                ActionInfos.Add(actionInfo);
            }
            else
            {
                selectedApplication.Insert(ActionInfos.IndexOf(ai), action);
                ActionInfos.Insert(ActionInfos.IndexOf(ai), actionInfo);
            }
            SelectAction(actionInfo.ActionName);

            ApplicationManager.Instance.SaveApplications();
        }

        private void RefreshAllActions()
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null) return;

            Action<object> refreshAction = (o) =>
             {
                 Dispatcher.Invoke(() => { ActionInfos.Clear(); }, DispatcherPriority.Loaded);
                 AddActionsToGroup(selectedApplication.Actions);
             };

            _addActionTask = _addActionTask?.ContinueWith(refreshAction) ?? Task.Factory.StartNew(refreshAction, null);
        }

        private void RefreshPartialActions()
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null) return;

            var newApp = selectedApplication.Actions.Where(a => !ActionInfos.Any(ai => ai.Equals(a))).ToList();
            var deletedApp = ActionInfos.Where(ai => !selectedApplication.Actions.Any(ai.Equals)).ToList();

            if (newApp.Count == 1 && deletedApp.Count == 1)
            {
                var newActionInfo = Action2ActionInfo(newApp[0]);
                ActionInfos[ActionInfos.IndexOf(deletedApp[0])] = newActionInfo;
                RefreshGroup(newApp[0].GestureName);
                SelectAction(newActionInfo.ActionName);
            }
            else
            {
                foreach (ActionInfo ai in deletedApp)
                    ActionInfos.Remove(ai);

                if (newApp.Count != 0)
                {
                    AddActionsToGroup(newApp);
                    SelectAction(newApp[0].Name);
                }
            }
        }

        private void AddActionsToGroup(List<IAction> actions)
        {
            foreach (var currentAction in actions)
            {
                var actionInfo = Action2ActionInfo(currentAction);

                var info = actionInfo;
                Dispatcher.Invoke(() =>
                {
                    ActionInfos.Add(info);
                }, DispatcherPriority.Input);
            }
        }

        void RefreshGroup(string gestureName)
        {
            lstAvailableActions.SelectedItem = null;
            var temp = ActionInfos.Where(ai => string.Equals(ai.GestureName, gestureName, StringComparison.Ordinal)).ToList();
            foreach (ActionInfo ai in temp)
            {
                int i = ActionInfos.IndexOf(ai);
                ActionInfos.Remove(ai);
                ActionInfos.Insert(i, ai);
            }
        }

        void SelectAction(string actionName)
        {
            lstAvailableActions.SelectedValue = actionName;
            lstAvailableActions.UpdateLayout();
            lstAvailableActions.ScrollIntoView(lstAvailableActions.SelectedItem);
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
            cmdDelete.IsEnabled = cmdEdit.IsEnabled = lstAvailableActions.SelectedItems.Count != 0;

            var selectedActionInfo = (lstAvailableActions.SelectedItem as ActionInfo);
            if (selectedActionInfo == null)
                MoveUpButton.IsEnabled = MoveDownButton.IsEnabled = false;
            else
            {
                var actionInfoGroup = ActionInfos.Where(ai => string.Equals(ai.GestureName, selectedActionInfo.GestureName, StringComparison.Ordinal)).ToList();
                int index = actionInfoGroup.IndexOf(selectedActionInfo);

                MoveUpButton.IsEnabled = index != 0;
                MoveDownButton.IsEnabled = index != actionInfoGroup.Count - 1;
            }
        }

        private bool SetClipboardAction()
        {
            _actionClipboard.Clear();
            IApplication currentApp = lstAvailableApplication.SelectedItem as IApplication;
            foreach (ActionInfo actionInfo in lstAvailableActions.SelectedItems)
            {
                if (actionInfo == null) continue;
                IAction selectedAction = currentApp?.Actions.Find(a => a.Name.Equals(actionInfo.ActionName, StringComparison.Ordinal));

                if (selectedAction != null)
                {
                    _actionClipboard.Add(selectedAction);
                }
            }
            return _actionClipboard.Count != 0;
        }

        private void GestureButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            GestureDefinition gestureDialog = new GestureDefinition(true);
            var result = gestureDialog.ShowDialog();

            if (result != null && result.Value)
            {
                var newGesture = gestureDialog.SimilarGesture == null ? GestureManager.Instance.GestureName : gestureDialog.SimilarGesture.Name;

                var gestureButton = (Button)sender;

                Expander expander = UIHelper.GetParentDependencyObject<Expander>(gestureButton);
                if (expander == null) return;
                var firstListBoxItem = UIHelper.FindVisualChild<ListBoxItem>(expander);
                if (firstListBoxItem == null) return;
                var listBoxItemParent = UIHelper.GetParentDependencyObject<StackPanel>(firstListBoxItem);
                if (listBoxItemParent == null) return;
                var listBoxItems = listBoxItemParent.Children;

                IApplication app = lstAvailableApplication.SelectedItem as IApplication;
                ActionInfo ai = null;
                if (app != null)
                {
                    foreach (ListBoxItem listBoxItem in listBoxItems)
                    {
                        ai = listBoxItem.Content as ActionInfo;
                        if (ai != null)
                        {
                            IAction action = app.Actions.First(a => a.Name.Equals(ai.ActionName, StringComparison.Ordinal));
                            ai.GestureName = action.GestureName = newGesture;

                            ApplicationManager.Instance.SaveApplications();
                        }
                        else return;
                    }
                }
                RefreshGroup(newGesture);
                SelectAction(ai?.ActionName);
            }
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.act",
                Title = LocalizationProvider.Instance.GetTextValue("Action.ImportActions"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                List<IApplication> newApplications = new List<IApplication>();
                var newApps = FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true);

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
                            newApplications.Add(newApp);
                        }
                    }
                }
                End:
                if (addcount != 0)
                {
                    ApplicationManager.Instance.AddApplicationRange(newApplications);
                    ApplicationManager.Instance.SaveApplications();
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
                                LimitNumberOfFingers = userApplication.LimitNumberOfFingers,
                                BlockTouchInputThreshold = userApplication.BlockTouchInputThreshold,
                                Name = userApplication.Name
                            });
                    }
                    FileManager.SaveObject(exportedApp, sfdApplications.FileName, true);
                }
            }
        }

        private void lstAvailableApplication_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteActionMenuItem2.IsEnabled = _actionClipboard.Count != 0;

            EditMenuItem.IsEnabled = DeleteMenuItem.IsEnabled = lstAvailableApplication.SelectedItem is UserApplication;
        }

        private void LstAvailableActions_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteActionMenuItem.IsEnabled = _actionClipboard.Count != 0;
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
            if (_actionClipboard.Count == 0) return;

            var targetApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (targetApplication == null) return;

            foreach (var action in _actionClipboard)
            {
                Applications.Action newAction = new Applications.Action()
                {
                    ActionSettings = action.ActionSettings,
                    GestureName = action.GestureName,
                    IsEnabled = action.IsEnabled,
                    Name = action.Name,
                    PluginClass = action.PluginClass,
                    PluginFilename = action.PluginFilename,
                    Condition = action.Condition
                };

                if (targetApplication.Actions.Exists(a => a.Name.Equals(newAction.Name, StringComparison.Ordinal)))
                {
                    newAction.Name = ApplicationManager.GetNextActionName(newAction.Name, targetApplication);
                }
                targetApplication.AddAction(newAction);
                _cutActionSource?.RemoveAction(action);
            }

            if (_cutActionSource != null)
            {
                _cutActionSource = null;
                _actionClipboard.Clear();
            }

            RefreshPartialActions();

            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            RefreshAllActions();
            IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApp == null)
            {
                ToggleAllActionsCheckBox.IsEnabled = false;
                return;
            }
            ToggleAllActionsCheckBox.IsEnabled = true;
            ToggleAllActionsCheckBox.IsChecked = selectedApp.Actions.All(a => a.IsEnabled);
        }

        private void NewApplicationButton_OnClick(object sender, RoutedEventArgs e)
        {
            ApplicationDialog applicationDialog = new ApplicationDialog(true);
            applicationDialog.ShowDialog();
        }

        private void EditApplication_Click(object sender, RoutedEventArgs e)
        {
            EditApplication();
        }

        private void EditApplication()
        {
            var userapp = lstAvailableApplication.SelectedItem as UserApplication;
            if (userapp != null)
            {
                ApplicationDialog applicationDialog = new ApplicationDialog(userapp);
                applicationDialog.ShowDialog();
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            UserApplication selectedApp = lstAvailableApplication.SelectedItem as UserApplication;
            if (selectedApp != null && UIHelper.GetParentWindow(this)
                .ShowModalMessageExternal(
                    LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteConfirmTitle"),
                    String.Format(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteAppConfirm"), selectedApp.Name),
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                        NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                        ColorScheme = MetroDialogColorScheme.Accented,
                    }) == MessageDialogResult.Affirmative)
            {
                ApplicationManager.Instance.RemoveApplication(selectedApp);

                lstAvailableApplication.SelectedIndex = 0;
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (lstAvailableActions.SelectedItem as ActionInfo);
            var actionInfoGroup =
                  ActionInfos.Where(ai => string.Equals(ai.GestureName, selected.GestureName, StringComparison.Ordinal)).ToList();
            int index = actionInfoGroup.IndexOf(selected);
            if (index > 0)
            {
                IApplication selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;

                var previousActionInfo = actionInfoGroup[index - 1];
                ActionInfos.Move(ActionInfos.IndexOf(selected), ActionInfos.IndexOf(previousActionInfo));
                RefreshGroup(selected.GestureName);
                lstAvailableActions.SelectedItem = selected;

                int selectedIndex = selectedApplication.Actions.FindIndex(a => a.Name.Equals(selected.ActionName, StringComparison.Ordinal));
                int previousActionIndex = selectedApplication.Actions.FindIndex(a => string.Equals(a.Name, previousActionInfo.ActionName, StringComparison.Ordinal));

                var temp = selectedApplication.Actions[previousActionIndex];
                selectedApplication.Actions[previousActionIndex] = selectedApplication.Actions[selectedIndex];
                selectedApplication.Actions[selectedIndex] = temp;

                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (lstAvailableActions.SelectedItem as ActionInfo);
            var actionInfoGroup =
                  ActionInfos.Where(ai => string.Equals(ai.GestureName, selected.GestureName, StringComparison.Ordinal)).ToList();
            int index = actionInfoGroup.IndexOf(selected);
            if (index + 1 < actionInfoGroup.Count)
            {
                IApplication selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;

                var nextActionInfo = actionInfoGroup[index + 1];
                ActionInfos.Move(ActionInfos.IndexOf(selected), ActionInfos.IndexOf(nextActionInfo));
                RefreshGroup(selected.GestureName);
                lstAvailableActions.SelectedItem = selected;

                int selectedIndex = selectedApplication.Actions.FindIndex(a => a.Name.Equals(selected.ActionName, StringComparison.Ordinal));
                int nextIndex = selectedApplication.Actions.FindIndex(a => string.Equals(a.Name, nextActionInfo.ActionName, StringComparison.Ordinal));

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

        private void ListBoxItem_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = (ListBoxItem)sender;
            var listBox = UIHelper.GetParentDependencyObject<ListBox>(listBoxItem);
            if (ReferenceEquals(listBox, lstAvailableActions))
                Dispatcher.InvokeAsync(EditAction, DispatcherPriority.Input);
            else if (ReferenceEquals(listBox, lstAvailableApplication))
                Dispatcher.InvokeAsync(EditApplication, DispatcherPriority.Input);
        }
    }
}
