using System;
using GestureSign.Common.Plugins;
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
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "PostMessage")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);

        [ComImport]
        [Guid("37c994e7-432b-4834-a2f7-dce1f13b834b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ITipInvocation
        {
            void Toggle(IntPtr hwnd);
        }

        private enum ABE : uint
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        private enum ABM : uint
        {
            New = 0x00000000,
            Remove = 0x00000001,
            QueryPos = 0x00000002,
            SetPos = 0x00000003,
            GetState = 0x00000004,
            GetTaskbarPos = 0x00000005,
            Activate = 0x00000006,
            GetAutoHideBar = 0x00000007,
            SetAutoHideBar = 0x00000008,
            WindowPosChanged = 0x00000009,
            SetState = 0x0000000A,
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr SHAppBarMessage(ABM dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public ABE uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

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

        public object GUI
        {
            get
            {
                if (_GUI == null)
                    _GUI = CreateGUI();

                return _GUI;
            }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public TouchKeyboardUI TypedGUI
        {
            get { return (TouchKeyboardUI)GUI; }
        }

        public object Icon => IconSource.Keyboard;

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
                using (Process Process = new Process())
                {
                    Process.StartInfo.FileName = path;
                    Process.Start();
                }
                return true;
            }
            catch { return false; }
        }

        private bool ToggleVisibilityByTipBand()
        {
            //find taskbar 
            IntPtr hwndTaskbar = FindWindow("Shell_TrayWnd", null);
            if (hwndTaskbar == IntPtr.Zero)
                return false;

            //Retrieves the autohide states of the Windows taskbar
            const int Autohide = 0x0000001;
            APPBARDATA data = new APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = hwndTaskbar
            };
            var result = SHAppBarMessage(ABM.GetState, ref data);
            if ((result.ToInt64() & Autohide) == Autohide)
                return false;

            //Win 10
            IntPtr hwndTrayNotifyWnd = FindWindowEx(hwndTaskbar, IntPtr.Zero, "TrayNotifyWnd", null);
            IntPtr hwndTIPBand = FindWindowEx(hwndTrayNotifyWnd, IntPtr.Zero, "TIPBand", null);
            if (hwndTIPBand == IntPtr.Zero)
            {
                //Win 8
                IntPtr hwndReBar = FindWindowEx(hwndTaskbar, IntPtr.Zero, "ReBarWindow32", null);
                hwndTIPBand = FindWindowEx(hwndReBar, IntPtr.Zero, "TIPBand", null);
                if (hwndTIPBand == IntPtr.Zero)
                    return false;
            }
            else
            {
                SystemWindow ww = new SystemWindow(hwndTIPBand);
                if (ww.Size.Height == 0 || ww.Size.Width == 0)
                {
                    return false;
                }
            }
            SendMessage(hwndTIPBand, WM_LBUTTONDOWN, (IntPtr)1, 0x160010);
            SendMessage(hwndTIPBand, WM_LBUTTONUP, (IntPtr)0, 0x160010);

            return true;
        }

        private bool ShowKeyboard()
        {
            if (!IsKeyboardOpen())
            {
                if (!ToggleVisibilityByTipBand())
                {
                    var processes = Process.GetProcessesByName("TabTip");
                    if (processes.Length != 0)
                    {
                        foreach (var p in processes)
                        {
                            p.Dispose();
                        }
                        try
                        {
                            ToggleVisibility();
                            return true;
                        }
                        catch { }
                    }
                    else
                    {
                        if (StartProcess())
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                System.Threading.Thread.Sleep(100);
                                if (IsKeyboardOpen())
                                    return true;
                            }
                            ToggleVisibility();
                        }
                    }
                }
            }
            return true;
        }

        private bool HideKeyboard()
        {
            try
            {
                if (IsKeyboardOpen())
                    if (!ToggleVisibilityByTipBand())
                    {
                        ToggleVisibility();
                    }
                return true;
            }
            catch
            {
                var keyboardHwnd = FindWindow("IPTip_Main_Window", null);

                if (keyboardHwnd != IntPtr.Zero)
                {
                    PostMessage(keyboardHwnd, WM_SYSCOMMAND, SC_CLOSE, 0);
                    return true;
                }
                else return false;
            }
        }

        private bool IsKeyboardOpen()
        {
            // Reference https://stackoverflow.com/a/48545074
            if (Environment.OSVersion.Version >= new Version(10, 0, 16299))
            {
                const string WindowParentClass1709 = "ApplicationFrameWindow";
                const string WindowClass1709 = "Windows.UI.Core.CoreWindow";
                const string WindowCaption1709 = "Microsoft Text Input Application";

                // if there is a top-level window - the keyboard is closed
                var wnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, WindowClass1709, WindowCaption1709);
                if (wnd != IntPtr.Zero)
                    return false;

                var parent = IntPtr.Zero;
                while (true)
                {
                    parent = FindWindowEx(IntPtr.Zero, parent, WindowParentClass1709, null);
                    if (parent == IntPtr.Zero)
                        break; // no more windows, keyboard state is unknown

                    // if it's a child of a WindowParentClass1709 window - the keyboard is open
                    wnd = FindWindowEx(parent, IntPtr.Zero, WindowClass1709, WindowCaption1709);
                    if (wnd != IntPtr.Zero)
                        return true;
                }
            }

            var keyboardHwnd = FindWindow("IPTip_Main_Window", null);
            if (keyboardHwnd == IntPtr.Zero)
                return false;
            var taptipWindow = new SystemWindow(keyboardHwnd);
            return taptipWindow.Visible && taptipWindow.Enabled;
        }

        private void ToggleVisibility()
        {
            var type = Type.GetTypeFromCLSID(Guid.Parse("4ce576fa-83dc-4F88-951c-9d0782b4e376"));
            var instance = (ITipInvocation)Activator.CreateInstance(type);
            instance.Toggle(SystemWindow.DesktopWindow.HWnd);
            Marshal.ReleaseComObject(instance);
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
                return IsKeyboardOpen() ? HideKeyboard() : ShowKeyboard();
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
