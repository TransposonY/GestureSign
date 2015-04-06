using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GestureSign.Common.Input;
using System.Drawing;
using System.Runtime.InteropServices;

using Microsoft.Win32;

using GestureSign.Common.Configuration;
using System.Threading;

namespace GestureSignDaemon.Input
{
    public class MessageWindow : Form
    {

        #region const definitions

        private const int RIDEV_INPUTSINK = 0x00000100;
        private const int RID_INPUT = 0x10000003;

        private const int FAPPCOMMAND_MASK = 0xF000;
        private const int FAPPCOMMAND_MOUSE = 0x8000;
        private const int FAPPCOMMAND_OEM = 0x1000;

        private const int RIM_TYPEMOUSE = 0;
        private const int RIM_TYPEKEYBOARD = 1;
        private const int RIM_TYPEHID = 2;

        private const int RIDI_DEVICENAME = 0x20000007;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_INPUT = 0x00FF;
        private const int VK_OEM_CLEAR = 0xFE;
        private const int VK_LAST_KEY = VK_OEM_CLEAR; // this is a made up value used as a sentinal

        const int

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
           WM_POINTERHWHEEL = 0x024F,

           // WM_POINTERACTIVATE return codes
           PA_ACTIVATE = 1,
           PA_NOACTIVATE = 3,

           MAX_TOUCH_COUNT = 256;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        #endregion const definitions

        static bool? XAxisDirection = null;
        static bool? YAxisDirection = null;
        static bool? IsAxisCorresponds = null;
        public event RawPointsDataMessageEventHandler PointsIntercepted;
        public event EventHandler<PointerMessageEventArgs> PointerIntercepted;
        public event EventHandler<IntPtr> OnForegroundChange;
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        WinEventDelegate dele;
        InitializationRatio initializationRatio;

        Type touchDataType;
        List<RawTouchData> outputTouchs = new List<RawTouchData>(1);
        int requiringTouchDataCount = 0;
        int touchdataCount = 0;
        int touchlength = 0;
        bool isRegistered = false;

        public bool IsRegistered
        {
            get { return isRegistered; }
            private set
            {
                if (value)
                {
                    if (isRegistered) return;
                    if (RegisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                        isRegistered = true;
                }
                else
                {
                    if (isRegistered && UnregisterPointerInputTarget(Handle, POINTER_INPUT_TYPE.TOUCH))
                        isRegistered = false;
                }
            }
        }
        public int NumberOfTouchscreens { get; set; }

        #region DllImports

        [System.Runtime.InteropServices.DllImport("user32.dll")]
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


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetPointerFrameInfo(int pointerID, ref int pointerCount, [MarshalAs(UnmanagedType.LPArray), In, Out] POINTER_INFO[] pointerInfo);


        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        #endregion DllImports

        #region  Windows.h structure declarations

        [StructLayoutAttribute(LayoutKind.Sequential)]
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
            public uint dwSizHid;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwCount;
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


            System.Diagnostics.Debug.WriteLine("size:" + Marshal.SizeOf(typeof(ntrgTouchData)));
            var accessHandle = this.Handle;
            dele = WinEventProc;
            IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);

            try
            {
                RegisterDevices(accessHandle);
                NumberOfTouchscreens = EnumerateDevices();
                if (AppConfig.XRatio == 0 && NumberOfTouchscreens > 0)
                {
                    IsRegistered = true;
                    initializationRatio = new InitializationRatio(this);
                }
            }
            catch (Exception) { }
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

        private void RegisterDevices(IntPtr hwnd)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

