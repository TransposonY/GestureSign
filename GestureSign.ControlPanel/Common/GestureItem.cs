using System.Windows.Media;
using GestureSign.Common.Gestures;

namespace GestureSign.ControlPanel.Common
{
    public class GestureItem
    {
        public IGesture Gesture { get; set; }
        public string Applications { get; set; }
        public string[] ApplicationNames { get; set; } = new string[0];
        public string Features { get; set; }
        public int PatternCount { get; set; }
        public DrawingImage GestureImage { get; set; }

        /// <summary>A finger is held in place (the dot) while the others move.</summary>
        public bool HasDot { get; set; }
        public GestureShape Shape { get; set; }

        /// <summary>Plain-language summary, e.g. "2 fingers, with dot, curve, up-right". Shown in tooltips and searched by the filter bar.</summary>
        public string Description { get; set; }

        /// <summary>Lower-cased, whitespace-free text the description search matches against.</summary>
        public string SearchKey { get; set; }
    }
}
