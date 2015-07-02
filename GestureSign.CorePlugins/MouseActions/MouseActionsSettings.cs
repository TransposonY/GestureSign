using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            DescriptionDict = new Dictionary<MouseActions, String>() 
            {
                {MouseActions.LeftButtonClick, "单击左键"},
                {MouseActions.LeftButtonDoubleClick, "双击左键"},
                {MouseActions.LeftButtonDown, "按住左键"}, 
                {MouseActions.LeftButtonUp, "释放左键"},

                {MouseActions.RightButtonClick, "单击右键"},
                {MouseActions.RightButtonDoubleClick, "双击右键"},
                {MouseActions.RightButtonDown, "按住右键"}, 
                {MouseActions.RightButtonUp, "释放右键"},

                {MouseActions.VerticalScroll,"垂直滚动"},
                {MouseActions.HorizontalScroll,"水平滚动"},
                
                {MouseActions.MoveMouseTo, "移动鼠标至指定坐标"}, 
                {MouseActions.MoveMouseBy, "将鼠标位移一段距离"},
            };
        }

        public static Dictionary<MouseActions, String> DescriptionDict { get; private set; }
    }

    public class ClickPositionDescription
    {
        static ClickPositionDescription()
        {
            DescriptionDict = new Dictionary<ClickPositions, String>()
            {
                {ClickPositions.FirstDown, "第一根手指落点"},
                {ClickPositions.FirstUp, "第一根手指抬起点"},
                {ClickPositions.LastDown, "最后手指落点"},
                {ClickPositions.LastUp, "最后手指抬起点"},
                {ClickPositions.Original, "不移动鼠标位置"}
            };
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
