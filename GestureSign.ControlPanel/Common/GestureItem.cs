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

        /// <summary>Actions (across all applications) that run when this gesture is drawn.</summary>
        public int ActionCount { get; set; }

        /// <summary>Strokes of the first step that actually move (the others are held fingers, drawn as dots).</summary>
        public int MovingStrokeCount { get; set; }

        /// <summary>Plain-language summary, e.g. "2 fingers, with dot, curve, up-right". Shown in tooltips and the inspector.</summary>
        public string Description { get; set; }

        /// <summary>0-100 similarity to the stroke drawn on the sketch pad; 0 while nothing is drawn. Read by the list's sort.</summary>
        public double Similarity { get; set; }
    }
}
