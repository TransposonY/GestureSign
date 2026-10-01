using System;
using System.Runtime.InteropServices;

namespace GestureSign.InputRecorder.Native
{
    /// <summary>
    /// P/Invokes the daemon's NativeMethods does not provide. Everything else comes from
    /// GestureSign.Daemon.Native.NativeMethods (InternalsVisibleTo) so the recorder reads devices the same way.
    /// </summary>
    internal static class RecorderNativeMethods
    {
        public const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Unicode variant: for RIDI_DEVICENAME pcbSize is in characters. The daemon declares the
        /// charset-less (ANSI) entry point; the recorder wants the exact interface path.
        /// </summary>
        [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", CharSet = CharSet.Unicode)]
        public static extern uint GetRawInputDeviceInfoW(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int dwProcessId);
    }
}
