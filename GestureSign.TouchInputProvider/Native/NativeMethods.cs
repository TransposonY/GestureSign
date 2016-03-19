using System;
using System.Runtime.InteropServices;

namespace GestureSign.TouchInputProvider.Native
{
    internal static class NativeMethods
    {
        #region const definitions

        internal const int RIDEV_INPUTSINK = 0x00000100;
        internal const int RID_INPUT = 0x10000003;

        internal const int RIM_TYPEHID = 2;

        internal const uint RIDI_DEVICENAME = 0x20000007;
        internal const uint RIDI_PREPARSEDDATA = 0x20000005;

        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_SYSKEYDOWN = 0x0104;
        internal const int WM_INPUT = 0x00FF;
        internal const int VK_OEM_CLEAR = 0xFE;
        internal const int VK_LAST_KEY = VK_OEM_CLEAR; // this is a made up value used as a sentinal

        internal const ushort GenericDesktopPage = 0x01;
        internal const ushort TouchScreenUsagePage = 0x0D;
        internal const ushort ContactIdentifierId = 0x51;
        internal const ushort ContactCountId = 0x54;
        internal const ushort ScanTimeId = 0x56;
        internal const ushort TipId = 0x42;
        internal const ushort XCoordinateId = 0x30;
        internal const ushort YCoordinateId = 0x31;

        #endregion const definitions

        #region DllImports

        [DllImport("user32.dll")]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("User32.dll")]
        internal static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("User32.dll")]
        internal static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        internal static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        internal static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

        #endregion DllImports

        #region  Windows.h structure declarations

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUT
        {

            /// RAWINPUTHEADER->tagRAWINPUTHEADER
            public RAWINPUTHEADER header;
            public RAWHID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWHID
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwSizHid;
            [MarshalAs(UnmanagedType.U4)]
            public int dwCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTDEVICE
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort usUsagePage;
            [MarshalAs(UnmanagedType.U2)]
            public ushort usUsage;
            [MarshalAs(UnmanagedType.U4)]
            public int dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RAWINPUTHEADER
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
            [MarshalAs(UnmanagedType.U4)]
            public int dwSize;
            public IntPtr hDevice;
            public int wParam;
        }

        #endregion Windows.h structure declarations

    }
}
