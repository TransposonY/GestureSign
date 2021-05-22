using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Input
{
    public class MessageWindow : NativeWindow
    {
        private Screen _currentScr;

        private static readonly HandleRef HwndMessage = new HandleRef(null, new IntPtr(-3));

        private List<RawData> _outputTouchs = new List<RawData>(1);
        private int _requiringContactCount;
        private Dictionary<IntPtr, ushort> _validDevices = new Dictionary<IntPtr, ushort>();

        private Devices _sourceDevice;
        private List<ushort> _registeredDeviceList = new List<ushort>(1);
        private int? _penLastActivity;
        private bool _ignoreTouchInputWhenUsingPen;
        private DeviceStates _penGestureButton;

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public MessageWindow()
        {
            CreateWindow();
            UpdateRegistration();
        }

        ~MessageWindow()
        {
            DestroyWindow();
        }

        public bool CreateWindow()
        {
            if (Handle == IntPtr.Zero)
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateHandle(new CreateParams
                {
                    Style = 0,
                    ExStyle = WS_EX_NOACTIVATE,
                    ClassStyle = 0,
                    Caption = "GSMessageWindow",
                    Parent = (IntPtr)HwndMessage
                });
            }
            return Handle != IntPtr.Zero;
        }

        public void DestroyWindow()
        {
            DestroyWindow(true, IntPtr.Zero);
        }

        public override void DestroyHandle()
        {
            DestroyWindow(false, IntPtr.Zero);
            base.DestroyHandle();
        }

        protected override void OnHandleChange()
        {
            UpdateRegistration();
            base.OnHandleChange();
        }

        private bool GetInvokeRequired(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            int pid;
            var hwndThread = NativeMethods.GetWindowThreadProcessId(new HandleRef(this, hWnd), out pid);
            var currentThread = NativeMethods.GetCurrentThreadId();
            return (hwndThread != currentThread);
        }

        private void DestroyWindow(bool destroyHwnd, IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                hWnd = Handle;
            }

            if (GetInvokeRequired(hWnd))
            {
                NativeMethods.PostMessage(new HandleRef(this, hWnd), NativeMethods.WmClose, 0, 0);
                return;
            }

            lock (this)
            {
                if (destroyHwnd)
                {
                    base.DestroyHandle();
                }
            }
        }

        public void UpdateRegistration()
        {
            _ignoreTouchInputWhenUsingPen = AppConfig.IgnoreTouchInputWhenUsingPen;
            var penSetting = AppConfig.PenGestureButton;
            _penGestureButton = penSetting & (DeviceStates.Invert | DeviceStates.RightClickButton);

            _validDevices.Clear();

            UpdateRegisterState(AppConfig.RegisterTouchScreen, NativeMethods.TouchScreenUsage);
            UpdateRegisterState(_ignoreTouchInputWhenUsingPen || _penGestureButton != 0 && (penSetting & (DeviceStates.InRange | DeviceStates.Tip)) != 0, NativeMethods.PenUsage);
            UpdateRegisterState(AppConfig.RegisterTouchPad, NativeMethods.TouchPadUsage);
        }

        private void UpdateRegisterState(bool register, ushort usage)
        {
            if (register)
            {
                RegisterDevice(usage);
            }
            else
            {
                UnregisterDevice(usage);
            }
        }

        private void RegisterDevice(ushort usage)
        {
            UnregisterDevice(usage);

            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

            rid[0].usUsagePage = NativeMethods.DigitizerUsagePage;
            rid[0].usUsage = usage;
            rid[0].dwFlags = NativeMethods.RIDEV_INPUTSINK | NativeMethods.RIDEV_DEVNOTIFY;
            rid[0].hwndTarget = Handle;

            if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).");
            }
            _registeredDeviceList.Add(usage);
        }

        private void UnregisterDevice(ushort usage)
        {
            if (_registeredDeviceList.Contains(usage))
            {
                RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

                rid[0].usUsagePage = NativeMethods.DigitizerUsagePage;
                rid[0].usUsage = usage;
                rid[0].dwFlags = NativeMethods.RIDEV_REMOVE;
                rid[0].hwndTarget = IntPtr.Zero;

                if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
                {
                    throw new ApplicationException("Failed to unregister raw input device.");
                }
                _registeredDeviceList.Remove(usage);
            }
        }

        private bool ValidateDevice(IntPtr hDevice, out ushort usage)
        {
            usage = 0;
            uint pcbSize = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);
            if (pcbSize <= 0)
                return false;

            IntPtr pInfo = Marshal.AllocHGlobal((int)pcbSize);
            using (new SafeUnmanagedMemoryHandle(pInfo))
            {
                NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICEINFO, pInfo, ref pcbSize);
                var info = (RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RID_DEVICE_INFO));
                switch (info.hid.usUsage)
                {
                    case NativeMethods.TouchPadUsage:
                    case NativeMethods.TouchScreenUsage:
                    case NativeMethods.PenUsage:
                        break;
                    default:
                        return true;
                }

                NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                if (pcbSize <= 0)
                    return false;

                IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                using (new SafeUnmanagedMemoryHandle(pData))
                {
                    NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_DEVICENAME, pData, ref pcbSize);
                    string deviceName = Marshal.PtrToStringAnsi(pData);

                    if (string.IsNullOrEmpty(deviceName) || deviceName.IndexOf("VIRTUAL_DIGITIZER", StringComparison.OrdinalIgnoreCase) >= 0 || deviceName.IndexOf("ROOT", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    usage = info.hid.usUsage;
                    return true;
                }
            }
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case NativeMethods.WM_INPUT:
                    {
                        ProcessInputCommand(message.LParam);
                        break;
                    }
                case NativeMethods.WM_INPUT_DEVICE_CHANGE:
                    {
                        _validDevices.Clear();
                        break;
                    }
            }
            base.WndProc(ref message);
        }

        private void CheckLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode != 0)
            {
                throw new Win32Exception(errCode);
            }
        }

        #region ProcessInput

        /// <summary>
        /// Processes WM_INPUT messages to retrieve information about any
        /// touch events that occur.
        /// </summary>
        /// <param name="LParam">The WM_INPUT message to process.</param>
        private void ProcessInputCommand(IntPtr LParam)
        {
            uint dwSize = 0;

            // First call to GetRawInputData sets the value of dwSize
            // dwSize can then be used to allocate the appropriate amount of memore,
            // storing the pointer in "buffer".
            NativeMethods.GetRawInputData(LParam, NativeMethods.RID_INPUT, IntPtr.Zero,
                             ref dwSize,
                             (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                // Check that buffer points to something, and if so,
                // call GetRawInputData again to fill the allocated memory
                // with information about the input
                if (buffer == IntPtr.Zero ||
                   NativeMethods.GetRawInputData(LParam, NativeMethods.RID_INPUT,
                                     buffer,
                                     ref dwSize,
                                     (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize)
                {
                    throw new ApplicationException("GetRawInputData does not return correct size !\n.");
                }

                RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));

                ushort usage;
                if (!_validDevices.TryGetValue(raw.header.hDevice, out usage))
                {
                    if (ValidateDevice(raw.header.hDevice, out usage))
                        _validDevices.Add(raw.header.hDevice, usage);
                }

                if (usage == 0)
                    return;
                if (usage == NativeMethods.PenUsage)
                {
                    if (_ignoreTouchInputWhenUsingPen)
                        _penLastActivity = Environment.TickCount;
                    else
                        _penLastActivity = null;

                    if (_penGestureButton == 0)
                        return;

                    switch (_sourceDevice)
                    {
                        case Devices.TouchScreen:
                        case Devices.None:
                        case Devices.Pen:
                            break;
                        default:
                            return;
                    }

                    using (PenDevice penDevice = new PenDevice(buffer, ref raw))
                    {
                        DeviceStates state = penDevice.GetPenState();

                        if (_sourceDevice == Devices.None || _sourceDevice == Devices.TouchScreen)
                        {
                            if ((state & _penGestureButton) != 0)
                            {
                                _currentScr = Screen.FromPoint(Cursor.Position);
                                if (_currentScr == null)
                                    return;
                                _sourceDevice = Devices.Pen;
                                PenDevice.GetCurrentScreenOrientation();
                            }
                            else
                                return;
                        }
                        else if (_sourceDevice == Devices.Pen)
                        {
                            if ((state & _penGestureButton) == 0 || (state & DeviceStates.InRange) == 0)
                            {
                                state = DeviceStates.None;
                            }
                        }
                        penDevice.GetPhysicalMax(1);
                        Point point = penDevice.GetCoordinate(0, _currentScr);
                        _outputTouchs = new List<RawData>(1) { new RawData(state, 0, point) };
                    }
                }
                else if (usage == NativeMethods.TouchScreenUsage)
                {
                    if (_penLastActivity != null && Environment.TickCount - _penLastActivity < 100)
                        return;
                    if (_sourceDevice == Devices.None)
                    {
                        _currentScr = Screen.FromPoint(Cursor.Position);
                        if (_currentScr == null)
                            return;
                        _sourceDevice = Devices.TouchScreen;
                        TouchScreenDevice.GetCurrentScreenOrientation();
                    }
                    else if (_sourceDevice != Devices.TouchScreen)
                        return;

                    using (TouchScreenDevice touchScreen = new TouchScreenDevice(buffer, ref raw))
                    {
                        int contactCount = touchScreen.GetContactCount();
                        HidNativeApi.HIDP_LINK_COLLECTION_NODE[] linkCollection = touchScreen.GetLinkCollectionNodes();
                        touchScreen.GetPhysicalMax(linkCollection.Length);

                        if (contactCount != 0)
                        {
                            _requiringContactCount = contactCount;
                            _outputTouchs = new List<RawData>(contactCount);
                        }
                        if (_requiringContactCount == 0) return;

                        touchScreen.GetRawDatas(linkCollection[0].NumberOfChildren, _currentScr, ref _requiringContactCount, ref _outputTouchs);
                    }
                }
                else if (usage == NativeMethods.TouchPadUsage)
                {
                    if (_sourceDevice == Devices.None)
                    {
                        _currentScr = Screen.FromPoint(Cursor.Position);
                        if (_currentScr == null)
                            return;
                        _sourceDevice = Devices.TouchPad;
                    }
                    else if (_sourceDevice != Devices.TouchPad)
                        return;

                    using (TouchPadDevice touchPad = new TouchPadDevice(buffer, ref raw))
                    {
                        int contactCount = touchPad.GetContactCount();
                        HidNativeApi.HIDP_LINK_COLLECTION_NODE[] linkCollection = touchPad.GetLinkCollectionNodes();
                        touchPad.GetPhysicalMax(linkCollection.Length);

                        if (contactCount != 0)
                        {
                            _requiringContactCount = contactCount;
                            _outputTouchs = new List<RawData>(contactCount);
                        }
                        if (_requiringContactCount == 0) return;

                        touchPad.GetRawDatas(linkCollection[0].NumberOfChildren, _currentScr, ref _requiringContactCount, ref _outputTouchs);
                    }
                }

                if (_requiringContactCount == 0 && PointsIntercepted != null)
                {
                    PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs, _sourceDevice));
                    if (_outputTouchs.TrueForAll(rd => rd.State == DeviceStates.None))
                    {
                        _sourceDevice = Devices.None;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }


        #endregion ProcessInput
    }
}

