using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Input;
using GestureSign.TouchInputProvider.Native;
using Microsoft.Win32;

namespace GestureSign.TouchInputProvider
{
    public class MessageWindow : Form
    {
        private bool _xAxisDirection;
        private bool _yAxisDirection;
        private bool _isAxisCorresponds;

        private List<RawTouchData> _outputTouchs = new List<RawTouchData>(1);
        private int _requiringContactCount;

        private Dictionary<IntPtr, Point> _touchScreenPhysicalMax = new Dictionary<IntPtr, Point>(1);

        public event RawPointsDataMessageEventHandler PointsIntercepted;
        public int NumberOfTouchscreens { get; set; }

        public MessageWindow()
        {
            try
            {
                RegisterDevices();
                NumberOfTouchscreens = EnumerateDevices();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void RegisterDevices()
        {
            NativeMethods.RAWINPUTDEVICE[] rid = new NativeMethods.RAWINPUTDEVICE[1];

            rid[0].usUsagePage = NativeMethods.TouchScreenUsagePage;
            rid[0].usUsage = 0x04;
            rid[0].dwFlags = NativeMethods.RIDEV_INPUTSINK;
            rid[0].hwndTarget = Handle;

            if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).");
            }
        }

        private bool CheckDeviceIsTouchScreen(string item)
        {
            // Example Device Identification string
            // @"\??\ACPI#PNP0303#3&13c0b0c5&0#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}";
            try
            {
                // remove the \??\
                item = item.Substring(4);

                string[] split = item.Split('#');
                if (split.Length < 3)
                {
                    return false;
                }
                string id_01 = split[0]; // ACPI (Class code)
                string id_02 = split[1]; // PNP0303 (SubClass code)
                string id_03 = split[2]; // 3&13c0b0c5&0 (Protocol code)
                //The final part is the class GUID and is not needed here

                //Open the appropriate key as read-only so no permissions
                //are needed.
                RegistryKey ourKey = Registry.LocalMachine;

                string findme = string.Format(@"System\CurrentControlSet\Enum\{0}\{1}\{2}", id_01, id_02, id_03);

                ourKey = ourKey.OpenSubKey(findme, false);

                if (ourKey == null) return false;

                //Retrieve the desired information and set isKeyboard
                string deviceDesc = (string)ourKey.GetValue("DeviceDesc");

                return deviceDesc.ToLower().Contains("touch_screen");
            }
            catch
            {
                return false;
            }
        }

