using System;

namespace GestureSign.Common.Input
{
    [Flags]
    public enum Device
    {
        None = 0,
        TouchScreen = 1 << 0,
        TouchPad = 1 << 1,
        Mouse = 1 << 2,
        Touch = TouchScreen | TouchPad,
    }
}
