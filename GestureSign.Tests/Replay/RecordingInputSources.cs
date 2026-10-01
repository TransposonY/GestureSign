using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Native;
using GestureSign.InputRecorder.Recording;

namespace GestureSign.Tests.Replay
{
    /// <summary>Serves RIDI_* queries from a recording. Device handles are the recording's device ids.</summary>
    internal sealed class RecordingDeviceSource : IRawInputDeviceSource
    {
        private const int RIM_TYPEHID = 2;
        private readonly Dictionary<IntPtr, RecordedDevice> _devices;
        private readonly Dictionary<IntPtr, byte[]> _preparsed;

        public RecordingDeviceSource(IEnumerable<RecordedDevice> devices)
        {
            _devices = devices.ToDictionary(d => HandleOf(d.Id));
            _preparsed = _devices.ToDictionary(p => p.Key, p => Convert.FromBase64String(p.Value.PreparsedData ?? string.Empty));
        }

        public static IntPtr HandleOf(int deviceId) => new IntPtr(deviceId);

        public bool TryGetDeviceInfo(IntPtr hDevice, out RID_DEVICE_INFO info)
        {
            info = default(RID_DEVICE_INFO);
            RecordedDevice device;
            if (!_devices.TryGetValue(hDevice, out device))
                return false;
            info.cbSize = (uint)Marshal.SizeOf(typeof(RID_DEVICE_INFO));
            info.dwType = RIM_TYPEHID;
            info.hid.dwVendorId = (uint)device.VendorId;
            info.hid.dwProductId = (uint)device.ProductId;
            info.hid.dwVersionNumber = (uint)device.VersionNumber;
            info.hid.usUsagePage = (ushort)device.UsagePage;
            info.hid.usUsage = (ushort)device.Usage;
            return true;
        }

        public bool TryGetDeviceName(IntPtr hDevice, out string name)
        {
            RecordedDevice device;
            name = _devices.TryGetValue(hDevice, out device) ? device.Name : null;
            return name != null;
        }

        public SafeUnmanagedMemoryHandle GetPreparsedData(IntPtr hDevice)
        {
            byte[] blob = _preparsed[hDevice];
            IntPtr memory = Marshal.AllocHGlobal(Math.Max(blob.Length, 1));
            Marshal.Copy(blob, 0, memory, blob.Length);
            return new SafeUnmanagedMemoryHandle(memory);
        }
    }

    /// <summary>Environment as captured in a recording; the current event supplies tick count and cursor.</summary>
    internal sealed class RecordingEnvironment : IRawInputEnvironment
    {
        private readonly List<Rectangle> _screens;

        public RecordingEnvironment(RecordedEnvironment environment)
        {
            _screens = environment.Screens.Select(s => new Rectangle(s.X, s.Y, s.Width, s.Height)).ToList();
            ScreenOrientation = ToOrientation(environment.Orientation);
        }

        public int TickCount { get; set; }

        public Point Cursor { get; set; }

        public ScreenOrientation ScreenOrientation { get; }

        /// <summary>Screen.FromPoint semantics: the screen containing the point, else the nearest one.</summary>
        public Rectangle? CurrentScreenBounds
        {
            get
            {
                if (_screens.Count == 0)
                    return null;
                var containing = _screens.Where(s => s.Contains(Cursor)).ToList();
                if (containing.Count != 0)
                    return containing[0];
                return _screens.OrderBy(DistanceToCursor).First();
            }
        }

        private long DistanceToCursor(Rectangle screen)
        {
            long dx = Math.Max(Math.Max(screen.Left - Cursor.X, 0), Cursor.X - (screen.Right - 1));
            long dy = Math.Max(Math.Max(screen.Top - Cursor.Y, 0), Cursor.Y - (screen.Bottom - 1));
            return dx * dx + dy * dy;
        }

        private static ScreenOrientation ToOrientation(int degrees)
        {
            switch (degrees)
            {
                case 0: return ScreenOrientation.Angle0;
                case 90: return ScreenOrientation.Angle90;
                case 180: return ScreenOrientation.Angle180;
                case 270: return ScreenOrientation.Angle270;
                default: throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "Orientation must be 0, 90, 180 or 270.");
            }
        }
    }

    /// <summary>Builds the unmanaged RAWINPUT block GetRawInputData would return, for the current process bitness.</summary>
    internal static class RawInputBuffer
    {
        private const int RIM_TYPEHID = 2;

        /// <returns>HGlobal memory; free with Marshal.FreeHGlobal.</returns>
        public static IntPtr Allocate(IntPtr hDevice, int sizeHid, int count, byte[] data)
        {
            if (data.Length != sizeHid * count)
                throw new ArgumentException($"HID payload is {data.Length} bytes, expected {sizeHid} x {count}.");

            int headerSize = Marshal.SizeOf(typeof(RAWINPUTHEADER));
            int payloadOffset = Marshal.SizeOf(typeof(RAWINPUT));
            int total = payloadOffset + data.Length;

            IntPtr buffer = Marshal.AllocHGlobal(total);
            var header = new RAWINPUTHEADER { dwType = RIM_TYPEHID, dwSize = total, hDevice = hDevice, wParam = IntPtr.Zero };
            Marshal.StructureToPtr(header, buffer, false);
            Marshal.WriteInt32(buffer, headerSize, sizeHid);
            Marshal.WriteInt32(buffer, headerSize + 4, count);
            Marshal.Copy(data, 0, IntPtr.Add(buffer, payloadOffset), data.Length);
            return buffer;
        }
    }
}
