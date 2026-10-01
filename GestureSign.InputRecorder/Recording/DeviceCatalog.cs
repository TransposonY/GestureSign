using GestureSign.Daemon.Native;
using GestureSign.InputRecorder.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>Raw input device queries (RIDI_DEVICENAME / RIDI_DEVICEINFO / RIDI_PREPARSEDDATA).</summary>
    internal static class DeviceCatalog
    {
        /// <summary>
        /// All HID raw input devices whose top-level collection is a digitizer pen / touch screen / touch pad,
        /// including ROOT/virtual ones (the replay applies the daemon's filtering), keyed by raw input handle.
        /// </summary>
        public static List<KeyValuePair<IntPtr, RecordedDevice>> EnumerateDigitizers()
        {
            var result = new List<KeyValuePair<IntPtr, RecordedDevice>>();
            uint deviceCount = 0;
            int itemSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
            if (NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)itemSize) != 0 || deviceCount == 0)
                return result;

            IntPtr list = Marshal.AllocHGlobal(itemSize * (int)deviceCount);
            try
            {
                uint written = NativeMethods.GetRawInputDeviceList(list, ref deviceCount, (uint)itemSize);
                if (written == uint.MaxValue)
                    return result;

                for (int i = 0; i < written; i++)
                {
                    var item = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(IntPtr.Add(list, itemSize * i), typeof(RAWINPUTDEVICELIST));
                    if (item.dwType != NativeMethods.RIM_TYPEHID)
                        continue;
                    RecordedDevice device = Describe(item.hDevice, 0, includePreparsedData: false);
                    if (IsDigitizer(device))
                        result.Add(new KeyValuePair<IntPtr, RecordedDevice>(item.hDevice, device));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(list);
            }
            return result;
        }

        public static bool IsDigitizer(RecordedDevice device)
        {
            return device.UsagePage == NativeMethods.DigitizerUsagePage &&
                   (device.Usage == NativeMethods.PenUsage ||
                    device.Usage == NativeMethods.TouchScreenUsage ||
                    device.Usage == NativeMethods.TouchPadUsage);
        }

        public static string UsageName(int usagePage, int usage)
        {
            if (usagePage == NativeMethods.DigitizerUsagePage)
            {
                switch (usage)
                {
                    case NativeMethods.PenUsage: return "Pen";
                    case NativeMethods.TouchScreenUsage: return "TouchScreen";
                    case NativeMethods.TouchPadUsage: return "TouchPad";
                }
            }
            return string.Format("0x{0:X2}/0x{1:X2}", usagePage, usage);
        }

        /// <summary>Queries name, HID info and (optionally) preparsed data. Fields stay default when a query fails.</summary>
        public static RecordedDevice Describe(IntPtr hDevice, int id, bool includePreparsedData = true)
        {
            var device = new RecordedDevice { Id = id, Name = GetDeviceName(hDevice) };
            ReadHidInfo(hDevice, device);
            if (includePreparsedData)
            {
                byte[] preparsed = GetPreparsedData(hDevice);
                if (preparsed != null)
                    device.PreparsedData = Convert.ToBase64String(preparsed);
            }
            return device;
        }

        private static string GetDeviceName(IntPtr hDevice)
        {
            uint chars = 0;
            RecorderNativeMethods.GetRawInputDeviceInfoW(hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref chars);
            if (chars == 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)(chars + 1) * 2);
            try
            {
                uint copied = RecorderNativeMethods.GetRawInputDeviceInfoW(hDevice, NativeMethods.RIDI_DEVICENAME, buffer, ref chars);
                if (copied == uint.MaxValue || copied == 0)
                    return null;
                return Marshal.PtrToStringUni(buffer, (int)copied).TrimEnd('\0');
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static void ReadHidInfo(IntPtr hDevice, RecordedDevice device)
        {
            uint size = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref size);
            if (size == 0)
                return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                // RID_DEVICE_INFO.cbSize must be initialised before the call.
                Marshal.WriteInt32(buffer, (int)size);
                if (NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, buffer, ref size) == uint.MaxValue)
                    return;

                var info = (RID_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(RID_DEVICE_INFO));
                if (info.dwType != NativeMethods.RIM_TYPEHID)
                    return;
                device.VendorId = (int)info.hid.dwVendorId;
                device.ProductId = (int)info.hid.dwProductId;
                device.VersionNumber = (int)info.hid.dwVersionNumber;
                device.UsagePage = info.hid.usUsagePage;
                device.Usage = info.hid.usUsage;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>RIDI_PREPARSEDDATA bytes, or null when the query fails.</summary>
        public static byte[] GetPreparsedData(IntPtr hDevice)
        {
            uint size = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref size);
            if (size == 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint copied = NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, buffer, ref size);
                if (copied == uint.MaxValue || copied == 0)
                    return null;
                var bytes = new byte[copied];
                Marshal.Copy(buffer, bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
