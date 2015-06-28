using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using GestureSign.Common.Plugins;
using System.Threading;

using System.Windows.Controls;
using GestureSign.CorePlugins.HotKey;
using Newtonsoft.Json;

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
            get { return "命令或程序"; }
        }

        public string Description
        {
            get { return "运行指定的命令或程序"; }
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
            get { return "运行"; }
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
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(SerializedData))
            {
                _Settings = new RunCommandSettings();
                return true;
            }
            try
            {
                _Settings = JsonConvert.DeserializeObject<RunCommandSettings>(SerializedData) ?? new RunCommandSettings();
            }
            catch (Exception e)
            {
                _Settings = new RunCommandSettings();
                Console.WriteLine(e.Message);
                return false;
            }
            if (_Settings == null)
                _Settings = new RunCommandSettings();
            return true;
        }

        public string Serialize()
        {
            if (_GUI != null)
                _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new RunCommandSettings();

            return JsonConvert.SerializeObject(_Settings);
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