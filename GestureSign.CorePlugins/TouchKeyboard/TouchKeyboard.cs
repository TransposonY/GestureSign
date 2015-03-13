using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;

using System.Windows.Controls;

using System.Runtime.InteropServices;
using System.Diagnostics;

namespace GestureSign.CorePlugins.TouchKeyboard
{
    public class TouchKeyboard : IPlugin
    {
        #region Private Variables

        IHostControl _HostControl = null;
        TouchKeyboardUI _GUI = null;
        bool isShow;
        #endregion

        #region Win32API

        //按下鼠标左键  
        private const int WM_LBUTTONDOWN = 0x201;
        //释放鼠标左键  
        private const int WM_LBUTTONUP = 0x202;
        private const Int32 WM_MOVE = 0x0003;
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
            get { return "触摸键盘"; }
        }

        public string Description
        {
            get { return isShow ? "显示触摸键盘" : "隐藏触摸键盘"; }
        }


        public string Category
        {
            get { return "键盘"; }
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
            return new TouchKeyboardUI();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {

        }

        public bool Gestured(Common.Plugins.PointInfo ActionPoint)
        {
            if (isShow)
            {
                //find taskbar 
                IntPtr hwndTaskbar = FindWindow("Shell_TrayWnd", null);
                IntPtr hwndReBar = FindWindowEx(hwndTaskbar, 0, "ReBarWindow32", null);
                IntPtr hwndTIPBand = FindWindowEx(hwndReBar, 0, "TIPBand", null);
                if (hwndTIPBand != IntPtr.Zero)
                {
                    SendMessage(hwndTIPBand, WM_LBUTTONDOWN, (IntPtr)1, 0x160010);
                    SendMessage(hwndTIPBand, WM_LBUTTONUP, (IntPtr)0, 0x160010);
                }
                else
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
                    }
                    catch { return false; }
                }
            }
            else
            {
                IntPtr keyboardHwnd;

                keyboardHwnd = FindWindow("IPTip_Main_Window", null);

                if (keyboardHwnd != IntPtr.Zero)
                    PostMessage(keyboardHwnd, WM_SYSCOMMAND, SC_CLOSE, 0);
            }
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            bool parseSuccess = Boolean.TryParse(SerializedData, out isShow);
            TypedGUI.ShowTouchKeyboardRB.IsChecked = isShow;
            TypedGUI.HideTouchKeyboardRB.IsChecked = !isShow;
            return parseSuccess;
        }

        public string Serialize()
        {
            if (TypedGUI.ShowTouchKeyboardRB.IsChecked.HasValue)
            {
                return TypedGUI.ShowTouchKeyboardRB.IsChecked.Value ? Boolean.TrueString : Boolean.FalseString;
            }
            else return "";
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
