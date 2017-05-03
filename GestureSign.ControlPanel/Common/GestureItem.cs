using GestureSign.Common.Gestures;

namespace GestureSign.ControlPanel.Common
{
    public class GestureItem
    {
        public string Name { get; set; }
        public PointPattern[] PointPattern { get; set; }
        public string Applications { get; set; }
    }
}
