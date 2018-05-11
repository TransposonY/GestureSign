using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

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
                process.StartInfo.FileName = ExpandEnvironmentVariables(_settings.Path, pointInfo);
                process.StartInfo.Arguments = ExpandEnvironmentVariables(_settings.Variables, pointInfo);
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

        private string ExpandEnvironmentVariables(string command, PointInfo pointInfo)
        {
            if (string.IsNullOrWhiteSpace(command)) return command;

            command = Environment.ExpandEnvironmentVariables(command);

            if (command.Contains("%GS_Clipboard%"))
            {
                string clipboardString = string.Empty;
                pointInfo.Invoke(() =>
                {
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData != null && iData.GetDataPresent(DataFormats.Text))
                    {
                        clipboardString = (string)iData.GetData(DataFormats.Text);
                    }
                });
                command = command.Replace("%GS_Clipboard%", clipboardString);
            }

            if (command.Contains("%GS_ClassName%"))
            {
                command = command.Replace("%GS_ClassName%", pointInfo.Window.ClassName);
            }
            if (command.Contains("%GS_Title%"))
            {
                command = command.Replace("%GS_Title%", pointInfo.Window.Title);
            }
            if (command.Contains("%GS_PID%"))
            {
                command = command.Replace("%GS_PID%", pointInfo.Window.ProcessId.ToString());
            }

            return command.Replace("%GS_StartPoint_X%", pointInfo.PointLocation.First().X.ToString()).
              Replace("%GS_StartPoint_Y%", pointInfo.PointLocation.First().Y.ToString()).
              Replace("%GS_EndPoint_X%", pointInfo.Points[0].Last().X.ToString()).
              Replace("%GS_EndPoint_Y%", pointInfo.Points[0].Last().Y.ToString()).
              Replace("%GS_WindowHandle%", pointInfo.WindowHandle.ToString());
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}