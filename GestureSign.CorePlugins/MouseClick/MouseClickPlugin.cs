using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using GestureSign.Common.Plugins;
using System.Windows.Controls;

namespace GestureSign.CorePlugins.MouseClick
{
    public class MouseClickPlugin : IPlugin
    {
        #region Private Variables

        private MouseClickUI _gui = null;
        private MouseClickSettings _settings = null;

        #endregion

        #region PInvoke Declarations

        [DllImport("user32.dll")]
        private static extern void SetCursorPos(int x, int y);

        #endregion

        #region Public Properties

        public string Name
        {
            get { return "鼠标按键"; }
        }

        public string Description
        {
            get { return GetDescription(); }
        }

        public UserControl GUI
        {
            get { return _gui ?? (_gui = CreateGUI()); }
        }

        public MouseClickUI TypedGUI
        {
            get { return (MouseClickUI)GUI; }
        }

        public string Category
        {
            get { return "鼠标"; }
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

        public bool Gestured(PointInfo actionPoint)
        {
            if (_settings == null)
                return false;

            InputSimulator simulator = new InputSimulator();
            try
            {
                if (_settings.ClickPosition != ClickPositions.Original)
                {
                    switch (_settings.ClickPosition)
                    {
                        case ClickPositions.LastUp:
                            var lastUpPoint = actionPoint.Points.Last().Last();
                            SetCursorPos(lastUpPoint.X, lastUpPoint.Y);
                            break;
                        case ClickPositions.LastDown:
                            var lastDownPoint = actionPoint.Points.Last().First();
                            SetCursorPos(lastDownPoint.X, lastDownPoint.Y);
                            break;
                        case ClickPositions.FirstUp:
                            var firstUpPoint = actionPoint.Points.First().Last();
                            SetCursorPos(firstUpPoint.X, firstUpPoint.Y);
                            break;
                        case ClickPositions.FirstDown:
                            var firstDownPoint = actionPoint.Points.First().First();
                            SetCursorPos(firstDownPoint.X, firstDownPoint.Y);
                            break;
                    }
                }
                MethodInfo clickMethod = typeof(IMouseSimulator).GetMethod(_settings.MouseButtonAction.ToString());
                clickMethod.Invoke(simulator.Mouse, null);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return PluginHelper.DeserializeSettings(SerializedData, out _settings);
        }

        public string Serialize()
        {
            if (_gui != null)
                _settings = _gui.Settings;

            if (_settings == null)
                _settings = new MouseClickSettings();

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Methods

        private MouseClickUI CreateGUI()
        {
            MouseClickUI newGUI = new MouseClickUI();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _settings;
            };

            return newGUI;
        }

        private string GetDescription()
        {
            return String.Format("在 {0} {1}",
                ClickPositionDescription.DescriptionDict[_settings.ClickPosition],
                 MouseButtonActionDescription.DescriptionDict[_settings.MouseButtonAction]);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
