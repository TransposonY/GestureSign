using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Input;
using GestureSign.TouchInputProvider.Native;

namespace GestureSign.TouchInputProvider
{
    public class MessageWindow : Form
    {
        private bool _xAxisDirection;
        private bool _yAxisDirection;
        private bool _isAxisCorresponds;
        private int _screenHeight;
        private int _screenWidth;

        private List<RawData> _outputTouchs = new List<RawData>(1);
        private int _requiringContactCount;
        private Dictionary<IntPtr, ushort> _validDevices = new Dictionary<IntPtr, ushort>();
        private Point _physicalMax;

        private Device _sourceDevice;
        private bool _isTouchpadRegistered;

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public bool RegisterTouchPad
        {
            get { return _isTouchpadRegistered; }
            set
            {
                if (value == _isTouchpadRegistered) return;

                if (value)
                {
                    Invoke(new Action(() => RegisterDevice(NativeMethods.TouchPadUsage)));
                }
                else
                {
                    Invoke(new Action(() => UnregisterDevice(NativeMethods.TouchPadUsage)));
                }
                _isTouchpadRegistered = value;
            }
        }

        public MessageWindow()
        {
            RegisterDevice(NativeMethods.TouchScreenUsage);
            EnumerateDevices();
        }

        private void RegisterDevice(ushort usage)
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
        }

        private void UnregisterDevice(ushort usage)
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
                            if (info.hid.usUsage != NativeMethods.TouchPadUsage && info.hid.usUsage != NativeMethods.TouchScreenUsage) continue;

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
                            EnumerateDevices();
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

                if (usage == NativeMethods.TouchScreenUsage)
                {
                    if (_sourceDevice == Device.None)
                    {
                        _sourceDevice = Device.TouchScreen;
                        GetCurrentScreenOrientation();
                        GetScreenSize();
                    }
                    else if (_sourceDevice != Device.TouchScreen)
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

                                int usageLength = 0;
                                HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, null, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);
                                HidNativeApi.HIDP_DATA[] hd = new HidNativeApi.HIDP_DATA[usageLength];
                                HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, hd, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);
                                int x, y;
                                if (_isAxisCorresponds)
                                {
                                    x = physicalX * _screenWidth / _physicalMax.X;
                                    y = physicalY * _screenHeight / _physicalMax.Y;
                                }
                                else
                                {
                                    x = physicalY * _screenWidth / _physicalMax.Y;
                                    y = physicalX * _screenHeight / _physicalMax.X;
                                }
                                x = _xAxisDirection ? x : _screenWidth - x;
                                y = _yAxisDirection ? y : _screenHeight - y;
                                bool tip = hd.Length != 0 && hd[0].DataIndex == NativeMethods.TipId;
                                _outputTouchs.Add(new RawData(tip, contactIdentifier, new Point(x, y)));

                                if (--_requiringContactCount == 0) break;
                            }
                            if (_requiringContactCount == 0) break;
                        }
                    }
                }
                else if (usage == NativeMethods.TouchPadUsage)
                {
                    if (_sourceDevice == Device.None)
                    {
                        _sourceDevice = Device.TouchPad;
                        GetScreenSize();
                    }
                    else if (_sourceDevice != Device.TouchPad)
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

                                int usageLength = 0;
                                HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, null, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);
                                HidNativeApi.HIDP_DATA[] hd = new HidNativeApi.HIDP_DATA[usageLength];
                                HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, hd, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);

                                int x, y;
                                x = physicalX * _screenWidth / _physicalMax.X;
                                y = physicalY * _screenHeight / _physicalMax.Y;

                                bool tip = hd.Length != 0 && hd[0].DataIndex == NativeMethods.TipId;
                                _outputTouchs.Add(new RawData(tip, contactIdentifier, new Point(x, y)));

                                if (--_requiringContactCount == 0) break;
                            }
                            if (_requiringContactCount == 0) break;
                        }
                    }
                }

                if (_requiringContactCount == 0 && PointsIntercepted != null)
                {
                    PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs, _sourceDevice));
                    if (_outputTouchs.TrueForAll(rd => !rd.Tip))
                    {
                        _sourceDevice = Device.None;
                        _physicalMax = Point.Empty;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
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

        private void GetScreenSize()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            if (screen != null)
            {
                _screenHeight = screen.Bounds.Height;
                _screenWidth = screen.Bounds.Width;
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

