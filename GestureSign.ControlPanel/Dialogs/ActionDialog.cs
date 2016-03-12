using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestureSign.Common.Applications;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    public partial class ActionDialog : MetroWindow
    {
        public ActionDialog()
        {
            InitializeComponent();
        }

        //Add action by new gesture
        public ActionDialog(string currentGestureName) : this()
        {
            _gestureName = currentGestureName;
        }

        //Add action by existing gesture
        public ActionDialog(string currentGestureName, IApplication selectedApplication) : this(currentGestureName)
        {
            _selectedApplication = selectedApplication;
        }

        //Edit action
        public ActionDialog(IAction selectedAction, IApplication selectedApplication) : this()
        {
            Title = LocalizationProvider.Instance.GetTextValue("ActionDialog.EditActionTitle");
            _selectedApplication = selectedApplication;
            _currentAction = selectedAction;
            if (_currentAction != null)
            {
                _gestureName = _currentAction.GestureName;
            }
        }

        #region Private Instance Fields

        private readonly IApplication _selectedApplication;
        // Create variable to hold current selected plugin
        IPluginInfo _pluginInfo;
        IAction _currentAction;
        string _gestureName;

        #endregion

        #region Public Instance Fields

        public static event EventHandler<ActionChangedEventArgs> ActionsChanged;

        #endregion


        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BindExistingApplications();
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

        private void availableGesturesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            SelectCurrentGesture(_gestureName);
        }

        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAction())
            {
                Close();
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

        private void SelectCurrentGesture(string currentGesture)
        {
            if (currentGesture == null) return;

            foreach (GestureItem item in availableGesturesComboBox.Items)
            {
                if (item.Name == currentGesture)
                    availableGesturesComboBox.SelectedIndex = availableGesturesComboBox.Items.IndexOf(item);
            }
        }

        #endregion

        private bool SaveAction()
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
                // Save new action to specific application
                ApplicationManager.Instance.CurrentApplication.AddAction(_currentAction);
            }
            else
            {
                if (cmbExistingApplication.SelectedItem as IApplication != _selectedApplication)
                {
                    _selectedApplication.RemoveAction(_currentAction);
                    ((IApplication)cmbExistingApplication.SelectedItem).AddAction(_currentAction);
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
            if (selectedItem != null && (((IApplication)selectedItem).Actions.Exists(a => a.Name.Equals(actionName))))
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
    }
}
