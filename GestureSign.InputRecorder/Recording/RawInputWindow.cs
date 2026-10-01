using GestureSign.Daemon.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>
    /// Hidden window receiving WM_INPUT / WM_INPUT_DEVICE_CHANGE for digitizer collections,
    /// registered the same way as the daemon's MessageWindow (RIDEV_INPUTSINK | RIDEV_DEVNOTIFY).
    /// </summary>
    internal sealed class RawInputWindow : NativeWindow, IDisposable
    {
        private static readonly ushort[] DigitizerUsages =
        {
            NativeMethods.PenUsage, NativeMethods.TouchScreenUsage, NativeMethods.TouchPadUsage
        };

        /// <summary>Raised with the complete RAWINPUT buffer returned by GetRawInputData(RID_INPUT).</summary>
        public event Action<byte[]> RawInput;

        /// <summary>Raised for WM_INPUT_DEVICE_CHANGE with (wParam, hDevice).</summary>
        public event Action<int, IntPtr> DeviceChanged;

        public RawInputWindow()
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            CreateHandle(new CreateParams { Style = 0, ExStyle = WS_EX_NOACTIVATE });

            var rid = new RAWINPUTDEVICE[DigitizerUsages.Length];
            for (int i = 0; i < rid.Length; i++)
            {
                rid[i].usUsagePage = NativeMethods.DigitizerUsagePage;
                rid[i].usUsage = DigitizerUsages[i];
                rid[i].dwFlags = NativeMethods.RIDEV_INPUTSINK | NativeMethods.RIDEV_DEVNOTIFY;
                rid[i].hwndTarget = Handle;
            }
            if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterRawInputDevices failed");
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_INPUT:
                    byte[] buffer = ReadRawInput(m.LParam);
                    if (buffer != null)
                        RawInput?.Invoke(buffer);
                    break;
                case NativeMethods.WM_INPUT_DEVICE_CHANGE:
                    DeviceChanged?.Invoke(m.WParam.ToInt32(), m.LParam);
                    break;
            }
            // DefWindowProc performs the WM_INPUT cleanup.
            base.WndProc(ref m);
        }

        private static byte[] ReadRawInput(IntPtr hRawInput)
        {
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            uint size = 0;
            NativeMethods.GetRawInputData(hRawInput, NativeMethods.RID_INPUT, IntPtr.Zero, ref size, headerSize);
            if (size == 0)
                return null;

            IntPtr native = Marshal.AllocHGlobal((int)size);
            try
            {
                if (NativeMethods.GetRawInputData(hRawInput, NativeMethods.RID_INPUT, native, ref size, headerSize) != size)
                    return null;
                var bytes = new byte[size];
                Marshal.Copy(native, bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(native);
            }
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
