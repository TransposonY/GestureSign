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

namespace GestureSign.Input
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

        private const int HistoryCount = 10;
        #endregion const definitions

        static Queue<Point> cursorHistory = new Queue<Point>(HistoryCount);
        static List<double> XRatioHistory = new List<double>(HistoryCount);
        static List<double> YRatioHistory = new List<double>(HistoryCount);
        static bool? XAxisDirection = null;
        static bool? YAxisDirection = null;
        static bool? IsAxisCorresponds = null;
        public event PointsMessageEventHandler PointsIntercepted;

        bool IsIntTouchData;
        RawTouchData[] outputTouchs;
        int requiringTouchDataCount = 0;
        int touchdataCount;
        int touchlength;
        static public int NumberOfTouchscreens { get; set; }

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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out Point lpPoint);

        #endregion DllImports

        #region  Windows.h structure declarations

        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RAWINPUT
        {

            /// RAWINPUTHEADER->tagRAWINPUTHEADER
            public RAWINPUTHEADER header;

            public rawData data;
        }

        [StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public struct rawData
        {
            /// RAWHID->tagRAWHID
            [System.Runtime.InteropServices.FieldOffset(0)]
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
            [MarshalAs(UnmanagedType.U4)]
            public int wParam;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct sTouchData
        {
            /// BYTE->unsigned char
            public byte status;
            /// BYTE->unsigned char
            public byte num;
            /// short
            public short x_position;
            /// short
            public short y_position;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
        private struct iTouchData
        {
            /// BYTE->unsigned char
            public byte status;
            /// BYTE->unsigned char
            public byte num;
            /// short
            public int x_position;
            /// short
            public int y_position;
        }

        #endregion Windows.h structure declarations

        public MessageWindow()
        {
            var accessHandle = this.Handle;
            try
            {
                RegisterDevices(accessHandle);
                NumberOfTouchscreens = EnumerateDevices();
            }
            catch (Exception) { }
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
                                               new IntPtr((pRawInputDeviceList.ToInt32() + (dwSize * i))),
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
                                if (!GestureSign.Configuration.AppConfig.DeviceName.Equals(deviceName))
                                {
                                    GestureSign.Configuration.AppConfig.DeviceName = deviceName;
                                    GestureSign.Configuration.AppConfig.XRatio = GestureSign.Configuration.AppConfig.YRatio = 0;
                                }
                            }
                        }
                        Marshal.FreeHGlobal(pData);
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
                if (NumberOfDevices == 0) { GestureSign.Configuration.AppConfig.DeviceName = String.Empty; }
                GestureSign.Configuration.AppConfig.Save();
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
            }
            base.WndProc(ref message);
        }

        #region ProcessInputCommand( Message message )

        private static double GetEquationa()
        {


            double average = XRatioHistory.Average() + YRatioHistory.Average();
            double fto = 0;
            for (int i = 0; i < XRatioHistory.Count; i++)
                fto += Math.Pow((XRatioHistory[i] + YRatioHistory[i] - average), 2);
            double equation = fto / XRatioHistory.Count;
            return equation;
        }

        private Object BytesToStruct(Byte[] bytes, Type strcutType)
        {
            Int32 size = Marshal.SizeOf(strcutType);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return Marshal.PtrToStructure(buffer, strcutType);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Processes WM_INPUT messages to retrieve information about any
        /// touch events that occur.
        /// </summary>
        /// <param name="LParam">The WM_INPUT message to process.</param>
        public void ProcessInputCommand(IntPtr LParam)
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
                        byte[] rawdate = new byte[dwSize];
                        Marshal.Copy(buffer, rawdate, 0, (int)dwSize);
                        if (touchdataCount == 0)
                        {
                            if (rawdate[29] == 0 && rawdate[30] == 0x0 && rawdate[33] == 0 && rawdate[34] == 0)
                            {
                                IsIntTouchData = true;
                                touchdataCount = (int)(raw.data.hid.dwSizHid - 4) / Marshal.SizeOf(typeof(iTouchData));
                                touchlength = Marshal.SizeOf(typeof(iTouchData));
                            }
                            else
                            {
                                IsIntTouchData = false;
                                touchdataCount = (int)(raw.data.hid.dwSizHid - 4) / Marshal.SizeOf(typeof(sTouchData));
                                touchlength = Marshal.SizeOf(typeof(sTouchData));
                            }
                        }
                        int activeTouchCount = Marshal.ReadByte(buffer, (int)dwSize - 1);
                        ushort timeStamp = BitConverter.ToUInt16(rawdate, (int)dwSize - 3);
                        if (activeTouchCount != 0)
                        {
                            requiringTouchDataCount = activeTouchCount;
                            outputTouchs = new RawTouchData[activeTouchCount];
                        }
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

                        for (int dataIndex = 0; dataIndex < touchdataCount; dataIndex++)
                        {
                           byte[] rawtouch = new byte[touchlength];
                            Array.Copy(rawdate, raw.header.dwSize - raw.data.hid.dwSizHid + 1 + dataIndex * touchlength, rawtouch, 0, touchlength);
                            if (IsIntTouchData)
                            {
                                iTouchData touch = (iTouchData)BytesToStruct(rawtouch, typeof(iTouchData));
                                outputTouchs[requiringTouchDataCount - 1] = new RawTouchData(
                                    Convert.ToBoolean(touch.status),
                                    touch.num,
                                    IsAxisCorresponds.Value ? new Point(touch.x_position, touch.y_position) : new Point(touch.y_position, touch.x_position));
                            }
                            else
                            {
                                sTouchData touch = (sTouchData)BytesToStruct(rawtouch, typeof(sTouchData));
                                outputTouchs[requiringTouchDataCount - 1] = new RawTouchData(
                                     Convert.ToBoolean(touch.status),
                                     touch.num,
                                     IsAxisCorresponds.Value ? new Point(touch.x_position, touch.y_position) : new Point(touch.y_position, touch.x_position));
                            }
                            if (GestureSign.Configuration.AppConfig.XRatio == 0 && dataIndex == 0)
                            {
                                Point c;
                                if (GetCursorPos(out c))
                                {
                                    double rateX;
                                    double rateY;
                                    rateX = XAxisDirection.Value ?
                                        ((double)outputTouchs[requiringTouchDataCount - 1].RawPointsData.X / (double)c.X) :
                                        (double)outputTouchs[requiringTouchDataCount - 1].RawPointsData.X / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width - c.X);

                                    rateY = YAxisDirection.Value ?
                                        ((double)outputTouchs[requiringTouchDataCount - 1].RawPointsData.Y / (double)c.Y) :
                                        (double)outputTouchs[requiringTouchDataCount - 1].RawPointsData.Y / (double)(System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height - c.Y);

                                    if (XRatioHistory.Count >= HistoryCount)
                                    {
                                        XRatioHistory.RemoveAt(0);
                                        YRatioHistory.RemoveAt(0);
                                        cursorHistory.Dequeue();
                                    }
                                    cursorHistory.Enqueue(c);
                                    XRatioHistory.Add(rateX);
                                    YRatioHistory.Add(rateY);
                                    if (XRatioHistory.Count == HistoryCount &&
                                        GetEquationa() < 1E-5 &&
                                       !c.Equals(cursorHistory.Peek()))
                                    {
                                        GestureSign.Configuration.AppConfig.XRatio = XRatioHistory.Average();
                                        GestureSign.Configuration.AppConfig.YRatio = YRatioHistory.Average();
                                    }
                                }

                            }
                            if (GestureSign.Configuration.AppConfig.XRatio != 0.0 && GestureSign.Configuration.AppConfig.YRatio != 0.0 && YAxisDirection.HasValue && XAxisDirection.HasValue)
                            {
                                outputTouchs[requiringTouchDataCount - 1].RawPointsData = new Point((int)Math.Round(XAxisDirection.Value ?
                                    (outputTouchs[requiringTouchDataCount - 1].RawPointsData.X / GestureSign.Configuration.AppConfig.XRatio) :
                                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - outputTouchs[requiringTouchDataCount - 1].RawPointsData.X / GestureSign.Configuration.AppConfig.XRatio),
                                    (int)Math.Round(YAxisDirection.Value ?
                                    (outputTouchs[requiringTouchDataCount - 1].RawPointsData.Y / GestureSign.Configuration.AppConfig.YRatio) :
                                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - outputTouchs[requiringTouchDataCount - 1].RawPointsData.Y / GestureSign.Configuration.AppConfig.YRatio));
                            }
                            if (--requiringTouchDataCount == 0) break;
                        }

                        if (requiringTouchDataCount < 1)
                        {
                            PointsIntercepted(this, new PointsMessageEventArgs(outputTouchs.OrderBy(rtd => rtd.Num).ToArray(), timeStamp));
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

        #endregion ProcessInputCommand( Message message )
    }
}

