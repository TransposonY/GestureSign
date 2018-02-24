using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.KeyDownKeyUp
{
    class KeyDownKeyUpPlugin : IPlugin
    {
        #region Private Variables

        private KeyDownKeyUpUI _GUI = null;
        private KeyDownKeyUpSettings _settings = null;

        #endregion

        #region PInvoke Declarations

        private const string User32 = "user32.dll";

        [DllImport(User32)]
        private static extern int GetKeyNameText(int lParam, [Out] StringBuilder lpString, int nSize);

        [DllImport(User32)]
        private static extern int MapVirtualKey(int uCode, int uMapType);
        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.KeyDownKeyUp.Name"); }
        }

        public string Description
        {
            get { return GetDescription(); }
        }

        public object GUI
        {
            get { return _GUI ?? (_GUI = CreateGui()); }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public KeyDownKeyUpUI TypedGUI
        {
            get { return (KeyDownKeyUpUI)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.KeyDownKeyUp.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object Icon => IconSource.Keyboard;

        #endregion

        #region Public Methods
        public static string GetKeyName(Keys key)
        {
            bool extended;
            switch (key)
            {
                case Keys.Insert:
                case Keys.Delete:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    extended = true;
                    break;
                default:
                    extended = false;
                    break;
            }
            StringBuilder sb = new StringBuilder(64);
            int scancode = MapVirtualKey((int)key, 0);
            if (extended)
                scancode += 0x100;
            GetKeyNameText(scancode << 16, sb, sb.Capacity);
            if (sb.Length == 0)
            {
                switch (key)
                {
                    case Keys.BrowserBack:
                        sb.Append("Back");
                        break;
                    case Keys.BrowserForward:
                        sb.Append("Forward");
                        break;
                    case (Keys)19:
                        sb.Append("Break");
                        break;
                    case Keys.Apps:
                        sb.Append("ContextMenu");
                        break;
                    case Keys.LWin:
                    case Keys.RWin:
                        sb.Append("Windows");
                        break;
                    case Keys.PrintScreen:
                        sb.Append("PrintScreen");
                        break;
                }
            }
            return sb.ToString();
        }
        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            if (_settings == null) return false;

            InputSimulator simulator = new InputSimulator();

            if (_settings.KeyCode != null)
                foreach (var k in _settings.KeyCode)
                {
                    if (!Enum.IsDefined(typeof(VirtualKeyCode), k.GetHashCode())) continue;

                    var key = (VirtualKeyCode)k;
                    if (_settings.IsKeyDown)
                    {
                        try
                        {
                            simulator.Keyboard.KeyDown(key).Sleep(30);
                        }
                        catch
                        {
                            KeyboardHelper.ResetKeyState(ActionPoint.Window, k);
                            return false;
                        }
                    }
                    else simulator.Keyboard.KeyUp(key).Sleep(30);
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
                _settings = new KeyDownKeyUpSettings();

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Methods

        private KeyDownKeyUpUI CreateGui()
        {
            KeyDownKeyUpUI newGui = new KeyDownKeyUpUI();

            newGui.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _settings;
            };

            return newGui;
        }

        private string GetDescription()
        {
            if (_settings == null || _settings.KeyCode == null)
                return Name;

            // Create string to store key combination and final output description
            string strKeyCombo = "";
            string strFormattedOutput = _settings.IsKeyDown
                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.KeyDownKeyUp.Description.KeyDown")
                : LocalizationProvider.Instance.GetTextValue("CorePlugins.KeyDownKeyUp.Description.KeyUp");


            if (_settings.KeyCode.Count != 0)
            {
                strKeyCombo = _settings.KeyCode.Aggregate(strKeyCombo, (current, k) => current + (GetKeyName(k) + " + "));
            }
            strKeyCombo = strKeyCombo.TrimEnd(' ', '+');

            // Return final formatted string
            return String.Format(strFormattedOutput, strKeyCombo);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
