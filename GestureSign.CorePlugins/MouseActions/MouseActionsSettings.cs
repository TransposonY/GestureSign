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
        RightButtonClick,
        RightButtonUp,
        RightButtonDown,
        RightButtonDoubleClick,

        VerticalScroll,
        HorizontalScroll,

        MoveMouseTo,
        MoveMouseBy
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
            DescriptionDict = new Dictionary<MouseActions, String>(12);
            foreach (MouseActions mouseAction in Enum.GetValues(typeof(MouseActions)))
            {
                DescriptionDict.Add(mouseAction,
                    LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.MouseActions." + mouseAction));
            }
        }

        public static Dictionary<MouseActions, String> DescriptionDict { get; private set; }
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
