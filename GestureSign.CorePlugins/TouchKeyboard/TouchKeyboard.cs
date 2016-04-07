using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;

using System.Windows.Controls;

using System.Runtime.InteropServices;
using System.Diagnostics;
using GestureSign.Common.Localization;
using ManagedWinapi.Windows;

namespace GestureSign.CorePlugins.TouchKeyboard
{
    public class TouchKeyboard : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;
        TouchKeyboardUI _GUI = null;
        bool? _showKeyboard;
        #endregion

        #region Win32API

        //按下鼠标左键  
        private const int WM_LBUTTONDOWN = 0x201;
        //释放鼠标左键  
        private const int WM_LBUTTONUP = 0x202;
        private const Int32 WM_SYSCOMMAND = 274;
        private const UInt32 SC_CLOSE = 61536;

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, uint lParam);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, uint hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "PostMessage")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);
        #endregion

        #region Public Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.TouchKeyboard.Name"); }
        }

        public string Description
        {
            get
            {
                return _showKeyboard.HasValue ?
                    _showKeyboard.Value
                    ? LocalizationProvider.Instance.GetTextValue("CorePlugins.TouchKeyboard.Show")
                    : LocalizationProvider.Instance.GetTextValue("CorePlugins.TouchKeyboard.Hide")
                    : LocalizationProvider.Instance.GetTextValue("CorePlugins.TouchKeyboard.Auto");
            }
        }


        public string Category
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.TouchKeyboard.Category"); }
        }

        public bool IsAction
        {
            get { return true; }
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
        public TouchKeyboardUI TypedGUI
        {
            get { return (TouchKeyboardUI)GUI; }
        }
        #endregion

        #region Private Instance Methods

        private TouchKeyboardUI CreateGUI()
        {
            TouchKeyboardUI touchKeyboardUI = new TouchKeyboardUI();
            touchKeyboardUI.Loaded += (s, o) =>
            {
                if (_showKeyboard == null)
                    TypedGUI.AutoRadioButton.IsChecked = true;
                else
                {
                    TypedGUI.ShowTouchKeyboardRB.IsChecked = _showKeyboard.Value;
                    TypedGUI.HideTouchKeyboardRB.IsChecked = !_showKeyboard.Value;
                }
            };
            return touchKeyboardUI;
        }

        #endregion

        #region   Private Methods

        private bool StartProcess()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + @"\Microsoft Shared\ink\TabTip.exe";

            try
            {
                if (!System.IO.File.Exists(path))
                {
                    // older windows versions
                    path = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\osk.exe";
                }
                Process Process = new Process();
                // Expand environment variable to support %SYSTEMROOT%, etc.
                Process.StartInfo.FileName = path;
                Process.Start();
                return true;
            }
            catch { return false; }
        }

        private bool ShowKeyboard()
        {
            //find taskbar 
            IntPtr hwndTaskbar = FindWindow("Shell_TrayWnd", null);
            //Win 10
            IntPtr hwndTrayNotifyWnd = FindWindowEx(hwndTaskbar, 0, "TrayNotifyWnd", null);
            IntPtr hwndTIPBand = FindWindowEx(hwndTrayNotifyWnd, 0, "TIPBand", null);
            if (hwndTIPBand == IntPtr.Zero)
            {
                //Win 8
                IntPtr hwndReBar = FindWindowEx(hwndTaskbar, 0, "ReBarWindow32", null);
                hwndTIPBand = FindWindowEx(hwndReBar, 0, "TIPBand", null);
                if (hwndTIPBand == IntPtr.Zero) return StartProcess();
            }
            else
            {
                SystemWindow ww = new SystemWindow(hwndTIPBand);
                if (ww.Size.Height == 0 || ww.Size.Width == 0)
                {
                    return StartProcess();
                }
            }
            SendMessage(hwndTIPBand, WM_LBUTTONDOWN, (IntPtr)1, 0x160010);
            SendMessage(hwndTIPBand, WM_LBUTTONUP, (IntPtr)0, 0x160010);

            return true;
        }

        private bool HideKeyboard()
        {
            var keyboardHwnd = FindWindow("IPTip_Main_Window", null);

            if (keyboardHwnd != IntPtr.Zero)
                PostMessage(keyboardHwnd, WM_SYSCOMMAND, SC_CLOSE, 0);
            return true;
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo ActionPoint)
        {
            if (_showKeyboard.HasValue)
            {
                return _showKeyboard.Value ? ShowKeyboard() : HideKeyboard();
            }
            else
            {
                var keyboardHwnd = FindWindow("IPTip_Main_Window", null);
                if (keyboardHwnd == IntPtr.Zero) return false;

                var taptipWindow = new SystemWindow(keyboardHwnd);
                return taptipWindow.Visible && taptipWindow.Enabled ? HideKeyboard() : ShowKeyboard();
            }
        }

        public bool Deserialize(string SerializedData)
        {
            if (string.IsNullOrEmpty(SerializedData))
            {
                _showKeyboard = null;
                return true;
            }
            else
            {
                bool show;
                var result = Boolean.TryParse(SerializedData, out show);
                _showKeyboard = show;
                return result;
            }
        }

        public string Serialize()
        {
            if (_GUI?.ShowTouchKeyboardRB.IsChecked != null)
            {
                if (_GUI.AutoRadioButton.IsChecked != null && _GUI.AutoRadioButton.IsChecked.Value) return null;
                return _GUI.ShowTouchKeyboardRB.IsChecked.Value ? Boolean.TrueString : Boolean.FalseString;
            }
            else return _showKeyboard.HasValue ? _showKeyboard.Value ? Boolean.TrueString : Boolean.FalseString : null;
        }

        #endregion

        #region Host Control

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        #endregion
    }
}
