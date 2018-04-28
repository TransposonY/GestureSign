using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
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

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"{(_Settings.ShowCmd ? "/K " : "/C ")}\"{string.Join(" & ", _Settings.Command.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))}\"";
                process.StartInfo.WindowStyle = _Settings.ShowCmd ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = !_Settings.ShowCmd;
                process.StartInfo.UseShellExecute = false;

                process.StartInfo.EnvironmentVariables.Add("GS_StartPoint_X", pointInfo.PointLocation.First().X.ToString());
                process.StartInfo.EnvironmentVariables.Add("GS_StartPoint_Y", pointInfo.PointLocation.First().Y.ToString());
                process.StartInfo.EnvironmentVariables.Add("GS_EndPoint_X", pointInfo.Points[0].Last().X.ToString());
                process.StartInfo.EnvironmentVariables.Add("GS_EndPoint_Y", pointInfo.Points[0].Last().Y.ToString());
                process.StartInfo.EnvironmentVariables.Add("GS_Title", pointInfo.Window.Title);
                process.StartInfo.EnvironmentVariables.Add("GS_PID", pointInfo.Window.ProcessId.ToString());
                process.StartInfo.EnvironmentVariables.Add("GS_WindowHandle", pointInfo.WindowHandle.ToString());
                if (_Settings.Command.Contains("GS_ClassName"))
                {
                    process.StartInfo.EnvironmentVariables.Add("GS_ClassName", pointInfo.Window.ClassName);
                }
                if (_Settings.Command.Contains("GS_Clipboard"))
                {
                    pointInfo.Invoke(() =>
                    {
                        IDataObject iData = Clipboard.GetDataObject();
                        if (iData != null && iData.GetDataPresent(DataFormats.Text))
                        {
                            clipboardString = (string)iData.GetData(DataFormats.Text);
                        }
                    });
                    process.StartInfo.EnvironmentVariables.Add("GS_Clipboard", clipboardString);
                }
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