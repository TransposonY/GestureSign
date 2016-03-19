using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace GestureSign.Daemon.Native
{

    #region POINTER_INFO
    public enum POINTER_INPUT_TYPE
    {
        POINTER = 0x00000001,
        TOUCH = 0x00000002,
        PEN = 0x00000003,
        MOUSE = 0x00000004
    }
    [Flags]
    public enum POINTER_FLAGS
    {
        NONE = 0x00000000,
        NEW = 0x00000001,
        INRANGE = 0x00000002,
        INCONTACT = 0x00000004,
        FIRSTBUTTON = 0x00000010,
        SECONDBUTTON = 0x00000020,
        THIRDBUTTON = 0x00000040,
        FOURTHBUTTON = 0x00000080,
        FIFTHBUTTON = 0x00000100,
        PRIMARY = 0x00002000,
        CONFIDENCE = 0x00004000,
        CANCELED = 0x00008000,
        DOWN = 0x00010000,
        UPDATE = 0x00020000,
        UP = 0x00040000,
        WHEEL = 0x00080000,
        HWHEEL = 0x00100000,
        CAPTURECHANGED = 0x00200000,
    }
    #region POINT

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    #endregion

    [Flags]
    public enum VIRTUAL_KEY_STATES
    {
        NONE = 0x0000,
        LBUTTON = 0x0001,
        RBUTTON = 0x0002,
        SHIFT = 0x0004,
        CTRL = 0x0008,
        MBUTTON = 0x0010,
        XBUTTON1 = 0x0020,
        XBUTTON2 = 0x0040
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_INFO
    {
        public POINTER_INPUT_TYPE pointerType;
        public int PointerID;
        public int FrameID;
        public POINTER_FLAGS PointerFlags;
        public IntPtr SourceDevice;
        public IntPtr WindowTarget;
        [MarshalAs(UnmanagedType.Struct)]
        public POINT PtPixelLocation;
        [MarshalAs(UnmanagedType.Struct)]
        public POINT PtPixelLocationRaw;
        [MarshalAs(UnmanagedType.Struct)]
        public POINT PtHimetricLocation;
        [MarshalAs(UnmanagedType.Struct)]
        public POINT PtHimetricLocationRaw;
        public uint Time;
        public uint HistoryCount;
        public uint InputData;
        public VIRTUAL_KEY_STATES KeyStates;
        public long PerformanceCount;
        public int ButtonChangeType;
    }

    #endregion


    #region POINTER_TOUCH_INFO

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTER_TOUCH_INFO
    {
        [MarshalAs(UnmanagedType.Struct)]
        public POINTER_INFO PointerInfo;
        public TOUCH_FLAGS TouchFlags;
        public TOUCH_MASK TouchMask;
        [MarshalAs(UnmanagedType.Struct)]
        public RECT ContactArea;
        [MarshalAs(UnmanagedType.Struct)]
        public RECT ContactAreaRaw;
        public uint Orientation;
        public uint Pressure;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int x, int y, int width, int height)
        {
            Left = x;
            Top = y;
            Right = Left + width;
            Bottom = Top + height;
        }

        public int Width
        {
            get { return Right - Left; }
        }

        public int Height
        {
            get { return Bottom - Top; }
        }
    }

    [Flags]
    public enum TOUCH_MASK
    {
        NONE = 0x00000000,
        CONTACTAREA = 0x00000001,
        ORIENTATION = 0x00000002,
        PRESSURE = 0x00000004,
    }

    [Flags]
    public enum TOUCH_FLAGS
    {
        NONE = 0x00000000
    }

    public enum TOUCH_FEEDBACK
    {
        DEFAULT = 0x1,
        INDIRECT = 0x2,
        NONE = 0x3
    }

    #endregion

}
