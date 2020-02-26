using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.CorePlugins.Common;
using System.Diagnostics;

namespace GestureSign.CorePlugins.OpenFile
{
    public class OpenFilePlugin : IPlugin
    {
        #region Private Variables

        internal class OpenFileSetting
        {
            public string Path { get; set; }

            public string Variables { get; set; }
        }

        private OpenFileControl _GUI = null;
        private OpenFileSetting _settings = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenFile.Name"); }
        }

        public string Description
        {
            get { return string.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenFile.Description"), _settings.Path); }
        }

        public object GUI
        {
            get
            {
                if (_GUI == null)
                {
                    _GUI = new OpenFileControl() { Settings = _settings };
                    _GUI.Loaded += (o, e) => { TypedGUI.Settings = _settings; };
                }

                return _GUI;
            }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public OpenFileControl TypedGUI
        {
            get { return (OpenFileControl)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.OpenFile.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.Open;

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo pointInfo)
        {
            if (_settings == null) return false;

            using (Process process = new Process())
            {
                var parser = new EnvironmentVariablesParser(pointInfo);
                process.StartInfo.FileName = parser.ExpandEnvironmentVariables(_settings.Path);
                process.StartInfo.Arguments = parser.ExpandEnvironmentVariables(_settings.Variables);
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }

            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return PluginHelper.DeserializeSettings(SerializedData, out _settings);
        }

        public string Serialize()
        {
            if (_GUI != null)
                _settings = _GUI.Settings;

            if (_settings == null)
                _settings = new OpenFileSetting();

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Methods

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}