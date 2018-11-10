using System;

namespace GestureSign.CorePlugins.MouseActions
{
    public enum LegacyClickPositions
    {
        Current,
        FirstDown,
        FirstUp,
        LastDown,
        LastUp,
    }

    [Flags]
    public enum ClickPositions
    {
        None = 0,
        Custom = 1,
        Current = 1 << 1,
        Down = 1 << 4,
        Up = 1 << 5,
        FirstPoint = 1 << 8,
        LastPoint = 1 << 9,
        FirstDown = FirstPoint | Down,
        FirstUp = FirstPoint | Up,
        LastDown = LastPoint | Down,
        LastUp = LastPoint | Up,
    }

    public static class ClickPositionsExtensions
    {
        public static ClickPositions ToClickPositions(this LegacyClickPositions legacy)
        {
            switch (legacy)
            {
                case LegacyClickPositions.Current:
                    return ClickPositions.Current;
                case LegacyClickPositions.FirstDown:
                    return ClickPositions.FirstDown;
                case LegacyClickPositions.FirstUp:
                    return ClickPositions.FirstUp;
                case LegacyClickPositions.LastDown:
                    return ClickPositions.LastDown;
                case LegacyClickPositions.LastUp:
                    return ClickPositions.LastUp;
                default:
                    return ClickPositions.None;
            }
        }
    }
}