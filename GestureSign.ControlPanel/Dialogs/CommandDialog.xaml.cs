using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestureSign.Common.Applications;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    public partial class CommandDialog : MetroWindow
    {
        protected CommandDialog()
        {
            InitializeComponent();
        }

        //Edit action
        public CommandDialog(ICommand selectedCommand, IAction selectedAction) : this()
        {
            _selectedAction = selectedAction;
            _currentCommand = selectedCommand;
        }

        #region Private Instance Fields

        private readonly IAction _selectedAction;
        // Create variable to hold current selected plugin
        IPluginInfo _pluginInfo;
        private readonly ICommand _currentCommand;

        #endregion

        #region Public Instance Fields

        public ICommand CurrentCommand => _currentCommand;

        #endregion

        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BindPlugins();

            if (_currentCommand != null)
            {
                foreach (object comboItem in cmbPlugins.Items)
                {
                    IPluginInfo pluginInfo = (IPluginInfo)comboItem;

                    if (pluginInfo.Class == _currentCommand.PluginClass && pluginInfo.Filename == _currentCommand.PluginFilename)
                    {
                        cmbPlugins.SelectedIndex = cmbPlugins.Items.IndexOf(comboItem);
                        break;
                    }
                }
            }
        }

        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveCommand())
            {
                if (!DialogResult.GetValueOrDefault())
                    DialogResult = true;
                Close();
            }
        }

        private void cmbPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cmbPlugins.SelectedItem == null) return;
            LoadPlugin((IPluginInfo)cmbPlugins.SelectedItem);
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

        private bool SaveCommand()
        {
            string newCommandName = CommandNameTextBox.Text.Trim();
            if (String.IsNullOrEmpty(newCommandName))
                return
                    ShowErrorMessage(
                        LocalizationProvider.Instance.GetTextValue("CommandDialog.Messages.NoCommandNameTitle"),
                        LocalizationProvider.Instance.GetTextValue("CommandDialog.Messages.NoCommandName"));


            // Store new values
            _currentCommand.Name = newCommandName;
            _currentCommand.PluginClass = _pluginInfo.Class;
            _currentCommand.PluginFilename = _pluginInfo.Filename;
            _currentCommand.CommandSettings = _pluginInfo.Plugin.Serialize();
            _currentCommand.IsEnabled = true;

            if (string.IsNullOrWhiteSpace(_selectedAction.Name))
            {
                _selectedAction.Name = newCommandName;
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
            _pluginInfo = selectedPlugin;

            // Set action name
            if (IsPluginMatch(_currentCommand, selectedPlugin.Class, selectedPlugin.Filename))
            {
                CommandNameTextBox.Text = _currentCommand.Name;
                // Load action settings or no settings
                _pluginInfo.Plugin.Deserialize(_currentCommand.CommandSettings);
            }
            else
            {
                CommandNameTextBox.Text = ApplicationManager.GetNextCommandName(_pluginInfo.Plugin.Name, _selectedAction);
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

        private bool IsPluginMatch(ICommand command, string PluginClass, string PluginFilename)
        {
            return (command != null && command.PluginClass == PluginClass && command.PluginFilename == PluginFilename);
        }
    }
}
