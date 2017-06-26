using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using ManagedWinapi.Windows;

namespace GestureSign.CorePlugins.SendMessage
{
    public class SendMessagePlugin : IPlugin
    {
        #region Private Variables

        private SendMessageView _gui;
        private SendMessageSetting _settings;
        private delegate bool CallBackEnumWindowsProc(IntPtr hWnd, int lParam);
        private bool _isFound;
        private const string User32 = "user32.dll";

        #endregion

        #region PInvoke Declarations

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(User32, SetLastError = true)]
        static extern bool PostMessage(HandleRef hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport(User32, CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);
        [DllImport(User32)]
        private static extern int EnumWindows(CallBackEnumWindowsProc ewp, int lParam);

        //[DllImport(User32)]
        //private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport(User32)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #endregion

        #region Public Properties
        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.Name"); }
        }

        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.Category"); }
        }

        public string Description
        {
            get { return GetDescription(); }
        }

        public bool IsAction
        {
            get { return true; }
        }

        public object GUI
        {
            get { return _gui ?? (_gui = CreateGUI()); }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public object Icon => IconSource.Window;

        #endregion

        #region Public Methods
        public void Initialize()
        {
        }

        public bool Gestured(PointInfo ActionPoint)
        {
            if (_settings.IsSpecificWindow)
            {
                if (_settings.IsRegEx)
                {
                    CallBackEnumWindowsProc ewp = EnumWindowsProc;
                    EnumWindows(ewp, 0);
                    return _isFound;
                }
                string className = String.IsNullOrWhiteSpace(_settings.ClassName) ? null : _settings.ClassName;
                string title = String.IsNullOrWhiteSpace(_settings.Title) ? null : _settings.Title;
                IntPtr hWnd = FindWindow(className, title);
                if (hWnd != IntPtr.Zero)
                {
                    Send(hWnd);
                    return true;
                }
            }
            else Send(ActionPoint.WindowHandle);
            return false;
        }

        public bool Deserialize(string serializedData)
        {
            return PluginHelper.DeserializeSettings(serializedData, out _settings);
        }

        public string Serialize()
        {
            if (_gui != null) _settings = _gui.Settings;
            if (_settings == null) _settings = new SendMessageSetting();

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Methods

        private SendMessageView CreateGUI()
        {
            SendMessageView newGUI = new SendMessageView();

            newGUI.Loaded += (o, e) =>
            {
                newGUI.Settings = _settings;
            };

            return newGUI;
        }

        private string GetDescription()
        {
            string descriptionTemplate = LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.DescriptionTemplate");
            if (_settings == null) return String.Format(descriptionTemplate,
                   LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.Name"),
                   LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.SpecificWindow"));

            var targetWindow = _settings.IsSpecificWindow
                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.SpecificWindow")
                : LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.CurrentWindow");

            var messageContent = _settings.HotKey == null
                ? LocalizationProvider.Instance.GetTextValue("CorePlugins.SendMessage.Name")
                : HotKey.HotKeyPlugin.GetDescription(_settings.HotKey);

            return String.Format(descriptionTemplate, messageContent, targetWindow);
        }

        private bool EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            try
            {
                var window = new SystemWindow(hWnd);

                if (
                    Regex.IsMatch(window.Title, _settings.Title,
                    RegexOptions.Singleline | RegexOptions.IgnoreCase) &&
                    Regex.IsMatch(window.ClassName, _settings.ClassName,
                    RegexOptions.Singleline | RegexOptions.IgnoreCase))
                {
                    _isFound = true;
                    Send(hWnd);
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void Send(IntPtr hWnd)
        {
            if (_settings.HotKey == null)
            {
                SendMsg(hWnd, _settings.Message, _settings.WParam, _settings.LParam);
            }
            else
            {
                const uint WM_KEYDOWN = 0x0100;
                const uint WM_KEYUP = 0x0101;
                const int VK_MENU = 0x12;
                const int VK_SHIFT = 0x10;
                const int VK_CONTROL = 0x11;
                const int VK_LWIN = 0x5B;

                if (_settings.HotKey.Windows)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYDOWN, new IntPtr(VK_LWIN), IntPtr.Zero);
                if (_settings.HotKey.Control)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYDOWN, new IntPtr(VK_CONTROL), IntPtr.Zero);
                if (_settings.HotKey.Alt)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYDOWN, new IntPtr(VK_MENU), IntPtr.Zero);
                if (_settings.HotKey.Shift)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYDOWN, new IntPtr(VK_SHIFT), IntPtr.Zero);

                foreach (var k in _settings.HotKey.KeyCode)
                {
                    PostMessage(new HandleRef(null, hWnd), WM_KEYDOWN, new IntPtr((int)k), IntPtr.Zero);
                    PostMessage(new HandleRef(null, hWnd), WM_KEYUP, new IntPtr((int)k), IntPtr.Zero);
                }

                if (_settings.HotKey.Windows)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYUP, new IntPtr(VK_LWIN), IntPtr.Zero);
                if (_settings.HotKey.Control)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYUP, new IntPtr(VK_CONTROL), IntPtr.Zero);
                if (_settings.HotKey.Alt)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYUP, new IntPtr(VK_MENU), IntPtr.Zero);
                if (_settings.HotKey.Shift)
                    PostMessage(new HandleRef(null, hWnd), WM_KEYUP, new IntPtr(VK_SHIFT), IntPtr.Zero);
            }
        }

        private void SendMsg(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (_settings.IsSendMessage)
            {
                SendMessage(hWnd, message, wParam, lParam);
            }
            else PostMessage(new HandleRef(null, hWnd), message, wParam, lParam);
        }
        #endregion

        #region Host Control

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
