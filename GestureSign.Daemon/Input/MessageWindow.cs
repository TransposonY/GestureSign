using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Input
{
    public class MessageWindow : Form
    {
        private bool _xAxisDirection;
        private bool _yAxisDirection;
        private bool _isAxisCorresponds;
        private Screen _currentScr;

        private List<RawData> _outputTouchs = new List<RawData>(1);
        private int _requiringContactCount;
        private Dictionary<IntPtr, ushort> _validDevices = new Dictionary<IntPtr, ushort>();
        private Point _physicalMax;

        private Devices _sourceDevice;
        private List<ushort> _registeredDeviceList = new List<ushort>(1);
        private int? _penLastActivity;
        private bool _ignoreTouchInputWhenUsingPen;
        private DeviceStates _penGestureButton;

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public MessageWindow()
        {
            CreateHandle();
            UpdateRegistration();
        }

        public void UpdateRegistration()
        {
            EnumerateDevices();

            _ignoreTouchInputWhenUsingPen = AppConfig.IgnoreTouchInputWhenUsingPen;
            var penSetting = AppConfig.PenGestureButton;
            _penGestureButton = penSetting & (DeviceStates.Invert | DeviceStates.RightClickButton);

            UpdateRegisterState(true, NativeMethods.TouchScreenUsage);
            UpdateRegisterState(_ignoreTouchInputWhenUsingPen || _penGestureButton != 0 && (penSetting & (DeviceStates.InRange | DeviceStates.Tip)) != 0, NativeMethods.PenUsage);
            UpdateRegisterState(AppConfig.RegisterTouchPad, NativeMethods.TouchPadUsage);
        }

        private void UpdateRegisterState(bool register, ushort usage)
        {
            if (_validDevices.Values.Contains(usage) && register)
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
            if (!_registeredDeviceList.Contains(usage))
            {
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

        private void EnumerateDevices()
        {
            uint deviceCount = 0;
            int dwSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

            if (NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                using (new SafeUnmanagedMemoryHandle(pRawInputDeviceList))
                {
                    _validDevices.Clear();

                    NativeMethods.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                    for (int i = 0; i < deviceCount; i++)
                    {
                        RAWINPUTDEVICELIST rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * i), typeof(RAWINPUTDEVICELIST));
                        if (rid.dwType != NativeMethods.RIM_TYPEHID) continue;

                        uint pcbSize = 0;
                        NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);
                        if (pcbSize <= 0) continue;

                        IntPtr pInfo = Marshal.AllocHGlobal((int)pcbSize);
                        using (new SafeUnmanagedMemoryHandle(pInfo))
                        {
                            NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICEINFO, pInfo, ref pcbSize);
                            var info = (RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RID_DEVICE_INFO));
                            if (info.hid.usUsage != NativeMethods.TouchPadUsage && info.hid.usUsage != NativeMethods.TouchScreenUsage && info.hid.usUsage != NativeMethods.PenUsage) continue;

                            NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                            if (pcbSize <= 0) continue;

                            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                            using (new SafeUnmanagedMemoryHandle(pData))
                            {
                                NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICENAME, pData, ref pcbSize);
                                string deviceName = Marshal.PtrToStringAnsi(pData);

                                if (string.IsNullOrEmpty(deviceName) || deviceName.IndexOf("VIRTUAL_DIGITIZER", StringComparison.OrdinalIgnoreCase) >= 0 || deviceName.IndexOf("ROOT", StringComparison.OrdinalIgnoreCase) >= 0)
                                    continue;

                                _validDevices.Add(rid.hDevice, info.hid.usUsage);
                            }
                        }
                    }
                }
            }
            else
            {
                throw new ApplicationException("Error!");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ChangeToMessageOnlyWindow();
        }

        private void ChangeToMessageOnlyWindow()
        {
            IntPtr HWND_MESSAGE = new IntPtr(-3);
            NativeMethods.SetParent(this.Handle, HWND_MESSAGE);
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
                        //GIDC_ARRIVAL=1
                        if (message.WParam.ToInt32() == 1)
                            UpdateRegistration();
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
                if (!_validDevices.TryGetValue(raw.header.hDevice, out usage)) return;

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

                    uint pcbSize = 0;
                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
                    IntPtr pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
                    using (new SafeUnmanagedMemoryHandle(pPreparsedData))
                    {
                        NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);
                        IntPtr pRawData = new IntPtr(buffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));

                        ushort[] usageList = GetButtonList(pPreparsedData, pRawData, 0, raw.hid.dwSizHid);
                        DeviceStates state = DeviceStates.None;
                        foreach (var u in usageList)
                        {
                            switch (u)
                            {
                                case NativeMethods.TipId:
                                    state |= DeviceStates.Tip;
                                    break;
                                case NativeMethods.InRangeId:
                                    state |= DeviceStates.InRange;
                                    break;
                                case NativeMethods.BarrelButtonId:
                                    state |= DeviceStates.RightClickButton;
                                    break;
                                case NativeMethods.InvertId:
                                    state |= DeviceStates.Invert;
                                    break;
                                case NativeMethods.EraserId:
                                    state |= DeviceStates.Eraser;
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (_sourceDevice == Devices.None || _sourceDevice == Devices.TouchScreen)
                        {
                            if ((state & _penGestureButton) != 0)
                            {
                                _currentScr = Screen.FromPoint(Cursor.Position);
                                if (_currentScr == null)
                                    return;
                                _sourceDevice = Devices.Pen;
                                GetCurrentScreenOrientation();
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
                        if (_physicalMax.IsEmpty)
                            _physicalMax = GetPhysicalMax(1, pPreparsedData);

                        int physicalX = 0;
                        int physicalY = 0;

                        HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.XCoordinateId, ref physicalX, pPreparsedData, pRawData, raw.hid.dwSizHid);
                        HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.YCoordinateId, ref physicalY, pPreparsedData, pRawData, raw.hid.dwSizHid);

                        int x, y;
                        if (_isAxisCorresponds)
                        {
                            x = physicalX * _currentScr.Bounds.Width / _physicalMax.X;
                            y = physicalY * _currentScr.Bounds.Height / _physicalMax.Y;
                        }
                        else
                        {
                            x = physicalY * _currentScr.Bounds.Width / _physicalMax.Y;
                            y = physicalX * _currentScr.Bounds.Height / _physicalMax.X;
                        }
                        x = _xAxisDirection ? x : _currentScr.Bounds.Width - x;
                        y = _yAxisDirection ? y : _currentScr.Bounds.Height - y;
                        _outputTouchs = new List<RawData>(1);
                        _outputTouchs.Add(new RawData(state, 0, new Point(x + _currentScr.Bounds.X, y + _currentScr.Bounds.Y)));
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
                        GetCurrentScreenOrientation();
                    }
                    else if (_sourceDevice != Devices.TouchScreen)
                        return;

                    uint pcbSize = 0;
                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);

                    IntPtr pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
                    using (new SafeUnmanagedMemoryHandle(pPreparsedData))
                    {
                        NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);

                        int contactCount = 0;
                        IntPtr pRawData = new IntPtr(buffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));
                        HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, 0, NativeMethods.ContactCountId,
                            ref contactCount, pPreparsedData, pRawData, raw.hid.dwSizHid);
                        int linkCount = 0;
                        HidNativeApi.HidP_GetLinkCollectionNodes(null, ref linkCount, pPreparsedData);
                        HidNativeApi.HIDP_LINK_COLLECTION_NODE[] lcn = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[linkCount];
                        HidNativeApi.HidP_GetLinkCollectionNodes(lcn, ref linkCount, pPreparsedData);

                        if (_physicalMax.IsEmpty)
                            _physicalMax = GetPhysicalMax(linkCount, pPreparsedData);

                        if (contactCount != 0)
                        {
                            _requiringContactCount = contactCount;
                            _outputTouchs = new List<RawData>(contactCount);
                        }
                        if (_requiringContactCount == 0) return;
                        int contactIdentifier = 0;
                        int physicalX = 0;
                        int physicalY = 0;
                        for (int dwIndex = 0; dwIndex < raw.hid.dwCount; dwIndex++)
                        {
                            for (short nodeIndex = 1; nodeIndex <= lcn[0].NumberOfChildren; nodeIndex++)
                            {
                                IntPtr pRawDataPacket = new IntPtr(pRawData.ToInt64() + dwIndex * raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, NativeMethods.ContactIdentifierId, ref contactIdentifier, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, nodeIndex, NativeMethods.XCoordinateId, ref physicalX, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, nodeIndex, NativeMethods.YCoordinateId, ref physicalY, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);

                                ushort[] usageList = GetButtonList(pPreparsedData, pRawData, nodeIndex, raw.hid.dwSizHid);

                                int x, y;
                                if (_isAxisCorresponds)
                                {
                                    x = physicalX * _currentScr.Bounds.Width / _physicalMax.X;
                                    y = physicalY * _currentScr.Bounds.Height / _physicalMax.Y;
                                }
                                else
                                {
                                    x = physicalY * _currentScr.Bounds.Width / _physicalMax.Y;
                                    y = physicalX * _currentScr.Bounds.Height / _physicalMax.X;
                                }
                                x = _xAxisDirection ? x : _currentScr.Bounds.Width - x;
                                y = _yAxisDirection ? y : _currentScr.Bounds.Height - y;
                                bool tip = usageList.Length != 0 && usageList[0] == NativeMethods.TipId;
                                _outputTouchs.Add(new RawData(tip ? DeviceStates.Tip : DeviceStates.None, contactIdentifier, new Point(x + _currentScr.Bounds.X, y + _currentScr.Bounds.Y)));

                                if (--_requiringContactCount == 0) break;
                            }
                            if (_requiringContactCount == 0) break;
                        }
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

                    uint pcbSize = 0;
                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
                    IntPtr pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
                    using (new SafeUnmanagedMemoryHandle(pPreparsedData))
                    {
                        NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);

                        int contactCount = 0;
                        IntPtr pRawData = new IntPtr(buffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));
                        HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, 0, NativeMethods.ContactCountId,
                            ref contactCount, pPreparsedData, pRawData, raw.hid.dwSizHid);

                        int linkCount = 0;
                        HidNativeApi.HidP_GetLinkCollectionNodes(null, ref linkCount, pPreparsedData);
                        HidNativeApi.HIDP_LINK_COLLECTION_NODE[] lcn = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[linkCount];
                        HidNativeApi.HidP_GetLinkCollectionNodes(lcn, ref linkCount, pPreparsedData);

                        if (_physicalMax.IsEmpty)
                            _physicalMax = GetPhysicalMax(linkCount, pPreparsedData);

                        if (contactCount != 0)
                        {
                            _requiringContactCount = contactCount;
                            _outputTouchs = new List<RawData>(contactCount);
                        }
                        if (_requiringContactCount == 0) return;

                        int contactIdentifier = 0;
                        int physicalX = 0;
                        int physicalY = 0;

                        for (int dwIndex = 0; dwIndex < raw.hid.dwCount; dwIndex++)
                        {
                            for (short nodeIndex = 1; nodeIndex <= lcn[0].NumberOfChildren; nodeIndex++)
                            {
                                IntPtr pRawDataPacket = new IntPtr(pRawData.ToInt64() + dwIndex * raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, NativeMethods.ContactIdentifierId, ref contactIdentifier, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, nodeIndex, NativeMethods.XCoordinateId, ref physicalX, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, nodeIndex, NativeMethods.YCoordinateId, ref physicalY, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);

                                ushort[] usageList = GetButtonList(pPreparsedData, pRawData, nodeIndex, raw.hid.dwSizHid);

                                int x, y;
                                x = physicalX * _currentScr.Bounds.Width / _physicalMax.X;
                                y = physicalY * _currentScr.Bounds.Height / _physicalMax.Y;

                                bool tip = usageList.Length != 0 && usageList[0] == NativeMethods.TipId;
                                _outputTouchs.Add(new RawData(tip ? DeviceStates.Tip : DeviceStates.None, contactIdentifier, new Point(x + _currentScr.Bounds.X, y + _currentScr.Bounds.Y)));

                                if (--_requiringContactCount == 0) break;
                            }
                            if (_requiringContactCount == 0) break;
                        }
                    }
                }

                if (_requiringContactCount == 0 && PointsIntercepted != null)
                {
                    PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs, _sourceDevice));
                    if (_outputTouchs.TrueForAll(rd => rd.State == DeviceStates.None))
                    {
                        _sourceDevice = Devices.None;
                        _physicalMax = Point.Empty;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static ushort[] GetButtonList(IntPtr pPreparsedData, IntPtr pRawData, short nodeIndex, int rawDateSize)
        {
            int usageLength = 0;
            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, null, ref usageLength, pPreparsedData, pRawData, rawDateSize);
            var usageList = new ushort[usageLength];
            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, usageList, ref usageLength, pPreparsedData, pRawData, rawDateSize);
            return usageList;
        }

        private void GetCurrentScreenOrientation()
        {
            switch (SystemInformation.ScreenOrientation)
            {
                case ScreenOrientation.Angle0:
                    _xAxisDirection = _yAxisDirection = true;
                    _isAxisCorresponds = true;
                    break;
                case ScreenOrientation.Angle90:
                    _isAxisCorresponds = false;
                    _xAxisDirection = false;
                    _yAxisDirection = true;
                    break;
                case ScreenOrientation.Angle180:
                    _xAxisDirection = _yAxisDirection = false;
                    _isAxisCorresponds = true;
                    break;
                case ScreenOrientation.Angle270:
                    _isAxisCorresponds = false;
                    _xAxisDirection = true;
                    _yAxisDirection = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Point GetPhysicalMax(int collectionCount, IntPtr pPreparsedData)
        {
            short valueCapsLength = (short)collectionCount;
            Point p = new Point();
            HidNativeApi.HidP_Value_Caps[] hvc = new HidNativeApi.HidP_Value_Caps[valueCapsLength];

            HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.XCoordinateId, hvc, ref valueCapsLength, pPreparsedData);
            p.X = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;

            HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.YCoordinateId, hvc, ref valueCapsLength, pPreparsedData);
            p.Y = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;
            return p;
        }

        #endregion ProcessInput

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateParams myParams = base.CreateParams;
                myParams.ExStyle = myParams.ExStyle | WS_EX_NOACTIVATE;
                return myParams;
            }
        }
    }
}

