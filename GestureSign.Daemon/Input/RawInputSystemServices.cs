using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Input
{
    internal sealed class RawInputDeviceSource : IRawInputDeviceSource
    {
        public static readonly RawInputDeviceSource Instance = new RawInputDeviceSource();

        private RawInputDeviceSource() { }

        public bool TryGetDeviceInfo(IntPtr hDevice, out RID_DEVICE_INFO info)
        {
            info = default(RID_DEVICE_INFO);
            uint pcbSize = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);
            if (pcbSize <= 0)
                return false;

            IntPtr pInfo = Marshal.AllocHGlobal((int)pcbSize);
            using (new SafeUnmanagedMemoryHandle(pInfo))
            {
                NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, pInfo, ref pcbSize);
                info = (RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RID_DEVICE_INFO));
                return true;
            }
        }

        public bool TryGetDeviceName(IntPtr hDevice, out string name)
        {
            name = null;
            uint pcbSize = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
            if (pcbSize <= 0)
                return false;

            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            using (new SafeUnmanagedMemoryHandle(pData))
            {
                NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, pData, ref pcbSize);
                name = Marshal.PtrToStringAnsi(pData);
                return true;
            }
        }

        public SafeUnmanagedMemoryHandle GetPreparsedData(IntPtr hDevice)
        {
            uint pcbSize = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
            IntPtr pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);
            return new SafeUnmanagedMemoryHandle(pPreparsedData);
        }
    }

    internal sealed class SystemRawInputEnvironment : IRawInputEnvironment
    {
        public static readonly SystemRawInputEnvironment Instance = new SystemRawInputEnvironment();

        private SystemRawInputEnvironment() { }

        public int TickCount => Environment.TickCount;

        public Rectangle? CurrentScreenBounds => Screen.FromPoint(Cursor.Position)?.Bounds;

        public ScreenOrientation ScreenOrientation => SystemInformation.ScreenOrientation;
    }
}
