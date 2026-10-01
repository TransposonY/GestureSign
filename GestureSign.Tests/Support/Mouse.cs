using System;
using System.Windows.Forms;
using ManagedWinapi.Hooks;
using ManagedWinapi.Windows;

namespace GestureSign.Tests.Support
{
    /// <summary>Builds low-level mouse hook messages as the hook would deliver them.</summary>
    internal static class Mouse
    {
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        public static LowLevelMouseMessage Move(int x, int y)
        {
            return new LowLevelMouseMessage(WM_MOUSEMOVE, new POINT(x, y), 0, 0, 0, IntPtr.Zero);
        }

        public static LowLevelMouseMessage Down(MouseActions button, int x, int y)
        {
            return Button(button, x, y, down: true);
        }

        public static LowLevelMouseMessage Up(MouseActions button, int x, int y)
        {
            return Button(button, x, y, down: false);
        }

        private static LowLevelMouseMessage Button(MouseActions button, int x, int y, bool down)
        {
            int msg;
            int mouseData = 0;
            switch (button)
            {
                case MouseActions.Left: msg = down ? WM_LBUTTONDOWN : WM_LBUTTONUP; break;
                case MouseActions.Right: msg = down ? WM_RBUTTONDOWN : WM_RBUTTONUP; break;
                case MouseActions.Middle: msg = down ? WM_MBUTTONDOWN : WM_MBUTTONUP; break;
                case MouseActions.XButton1: msg = down ? WM_XBUTTONDOWN : WM_XBUTTONUP; mouseData = 1 << 16; break;
                case MouseActions.XButton2: msg = down ? WM_XBUTTONDOWN : WM_XBUTTONUP; mouseData = 2 << 16; break;
                default: throw new ArgumentOutOfRangeException(nameof(button));
            }
            // MouseActions button values equal System.Windows.Forms.MouseButtons values.
            return new LowLevelMouseMessage(msg, new POINT(x, y), mouseData, 0, 0, IntPtr.Zero, (MouseButtons)button);
        }
    }
}