            rid[0].usUsagePage = 0x0D;
            rid[0].usUsage = 0x04;
            rid[0].dwFlags = RIDEV_INPUTSINK;
            rid[0].hwndTarget = hwnd;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).");
            }
        }

        private static string ReadReg(string item, ref bool isTouchScreen)
        {
            // Example Device Identification string
            // @"\??\ACPI#PNP0303#3&13c0b0c5&0#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}";

            // remove the \??\
            item = item.Substring(4);

            string[] split = item.Split('#');

            string id_01 = split[0];    // ACPI (Class code)
            string id_02 = split[1];    // PNP0303 (SubClass code)
            string id_03 = split[2];    // 3&13c0b0c5&0 (Protocol code)
            //The final part is the class GUID and is not needed here

            //Open the appropriate key as read-only so no permissions
            //are needed.
            RegistryKey OurKey = Registry.LocalMachine;

            string findme = string.Format(@"System\CurrentControlSet\Enum\{0}\{1}\{2}", id_01, id_02, id_03);

            OurKey = OurKey.OpenSubKey(findme, false);

            //Retrieve the desired information and set isKeyboard
            string deviceDesc = (string)OurKey.GetValue("DeviceDesc");

            if (deviceDesc.ToUpper().Contains("TOUCH"))
            {
                isTouchScreen = true;
            }
            else
            {
                isTouchScreen = false;
            }
            return deviceDesc;
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
                            bool IsTouchDevice = false;

                            string DeviceDesc = ReadReg(deviceName, ref IsTouchDevice);

                            if (IsTouchDevice)
                            {
                                NumberOfDevices++;
                                if (!GestureSign.Common.Configuration.AppConfig.DeviceName.Equals(deviceName))
                                {
                                    GestureSign.Common.Configuration.AppConfig.DeviceName = deviceName;
                                    GestureSign.Common.Configuration.AppConfig.XRatio = GestureSign.Common.Configuration.AppConfig.YRatio = 0;
                                    GestureSign.Common.Configuration.AppConfig.Save();
                                }
                            }
                        }
                        Marshal.FreeHGlobal(pData);
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
                if (NumberOfDevices == 0)
                {
                    if (AppConfig.DeviceName != String.Empty)
                    {
                        AppConfig.DeviceName = String.Empty;
                        AppConfig.Save();
                    }
                }
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
                        ProcessInputCommand(message.LParam);
                    }
                    break;
                //case WM_POINTERENTER:
                //case WM_POINTERLEAVE:
                //case WM_POINTERCAPTURECHANGED:
                case WM_POINTERDOWN:
                case WM_POINTERUP:
                case WM_POINTERUPDATE:
                    ProcessPointerMessage(message);
                    break;

            }
            base.WndProc(ref message);
        }

        #region ProcessInput
        private void CheckLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode != 0)
            {
                throw new System.ComponentModel.Win32Exception(errCode);
            }
        }
        private void ProcessPointerMessage(Message message)
        {
            if (PointerIntercepted != null && (AppConfig.CompatibilityMode || AppConfig.XRatio == 0))
            {
                int pointerID = (int)(message.WParam.ToInt64() & 0xffff);
                int pCount = 0;
                try
                {
                    if (!GetPointerFrameInfo(pointerID, ref pCount, null))
                    {
                        CheckLastError();
                    }
                    POINTER_INFO[] pointerInfos = new POINTER_INFO[pCount];
                    if (!GetPointerFrameInfo(pointerID, ref pCount, pointerInfos))
                    {
                        CheckLastError();
                    }
                    PointerIntercepted(this, new PointerMessageEventArgs(pointerInfos));
                }
                catch (System.ComponentModel.Win32Exception) { }
            }
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

                    if (raw.header.dwType == RIM_TYPEHID)
                    {
                        int headLength = raw.header.dwSize - (int)raw.hid.dwSizHid;
                        byte[] rawdate = new byte[dwSize];
                        Marshal.Copy(buffer, rawdate, 0, (int)dwSize);
                        int activeTouchCount;
                        ushort timeStamp;
                        int offset;
                        //If no position data
                        if (rawdate[headLength + 3] == 0 && rawdate[headLength + 4] == 0) return;
                        if (AppConfig.DeviceName.Contains("NTRG"))
                        {
                            offset = 1;
                            activeTouchCount = rawdate[dwSize - 5];
                            timeStamp = BitConverter.ToUInt16(rawdate, (int)dwSize - 4);
                            touchDataType = typeof(ntrgTouchData);
                        }
                        else if (rawdate[headLength] == 0x0C && rawdate[headLength + 1] == 0x00)
                        {
                            offset = 3;
                            activeTouchCount = rawdate[headLength + 2];
                            timeStamp = BitConverter.ToUInt16(rawdate, (int)dwSize - 2);
                            touchDataType = typeof(wcTouchData);
                        }
                        else
                        {
                            offset = 1;
                            activeTouchCount = Marshal.ReadByte(buffer, (int)dwSize - 1);
                            timeStamp = BitConverter.ToUInt16(rawdate, (int)dwSize - 3);

                            if (rawdate[headLength + 3] == rawdate[headLength + 5] && rawdate[headLength + 4] == rawdate[headLength + 6] &&
                                rawdate[headLength + 7] == rawdate[headLength + 9] && rawdate[headLength + 8] == rawdate[headLength + 10])
                            {
                                touchDataType = typeof(dTouchData);
                            }
                            else if (activeTouchCount > 1 && rawdate[headLength + 7] == 0 && rawdate[headLength + 8] == 0)
                            {
                                touchDataType = typeof(gTouchData);
                            }
                            else if (rawdate[headLength + 5] == 0 && rawdate[headLength + 6] == 0x0 && rawdate[headLength + 9] == 0 && rawdate[headLength + 10] == 0 &&
                                    (rawdate[headLength + 3] != 0 || rawdate[headLength + 4] != 0 || rawdate[headLength + 7] != 0 || rawdate[headLength + 8] != 0))
                            {
                                touchDataType = typeof(iTouchData);
                            }
                            else
                            {
                                touchDataType = typeof(sTouchData);
                            }
                        }
                        if (activeTouchCount != 0)
                        {
                            requiringTouchDataCount = activeTouchCount;
                            outputTouchs = new List<RawTouchData>(activeTouchCount);
                        }
                        touchlength = Marshal.SizeOf(touchDataType);
                        touchdataCount = (int)(raw.hid.dwSizHid - 3) / touchlength;

                        switch (System.Windows.Forms.SystemInformation.ScreenOrientation)
                        {
                            case System.Windows.Forms.ScreenOrientation.Angle0:
                                XAxisDirection = YAxisDirection = true;
                                IsAxisCorresponds = true;
                                break;
                            case System.Windows.Forms.ScreenOrientation.Angle90:
                                IsAxisCorresponds = false;
                                XAxisDirection = false;
                                YAxisDirection = true;
                                break;
                            case System.Windows.Forms.ScreenOrientation.Angle180:
                                XAxisDirection = YAxisDirection = false;
                                IsAxisCorresponds = true;
                                break;
                            case System.Windows.Forms.ScreenOrientation.Angle270:
                                IsAxisCorresponds = false;
                                XAxisDirection = true;
                                YAxisDirection = false;
                                break;
                            default: break;
                        }
                        for (int dwIndex = 0; dwIndex < raw.hid.dwCount; dwIndex++)
                        {
                            for (int dataIndex = 0; dataIndex < touchdataCount; dataIndex++)
                            {
                                TouchData touch = (TouchData)Marshal.PtrToStructure(IntPtr.Add(buffer, headLength + offset + dwIndex * (int)raw.hid.dwSizHid + dataIndex * touchlength), touchDataType);

                                if (AppConfig.XRatio != 0.0 && AppConfig.YRatio != 0.0 && YAxisDirection.HasValue && XAxisDirection.HasValue)
                                {
                                    int rawX = (int)Math.Round(touch.X / AppConfig.XRatio);
                                    int rawY = (int)Math.Round(touch.Y / AppConfig.YRatio);

                                    int X = IsAxisCorresponds.Value ? rawX : rawY;
                                    int Y = IsAxisCorresponds.Value ? rawY : rawX;

                                    X = XAxisDirection.Value ? X : System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - X;
                                    Y = YAxisDirection.Value ? Y : System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - Y;

                                    outputTouchs.Add(new RawTouchData(touch.Status, touch.ID, new Point(X, Y)));

                                }
                                else
                                {
                                    outputTouchs.Add(new RawTouchData(
                                     touch.Status,
                                      touch.ID,
                                      IsAxisCorresponds.Value ? new Point(touch.X, touch.Y) : new Point(touch.Y, touch.X)));

                                }
                                if (--requiringTouchDataCount == 0) break;
                            }
                        }
                        if (requiringTouchDataCount == 0 && PointsIntercepted != null)
                        {
                            PointsIntercepted(this, new RawPointsDataMessageEventArgs(outputTouchs.OrderBy(rtd => rtd.Num).ToArray(), timeStamp));
                        }

                    }
                }
                else throw new ApplicationException("GetRawInputData does not return correct size !\n.");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        #endregion ProcessInput
    }
}

