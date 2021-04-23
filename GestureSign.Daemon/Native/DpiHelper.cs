using GestureSign.Common;
using System;

namespace GestureSign.Daemon.Native
{
    public class DpiHelper
    {
        //public static int GetDpiForWindow(IntPtr hwnd)
        //{
        //    var h = LoadLibrary("user32.dll");
        //    var ptr = GetProcAddress(h, "GetDpiForWindow"); // Windows 10 1607
        //    if (ptr == IntPtr.Zero)
        //        return GetDpiForNearestMonitor(hwnd);

        //    return Marshal.GetDelegateForFunctionPointer<GetDpiForWindowFn>(ptr)(hwnd);
        //}

        public static int GetScreenDpi(System.Drawing.Point point)
        {
            if (VersionHelper.IsWindows8Point1OrGreater())
            {
                try
                {
                    const int _S_OK = 0;
                    const int MONITOR_DEFAULTTONEAREST = 2;

                    IntPtr hmonitor = NativeMethods.MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
                    if (NativeMethods.GetDpiForMonitor(hmonitor, NativeMethods.MonitorDpiType.Effective, out uint dpiX, out uint dpiY).ToInt32() == _S_OK)
                    {
                        return (int)Math.Max(dpiX, dpiY);
                    }
                }
                catch (Exception)
                {
                }
            }
            return GetSystemDpi();
        }

        public static int GetSystemDpi()
        {
            const int LOGPIXELSX = 88;

            var scrDc = NativeMethods.GetDC(IntPtr.Zero);

            int dpi = NativeMethods.GetDeviceCaps(scrDc, LOGPIXELSX);

            NativeMethods.ReleaseDC(IntPtr.Zero, scrDc);

            return dpi;
        }
    }
}
