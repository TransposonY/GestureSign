using System;
using System.Runtime.InteropServices;

namespace GestureSign.Daemon.Native
{
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
        public IntPtr wParam;
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
