using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using GestureSign.Common.Plugins;

using System.Windows.Controls;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeyPlugin : IPlugin
    {
        #region Private Variables

        private HotKey _GUI = null;
        private HotKeySettings _Settings = null;

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
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
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

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            if (ActionPoint.WindowHandle.ToInt64() != ManagedWinapi.Windows.SystemWindow.ForegroundWindow.HWnd.ToInt64())
                ManagedWinapi.Windows.SystemWindow.ForegroundWindow = ActionPoint.Window;

            SendShortcutKeys(_Settings);

            return true;
        }

        public void Deserialize(string SerializedData)
        {
            // Clear existing settings if nothing was passed in
            if (String.IsNullOrEmpty(SerializedData))
            {
                _Settings = new HotKeySettings();
                return;
            }

            // Create memory stream from serialized data string
            MemoryStream memStream = new MemoryStream(Encoding.Default.GetBytes(SerializedData));

            // Create json serializer to deserialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(HotKeySettings));

            // Deserialize json file into actions list
            _Settings = jSerial.ReadObject(memStream) as HotKeySettings;

            if (_Settings == null)
                _Settings = new HotKeySettings();
        }

        public string Serialize()
        {
            _Settings = _GUI.Settings;

            if (_Settings == null)
                _Settings = new HotKeySettings();

            // Create json serializer to serialize json file
            DataContractJsonSerializer jSerial = new DataContractJsonSerializer(typeof(HotKeySettings));

            // Open json file
            MemoryStream mStream = new MemoryStream();
            StreamWriter sWrite = new StreamWriter(mStream);

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
            if (Settings == null)
                return "发送快捷键组合到程序";

            // Create string to store key combination and final output description
            string strKeyCombo = "";
            string strFormattedOutput = "发送快捷键 ({0}) 到程序";

            // Build output string
            if (Settings.Windows)
                strKeyCombo = "Win + ";

            if (Settings.Control)
                strKeyCombo += "Ctrl + ";

            if (Settings.Alt)
                strKeyCombo += "Alt + ";

            if (Settings.Shift)
                strKeyCombo += "Shift + ";

            strKeyCombo += Settings.KeyCode.ToString();

            // Return final formatted string
            return String.Format(strFormattedOutput, strKeyCombo);
        }

        private void SendShortcutKeys(HotKeySettings Settings)
        {
            if (Settings == null)
                return;

            // Create keyboard keys to represent hot key combinations
            ManagedWinapi.KeyboardKey winKey = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.LWin);
            ManagedWinapi.KeyboardKey controlKey = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.LControlKey);
            ManagedWinapi.KeyboardKey altKey = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.LMenu);
            ManagedWinapi.KeyboardKey shiftKey = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.LShiftKey);
            ManagedWinapi.KeyboardKey modifierKey = new ManagedWinapi.KeyboardKey(Settings.KeyCode);

            // Deceide which keys to press
            // Windows
            if (Settings.Windows)
                winKey.Press();

            // Control
            if (Settings.Control)
                controlKey.Press();

            // Alt
            if (Settings.Alt)
                altKey.Press();

            // Shift
            if (Settings.Shift)
                shiftKey.Press();

            // Modifier
            if (!String.IsNullOrEmpty(modifierKey.KeyName))
                modifierKey.PressAndRelease();

            // Release Shift
            if (Settings.Shift)
                shiftKey.Release();

            // Release Alt
            if (Settings.Alt)
                altKey.Release();

            // Release Control
            if (Settings.Control)
                controlKey.Release();

            // Release Windows
            if (Settings.Windows)
                winKey.Release();
        }

        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}