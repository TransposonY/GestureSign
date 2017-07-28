using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins.MouseActions
{
    public enum MouseActions
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

    public enum ClickPositions
    {
        Original,
        FirstDown,
        FirstUp,
        LastDown,
        LastUp
    }
    public class MouseActionDescription
    {
        static MouseActionDescription()
        {
            DescriptionDict = new Dictionary<string, string>(8);
            ButtonDescription = new Dictionary<string, string>(5);
            foreach (var button in new string[5] { "LeftButton", "MiddleButton", "RightButton", "XButton1", "XButton2" })
            {
                ButtonDescription.Add(button, LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Buttons." + button));
            }
            foreach (var mouseAction in new List<string>() { "Click", "DoubleClick", "Down", "Up", MouseActions.VerticalScroll.ToString(), MouseActions.HorizontalScroll.ToString(), MouseActions.MoveMouseTo.ToString(), MouseActions.MoveMouseBy.ToString() })
            {
                DescriptionDict.Add(mouseAction,
                    LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.MouseActions." + mouseAction));
            }
        }

        public static Dictionary<string, string> DescriptionDict { get; private set; }
        public static Dictionary<string, string> ButtonDescription { get; private set; }
    }

    public class ClickPositionDescription
    {
        static ClickPositionDescription()
        {
            DescriptionDict = new Dictionary<ClickPositions, String>(5);
            foreach (ClickPositions position in Enum.GetValues(typeof(ClickPositions)))
            {
                DescriptionDict.Add(position,
                    LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.ClickPositions." + position));
            }
        }
        public static Dictionary<ClickPositions, String> DescriptionDict { get; private set; }
    }

    public class MouseActionsSettings
    {
        public MouseActions MouseAction { get; set; }
        public ClickPositions ClickPosition { get; set; }
        public Point MovePoint { get; set; }
        public int ScrollAmount { get; set; }
    }
}
