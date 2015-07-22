using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.RunCommand
{
    public class RunCommandPlugin : IPlugin
    {
        #region Private Variables

        private RunCommand _GUI = null;
        private RunCommandSettings _Settings = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.RunCommand.Name"); }
        }

        public string Description
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.RunCommand.Description"); }
        }

        public UserControl GUI
        {
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
        }

        public RunCommand TypedGUI
        {
            get { return (RunCommand)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.RunCommand.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            Thread newThread = new Thread(new ParameterizedThreadStart(ExecuteCommand));
            newThread.Start(_Settings);

            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return PluginHelper.DeserializeSettings(SerializedData, out _Settings);
        }

        public string Serialize()
        {
            if (_GUI != null)
                _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new RunCommandSettings();

            return PluginHelper.SerializeSettings(_Settings);
        }

        #endregion

        #region Private Methods

        private RunCommand CreateGUI()
        {
            RunCommand newGUI = new RunCommand() { Settings = _Settings };
            newGUI.Loaded += (o, e) => { TypedGUI.Settings = _Settings; };

            return newGUI;
        }

        private void ExecuteCommand(object Settings)
        {
            // Cast object parameter as RunCommandSettings object
            RunCommandSettings rcSettings = Settings as RunCommandSettings;
            if (rcSettings == null) return;

            // Catch any errors (i.e. bad command, bad filename, bad anything)
            try
            {
                Process Process = new Process();
                // Expand environment variable to support %SYSTEMROOT%, etc.
                Process.StartInfo.FileName = "cmd.exe";
                Process.StartInfo.Arguments = (rcSettings.ShowCmd ? "/K " : "/C ") + string.Join(" & ", rcSettings.Command.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
                Process.StartInfo.WindowStyle = rcSettings.ShowCmd ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
                Process.Start();
            }
            catch
            {
                // Errors are stupid
            }
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}