using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    public partial class ActionDialog : MetroWindow
    {
        public ActionDialog()
        {
            InitializeComponent();
        }

        //Edit action
        public ActionDialog(IAction selectedAction, IApplication selectedApplication) : this()
        {
            _selectedApplication = selectedApplication;
            _currentAction = selectedAction;
        }

        #region Private Instance Fields

        private readonly IApplication _selectedApplication;
        // Create variable to hold current selected plugin
        IPluginInfo _pluginInfo;
        IAction _currentAction;

        #endregion

        #region Public Instance Fields

        public static event EventHandler<ActionChangedEventArgs> ActionsChanged;

        #endregion

        #region Dependency Properties

        public IGesture CurrentGesture
        {
            get { return (IGesture)GetValue(CurrentGestureProperty); }
            set { SetValue(CurrentGestureProperty, value); }
        }

        public static readonly DependencyProperty CurrentGestureProperty =
            DependencyProperty.Register(nameof(CurrentGesture), typeof(IGesture), typeof(ActionDialog), new FrameworkPropertyMetadata(new Gesture()));


        #endregion

        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BindPlugins();

            if (_currentAction != null)
            {
                foreach (object comboItem in cmbPlugins.Items)
                {
                    IPluginInfo pluginInfo = (IPluginInfo)comboItem;

                    if (pluginInfo.Class == _currentAction.PluginClass && pluginInfo.Filename == _currentAction.PluginFilename)
                    {
                        cmbPlugins.SelectedIndex = cmbPlugins.Items.IndexOf(comboItem);
                        break;
                    }
                }
                ConditionTextBox.Text = _currentAction.Condition;

                var gesture = GestureManager.Instance.GetNewestGestureSample(_currentAction.GestureName);
                if (gesture != null)
                    CurrentGesture = gesture;
            }
        }

        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveAction())
            {
                Close();
            }
        }

        private void cmbPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cmbPlugins.SelectedItem == null) return;
            LoadPlugin((IPluginInfo)cmbPlugins.SelectedItem);
        }

        private void ConditionTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            EditConditionDialog editConditionDialog = new EditConditionDialog(ConditionTextBox.Text);
            if (editConditionDialog.ShowDialog().Value)
            {
                ConditionTextBox.Text = editConditionDialog.ConditionTextBox.Text;
            }
        }

        private void GestureButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            GestureDefinition gestureDialog = new GestureDefinition();
            var result = gestureDialog.ShowDialog();

            if (result != null && result.Value)
            {
                CurrentGesture = GestureManager.Instance.GetNewestGestureSample(GestureManager.Instance.GestureName);
            }
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

        #endregion

        private bool SaveAction()
        {
            ApplicationManager.Instance.CurrentApplication = _selectedApplication;

            if (CurrentGesture == null)
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoSelectedGestureTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoSelectedGesture"));

            string newActionName = TxtActionName.Text.Trim();
            if (String.IsNullOrEmpty(newActionName))
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoActionNameTitle"),
                        LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.NoActionName"));

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

            try
            {
                var regex = new Regex("finger_[0-9]+_start_[XY]?|finger_[0-9]+_end_[XY]?|finger_[0-9]+_ID");
                var replaced = regex.Replace(ConditionTextBox.Text, "10");

                DataTable dataTable = new DataTable();
                dataTable.Compute(replaced, null);
            }
            catch (Exception exception)
            {
                return ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ConditionError"), exception.Message);
            }

            // Store new values
            _currentAction.GestureName = CurrentGesture?.Name ?? string.Empty;
            _currentAction.Name = newActionName;
            _currentAction.PluginClass = _pluginInfo.Class;
            _currentAction.PluginFilename = _pluginInfo.Filename;
            _currentAction.ActionSettings = _pluginInfo.Plugin.Serialize();
            _currentAction.Condition = ConditionTextBox.Text;
            _currentAction.IsEnabled = true;

            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
            ActionsChanged?.Invoke(this, new ActionChangedEventArgs(ApplicationManager.Instance.CurrentApplication, _currentAction));

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
                TxtActionName.Text = ApplicationManager.GetNextActionName(_pluginInfo.Plugin.Name, _selectedApplication);
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

        private void ShowSettings(IPluginInfo pluginInfo)
        {
            var pluginGui = pluginInfo.Plugin.GUI as UserControl;
            if (pluginGui?.Parent != null)
            {
                SettingsContent.Content = null;
            }
            // Add settings to settings panel
            SettingsContent.Content = pluginGui;

            SettingsContent.Height = pluginGui.Height;
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
