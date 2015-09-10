using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace PluginTest
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<IPluginInfo> _plugins = new List<IPluginInfo>();
        private IPluginInfo _pluginInfo;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LocalizationProvider.Instance.LoadFromFile("", null);
            LoadPlugins();
            PluginsComboBox.ItemsSource = _plugins;
            PluginsComboBox.SelectedIndex = 0;
        }

        private void LoadPlugins()
        {
            var directoryPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            if (directoryPath == null) return;

            var extraPlugins = Path.Combine(directoryPath, "Plugins");
            if (Directory.Exists(extraPlugins))
                foreach (var sFilePath in Directory.GetFiles(extraPlugins, "*.dll"))
                {
                    _plugins.AddRange(LoadPluginsFromAssembly(sFilePath));
                }
        }

        private List<IPluginInfo> LoadPluginsFromAssembly(string assemblyLocation)
        {
            var retPlugins = new List<IPluginInfo>();

            //To avoid exception System.NotSupportedException
            var file = File.ReadAllBytes(assemblyLocation);
            var aPlugin = Assembly.Load(file);

            var tPluginTypes = aPlugin.GetTypes();

            foreach (var tPluginType in tPluginTypes)
                if (tPluginType.GetInterface("IPlugin") != null)
                {
                    var plugin = Activator.CreateInstance(tPluginType) as IPlugin;

                    // If we have a new instance of a plugin, initialize it and add it to return list
                    if (plugin != null)
                    {
                        plugin.HostControl = null;
                        plugin.Initialize();
                        retPlugins.Add(new PluginInfo(plugin, tPluginType.FullName, Path.GetFileName(assemblyLocation)));
                    }
                }

            return retPlugins;
        }

        private void PluginsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PluginsComboBox.SelectedItem == null) return;
            LoadPlugin((IPluginInfo) PluginsComboBox.SelectedItem);
        }

        private void LoadPlugin(IPluginInfo selectedPlugin)
        {
            // Try to load plugin, and set current plugin to newly selected plugin
            _pluginInfo = selectedPlugin;

            selectedPlugin.Plugin.Deserialize(string.Empty);

            // Does the plugin have a graphical interface
            if (selectedPlugin.Plugin.GUI != null)
                // Show plugins graphical interface
                ShowSettings(selectedPlugin);
            else
            // There is no interface for this plugin, hide settings but leave action name input box
                HideSettings();
        }

        private void ShowSettings(IPluginInfo pluginInfo)
        {
            var pluginGui = pluginInfo.Plugin.GUI;
            if (pluginGui.Parent != null)
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

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            PointInfo pointInfo = null;
            int delay;
            if (!int.TryParse(delayTextBox.Text, out delay)) delay = 100;

            Thread.Sleep(delay);

            var setting = _pluginInfo.Plugin.Serialize();
            if (_pluginInfo.Plugin.Deserialize(setting))
            {
                _pluginInfo.Plugin.Gestured(pointInfo);
            }
        }
    }
}