using System;
using System.Diagnostics;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.CorePlugins.Common;

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
            get { return string.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.RunCommand.Description"), _Settings.Command); }
        }

        public object GUI
        {
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
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

        public object Icon => IconSource.Command;

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo pointInfo)
        {
            if (_Settings == null) return false;

            string clipboardString = string.Empty;
            var parser = new EnvironmentVariablesParser(pointInfo);
            string command = parser.ExpandEnvironmentVariables(_Settings.Command);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"{(_Settings.ShowCmd ? "/K " : "/C ")}\"{string.Join(" & ", command.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))}\"";
                process.StartInfo.WindowStyle = _Settings.ShowCmd ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = !_Settings.ShowCmd;
                process.StartInfo.UseShellExecute = false;
                process.Start();
            }

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

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}