using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public partial class AvailableActions : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
            nameof(SearchText), typeof(string), typeof(AvailableActions),
            new PropertyMetadata(string.Empty, (d, e) => ((AvailableActions)d).OnSearchTextChanged((string)e.NewValue)));

        // public static event EventHandler StartCapture;
        public AvailableActions()
        {
            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _searchTimer.Tick += SearchTimer_Tick;
            InitializeComponent();
            DataContext = this;
            _initializing = false;
            UpdateSelectedApplication();
        }

        private IApplication _cutActionSource;
        private readonly List<CommandInfo> _commandClipboard = new List<CommandInfo>();
        private readonly DispatcherTimer _searchTimer;
        private string[] _searchTokens = ActionListFilter.EmptyTokens;
        private int _fingerFilter;
        private bool _initializing = true;
        private bool _statusUpdatePending;
        private bool _synchronizingToggleAllActions;
        private bool _updatingAllActions;
        private string _statusActionCountText;
        private string _statusFingersText;
        private string _statusApplicationText;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Text typed in the window's search box; filters the action list.</summary>
        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public string StatusActionCountText
        {
            get { return _statusActionCountText; }
            private set { SetStatus(ref _statusActionCountText, value, nameof(StatusActionCountText)); }
        }

        public string StatusFingersText
        {
            get { return _statusFingersText; }
            private set { SetStatus(ref _statusFingersText, value, nameof(StatusFingersText)); }
        }

        public string StatusApplicationText
        {
            get { return _statusApplicationText; }
            private set { SetStatus(ref _statusApplicationText, value, nameof(StatusApplicationText)); }
        }

        private void SetStatus(ref string field, string value, string propertyName)
        {
            if (field == value) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UserControl_Initialized(object sender, EventArgs eArgs)
        {
            ApplicationManager.Instance.CollectionChanged += (o, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0 && !(e.NewItems[0] is IgnoredApp))
                    lstAvailableApplication.SelectedItem = (IApplication)e.NewItems[0];
            };

            // The count in the header and status bar follows whatever the filter currently lets through.
            ((INotifyCollectionChanged)lstAvailableActions.Items).CollectionChanged += (o, e) => ScheduleStatusUpdate();

            // Open on the first application (Global Actions) so the table is never empty by default.
            // Applications load asynchronously, so also watch for them arriving.
            ApplicationItemProvider.ApplicationItems.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    Dispatcher.InvokeAsync(SelectFirstApplication, DispatcherPriority.Background);
            };

            // Saved gestures, applications and continuous gestures change titles and finger counts; re-key and re-filter.
            GestureItemProvider.GestureMapChanged += (o, e) =>
            {
                var commandInfoProvider = ((ObjectDataProvider)Resources["CommandInfoProvider"]).ObjectInstance as CommandInfoProvider;
                if (commandInfoProvider == null) return;
                commandInfoProvider.RefreshGestureInfo();
                if (_fingerFilter > ActionListFilter.AllFingers || _searchTokens.Length != 0)
                    RefreshActionFilter();
            };
            Loaded += (o, e) => SelectFirstApplication();
        }

        private void SelectFirstApplication()
        {
            if (lstAvailableApplication.SelectedItem == null && lstAvailableApplication.Items.Count > 0)
                lstAvailableApplication.SelectedIndex = 0;
        }

        private void ScheduleStatusUpdate()
        {
            if (_statusUpdatePending) return;
            _statusUpdatePending = true;
            Dispatcher.InvokeAsync(() =>
            {
                _statusUpdatePending = false;
                UpdateStatus();
            }, DispatcherPriority.Background);
        }

        private void UpdateStatus()
        {
            var localization = LocalizationProvider.Instance;

            int actionCount = lstAvailableActions?.Items.Groups?.Count ?? 0;
            StatusActionCountText = string.Format(localization.GetTextValue("Action.ActionsSummary"), actionCount);

            string fingers = _fingerFilter <= ActionListFilter.AllFingers
                ? localization.GetTextValue("Action.AllFingers")
                : _fingerFilter >= ActionListFilter.MinimumFingerCount
                    ? localization.GetTextValue("Action.SixOrMoreFingers")
                    : _fingerFilter.ToString();
            StatusFingersText = string.Format(localization.GetTextValue("Action.FingersStatus"), fingers);

            StatusApplicationText = (lstAvailableApplication?.SelectedItem as IApplication)?.Name;
            SynchronizeToggleAllActions();
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

        private void UpdateHeaderScrollSpacer(ScrollViewer scrollViewer)
        {
            double width = 0;
            if (scrollViewer != null && scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            {
                var bar = scrollViewer.Template?.FindName("PART_VerticalScrollBar", scrollViewer) as FrameworkElement;
                width = bar != null && bar.ActualWidth > 0 ? bar.ActualWidth : SystemParameters.VerticalScrollBarWidth;
            }
            HeaderScrollSpacer.Width = new GridLength(width);
        }

        private void LstAvailableActions_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateHeaderScrollSpacer(e.OriginalSource as ScrollViewer);

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
            if (_initializing) return;
            var toggleSwitch = (ToggleSwitch)sender;
            var info = toggleSwitch.DataContext as CommandInfo;
            if (info?.Command == null || info.Command.IsEnabled == toggleSwitch.IsOn) return;
            info.Command.IsEnabled = toggleSwitch.IsOn;
            info.IsEnabled = toggleSwitch.IsOn;
            ApplicationManager.Instance.SaveApplications();
            SynchronizeToggleAllActions();
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
                ShowAllActions();
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
                element.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
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
            ShowAllActions();

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
            ShowAllActions();

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
            var selectedApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (selectedApplication == null)
            {
                selectedApplication = ApplicationManager.Instance.GetAvailableUserApplications().FirstOrDefault(app => app.Actions.Contains(sourceAction));
                if (selectedApplication == null)
                    return;
            }
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
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void ExportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExportImportDialog exportImportDialog = new ExportImportDialog(true, false, ApplicationManager.Instance.Applications, GestureSign.Common.Gestures.GestureManager.Instance.Gestures, ContinuousGestureManager.Instance.ContinuousGestures);
            exportImportDialog.ShowDialog();
        }

        private void lstAvailableApplication_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteActionMenuItem2.IsEnabled = _commandClipboard.Count != 0;

            DeleteMenuItem.IsEnabled = lstAvailableApplication.SelectedItem is UserApp;
        }

        private void LstAvailableActions_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            PasteToNewActionMenuItem.Visibility = PasteToSelectedActionMenuItem.Visibility = _commandClipboard.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
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

        private void PasteToNewActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_commandClipboard.Count == 0) return;

            var targetApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (targetApplication == null) return;
            ShowAllActions();

            lstAvailableActions.SelectedItem = null;
            foreach (var actionGroup in _commandClipboard.GroupBy(ci => ci.Action))
            {
                var sourceAction = (GestureSign.Common.Applications.Action)actionGroup.Key;
                var newAction = sourceAction.DeepCopy();
                newAction.Commands = new List<ICommand>();

                targetApplication.AddAction(newAction);

                foreach (var info in actionGroup)
                {
                    if (_cutActionSource != null)
                    {
                        info.Action.RemoveCommand(info.Command);
                    }

                    var newCommand = ((Command)info.Command).Clone() as Command;
                    newCommand.Name = ApplicationManager.GetNextCommandName(newCommand.Name, info.Action);
                    newAction.AddCommand(newCommand);
                }
            }

            if (_cutActionSource != null)
            {
                _cutActionSource = null;
                _commandClipboard.Clear();
            }

            ApplicationManager.Instance.SaveApplications();
        }

        private void PasteToSelectedActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_commandClipboard.Count == 0) return;

            var targetApplication = lstAvailableApplication.SelectedItem as IApplication;
            if (targetApplication == null) return;
            var selectedCommand = lstAvailableActions.SelectedItem as CommandInfo;
            if (selectedCommand == null || selectedCommand.Action == null) return;
            ShowAllActions();
            lstAvailableActions.SelectedItem = null;

            IAction currentAction = selectedCommand.Action;
            foreach (var actionGroup in _commandClipboard.GroupBy(ci => ci.Action))
            {
                foreach (var info in actionGroup)
                {
                    if (_cutActionSource != null)
                    {
                        info.Action.RemoveCommand(info.Command);
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

        private void SortMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = (MenuItem)sender;
            if (!clickedMenuItem.IsChecked)
                clickedMenuItem.IsChecked = true;

            MenuItem parentMenuItem = clickedMenuItem.Parent as MenuItem;
            if (parentMenuItem != null)
                foreach (var item in parentMenuItem.Items)
                {
                    var current = item as MenuItem;
                    if (!ReferenceEquals(current, clickedMenuItem))
                        if (current != null)
                            current.IsChecked = false;
                }
        }

        private void SortMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            var lcv = lstAvailableActions?.ItemsSource as ListCollectionView;
            if (lcv == null) return;
            string propertyName = sender == SortByGestureMenuItem ? nameof(CommandInfo.GestureFeatures) : nameof(CommandInfo.ActionName);
            if (lcv.SortDescriptions.Any(sd => sd.PropertyName == propertyName))
                return;

            lcv.SortDescriptions.Clear();
            if (sender == SortByGestureMenuItem)
            {
                lcv.SortDescriptions.Add(new SortDescription(nameof(CommandInfo.PatternCount), ListSortDirection.Ascending));
            }
            lcv.SortDescriptions.Add(new SortDescription(".", ListSortDirection.Ascending));
            lcv.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
            lcv.Refresh();
        }

        private void lstAvailableApplication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || e.Source != lstAvailableApplication) return;
            UpdateSelectedApplication();
        }

        private void UpdateSelectedApplication()
        {
            IApplication selectedApp = lstAvailableApplication.SelectedItem as IApplication;
            EditApplicationButton.IsEnabled = selectedApp != null;
            DeleteApplicationButton.IsEnabled = selectedApp is UserApp;
            UpdateStatus();
            if (selectedApp == null) return;

            var commandInfoProvider = ((ObjectDataProvider)Resources["CommandInfoProvider"]).ObjectInstance as CommandInfoProvider;
            if (commandInfoProvider == null) return;
            commandInfoProvider.RefreshCommandInfos(selectedApp, lstAvailableActions);

            Dispatcher.InvokeAsync(() => lstAvailableApplication.ScrollIntoView(selectedApp), DispatcherPriority.Background);
        }

        private void SynchronizeToggleAllActions()
        {
            if (_updatingAllActions || ToggleAllActionsToggleSwitch == null) return;
            var selectedApp = lstAvailableApplication?.SelectedItem as IApplication;
            bool wasSynchronizing = _synchronizingToggleAllActions;
            _synchronizingToggleAllActions = true;
            try
            {
                ToggleAllActionsToggleSwitch.IsEnabled = selectedApp != null;
                ToggleAllActionsToggleSwitch.IsOn = selectedApp != null
                    && selectedApp.Actions.SelectMany(a => a.Commands).All(c => c.IsEnabled);
            }
            finally
            {
                _synchronizingToggleAllActions = wasSynchronizing;
            }
        }

        private void OnSearchTextChanged(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _searchTimer.Stop();
                SetSearchTokens(text);
                return;
            }

            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            SetSearchTokens(SearchText);
        }

        private void SetSearchTokens(string text)
        {
            var tokens = ActionListFilter.Tokenize(text);
            if (ActionListFilter.SameTokens(_searchTokens, tokens))
                return;
            _searchTokens = tokens;
            RefreshActionFilter();
        }

        private void FingerFilter_Checked(object sender, RoutedEventArgs e)
        {
            var radio = (RadioButton)sender;
            int value;
            _fingerFilter = int.TryParse(radio.Tag as string, out value) ? value : ActionListFilter.AllFingers;
            RefreshActionFilter();
            UpdateStatus();
        }

        private void ActionsViewSource_Filter(object sender, FilterEventArgs e)
        {
            var info = e.Item as CommandInfo;
            if (info == null)
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = ActionListFilter.MatchesFingerCount(
                    info.PatternCount,
                    ContinuousGestureManager.Instance.Find(info.Action?.ContinuousGestureName)?.ContactCount ?? 0,
                    _fingerFilter)
                && ActionListFilter.MatchesKey(info.SearchKey, _searchTokens);
        }

        private void RefreshActionFilter()
        {
            if (lstAvailableActions == null)
                return;
            var view = CollectionViewSource.GetDefaultView(lstAvailableActions.ItemsSource);
            view?.Refresh();
        }

        private void ShowAllActions()
        {
            if (AllFingersRadio != null && AllFingersRadio.IsChecked != true)
                AllFingersRadio.IsChecked = true;
            if (!string.IsNullOrEmpty(SearchText))
                SearchText = string.Empty;
        }

        private void NewApplicationButton_OnClick(object sender, RoutedEventArgs e)
        {
            ApplicationDialog applicationDialog = new ApplicationDialog(new UserApp(), true);
            applicationDialog.ShowDialog();
        }

        private void EditApplication_Click(object sender, RoutedEventArgs e)
        {
            EditApplication();
        }

        private void EditApplication()
        {
            var app = lstAvailableApplication.SelectedItem as IApplication;
            if (app != null)
            {
                ApplicationDialog applicationDialog = new ApplicationDialog(app);
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
            if (_initializing || _synchronizingToggleAllActions || _updatingAllActions) return;
            IApplication app = lstAvailableApplication?.SelectedItem as IApplication;
            if (app == null) return;
            bool isOn = ((ToggleSwitch)sender).IsOn;
            _updatingAllActions = true;
            try
            {
                foreach (var command in app.Actions.SelectMany(a => a.Commands))
                {
                    command.IsEnabled = isOn;
                }
                ApplicationManager.Instance.SaveApplications();

                var commandInfoProvider = ((ObjectDataProvider)Resources["CommandInfoProvider"]).ObjectInstance as CommandInfoProvider;
                if (commandInfoProvider != null)
                {
                    foreach (CommandInfo info in commandInfoProvider.CommandInfos)
                    {
                        info.IsEnabled = info.Command.IsEnabled;
                    }
                }
            }
            catch { }
            finally
            {
                _updatingAllActions = false;
                SynchronizeToggleAllActions();
            }
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
                var newContinuousGestures = new List<ContinuousGesture>();
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
                                    // Pre-catalog files embed continuous gestures in actions.
                                    ContinuousGestureCatalog.MigrateLegacy(apps, newContinuousGestures);
                                    newApps.AddRange(apps);
                                }
                                break;
                            case ".exe":
                                lstAvailableApplication.SelectedItem = ApplicationManager.Instance.AddApplication(new UserApp(), file);
                                break;
                            case ".lnk":
                                string targetPath = ShortcutHelper.GetTargetPath(file);
                                if (Path.GetExtension(targetPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    lstAvailableApplication.SelectedItem = ApplicationManager.Instance.AddApplication(new UserApp(), targetPath);
                                }
                                break;
                            case GestureSign.Common.Constants.ArchivesExtension:
                                {
                                    IEnumerable<IApplication> applications;
                                    IEnumerable<IGesture> gestures;
                                    List<ContinuousGesture> continuousGestures;
                                    Archive.LoadFromArchive(file, out applications, out gestures, out continuousGestures);
                                    ContinuousGestureCatalog.Merge(newContinuousGestures, continuousGestures, applications);

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
                        ExportImportDialog exportImportDialog = new ExportImportDialog(false, false, newApps, newGestures, newContinuousGestures);
                        exportImportDialog.ShowDialog();
                    }, DispatcherPriority.Background);
                }
            }
            e.Handled = true;
        }
    }
}
