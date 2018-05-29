using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using GestureSign.ControlPanel.ViewModel;
using IWshRuntimeLibrary;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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

        private IApplication _cutActionSource;
        private readonly List<CommandInfo> _commandClipboard = new List<CommandInfo>();

        private void UserControl_Initialized(object sender, EventArgs eArgs)
        {
            ApplicationManager.Instance.CollectionChanged += (o, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0 && !(e.NewItems[0] is IgnoredApp))
                    lstAvailableApplication.SelectedItem = (IApplication)e.NewItems[0];
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
            var selectedAction = selectedItem.Action;
            var selectedCommand = selectedItem.Command;
            if (selectedCommand == null) return;

            CommandDialog commandDialog = new CommandDialog(selectedCommand, selectedAction);
            var result = commandDialog.ShowDialog();
            if (result != null && result.Value)
            {
                int index = selectedAction.Commands.ToList().IndexOf(selectedCommand);
                selectedAction.RemoveCommand(selectedCommand);
                selectedAction.InsertCommand(index, selectedCommand);
            }
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
                selectedCommand.Action.RemoveCommand(selectedCommand.Command);
                if (selectedCommand.Action.IsEmpty())
                {
                    IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;

                    selectedApp.RemoveAction(selectedCommand.Action);
                }
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
            if (ci == null)
            {
                var newCommand = new Command
                {
                    Name = LocalizationProvider.Instance.GetTextValue("Action.NewCommand")
                };
                Dispatcher.Invoke(() =>
                {
                    lstAvailableActions.SelectedItem = null;
                    var newAction = new GestureSign.Common.Applications.Action();
                    newAction.AddCommand(newCommand);
                    selectedApplication.AddAction(newAction);
                    ApplicationManager.Instance.SaveApplications();
                }, DispatcherPriority.Input);
            }
            else
            {
                var element = (FrameworkElement)sender;
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
                element.ContextMenu.IsOpen = true;
            }
        }

        private void NewCommandMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null)
            {
                lstAvailableApplication.SelectedIndex = 0;
                selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
                if (selectedApplication == null) return;
            }

            var newCommand = new Command
            {
                Name = LocalizationProvider.Instance.GetTextValue("Action.NewCommand")
            };
            Dispatcher.Invoke(() =>
            {
                lstAvailableActions.SelectedItem = null;
                var newAction = new GestureSign.Common.Applications.Action();
                newAction.AddCommand(newCommand);
                selectedApplication.AddAction(newAction);
                ApplicationManager.Instance.SaveApplications();
            }, DispatcherPriority.Input);
        }

        private void FromSelectedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var ci = lstAvailableActions.SelectedItem as CommandInfo;
            if (ci == null) return;

            var newCommand = new Command
            {
                Name = LocalizationProvider.Instance.GetTextValue("Action.NewCommand")
            };
            lstAvailableActions.SelectedItem = null;
            int commandIndex = ci.Action.Commands.ToList().IndexOf(ci.Command);
            ci.Action.InsertCommand(commandIndex + 1, newCommand);
            ApplicationManager.Instance.SaveApplications();
        }

        private void EnableRelevantButtons()
        {
            cmdDelete.IsEnabled = cmdEdit.IsEnabled = lstAvailableActions.SelectedItems.Count != 0;

            var selectedInfo = (lstAvailableActions.SelectedItem as CommandInfo);
            if (selectedInfo == null)
                MoveUpButton.IsEnabled = MoveDownButton.IsEnabled = false;
            else
            {
                int index = selectedInfo.Action.Commands.ToList().IndexOf(selectedInfo.Command);

                MoveUpButton.IsEnabled = index > 0;
                MoveDownButton.IsEnabled = index < selectedInfo.Action.Commands.Count() - 1;
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
            var collectionViewGroup = groupItem.Content as CollectionViewGroup;
            if (collectionViewGroup == null) return;

            lstAvailableActions.SelectedItems.Clear();
            foreach (CommandInfo item in collectionViewGroup.Items)
            {
                lstAvailableActions.SelectedItems.Add(item);
                infoList.Add(item);
            }

            var sourceAction = infoList.First().Action;
            var selectedApplication = (IApplication)lstAvailableApplication.SelectedItem;
            ActionDialog actionDialog = new ActionDialog(sourceAction, selectedApplication);
            var result = actionDialog.ShowDialog();

            if (result != null && result.Value)
            {
                var newAction = actionDialog.NewAction;
                selectedApplication.RemoveAction(newAction);
                selectedApplication.AddAction(newAction);

                if (newAction != sourceAction)
                {
                    lstAvailableActions.SelectedItem = null;
                    foreach (CommandInfo info in infoList)
                    {
                        sourceAction.RemoveCommand(info.Command);
                        newAction.AddCommand(info.Command);
                    }
                }

                var emptyActions = selectedApplication.Actions.Where(a => a.Commands == null || a.Commands.Count() == 0).ToList();
                foreach (var action in emptyActions)
                {
                    selectedApplication.RemoveAction(action);
                }
                ApplicationManager.Instance.SaveApplications();
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

            lstAvailableActions.SelectedItem = null;
            foreach (var actionGroup in _commandClipboard.GroupBy(ci => ci.Action))
            {
                IAction currentAction;
                if (targetApplication.Actions.Contains(actionGroup.Key))
                {
                    currentAction = actionGroup.Key;
                }
                else
                {
                    currentAction = ((GestureSign.Common.Applications.Action)actionGroup.Key).Clone() as GestureSign.Common.Applications.Action;
                    currentAction.Commands = new List<ICommand>(1);
                    targetApplication.AddAction(currentAction);
                }

                foreach (var info in actionGroup)
                {
                    if (_cutActionSource != null)
                    {
                        info.Action.RemoveCommand(info.Command);
                        if (_cutActionSource != targetApplication && info.Action.Commands.Count() == 0)
                            _cutActionSource.RemoveAction(info.Action);
                    }

                    var newCommand = ((Command)info.Command).Clone() as Command;
                    newCommand.Name = ApplicationManager.GetNextCommandName(newCommand.Name, info.Action);

                    currentAction.AddCommand(newCommand);
                }
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
            IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApp == null)
            {
                ToggleAllActionsToggleSwitch.IsEnabled = false;
                return;
            }

            var commandInfoProvider = ((ObjectDataProvider)Resources["CommandInfoProvider"]).ObjectInstance as CommandInfoProvider;
            if (commandInfoProvider == null) return;
            commandInfoProvider.RefreshCommandInfos(selectedApp, lstAvailableActions);

            ToggleAllActionsToggleSwitch.IsEnabled = true;
            ToggleAllActionsToggleSwitch.IsChecked = selectedApp.Actions.SelectMany(a => a.Commands).All(c => c.IsEnabled);

            Dispatcher.InvokeAsync(() => lstAvailableApplication.ScrollIntoView(selectedApp), DispatcherPriority.Background);
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
            int commandIndex = selected.Action.Commands.ToList().IndexOf(selected.Command);
            if (commandIndex > 0)
            {
                selected.Action.MoveCommand(commandIndex, commandIndex - 1);
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (CommandInfo)lstAvailableActions.SelectedItem;
            int commandIndex = selected.Action.Commands.ToList().IndexOf(selected.Command);
            if (commandIndex + 1 < selected.Action.Commands.Count())
            {
                selected.Action.MoveCommand(commandIndex, commandIndex + 1);
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

                foreach (CommandInfo ai in lstAvailableActions.Items)
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

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var newApps = new List<IApplication>();
                var newGestures = GestureManager.Instance.Gestures.ToList();
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        switch (Path.GetExtension(file).ToLower())
                        {
                            case GestureSign.Common.Constants.ActionExtension:
                                var apps = FileManager.LoadObject<List<IApplication>>(file, false, true);
                                if (apps != null)
                                {
                                    newApps.AddRange(apps);
                                }
                                break;
                            case ".exe":
                                lstAvailableApplication.SelectedItem = ApplicationManager.Instance.AddApplication(new UserApp(), file);
                                break;
                            case ".lnk":
                                WshShell shell = new WshShell();
                                IWshShortcut link = (IWshShortcut)shell.CreateShortcut(file);
                                if (Path.GetExtension(link.TargetPath).ToLower() == ".exe")
                                {
                                    lstAvailableApplication.SelectedItem = ApplicationManager.Instance.AddApplication(new UserApp(), link.TargetPath);
                                }
                                break;
                            case GestureSign.Common.Constants.ArchivesExtension:
                                {
                                    IEnumerable<IApplication> applications;
                                    IEnumerable<IGesture> gestures;
                                    Archive.LoadFromArchive(file, out applications, out gestures);

                                    if (applications != null)
                                        newApps.AddRange(applications);
                                    if (gestures != null)
                                    {
                                        foreach (var gesture in gestures)
                                        {
                                            if (newGestures.Find(g => g.Name == gesture.Name) == null)
                                                newGestures.Add(gesture);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
                catch (Exception exception)
                {
                    UIHelper.GetParentWindow(this).ShowModalMessageExternal(exception.GetType().Name, exception.Message);
                }
                if (newApps.Count != 0)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ExportImportDialog exportImportDialog = new ExportImportDialog(false, false, newApps, newGestures);
                        exportImportDialog.ShowDialog();
                    }, DispatcherPriority.Background);
                }
            }
            e.Handled = true;
        }
    }
}
