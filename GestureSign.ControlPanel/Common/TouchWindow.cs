using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace GestureSign.ControlPanel.Common
{
#pragma warning disable CS0108
    /// <summary>
    /// https://devdept.zendesk.com/hc/en-us/articles/217843288-Enable-multitouch-on-WPF-controls
    /// </summary>
    public class TouchWindow : MetroWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct TOUCHINPUT
        {
            public int x;
            public int y;
            private IntPtr hSource;
            public int dwID;
            public int dwFlags;
            private int dwMask;
            private int dwTime;
            private IntPtr dwExtraInfo;
            private int cxContact;
            private int cyContact;
        }
        public const int WM_TOUCH = 0x0240;

        public enum TouchWindowFlag : uint
        {
            FineTouch = 0x1,
            WantPalm = 0x2
        }


        [DllImport("user32")]
        private static extern bool RegisterTouchWindow(System.IntPtr hWnd, TouchWindowFlag flags);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);


            var presentationSource = (HwndSource)PresentationSource.FromDependencyObject(this);
            if (presentationSource == null)
            {
                throw new Exception("Unable to find the parent element host.");
            }

            RegisterTouchWindow(presentationSource.Handle, TouchWindowFlag.WantPalm);
        }

        [DllImport("user32")]
        private static extern bool GetTouchInputInfo(System.IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        private static extern void CloseTouchInputHandle(System.IntPtr lParam);

        class TouchDeviceEmulator : TouchDevice
        {
            public System.Windows.Point Position;
            public TouchDeviceEmulator(int deviceId) : base(deviceId)
            {
            }

            public override TouchPoint GetTouchPoint(IInputElement relativeTo)
            {
                System.Windows.Point pt = Position;
                if (relativeTo != null)
                    pt = ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(Position);

                var rect = new Rect(pt, new Size(1.0, 1.0));
                return new TouchPoint(this, pt, rect, TouchAction.Move);
            }

            public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
            {
                throw new NotImplementedException();
            }

            public void SetActiveSource(PresentationSource activeSource)
            {
                base.SetActiveSource(activeSource);
            }

            public void Activate()
            {
                base.Activate();
            }

            public void ReportUp()
            {
                base.ReportUp();
            }

            public void ReportDown()
            {
                base.ReportDown();
            }

            public void ReportMove()
            {
                base.ReportMove();
            }

            public void Deactivate()
            {
                base.Deactivate();
            }
        }

        Dictionary<int, TouchDeviceEmulator> _devices = new Dictionary<int, TouchDeviceEmulator>();

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        public const int TOUCHEVENTF_MOVE = 0x0001;
        public const int TOUCHEVENTF_DOWN = 0x0002;
        public const int TOUCHEVENTF_UP = 0x0004;
        public const int TOUCHEVENTF_INRANGE = 0x0008;
        public const int TOUCHEVENTF_PRIMARY = 0x0010;
        public const int TOUCHEVENTF_NOCOALESCE = 0x0020;
        public const int TOUCHEVENTF_PEN = 0x0040;
        public const int TOUCHEVENTF_PALM = 0x0080;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // http://www.codeproject.com/Articles/692286/WPF-and-multi-touch
            // http://jayheu.lingohq.io/blog/archives/2015/07/25/multi-touch-and-wpf

            // Handle messages...
            if (msg == WM_TOUCH) //WM_TOUCH
            {
                handled = HandleTouch(wParam, lParam);
                return new IntPtr(1);
            }
            return IntPtr.Zero;
        }

        private bool HandleTouch(IntPtr wParam, IntPtr lParam)
        {
            bool handled = false;
            var inputCount = wParam.ToInt32() & 0xffff;
            var inputs = new TOUCHINPUT[inputCount];

            if (GetTouchInputInfo(lParam, inputCount, inputs, Marshal.SizeOf(inputs[0])))
            {
                for (int i = 0; i < inputCount; i++)
                {
                    var input = inputs[i];
                    var position = PointFromScreen(new System.Windows.Point((input.x * 0.01), (input.y * 0.01)));

                    TouchDeviceEmulator device;
                    if (!_devices.TryGetValue(input.dwID, out device))
                    {
                        device = new TouchDeviceEmulator(input.dwID);
                        _devices.Add(input.dwID, device);
                    }

                    device.Position = position;

                    if ((input.dwFlags & TOUCHEVENTF_DOWN) > 0)
                    {
                        device.SetActiveSource(PresentationSource.FromVisual(this));
                        device.Activate();
                        device.ReportDown();
                    }
                    else if (device.IsActive && (input.dwFlags & TOUCHEVENTF_UP) > 0)
                    {
                        device.ReportUp();
                        device.Deactivate();
                        _devices.Remove(input.dwID);
                    }
                    else if (device.IsActive && (input.dwFlags & TOUCHEVENTF_MOVE) > 0)
                    {
                        device.ReportMove();
                    }
                }

                CloseTouchInputHandle(lParam);
                handled = true;
            }

            return handled;
        }
    }
}
