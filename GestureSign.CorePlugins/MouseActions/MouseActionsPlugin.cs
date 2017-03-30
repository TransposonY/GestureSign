using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.MouseActions
{
    public class MouseActionsPlugin : IPlugin
    {
        #region Private Variables

        private MouseActionsUI _gui = null;
        private MouseActionsSettings _settings = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Name"); }
        }

        public string Description
        {
            get { return GetDescription(); }
        }

        public object GUI
        {
            get { return _gui ?? (_gui = CreateGUI()); }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public MouseActionsUI TypedGUI
        {
            get { return (MouseActionsUI)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Category"); }
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
                var referencePoint = GetReferencePoint(_settings.ClickPosition, actionPoint);
                switch (_settings.MouseAction)
                {
                    case MouseActions.HorizontalScroll:
                        simulator.Mouse.HorizontalScroll(_settings.ScrollAmount).Sleep(30);
                        return true;
                    case MouseActions.VerticalScroll:
                        simulator.Mouse.VerticalScroll(_settings.ScrollAmount).Sleep(30);
                        return true;
                    case MouseActions.MoveMouseTo:
                        MoveMouse(simulator, _settings.MovePoint);
                        return true;
                    case MouseActions.MoveMouseBy:
                        referencePoint.Offset(_settings.MovePoint);
                        MoveMouse(simulator, referencePoint);
                        break;
                    default:
                        {
                            if (_settings.ClickPosition != ClickPositions.Original)
                                MoveMouse(simulator, referencePoint);

                            MethodInfo clickMethod = typeof(IMouseSimulator).GetMethod(_settings.MouseAction.ToString());
                            clickMethod.Invoke(simulator.Mouse, null);
                            Thread.Sleep(30);
                            break;
                        }
                }
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
                _settings = new MouseActionsSettings();

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Methods

        private void MoveMouse(InputSimulator simulator, Point point)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            int realX = 0xffff * point.X / screenWidth;
            int realY = 0xffff * point.Y / screenHeight;

            simulator.Mouse.MoveMouseTo(realX, realY).Sleep(30);
        }

        private Point GetReferencePoint(ClickPositions position, PointInfo actionPoint)
        {
            Point referencePoint;
            switch (position)
            {
                case ClickPositions.LastUp:
                    referencePoint = actionPoint.Points.Last().Last();
                    break;
                case ClickPositions.LastDown:
                    referencePoint = actionPoint.Points.Last().First();
                    break;
                case ClickPositions.FirstUp:
                    referencePoint = actionPoint.Points.First().Last();
                    break;
                case ClickPositions.FirstDown:
                    referencePoint = actionPoint.Points.First().First();
                    break;
                default:
                    referencePoint = Point.Empty;
                    break;
            }
            return referencePoint;

        }

        private MouseActionsUI CreateGUI()
        {
            MouseActionsUI newGUI = new MouseActionsUI();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _settings;
            };

            return newGUI;
        }

        private string GetDescription()
        {
            switch (_settings.MouseAction)
            {
                case MouseActions.HorizontalScroll:
                    return
                        String.Format(
                            LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.HorizontalScroll"),
                            (_settings.ScrollAmount >= 0
                                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Right")
                                : LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Left")),
                            Math.Abs(_settings.ScrollAmount));
                case MouseActions.VerticalScroll:
                    return
                        String.Format(
                            LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.VerticalScroll"),
                            (_settings.ScrollAmount >= 0
                                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Up")
                                : LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Down")),
                            Math.Abs(_settings.ScrollAmount));
                case MouseActions.MoveMouseBy:
                    return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.MoveMouseBy") + _settings.MovePoint;
                case MouseActions.MoveMouseTo:
                    return LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.MoveMouseTo") + _settings.MovePoint;
            }

            return String.Format("{0} {1}",
                ClickPositionDescription.DescriptionDict[_settings.ClickPosition],
                 MouseActionDescription.DescriptionDict[_settings.MouseAction]);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
