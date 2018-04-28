using System;

namespace GestureSign.Common.Input
{
    [Flags]
    public enum DeviceStates
    {
        None = 0,
        Tip = 1 << 0,
        InRange = 1 << 1,
        RightClickButton = 1 << 2,
        Invert = 1 << 3,
        Eraser = 1 << 4,
    }
}
