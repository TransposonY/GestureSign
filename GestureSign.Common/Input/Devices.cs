using System;

namespace GestureSign.Common.Input
{
    [Flags]
    public enum Devices
    {
        None = 0,
        TouchScreen = 1 << 0,
        TouchPad = 1 << 1,
        Mouse = 1 << 2,
        Pen = 1 << 3,
        TouchDevice = TouchScreen | TouchPad,
    }
}
