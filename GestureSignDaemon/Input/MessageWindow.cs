using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using Microsoft.Win32;

namespace GestureSignDaemon.Input
{
    public class MessageWindow : Form
    {

        #region const definitions

        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;

        private const int RIM_TYPEHID = 2;

        private const uint RIDI_DEVICENAME = 0x20000007;
        private const uint RIDI_PREPARSEDDATA = 0x20000005;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_INPUT = 0x00FF;
        private const int VK_OEM_CLEAR = 0xFE;
        private const int VK_LAST_KEY = VK_OEM_CLEAR; // this is a made up value used as a sentinal

        private const int

            WM_PARENTNOTIFY = 0x0210,
            WM_NCPOINTERUPDATE = 0x0241,
            WM_NCPOINTERDOWN = 0x0242,
            WM_NCPOINTERUP = 0x0243,
            WM_POINTERUPDATE = 0x0245,
            WM_POINTERDOWN = 0x0246,
            WM_POINTERUP = 0x0247,
            WM_POINTERENTER = 0x0249,
            WM_POINTERLEAVE = 0x024A,
            WM_POINTERACTIVATE = 0x024B,
            WM_POINTERCAPTURECHANGED = 0x024C,
            WM_POINTERWHEEL = 0x024E,
            WM_POINTERHWHEEL = 0x024F;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint ANRUS_TOUCH_MODIFICATION_ACTIVE = 0x0000002;

        private const ushort GenericDesktopPage = 0x01;
        private const ushort TouchScreenUsagePage = 0x0D;
        private const ushort ContactIdentifierId = 0x51;
        private const ushort ContactCountId = 0x54;
        private const ushort ScanTimeId = 0x56;
        private const ushort TipId = 0x42;
        private const ushort XCoordinateId = 0x30;
        private const ushort YCoordinateId = 0x31;
        #endregion const definitions

        bool _xAxisDirection;
        bool _yAxisDirection;
        bool _isAxisCorresponds;
        public event RawPointsDataMessageEventHandler PointsIntercepted;
        public event EventHandler<PointerMessageEventArgs> PointerIntercepted;
        public event EventHandler<IntPtr> OnForegroundChange;
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        readonly WinEventDelegate _winEventDele;
        private readonly IntPtr _hWinEventHook;

        List<RawTouchData> _outputTouchs = new List<RawTouchData>(1);
        int _requiringContactCount;
        bool _isRegistered;
        bool _isPointerMove;
        POINT _lastPoint;
        private Dictionary<IntPtr, Point> _touchScreenPhysicalMax = new Dictionary<IntPtr, Point>(1);

