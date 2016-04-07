using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using ManagedWinapi;
using ManagedWinapi.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeyPlugin : IPlugin
    {
        #region Private Variables

        private HotKey _GUI;
        private HotKeySettings _Settings;
        private const string User32 = "user32.dll";

        #endregion

        #region PInvoke Declarations

        [DllImport(User32)]
        private static extern bool LockWorkStation();

        [DllImport(User32)]
        private static extern int GetKeyNameText(int lParam, [Out] StringBuilder lpString, int nSize);

        [DllImport(User32)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        #endregion


        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.Name"); }
        }

        public string Description
        {
            get { return GetDescription(_Settings); }
        }

        public UserControl GUI
        {
            get { return _GUI ?? (_GUI = CreateGUI()); }
        }

        public HotKey TypedGUI
        {
            get { return (HotKey)GUI; }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        #endregion

        #region Public Methods

        public static string GetKeyName(Keys key)
        {
            bool extended;
            switch (key)
            {
                case Keys.VolumeDown:
                case Keys.VolumeMute:
                case Keys.VolumeUp:
                case Keys.MediaNextTrack:
                case Keys.MediaPlayPause:
                case Keys.MediaPreviousTrack:
                case Keys.MediaStop:
                case Keys.BrowserBack:
                case Keys.BrowserForward:
                case Keys.BrowserHome:
                case Keys.BrowserRefresh:
                case Keys.BrowserSearch:
                case Keys.BrowserStop:
                    return key.ToString();
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
            if (ActionPoint.WindowHandle.ToInt64() != SystemWindow.ForegroundWindow.HWnd.ToInt64() &&
                ActionPoint.Window != null)
                SystemWindow.ForegroundWindow = ActionPoint.Window;
            try
            {
                SendShortcutKeys(_Settings);
            }
            catch (Exception exception)
            {
                throw new UnauthorizedAccessException(LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.UnauthorizedAccessException"), exception);
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
                _Settings = new HotKeySettings();

            return PluginHelper.SerializeSettings(_Settings);
        }

        #endregion

        #region Private Methods

        private HotKey CreateGUI()
        {
            HotKey newGUI = new HotKey();

            newGUI.Loaded += (o, e) =>
            {
                TypedGUI.Settings = _Settings;
                TypedGUI.HostControl = HostControl;
            };

            return newGUI;
        }

        public static string GetDescription(HotKeySettings Settings)
        {
            if (Settings == null || Settings.KeyCode == null)
                return LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.Description");

            // Create string to store key combination and final output description
            string strKeyCombo = "";
            string strFormattedOutput = LocalizationProvider.Instance.GetTextValue("CorePlugins.HotKey.SpecificDescription");

            // Build output string
            if (Settings.Windows)
                strKeyCombo = "Win + ";

            if (Settings.Control)
                strKeyCombo += "Ctrl + ";

            if (Settings.Alt)
                strKeyCombo += "Alt + ";

            if (Settings.Shift)
                strKeyCombo += "Shift + ";
            if (Settings.KeyCode.Count != 0)
            {
                foreach (var k in Settings.KeyCode)
                    strKeyCombo += GetKeyName(k) + " + ";
            }
            strKeyCombo = strKeyCombo.TrimEnd(' ', '+');

            // Return final formatted string
            return String.Format(strFormattedOutput, strKeyCombo);
        }

        private void SendShortcutKeys(HotKeySettings settings)
        {
            if (settings == null)
                return;
            if (settings.Windows &&
              settings.KeyCode.Count != 0 && settings.KeyCode[0] == Keys.L)
            {
                LockWorkStation();
                return;
            }

            if (settings.SendByKeybdEvent)
            {

                // Create keyboard keys to represent hot key combinations
                KeyboardKey winKey = new KeyboardKey(Keys.LWin);
                KeyboardKey controlKey = new KeyboardKey(Keys.LControlKey);
                KeyboardKey altKey = new KeyboardKey(Keys.LMenu);
                KeyboardKey shiftKey = new KeyboardKey(Keys.LShiftKey);

                // Deceide which keys to press
                // Windows
                if (settings.Windows)
                    winKey.Press();

                // Control
                if (settings.Control)
                    controlKey.Press();

                // Alt
                if (settings.Alt)
                    altKey.Press();

                // Shift
                if (settings.Shift)
                    shiftKey.Press();

                // Modifier
                if (settings.KeyCode != null)
                    foreach (var k in settings.KeyCode)
                    {
                        KeyboardKey modifierKey = new KeyboardKey(k);
                        if (!String.IsNullOrEmpty(modifierKey.KeyName))
                            modifierKey.PressAndRelease();
                    }
                // Release Shift
                if (settings.Shift)
                    shiftKey.Release();

                // Release Alt
                if (settings.Alt)
                    altKey.Release();

                // Release Control
                if (settings.Control)
                    controlKey.Release();

                // Release Windows
                if (settings.Windows)
                    winKey.Release();
            }
            else
            {

                InputSimulator simulator = new InputSimulator();

                // Deceide which keys to press
                // Windows
                if (settings.Windows)
                    simulator.Keyboard.KeyDown(VirtualKeyCode.LWIN);

                // Control
                if (settings.Control)
                    simulator.Keyboard.KeyDown(VirtualKeyCode.LCONTROL);

                // Alt
                if (settings.Alt)
                    simulator.Keyboard.KeyDown(VirtualKeyCode.LMENU);

                // Shift
                if (settings.Shift)
                    simulator.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);

                // Modifier
                if (settings.KeyCode != null)
                    foreach (var k in settings.KeyCode)
                    {
                        if (!Enum.IsDefined(typeof(VirtualKeyCode), k.GetHashCode())) continue;

                        var key = (VirtualKeyCode)k;
                        simulator.Keyboard.KeyPress(key).Sleep(30);
                    }
                // Release Shift
                if (settings.Shift)
                    simulator.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);

                // Release Alt
                if (settings.Alt)
                    simulator.Keyboard.KeyUp(VirtualKeyCode.LMENU);

                // Release Control
                if (settings.Control)
                    simulator.Keyboard.KeyUp(VirtualKeyCode.LCONTROL);

                // Release Windows
                if (settings.Windows)
                    simulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
            }
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}