        private int EnumerateDevices()
        {

            int NumberOfDevices = 0;
            uint deviceCount = 0;
            int dwSize = (Marshal.SizeOf(typeof(NativeMethods.RAWINPUTDEVICELIST)));

            if (NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                try
                {
                    NativeMethods.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                    for (int i = 0; i < deviceCount; i++)
                    {
                        uint pcbSize = 0;

                        NativeMethods.RAWINPUTDEVICELIST rid = (NativeMethods.RAWINPUTDEVICELIST)Marshal.PtrToStructure(
                            new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * i),
                            typeof(NativeMethods.RAWINPUTDEVICELIST));
                        NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);

                        if (pcbSize > 0)
                        {
                            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                            try
                            {
                                NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICENAME, pData, ref pcbSize);
                                string deviceName = Marshal.PtrToStringAnsi(pData);

                                if (deviceName.ToUpper().Contains("ROOT"))
                                {
                                    continue;
                                }

                                if (rid.dwType == NativeMethods.RIM_TYPEHID)
                                {
                                    var isTouchDevice = CheckDeviceIsTouchScreen(deviceName);

                                    if (isTouchDevice && !_touchScreenPhysicalMax.ContainsKey(rid.hDevice))
                                    {
                                        NumberOfDevices++;

                                        _touchScreenPhysicalMax.Add(rid.hDevice, Point.Empty);
                                    }
                                }
                            }
                            finally
                            { Marshal.FreeHGlobal(pData); }
                        }
                    }
                }
                finally
                { Marshal.FreeHGlobal(pRawInputDeviceList); }
                return NumberOfDevices;
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
                    }
                    break;
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
                             (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            IntPtr pPreparsedData = IntPtr.Zero;
            try
            {
                // Check that buffer points to something, and if so,
                // call GetRawInputData again to fill the allocated memory
                // with information about the input
                if (buffer != IntPtr.Zero &&
                   NativeMethods.GetRawInputData(LParam, NativeMethods.RID_INPUT,
                                     buffer,
                                     ref dwSize,
                                     (uint)Marshal.SizeOf(typeof(NativeMethods.RAWINPUTHEADER))) == dwSize)
                {
                    NativeMethods.RAWINPUT raw = (NativeMethods.RAWINPUT)Marshal.PtrToStructure(buffer, typeof(NativeMethods.RAWINPUT));

                    if (!_touchScreenPhysicalMax.ContainsKey(raw.header.hDevice)) return;
                    GetCurrentScreenOrientation();

                    uint pcbSize = 0;
                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
                    pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);

                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);

                    if (_touchScreenPhysicalMax[raw.header.hDevice].Equals(Point.Empty))
                    {
                        GetPhysicalMax(raw, pPreparsedData);
                        return;
                    }

                    int contactCount = 0;

                    IntPtr pRawData = new IntPtr(buffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));

                    HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.TouchScreenUsagePage, 0, NativeMethods.ContactCountId,
                        ref contactCount, pPreparsedData, pRawData, raw.hid.dwSizHid);

                    int linkCount = 0;
                    HidNativeApi.HidP_GetLinkCollectionNodes(null, ref linkCount, pPreparsedData);
                    HidNativeApi.HIDP_LINK_COLLECTION_NODE[] lcn = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[linkCount];
                    HidNativeApi.HidP_GetLinkCollectionNodes(lcn, ref linkCount, pPreparsedData);

                    if (contactCount != 0)
                    {
                        _requiringContactCount = contactCount;
                        _outputTouchs = new List<RawTouchData>(contactCount);
                    }
                    if (_requiringContactCount == 0) return;
                    int contactIdentifier = 0;
                    int physicalX = 0;
                    int physicalY = 0;
                    int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                    int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                    for (int dwIndex = 0; dwIndex < raw.hid.dwCount; dwIndex++)
                    {
                        for (short nodeIndex = 1; nodeIndex <= lcn[0].NumberOfChildren; nodeIndex++)
                        {
                            IntPtr pRawDataPacket = new IntPtr(pRawData.ToInt64() + dwIndex * raw.hid.dwSizHid);
                            HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.TouchScreenUsagePage, nodeIndex, NativeMethods.ContactIdentifierId, ref contactIdentifier, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, nodeIndex, NativeMethods.XCoordinateId, ref physicalX, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, nodeIndex, NativeMethods.YCoordinateId, ref physicalY, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);

                            int usageLength = 0;
                            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.TouchScreenUsagePage, nodeIndex, null, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);
                            HidNativeApi.HIDP_DATA[] hd = new HidNativeApi.HIDP_DATA[usageLength];
                            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.TouchScreenUsagePage, nodeIndex, hd, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);
                            int x, y;
                            if (_isAxisCorresponds)
                            {
                                x = physicalX * screenWidth / _touchScreenPhysicalMax[raw.header.hDevice].X;
                                y = physicalY * screenHeight / _touchScreenPhysicalMax[raw.header.hDevice].Y;
                            }
                            else
                            {
                                x = physicalY * screenWidth / _touchScreenPhysicalMax[raw.header.hDevice].Y;
                                y = physicalX * screenHeight / _touchScreenPhysicalMax[raw.header.hDevice].X;
                            }

                            x = _xAxisDirection ? x : screenWidth - x;
                            y = _yAxisDirection ? y : screenHeight - y;
                            bool tip = hd.Length != 0 && hd[0].DataIndex == NativeMethods.TipId;
                            _outputTouchs.Add(new RawTouchData(tip, contactIdentifier, new Point(x, y)));

                            if (--_requiringContactCount == 0) break;
                        }
                        if (_requiringContactCount == 0) break;
                    }

                    if (_requiringContactCount == 0 && PointsIntercepted != null)
                    {
                        PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs));
                    }
                }
                else throw new ApplicationException("GetRawInputData does not return correct size !\n.");
            }
            finally
            {
                Marshal.FreeHGlobal(pPreparsedData);
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

        private void GetPhysicalMax(NativeMethods.RAWINPUT rawInput, IntPtr pPreparsedData)
        {
            short valueCapsLength = 1;
            Point p = new Point();
            HidNativeApi.HidP_Value_Caps[] hvc = new HidNativeApi.HidP_Value_Caps[valueCapsLength];

            HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 1, NativeMethods.XCoordinateId, hvc, ref valueCapsLength, pPreparsedData);
            p.X = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;

            HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 1, NativeMethods.YCoordinateId, hvc, ref valueCapsLength, pPreparsedData);
            p.Y = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;

            _touchScreenPhysicalMax[rawInput.header.hDevice] = p;
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

