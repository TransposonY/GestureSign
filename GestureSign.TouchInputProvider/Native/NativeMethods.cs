﻿using System;
using System.Runtime.InteropServices;

namespace GestureSign.TouchInputProvider.Native
{
    internal static class NativeMethods
    {
        #region const definitions

        internal const int RIDEV_REMOVE = 0x00000001;
        internal const int RIDEV_INPUTSINK = 0x00000100;
        internal const int RIDEV_DEVNOTIFY = 0x00002000;
        internal const int RID_INPUT = 0x10000003;

        internal const int RIM_TYPEHID = 2;

        internal const uint RIDI_DEVICENAME = 0x20000007;
        internal const uint RIDI_DEVICEINFO = 0x2000000b;
        internal const uint RIDI_PREPARSEDDATA = 0x20000005;

        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_SYSKEYDOWN = 0x0104;
        internal const int WM_INPUT = 0x00FF;
        internal const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
        internal const int VK_OEM_CLEAR = 0xFE;
        internal const int VK_LAST_KEY = VK_OEM_CLEAR; // this is a made up value used as a sentinal

        internal const ushort GenericDesktopPage = 0x01;
        internal const ushort DigitizerUsagePage = 0x0D;
        internal const ushort ContactIdentifierId = 0x51;
        internal const ushort ContactCountId = 0x54;
        internal const ushort ScanTimeId = 0x56;
        internal const ushort TipId = 0x42;
        internal const ushort XCoordinateId = 0x30;
        internal const ushort YCoordinateId = 0x31;

        internal const ushort TouchPadUsage = 0x05;
        internal const ushort TouchScreenUsage = 0x04;
        internal const ushort PenUsage = 0x02;

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

    }

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

    /// <summary>
    /// Defines the raw input data coming from any device.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RID_DEVICE_INFO
    {
        /// <summary>
        /// The size, in bytes, of the <see cref="RID_DEVICE_INFO"/> structure.
        /// </summary>
        [FieldOffset(0)]
        public uint cbSize;

        /// <summary>
        /// The type of raw input data.
        /// </summary>
        [FieldOffset(4)]
        public int dwType;

        ///// <summary>
        ///// If <see cref="dwType"/> is <see cref="RIM_TYPE.MOUSE"/>, this is the <see cref="RID_DEVICE_INFO_MOUSE"/>
        ///// structure that defines the mouse.
        ///// </summary>
        //[FieldOffset(8)]
        //public RID_DEVICE_INFO_MOUSE mouse;

        ///// <summary>
        ///// If <see cref="dwType"/> is <see cref="RIM_TYPE.KEYBOARD"/>, this is the <see cref="RID_DEVICE_INFO_KEYBOARD"/>
        ///// structure that defines the keyboard.
        ///// </summary>
        //[FieldOffset(8)]
        //public RID_DEVICE_INFO_KEYBOARD keyboard;

        /// <summary>
        /// If <see cref="dwType"/> is <see cref="RIM_TYPE.HID"/>, this is the <see cref="RID_DEVICE_INFO_HID"/>
        /// structure that defines the HID device.
        /// </summary>
        [FieldOffset(8)]
        public RID_DEVICE_INFO_HID hid;
    }

    /// <summary>
    /// Defines the raw input data coming from the specified Human Interface Device (HID).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RID_DEVICE_INFO_HID
    {
        /// <summary>
        /// The vendor identifier for the HID.
        /// </summary>
        public uint dwVendorId;

        /// <summary>
        /// The product identifier for the HID.
        /// </summary>
        public uint dwProductId;

        /// <summary>
        /// The version number for the HID.
        /// </summary>
        public uint dwVersionNumber;

        /// <summary>
        /// The top-level collection Usage Page for the device.
        /// </summary>
        public ushort usUsagePage;

        /// <summary>
        /// The top-level collection Usage for the device.
        /// </summary>
        public ushort usUsage;
    }

    #endregion Windows.h structure declarations
}
