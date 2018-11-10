using System;

namespace GestureSign.CorePlugins.MouseActions
{
    [Flags]
    public enum MouseActions
    {
        None = 0,
        LeftButton = 1 << 0,
        RightButton = 1 << 1,
        MiddleButton = 1 << 2,
        XButton1 = 1 << 3,
        XButton2 = 1 << 4,

        Click = 1 << 8,
        DoubleClick = 1 << 9,
        Down = 1 << 10,
        Up = 1 << 11,

        VerticalScroll = 1 << 12,
        HorizontalScroll = 1 << 13,

        MoveMouseTo = 1 << 14,
        MoveMouseBy = 1 << 15,

        LeftButtonClick = LeftButton | Click,
        LeftButtonDoubleClick = LeftButton | DoubleClick,
        LeftButtonDown = LeftButton | Down,
        LeftButtonUp = LeftButton | Up,

        MiddleButtonClick = MiddleButton | Click,
        MiddleButtonDoubleClick = MiddleButton | DoubleClick,
        MiddleButtonDown = MiddleButton | Down,
        MiddleButtonUp = MiddleButton | Up,

        RightButtonClick = RightButton | Click,
        RightButtonDoubleClick = RightButton | DoubleClick,
        RightButtonDown = RightButton | Down,
        RightButtonUp = RightButton | Up,

        XButton1Click = XButton1 | Click,
        XButton1DoubleClick = XButton1 | DoubleClick,
        XButton1Down = XButton1 | Down,
        XButton1Up = XButton1 | Up,

        XButton2Click = XButton2 | Click,
        XButton2DoubleClick = XButton2 | DoubleClick,
        XButton2Down = XButton2 | Down,
        XButton2Up = XButton2 | Up,
    }

    public enum LegacyMouseActions
    {
        LeftButtonClick,
        LeftButtonDoubleClick,
        LeftButtonDown,
        LeftButtonUp,
        MiddleButtonClick,
        MiddleButtonDoubleClick,
        MiddleButtonDown,
        MiddleButtonUp,
        RightButtonClick,
        RightButtonUp,
        RightButtonDown,
        RightButtonDoubleClick,

        VerticalScroll,
        HorizontalScroll,

        MoveMouseTo,
        MoveMouseBy,

        XButton1Click,
        XButton1DoubleClick,
        XButton1Down,
        XButton1Up,
        XButton2Click,
        XButton2DoubleClick,
        XButton2Down,
        XButton2Up,
    }

    internal static class MouseActionsExtensions
    {
        public static MouseActions GetButtons(this MouseActions action)
        {
            return (MouseActions)((int)action & 0xFF);
        }

        public static MouseActions GetActions(this MouseActions action)
        {
            return (MouseActions)((int)action & ~0xFF);
        }

        public static MouseActions ToNewMouseActions(this LegacyMouseActions legacy)
        {
            MouseActions mouseActions = MouseActions.None;
            switch (legacy)
            {
                case LegacyMouseActions.VerticalScroll:
                    mouseActions |= MouseActions.VerticalScroll;
                    break;
                case LegacyMouseActions.HorizontalScroll:
                    mouseActions |= MouseActions.HorizontalScroll;
                    break;
                case LegacyMouseActions.MoveMouseTo:
                    mouseActions |= MouseActions.MoveMouseTo;
                    break;
                case LegacyMouseActions.MoveMouseBy:
                    mouseActions |= MouseActions.MoveMouseBy;
                    break;
            }

            string legacyStr = legacy.ToString();
            if (legacyStr.Contains("Left"))
            {
                mouseActions |= MouseActions.LeftButton;
            }
            else if (legacyStr.Contains("Middle"))
            {
                mouseActions |= MouseActions.MiddleButton;
            }
            else if (legacyStr.Contains("Right"))
            {
                mouseActions |= MouseActions.RightButton;
            }
            else if (legacyStr.Contains("1"))
            {
                mouseActions |= MouseActions.XButton1;
            }
            else if (legacyStr.Contains("2"))
            {
                mouseActions |= MouseActions.XButton2;
            }

            if (legacyStr.Contains("Click"))
            {
                mouseActions |= MouseActions.Click;
            }
            else if (legacyStr.Contains("DoubleClick"))
            {
                mouseActions |= MouseActions.DoubleClick;
            }
            else if (legacyStr.Contains("Down"))
            {
                mouseActions |= MouseActions.Down;
            }
            else if (legacyStr.Contains("Up"))
            {
                mouseActions |= MouseActions.Up;
            }

            return mouseActions;
        }
    }
}
