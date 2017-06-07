using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
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
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls;

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

        public ObservableCollection<CommandInfo> CommandInfos { get; } = new ObservableCollection<CommandInfo>();

        private Task _addCommandTask;
        private IApplication _cutActionSource;
        private readonly List<CommandInfo> _commandClipboard = new List<CommandInfo>();

        private void UserControl_Initialized(object sender, EventArgs eArgs)
        {
            ApplicationManager.ApplicationChanged += (o, e) =>
            {
                lstAvailableApplication.SelectedItem = e.Application;
                lstAvailableApplication.Dispatcher.InvokeAsync(() => lstAvailableApplication.ScrollIntoView(e.Application), DispatcherPriority.Input);
            };
        }

        private void cmdEditCommand_Click(object sender, RoutedEventArgs e)
        {
            EditCommand();
        }

        private void EditCommand()
        {
            // Make sure at least one item is selected
            if (lstAvailableActions.SelectedItems.Count == 0) return;

            // Get first item selected, associated action, and selected application
            CommandInfo selectedItem = (CommandInfo)lstAvailableActions.SelectedItem;
            var selectedCommand = selectedItem.Command;
            if (selectedCommand == null) return;

            CommandDialog commandDialog = new CommandDialog(selectedCommand, selectedItem.Action);
            var result = commandDialog.ShowDialog();
            if (result != null && result.Value)
            {
                int index = CommandInfos.IndexOf(selectedItem);
                var newActionInfo = CommandInfo.FromCommand(selectedCommand, selectedItem.Action);
                CommandInfos[index] = newActionInfo;
                RefreshActionGroup(newActionInfo.Action);
                SelectCommands(newActionInfo);
            }
        }

        private void RefreshActionGroup(IAction action)
        {
            var temp = CommandInfos.Where(ci => ci.Action == action).ToList();
            temp.ForEach(ci => CommandInfos.Remove(ci));
            action.Commands.ForEach(com => CommandInfos.Add(temp.Find(ci => ci.Command == com)));
        }

        private void cmdDeleteCommand_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0) return;

            // Confirm user really wants to delete selected items
            if (UIHelper.GetParentWindow(this)
                    .ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteConfirmTitle"),
                      string.Format(LocalizationProvider.Instance.GetTextValue("Action.Messages.DeleteCommandConfirm"), lstAvailableActions.SelectedItems.Count),
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                            ColorScheme = MetroDialogColorScheme.Accented,
                        }) != MessageDialogResult.Affirmative)
                return;

            var commandInfoList = lstAvailableActions.SelectedItems.Cast<CommandInfo>().ToList();
            // Loop through selected actions
            for (int i = commandInfoList.Count - 1; i >= 0; i--)
            {
                // Grab selected item
                CommandInfo selectedCommand = commandInfoList[i];
                selectedCommand.Action.Commands.Remove(selectedCommand.Command);
                if (selectedCommand.Action.Commands.Count == 0)
                {
                    IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;

                    selectedApp.RemoveAction(selectedCommand.Action);
                }

                CommandInfos.Remove(selectedCommand);
            }
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableRelevantButtons();
        }

        private void LstAvailableActions_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HitTestResult hitTest = VisualTreeHelper.HitTest(lstAvailableActions, new Point(5, 5));
            var element = hitTest.VisualHit as UIElement;
            if (element != null)
            {
                Rect bounds = element.TransformToAncestor(lstAvailableActions).TransformBounds(new Rect(0.0, 0.0, element.RenderSize.Width, element.RenderSize.Height));
                var gestureImageContainer = element.FindChild<Grid>("GestureImageGrid");
                if (gestureImageContainer == null) return;
                if (bounds.Top < 0)
                {
                    var topMargin = -bounds.Top + gestureImageContainer.ActualHeight > element.RenderSize.Height
                        ? element.RenderSize.Height - gestureImageContainer.ActualHeight
                        : Math.Abs(bounds.Top);
                    gestureImageContainer.Margin = new Thickness(0, topMargin, 0, 0);
                }
                else gestureImageContainer.Margin = new Thickness(0);
            }
        }

        private void CommandCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CommandInfo info = UIHelper.GetParentDependencyObject<ListBoxItem>(sender as ToggleSwitch).Content as CommandInfo;
            if (info == null) return;
            info.Command.IsEnabled = (sender as ToggleSwitch).IsChecked.Value;
            ApplicationManager.Instance.SaveApplications();
        }

        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null)
            {
                lstAvailableApplication.SelectedIndex = 0;
                selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;
            }
            var ci = lstAvailableActions.SelectedItem as CommandInfo;

            var newCommand = new Command
            {
                Name = LocalizationProvider.Instance.GetTextValue("Action.NewCommand")
            };
            Action<Task> addCommand = task =>
            {
                Dispatcher.Invoke(() =>
                {
                    CommandInfo newInfo;
                    if (ci == null)
                    {
                        var newAction = new GestureSign.Common.Applications.Action
                        {
                            Commands = new List<ICommand>()
                        };
                        newAction.Commands.Add(newCommand);
                        selectedApplication.AddAction(newAction);
                        newInfo = CommandInfo.FromCommand(newCommand, newAction);
                        CommandInfos.Add(newInfo);
                    }
                    else
                    {
                        newInfo = CommandInfo.FromCommand(newCommand, ci.Action);

                        int infoIndex = CommandInfos.IndexOf(ci);
                        int commandIndex = ci.Action.Commands.IndexOf(ci.Command);

                        if (commandIndex + 1 == ci.Action.Commands.Count)
                            ci.Action.Commands.Add(newCommand);
                        else
                            ci.Action.Commands.Insert(commandIndex + 1, newCommand);

                        if (infoIndex + 1 == CommandInfos.Count)
                            CommandInfos.Add(newInfo);
                        else
                            CommandInfos.Insert(infoIndex + 1, newInfo);
                    }
                    SelectCommands(newInfo);
                }, DispatcherPriority.Input);
            };

            if (_addCommandTask != null && !_addCommandTask.IsCompleted)
            {
                _addCommandTask.ContinueWith(addCommand);
            }
            else addCommand.Invoke(null);

            ApplicationManager.Instance.SaveApplications();
        }

        private void RefreshAllActions()
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null) return;

            Action<object> refreshAction = (o) =>
             {
                 Dispatcher.Invoke(() => { CommandInfos.Clear(); }, DispatcherPriority.Loaded);
                 foreach (var currentAction in selectedApplication.Actions)
                 {
                     if (currentAction.Commands == null) continue;
                     foreach (var command in currentAction.Commands)
                     {
                         var info = CommandInfo.FromCommand(command, currentAction);

                         Dispatcher.Invoke(() =>
                         {
                             CommandInfos.Add(info);
                         }, DispatcherPriority.Input);
                     }
                 }
             };

            _addCommandTask = _addCommandTask?.ContinueWith(refreshAction) ?? Task.Factory.StartNew(refreshAction, null);
        }

        void SelectCommands(params CommandInfo[] infos)
        {
            lstAvailableActions.SelectedItems.Clear();
            foreach (var info in infos)
            {
                lstAvailableActions.SelectedItems.Add(info);
            }
            lstAvailableActions.UpdateLayout();
            Dispatcher.InvokeAsync(() => lstAvailableActions.ScrollIntoView(lstAvailableActions.SelectedItem), DispatcherPriority.Background);
        }

        private void EnableRelevantButtons()
        {
            cmdDelete.IsEnabled = cmdEdit.IsEnabled = lstAvailableActions.SelectedItems.Count != 0;

            var selectedInfo = (lstAvailableActions.SelectedItem as CommandInfo);
            if (selectedInfo == null)
                MoveUpButton.IsEnabled = MoveDownButton.IsEnabled = false;
            else
            {
                int index = CommandInfos.Where(ci => ci.Action == selectedInfo.Action).ToList().IndexOf(selectedInfo);

                MoveUpButton.IsEnabled = index > 0;
                MoveDownButton.IsEnabled = index < selectedInfo.Action.Commands.Count - 1;
            }
        }

        private bool SetClipboardAction()
        {
            _commandClipboard.Clear();
            foreach (CommandInfo commandInfo in lstAvailableActions.SelectedItems)
            {
                if (commandInfo?.Command != null)
                    _commandClipboard.Add(commandInfo);
            }
            return _commandClipboard.Count != 0;
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            List<CommandInfo> infoList = new List<CommandInfo>();
            var groupItem = UIHelper.GetParentDependencyObject<GroupItem>((Button)sender);
            foreach (CommandInfo info in lstAvailableActions.SelectedItems)
            {
                var listItem = lstAvailableActions.ItemContainerGenerator.ContainerFromItem(info);
                if (ReferenceEquals(UIHelper.GetParentDependencyObject<GroupItem>(listItem), groupItem))
                    infoList.Add(info);
            }

            if (infoList.Count == 0)
            {
                var collectionViewGroup = groupItem.Content as CollectionViewGroup;
                if (collectionViewGroup != null)
                    foreach (CommandInfo item in collectionViewGroup.Items)
                    {
                        lstAvailableActions.SelectedItems.Add(item);
                        infoList.Add(item);
                    }
            }

            var sourceAction = infoList.First().Action;
            var selectedApplication = (IApplication)lstAvailableApplication.SelectedItem;
            ActionDialog actionDialog = new ActionDialog(sourceAction, selectedApplication);
            var result = actionDialog.ShowDialog();

            if (result != null && result.Value)
            {
                var newAction = actionDialog.NewAction;

                if (newAction != sourceAction)
                    foreach (CommandInfo info in infoList)
                    {
                        sourceAction.Commands.Remove(info.Command);
                        newAction.Commands.Add(info.Command);

                        info.Action = newAction;
                    }
                RefreshActionGroup(newAction);

                selectedApplication.Actions.RemoveAll(a => a.Commands == null || a.Commands.Count == 0);
                ApplicationManager.Instance.SaveApplications();

                SelectCommands(infoList.ToArray());
            }
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.gsa",
                Title = LocalizationProvider.Instance.GetTextValue("Common.Import"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                var newApps = FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true);
                if (newApps != null)
                {
                    ExportImportDialog exportImportDialog = new ExportImportDialog(false, false, newApps, GestureSign.Common.Gestures.GestureManager.Instance.Gestures);
                    exportImportDialog.ShowDialog();
                }
            }
        }

        private void ExportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExportImportDialog exportImportDialog = new ExportImportDialog(true, false, ApplicationManager.Instance.Applications, GestureSign.Common.Gestures.GestureManager.Instance.Gestures);
            exportImportDialog.ShowDialog();
        }

        private void lstAvailableApplication_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteActionMenuItem2.IsEnabled = _commandClipboard.Count != 0;

            EditMenuItem.IsEnabled = DeleteMenuItem.IsEnabled = lstAvailableApplication.SelectedItem is UserApp;
        }

        private void LstAvailableActions_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteActionMenuItem.IsEnabled = _commandClipboard.Count != 0;
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
            if (_commandClipboard.Count == 0) return;

            var targetApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (targetApplication == null) return;

            CommandInfo commandInfo;
            foreach (var info in _commandClipboard)
            {
                var newCommand = new Command()
                {
                    CommandSettings = info.Command.CommandSettings,
                    IsEnabled = info.IsEnabled,
                    Name = info.Command.Name,
                    PluginClass = info.Command.PluginClass,
                    PluginFilename = info.Command.PluginFilename,
                };
                if (targetApplication.Actions.Contains(info.Action))
                {
                    if (info.Action.Commands.Exists(c => c.Name == newCommand.Name))
                    {
                        newCommand.Name = ApplicationManager.GetNextCommandName(newCommand.Name, info.Action);
                    }
                    info.Action.Commands.Add(newCommand);
                    commandInfo = CommandInfo.FromCommand(newCommand, info.Action);
                }
                else
                {
                    var newAction = new GestureSign.Common.Applications.Action()
                    {
                        Commands = new List<ICommand>(),
                        Condition = info.Action.Condition,
                        GestureName = info.Action.GestureName,
                        Name = info.Action.Name
                    };
                    newAction.Commands.Add(newCommand);
                    targetApplication.AddAction(newAction);
                    commandInfo = CommandInfo.FromCommand(newCommand, newAction);
                }

                if (_cutActionSource != null)
                {
                    _cutActionSource.RemoveAction(info.Action);
                    if (_cutActionSource == targetApplication)
                        CommandInfos.Remove(info);
                }
                CommandInfos.Add(commandInfo);
            }

            if (_cutActionSource != null)
            {
                _cutActionSource = null;
                _commandClipboard.Clear();
            }

            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            RefreshAllActions();
            IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApp == null)
            {
                ToggleAllActionsToggleSwitch.IsEnabled = false;
                return;
            }
            ToggleAllActionsToggleSwitch.IsEnabled = true;
            ToggleAllActionsToggleSwitch.IsChecked = selectedApp.Actions.SelectMany(a => a.Commands).All(c => c.IsEnabled);
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
            var userapp = lstAvailableApplication.SelectedItem as UserApp;
            if (userapp != null)
            {
                ApplicationDialog applicationDialog = new ApplicationDialog(userapp);
                applicationDialog.ShowDialog();
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedApp = lstAvailableApplication.SelectedItem as UserApp;
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
            var selected = (CommandInfo)lstAvailableActions.SelectedItem;
            int commandIndex = selected.Action.Commands.IndexOf(selected.Command);
            if (commandIndex > 0)
            {
                var temp = selected.Action.Commands[commandIndex - 1];
                selected.Action.Commands[commandIndex - 1] = selected.Action.Commands[commandIndex];
                selected.Action.Commands[commandIndex] = temp;

                RefreshActionGroup(selected.Action);
                SelectCommands(selected);
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (CommandInfo)lstAvailableActions.SelectedItem;
            int commandIndex = selected.Action.Commands.IndexOf(selected.Command);
            if (commandIndex + 1 < selected.Action.Commands.Count)
            {
                var temp = selected.Action.Commands[commandIndex + 1];
                selected.Action.Commands[commandIndex + 1] = selected.Action.Commands[commandIndex];
                selected.Action.Commands[commandIndex] = temp;

                RefreshActionGroup(selected.Action);
                SelectCommands(selected);
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void ToggleAllActionsToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var toggleSwitch = ((ToggleSwitch)sender);

                IApplication app = lstAvailableApplication.SelectedItem as IApplication;
                if (app == null) return;
                foreach (var command in app.Actions.SelectMany(a => a.Commands))
                {
                    command.IsEnabled = toggleSwitch.IsChecked.Value;
                }
                ApplicationManager.Instance.SaveApplications();

                foreach (CommandInfo ai in CommandInfos)
                {
                    ai.IsEnabled = toggleSwitch.IsChecked.Value;
                }
            }
            catch { }
        }

        private void ListBoxItem_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listBoxItem = (ListBoxItem)sender;
            var listBox = UIHelper.GetParentDependencyObject<ListBox>(listBoxItem);
            if (ReferenceEquals(listBox, lstAvailableActions))
                Dispatcher.InvokeAsync(EditCommand, DispatcherPriority.Input);
            else if (ReferenceEquals(listBox, lstAvailableApplication))
                Dispatcher.InvokeAsync(EditApplication, DispatcherPriority.Input);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow DownloadWindow = new DownloadWindow();
            DownloadWindow.Show();
        }
    }
}
