using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using GestureSign.Common.Plugins;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using UserControl = System.Windows.Controls.UserControl;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeyPlugin : IPlugin
    {
        #region Private Variables

        private HotKey _GUI = null;
        private HotKeySettings _Settings = null;
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
            get { return "发送快捷键"; }
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
            get { return "键盘"; }
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
            //if (ActionPoint.WindowHandle.ToInt64() != ManagedWinapi.Windows.SystemWindow.ForegroundWindow.HWnd.ToInt64() &&
            //    ActionPoint.Window != null)
            //    ManagedWinapi.Windows.SystemWindow.ForegroundWindow = ActionPoint.Window;

            SendShortcutKeys(_Settings);

            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(SerializedData))
            {
                _Settings = new HotKeySettings();
                return true;
            }

            // Create memory stream from serialized data string
            MemoryStream memStream = new MemoryStream(Encoding.Default.GetBytes(SerializedData));

            // Create json serializer to deserialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(HotKeySettings));
            try
            {
                // Deserialize json file into actions list
                _Settings = jSerial.ReadObject(memStream) as HotKeySettings;
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                LoadOldSetting(SerializedData);
                return false;
            }

            if (_Settings == null)
                _Settings = new HotKeySettings();
            return true;
        }

        private void LoadOldSetting(string SerializedData)
        {
            MemoryStream memStream = new MemoryStream(Encoding.Default.GetBytes(SerializedData));

            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(oldHotKeySettings));
            try
            {
                // Deserialize json file into actions list
                var oldSettings = jSerial.ReadObject(memStream) as oldHotKeySettings;
                _Settings = new HotKeySettings()
                {
                    Windows = oldSettings.Windows,
                    Alt = oldSettings.Alt,
                    Control = oldSettings.Control,
                    KeyCode = new List<System.Windows.Forms.Keys>(2),
                    Shift = oldSettings.Shift
                };
                _Settings.KeyCode.Add(oldSettings.KeyCode);
            }
            catch { _Settings = new HotKeySettings(); }
        }

        public string Serialize()
        {
            if (_GUI != null)
                _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new HotKeySettings();

            // Create json serializer to serialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(HotKeySettings));

            // Open json file
            MemoryStream mStream = new MemoryStream();

            // Serialize actions into json file
            jSerial.WriteObject(mStream, _Settings);

            return Encoding.Default.GetString(mStream.ToArray());
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

        private string GetDescription(HotKeySettings Settings)
        {
            if (Settings == null || Settings.KeyCode == null)
                return "发送快捷键组合到程序";

            // Create string to store key combination and final output description
            string strKeyCombo = "";
            const string strFormattedOutput = "发送快捷键 ({0}) 到程序";

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
            InputSimulator simulator = new InputSimulator();

            // Deceide which keys to press
            // Windows
            if (settings.Windows)
                simulator.Keyboard.KeyDown(VirtualKeyCode.LWIN);

            // Control
            if (settings.Control)
                simulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);

            // Alt
            if (settings.Alt)
                simulator.Keyboard.KeyDown(VirtualKeyCode.MENU);

            // Shift
            if (settings.Shift)
                simulator.Keyboard.KeyDown(VirtualKeyCode.SHIFT);

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
                simulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);

            // Release Alt
            if (settings.Alt)
                simulator.Keyboard.KeyUp(VirtualKeyCode.MENU);

            // Release Control
            if (settings.Control)
                simulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);

            // Release Windows
            if (settings.Windows)
                simulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}