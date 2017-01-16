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

        private List<RawData> _outputTouchs = new List<RawData>(1);
        private int _requiringContactCount;

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public MessageWindow()
        {
            RegisterDevices();
        }

        private void RegisterDevices()
        {
            NativeMethods.RAWINPUTDEVICE[] rid = new NativeMethods.RAWINPUTDEVICE[1];

            rid[0].usUsagePage = NativeMethods.TouchScreenUsagePage;
            rid[0].usUsage = 0x04;
            rid[0].dwFlags = NativeMethods.RIDEV_INPUTSINK | NativeMethods.RIDEV_DEVNOTIFY;
            rid[0].hwndTarget = Handle;

            if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).");
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

                    GetCurrentScreenOrientation();

                    uint pcbSize = 0;
                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
                    pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
                    NativeMethods.GetRawInputDeviceInfo(raw.header.hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);

                    int contactCount = 0;
                    IntPtr pRawData = new IntPtr(buffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));
                    HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.TouchScreenUsagePage, 0, NativeMethods.ContactCountId,
                        ref contactCount, pPreparsedData, pRawData, raw.hid.dwSizHid);
                    int linkCount = 0;
                    HidNativeApi.HidP_GetLinkCollectionNodes(null, ref linkCount, pPreparsedData);
                    HidNativeApi.HIDP_LINK_COLLECTION_NODE[] lcn = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[linkCount];
                    HidNativeApi.HidP_GetLinkCollectionNodes(lcn, ref linkCount, pPreparsedData);

                    Point screenPhysicalMax = GetPhysicalMax(linkCount, pPreparsedData);

                    if (contactCount != 0)
                    {
                        _requiringContactCount = contactCount;
                        _outputTouchs = new List<RawData>(contactCount);
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
                                x = physicalX * screenWidth / screenPhysicalMax.X;
                                y = physicalY * screenHeight / screenPhysicalMax.Y;
                            }
                            else
                            {
                                x = physicalY * screenWidth / screenPhysicalMax.Y;
                                y = physicalX * screenHeight / screenPhysicalMax.X;
                            }
                            x = _xAxisDirection ? x : screenWidth - x;
                            y = _yAxisDirection ? y : screenHeight - y;
                            bool tip = hd.Length != 0 && hd[0].DataIndex == NativeMethods.TipId;
                            _outputTouchs.Add(new RawData(tip, contactIdentifier, new Point(x, y)));

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

