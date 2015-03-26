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

        #endregion const definitions

        static bool? XAxisDirection = null;
        static bool? YAxisDirection = null;
        static bool? IsAxisCorresponds = null;
        public event PointsMessageEventHandler PointsIntercepted;

        Type touchDataType;
        List<RawTouchData> outputTouchs = new List<RawTouchData>(1);
        int requiringTouchDataCount = 0;
        int touchdataCount = 0;
        int touchlength = 0;
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

        public interface TouchData
        {
            bool Status { get; }
            int ID { get; }
            int X { get; }
            int Y { get; }
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct sTouchData : TouchData
        {
            /// BYTE->unsigned char
            private byte status;
            /// BYTE->unsigned char
            private byte num;
            /// short
            private short x_position;
            /// short
            private short y_position;
            public int X { get { return x_position; } }
            public int Y { get { return y_position; } }
            public int ID { get { return num; } }
            public bool Status { get { return Convert.ToBoolean(status); } }
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
        private struct iTouchData : TouchData
        {
            /// BYTE->unsigned char
            private byte status;
            /// BYTE->unsigned char
            private byte num;
            /// short
            private int x_position;
            /// short
            private int y_position;
            public int X { get { return x_position; } }
            public int Y { get { return y_position; } }
            public int ID { get { return num; } }
            public bool Status { get { return Convert.ToBoolean(status); } }
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
        private struct gTouchData : TouchData
        {
            /// BYTE->unsigned char
            private byte status;
            /// BYTE->unsigned char
            private byte num;
            /// short
            private short x_position;
            /// short
            private short y_position;

            [MarshalAs(UnmanagedType.U4)]
            private int gap;
            public int X { get { return x_position; } }
            public int Y { get { return y_position; } }
            public int ID { get { return num; } }
            public bool Status { get { return Convert.ToBoolean(status); } }
        }
        // HID#FTSC0001&Col01#4&14bbeed5&0&0000#
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
        private struct dTouchData : TouchData
        {
            /// BYTE->unsigned char
            private byte status;
            /// BYTE->unsigned char
            private byte num;
            /// short
            private short x;
            private short x_position;
            /// short
            private short y;
            private short y_position;

            [MarshalAs(UnmanagedType.U4)]
            private int gap;
            public int X { get { return x_position; } }
            public int Y { get { return y_position; } }
            public int ID { get { return num; } }
            public bool Status { get { return Convert.ToBoolean(status); } }
        }
        //HID#WCOM5008&Col01#4&2b144297&0&0000
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        private struct wcTouchData : TouchData
        {
            /// BYTE->unsigned char
            private byte TouchDataStatus;
            /// BYTE->unsigned char
            private byte num;
            /// short
            private byte gap;

            private short x_position;
            /// short
            private short y_position;
            public int X { get { return x_position; } }
            public int Y { get { return y_position; } }
            public int ID { get { return num; } }
            public bool Status { get { return TouchDataStatus == (0x05); } }
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        private struct ntrgTouchData : TouchData
        {
            private short dataID;
            /// BYTE->unsigned char
            private byte status;
            /// BYTE->unsigned char
            private short num;
            /// short
            private short x;
            private short x_position;
            /// short
            private short y;
            private short y_position;

            public int X { get { return x_position; } }
            public int Y { get { return y_position; } }
            public int ID { get { return num; } }
            public bool Status { get { return (status) == 0xE7; } }
        }
        #endregion Windows.h structure declarations

        public MessageWindow()
        {
            System.Diagnostics.Debug.WriteLine("size:" + Marshal.SizeOf(typeof(ntrgTouchData)));
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
            GestureSignDaemon.Configuration.FileWatcher.Instance.EnableWatcher = false;

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
                    GestureSign.Common.Configuration.AppConfig.DeviceName = String.Empty;
                    GestureSign.Common.Configuration.AppConfig.Save();
                }
                GestureSignDaemon.Configuration.FileWatcher.Instance.EnableWatcher = true;
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
                        else if (rawdate[24] == 0x0C && rawdate[25] == 0x00)
                        {
                            offset = 3;
                            activeTouchCount = rawdate[26];
                            timeStamp = BitConverter.ToUInt16(rawdate, (int)dwSize - 2);
                            touchDataType = typeof(wcTouchData);
                        }
                        else
                        {
                            offset = 1;
                            activeTouchCount = Marshal.ReadByte(buffer, (int)dwSize - 1);
                            timeStamp = BitConverter.ToUInt16(rawdate, (int)dwSize - 3);

                            if (rawdate[27] == rawdate[29] && rawdate[28] == rawdate[30] &&
                                rawdate[31] == rawdate[33] && rawdate[32] == rawdate[34])
                            {
                                touchDataType = typeof(dTouchData);
                            }
                            else if (activeTouchCount > 1 && rawdate[31] == 0 && rawdate[32] == 0)
                            {
                                touchDataType = typeof(gTouchData);
                            }
                            else if (rawdate[29] == 0 && rawdate[30] == 0x0 && rawdate[33] == 0 && rawdate[34] == 0 &&
                                    (rawdate[27] != 0 || rawdate[28] != 0 || rawdate[31] != 0 || rawdate[32] != 0))
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

