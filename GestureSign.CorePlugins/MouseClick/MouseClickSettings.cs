using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSign.CorePlugins.MouseClick
{
    public enum MouseButtonActions
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
    public class MouseButtonActionDescription
    {
        static MouseButtonActionDescription()
        {
            DescriptionDict = new Dictionary<MouseButtonActions, String>() 
            {
                {MouseButtonActions.LeftButtonClick, "单击左键"},
                {MouseButtonActions.LeftButtonDoubleClick, "双击左键"},
                {MouseButtonActions.LeftButtonDown, "按住左键"}, 
                {MouseButtonActions.LeftButtonUp, "释放左键"},

                {MouseButtonActions.RightButtonClick, "单击右键"},
                {MouseButtonActions.RightButtonDoubleClick, "双击右键"},
                {MouseButtonActions.RightButtonDown, "按住右键"}, 
                {MouseButtonActions.RightButtonUp, "释放右键"},

                {MouseButtonActions.VerticalScroll,"垂直滚动"},
                {MouseButtonActions.HorizontalScroll,"水平滚动"},
                
                {MouseButtonActions.MoveMouseTo, "移动鼠标至指定坐标"}, 
                {MouseButtonActions.MoveMouseBy, "将鼠标位移一段距离"},
            };
        }

        public static Dictionary<MouseButtonActions, String> DescriptionDict { get; private set; }
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
                {ClickPositions.Original, "原始位置"}
            };
        }
        public static Dictionary<ClickPositions, String> DescriptionDict { get; private set; }
    }

    public class MouseClickSettings
    {
        public MouseButtonActions MouseButtonAction { get; set; }
        public ClickPositions ClickPosition { get; set; }
        public Point MovePoint { get; set; }
        public int ScrollAmount { get; set; }
    }
}
