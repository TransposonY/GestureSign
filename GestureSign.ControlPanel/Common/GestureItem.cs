using GestureSign.Common.Gestures;
using System.Windows.Media;

namespace GestureSign.ControlPanel.Common
{
    public class GestureItem
    {
        public IGesture Gesture { get; set; }
        public string Applications { get; set; }
        public string Features { get; set; }
        public int PatternCount { get; set; }
        public DrawingImage GestureImage { get; set; }
    }
}
