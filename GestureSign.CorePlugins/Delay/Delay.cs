using System;
using GestureSign.Common.Plugins;
using GestureSign.Common.Localization;
using System.Runtime.InteropServices;
using System.Threading;

namespace GestureSign.CorePlugins.Delay
{
    public class Delay : IPlugin
    {
        #region IPlugin Instance Fields

        private DelayUI _GUI = null;
        private DelaySettings _settings = new DelaySettings();

        #endregion

        #region PInvoke 
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_MENUSTART = 0x0004;
        private const uint EVENT_SYSTEM_MENUEND = 0x0005;
        private const uint EVENT_SYSTEM_MENUPOPUPSTART = 0x0006;
        private const uint EVENT_SYSTEM_MENUPOPUPEND = 0x0007;
        private const uint EVENT_SYSTEM_CAPTURESTART = 0x0008;
        private const uint EVENT_SYSTEM_CAPTUREEND = 0x0009;

        private const uint WM_TIMER = 0x0113;


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public UInt32 message;
            public IntPtr wParam;
            public IntPtr lParam;
            public UInt32 time;
            public POINT pt;
        }

        [DllImport("user32.dll")]
        static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

        [DllImport("user32.dll")]
        static extern void PostQuitMessage(int nExitCode);


        #endregion

        #region IPlugin Instance Properties

        public string Name
        {
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.Delay.Name"); }
        }

        public string Category
        {
            get { return "GestureSign"; }
        }

        public string Description
        {
            get
            {
                string output = String.Format(LocalizationProvider.Instance.GetTextValue("CorePlugins.Delay.Description"), _settings.Timeout);
                if (_settings.WaitType != WaitSetting.NotSet)
                {
                    output += " " + LocalizationProvider.Instance.GetTextValue("CorePlugins.Delay.WaitUntil") +
                        LocalizationProvider.Instance.GetTextValue("CorePlugins.Delay." + _settings.WaitType.ToString());
                }
                return output;
            }
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

        public DelayUI TypedGUI
        {
            get { return (DelayUI)GUI; }
        }

        public IHostControl HostControl { get; set; }

        public object Icon => IconSource.GestureSign;

        #endregion

        #region IPlugin Instance Methods

        public void Initialize()
        {

        }

        public bool Gestured(PointInfo info)
        {
            uint triggerType;

            if (_settings.Timeout <= 0)
                return false;

            switch (_settings.WaitType)
            {
                case WaitSetting.NotSet:
                    Thread.Sleep(_settings.Timeout);
                    return true;
                case WaitSetting.ForegroundWinChanged:
                    triggerType = EVENT_SYSTEM_FOREGROUND;
                    break;
                case WaitSetting.MenuDisplayed:
                    triggerType = EVENT_SYSTEM_MENUSTART;
                    break;
                case WaitSetting.MenuClosed:
                    triggerType = EVENT_SYSTEM_MENUEND;
                    break;
                case WaitSetting.MouseCaptured:
                    triggerType = EVENT_SYSTEM_CAPTURESTART;
                    break;
                case WaitSetting.MouseLost:
                    triggerType = EVENT_SYSTEM_CAPTUREEND;
                    break;
                default:
                    return false;
            }

            IntPtr hWinEventHook = IntPtr.Zero;
            IntPtr timerId = IntPtr.Zero;
            GCHandle gch;
            bool flag = false;
            var tick = Environment.TickCount;

            WinEventDelegate winEventDele = (IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) =>
            {
                if (eventType == triggerType || Environment.TickCount - tick >= _settings.Timeout)
                {
                    flag = true;
                    if (hWinEventHook != IntPtr.Zero)
                    {
                        UnhookWinEvent(hWinEventHook);
                        hWinEventHook = IntPtr.Zero;
                    }
                    PostQuitMessage(0);
                }
            };
            gch = GCHandle.Alloc(winEventDele);

            // WINEVENT_SKIPOWNPROCESS will skip menu events
            hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_CAPTUREEND, IntPtr.Zero, winEventDele, 0, 0, WINEVENT_OUTOFCONTEXT);

            try
            {
                timerId = SetTimer(IntPtr.Zero, IntPtr.Zero, (uint)_settings.Timeout, IntPtr.Zero);
                do
                {
                    MSG msg;
                    if (GetMessage(out msg, IntPtr.Zero, 0, 0) <= 0) break;
                    if (msg.message == WM_TIMER && msg.hwnd == IntPtr.Zero && msg.wParam == timerId)
                    {
                        return true;
                    }

                } while (!flag && Environment.TickCount - tick < _settings.Timeout);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (timerId != IntPtr.Zero)
                    KillTimer(IntPtr.Zero, timerId);
                if (hWinEventHook != IntPtr.Zero)
                    UnhookWinEvent(hWinEventHook);
                gch.Free();
            }
        }

        public bool Deserialize(string serializedData)
        {
            int timeout = 0;
            if (int.TryParse(serializedData, out timeout))
            {
                _settings.Timeout = timeout;
                _settings.WaitType = WaitSetting.NotSet;
                return true;
            }
            else
            {
                return PluginHelper.DeserializeSettings(serializedData, out _settings);
            }
        }

        public string Serialize()
        {
            if (_settings == null)
                _settings = new DelaySettings();
            if (_GUI != null)
            {
                _settings.Timeout = _GUI.Timeout;
                _settings.WaitType = _GUI.WaitType;
            }

            return PluginHelper.SerializeSettings(_settings);
        }

        #endregion

        #region Private Instance Methods

        private DelayUI CreateGUI()
        {
            DelayUI delayUI = new DelayUI();
            delayUI.Loaded += (s, o) =>
            {
                TypedGUI.Timeout = _settings.Timeout;
                TypedGUI.WaitType = _settings.WaitType;
            };
            return delayUI;
        }

        #endregion
    }
}