        public bool IsRegistered
        {
            get { return _isRegistered; }
            private set
            {
                if (value)
                {
                    if (_isRegistered) return;
                    if (RegisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
                        InitializeTouchInjection(10, TOUCH_FEEDBACK.NONE);

                        AccSetRunningUtilityState(Handle, ANRUS_TOUCH_MODIFICATION_ACTIVE, ANRUS_TOUCH_MODIFICATION_ACTIVE);
                        _isRegistered = true;
                    }
                }
                else
                {
                    if (_isRegistered && UnregisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                    {
                        AccSetRunningUtilityState(Handle, 0, 0);
                        _isRegistered = false;
                    }
                }
            }
        }
        public int NumberOfTouchscreens { get; set; }

        #region DllImports

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("User32.dll")]
        extern static uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("User32.dll")]
        extern static bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        extern static uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        extern static uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);



        [DllImport("User32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterPointerInputTarget(IntPtr handle, POINTER_INPUT_TYPE pointerType);

        [DllImport("User32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterPointerInputTarget(IntPtr hwnd, POINTER_INPUT_TYPE pointerType);
        [DllImport("Oleacc.dll")]
        static extern int AccSetRunningUtilityState(IntPtr hWnd, uint dwUtilityStateMask, uint dwUtilityState);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetPointerFrameInfo(int pointerID, ref int pointerCount, [MarshalAs(UnmanagedType.LPArray), In, Out] POINTER_INFO[] pointerInfo);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool InitializeTouchInjection(int maxCount, TOUCH_FEEDBACK feedbackMode);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool InjectTouchInput(int count, [MarshalAs(UnmanagedType.LPArray), In] POINTER_TOUCH_INFO[] contacts);


        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern bool GetPointerDeviceRects(IntPtr device, out RECT pointerDeviceRect, out RECT displayRect);
        #endregion DllImports

        #region  Windows.h structure declarations

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {

            /// RAWINPUTHEADER->tagRAWINPUTHEADER
            public RAWINPUTHEADER header;
            public RAWHID hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWHID
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwSizHid;
            [MarshalAs(UnmanagedType.U4)]
            public int dwCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
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
        public struct RAWINPUTHEADER
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
            [MarshalAs(UnmanagedType.U4)]
            public int dwSize;
            public IntPtr hDevice;
            public int wParam;
        }

        #endregion Windows.h structure declarations

        public MessageWindow()
        {
            if (AppConfig.IsInsideProgramFiles)
            {
                _winEventDele = WinEventProc;
                _hWinEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventDele, 0, 0, WINEVENT_OUTOFCONTEXT);
            }
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

        ~MessageWindow()
        {
            if (_hWinEventHook != IntPtr.Zero)
                UnhookWinEvent(_hWinEventHook);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (OnForegroundChange != null) OnForegroundChange(this, hwnd);
        }
        public void Unregister()
        {
            if (IsRegistered)
                Invoke(new Action(() => IsRegistered = false));
        }
        public void ToggleRegister(object sender, bool e)
        {
            if (e && !AppConfig.InterceptTouchInput) return;
            Invoke(new Action(() => IsRegistered = e));
        }

        private void RegisterDevices()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

            rid[0].usUsagePage = TouchScreenUsagePage;
            rid[0].usUsage = 0x04;
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = Handle;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
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
            int dwSize = (Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));

            if (GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                for (int i = 0; i < deviceCount; i++)
                {
                    uint pcbSize = 0;

                    RAWINPUTDEVICELIST rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(
                        IntPtr.Add(pRawInputDeviceList, dwSize * i),
                        typeof(RAWINPUTDEVICELIST));
                    GetRawInputDeviceInfo(rid.hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);

                    if (pcbSize > 0)
                    {
                        IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                        GetRawInputDeviceInfo(rid.hDevice, RIDI_DEVICENAME, pData, ref pcbSize);
                        string deviceName = Marshal.PtrToStringAnsi(pData);

                        if (deviceName.ToUpper().Contains("ROOT"))
                        {
                            continue;
                        }

                        if (rid.dwType == RIM_TYPEHID)
                        {
                            var isTouchDevice = CheckDeviceIsTouchScreen(deviceName);

                            if (isTouchDevice && !_touchScreenPhysicalMax.ContainsKey(rid.hDevice))
                            {
                                NumberOfDevices++;

                                RECT touchScreenRect;
                                RECT displayResolution;
                                GetPointerDeviceRects(rid.hDevice, out touchScreenRect, out displayResolution);
                                var screenOrientation = SystemInformation.ScreenOrientation;
                                if (screenOrientation == ScreenOrientation.Angle0 ||
                                    screenOrientation == ScreenOrientation.Angle180)
                                {
                                    _touchScreenPhysicalMax.Add(rid.hDevice, new Point(touchScreenRect.Right / 10, touchScreenRect.Bottom / 10));
                                }
                                else
                                {
                                    _touchScreenPhysicalMax.Add(rid.hDevice, new Point(touchScreenRect.Bottom / 10, touchScreenRect.Right / 10));
                                }

                            }
                        }
                        Marshal.FreeHGlobal(pData);
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
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
            SetParent(this.Handle, HWND_MESSAGE);
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case WM_INPUT:
                    {
                        if (!AppConfig.CompatibilityMode)
                            ProcessInputCommand(message.LParam);
                    }
                    break;
                //case WM_POINTERENTER:
                //case WM_POINTERLEAVE:
                //case WM_POINTERCAPTURECHANGED:
                case WM_POINTERDOWN:
                    _isPointerMove = false;
                    _lastPoint.X = _lastPoint.Y = 0;
                    if (ProcessPointerMessage(message)) return;
                    break;
                case WM_POINTERUP:
                case WM_POINTERUPDATE:
                    _isPointerMove |= ProcessPointerMessage(message);
                    if (_isPointerMove) return;
                    else break;

            }
            base.WndProc(ref message);
        }

        #region ProcessInput
        private void CheckLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode != 0)
            {
                throw new Win32Exception(errCode);
            }
        }
        private bool ProcessPointerMessage(Message message)
        {
            bool pointChanged = false;
            int pointerId = (int)(message.WParam.ToInt64() & 0xffff);
            int pCount = 0;
            try
            {
                if (!GetPointerFrameInfo(pointerId, ref pCount, null))
                {
                    CheckLastError();
                }
                POINTER_INFO[] pointerInfos = new POINTER_INFO[pCount];
                if (!GetPointerFrameInfo(pointerId, ref pCount, pointerInfos))
                {
                    CheckLastError();
                }
                if (PointerIntercepted != null && AppConfig.CompatibilityMode)
                {
                    PointerIntercepted(this, new PointerMessageEventArgs(pointerInfos));
                }

                if (pCount == 1)
                {
                    if (_lastPoint.X == 0 && _lastPoint.Y == 0) _lastPoint = pointerInfos[0].PtPixelLocation;
                    else
                    {
                        pointChanged = pointerInfos[0].PtPixelLocation.X != _lastPoint.X || pointerInfos[0].PtPixelLocation.Y != _lastPoint.Y;
                        _lastPoint = pointerInfos[0].PtPixelLocation;
                    }

                    POINTER_TOUCH_INFO[] ptis = new POINTER_TOUCH_INFO[pCount];
                    for (int i = 0; i < ptis.Length; i++)
                    {
                        ptis[i].TouchFlags = TOUCH_FLAGS.NONE;
                        ptis[i].PointerInfo = new POINTER_INFO
                        {
                            pointerType = POINTER_INPUT_TYPE.TOUCH,
                            PointerFlags = pointerInfos[i].PointerFlags.HasFlag(POINTER_FLAGS.UPDATE) ?
                            POINTER_FLAGS.INCONTACT | POINTER_FLAGS.INRANGE | POINTER_FLAGS.UPDATE : pointerInfos[i].PointerFlags.HasFlag(POINTER_FLAGS.UP) ?
                            POINTER_FLAGS.UP : POINTER_FLAGS.DOWN | POINTER_FLAGS.INRANGE | POINTER_FLAGS.INCONTACT,
                            PtPixelLocation = pointerInfos[i].PtPixelLocation,
                        };

                    }
                    InjectTouchInput(1, ptis);
                }
                else pointChanged = true;
            }
            catch (Win32Exception) { }
            return pointChanged;
        }

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
            GetRawInputData(LParam,
                             RID_INPUT, IntPtr.Zero,
                             ref dwSize,
                             (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            IntPtr pPreparsedData = IntPtr.Zero;
            try
            {
                // Check that buffer points to something, and if so,
                // call GetRawInputData again to fill the allocated memory
                // with information about the input
                if (buffer != IntPtr.Zero &&
                    GetRawInputData(LParam,
                                     RID_INPUT,
                                     buffer,
                                     ref dwSize,
                                     (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));

                    if (_touchScreenPhysicalMax.ContainsKey(raw.header.hDevice))
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
                            default: break;
                        }

                        uint pcbSize = 0;
                        GetRawInputDeviceInfo(raw.header.hDevice, RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
                        pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
                        GetRawInputDeviceInfo(raw.header.hDevice, RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);
                        int scanTime = 0;
                        int contactCount = 0;

                        IntPtr pRawData = IntPtr.Add(buffer, raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount);

                        HidNativeApi.HidP_GetUsageValue(HidReportType.Input, TouchScreenUsagePage, 0, ContactCountId,
                            ref contactCount, pPreparsedData, pRawData, raw.hid.dwSizHid);
                        HidNativeApi.HidP_GetUsageValue(HidReportType.Input, TouchScreenUsagePage, 0, ScanTimeId,
                            ref scanTime, pPreparsedData, pRawData, raw.hid.dwSizHid);

                        HidNativeApi.HIDP_CAPS capabilities = new HidNativeApi.HIDP_CAPS();
                        HidNativeApi.HidP_GetCaps(pPreparsedData, ref capabilities);
                        int linkCount = capabilities.NumberLinkCollectionNodes;
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
                        int usageLength = capabilities.NumberInputButtonCaps / lcn[0].NumberOfChildren;
                        if (usageLength == 0) usageLength = 1;
                        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                        int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                        for (int dwIndex = 0; dwIndex < raw.hid.dwCount; dwIndex++)
                        {
                            for (short nodeIndex = 1; nodeIndex <= lcn[0].NumberOfChildren; nodeIndex++)
                            {
                                IntPtr pRawDataPacket = IntPtr.Add(pRawData, dwIndex * raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetUsageValue(HidReportType.Input, TouchScreenUsagePage, nodeIndex, ContactIdentifierId, ref contactIdentifier, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, GenericDesktopPage, nodeIndex, XCoordinateId, ref physicalX, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);
                                HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, GenericDesktopPage, nodeIndex, YCoordinateId, ref physicalY, pPreparsedData, pRawDataPacket, raw.hid.dwSizHid);


                                HidNativeApi.HIDP_DATA[] hd = new HidNativeApi.HIDP_DATA[usageLength];
                                HidNativeApi.HidP_GetUsages(HidReportType.Input, TouchScreenUsagePage, nodeIndex, hd, ref usageLength, pPreparsedData, pRawData, raw.hid.dwSizHid);
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

                                _outputTouchs.Add(new RawTouchData(hd[0].DataIndex == TipId, contactIdentifier, new Point(x, y)));

                                if (--_requiringContactCount == 0) break;
                            }
                            if (_requiringContactCount == 0) break;
                        }

                        if (_requiringContactCount == 0 && PointsIntercepted != null)
                        {
                            PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs.OrderBy(rtd => rtd.ContactIdentifier).ToArray(), scanTime));
                        }

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

