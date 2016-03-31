using System.Collections.Generic;
using System.Drawing;
using GestureSign.PointPatterns;

namespace GestureSign.Common.Gestures
{
    public class PointPattern : IPointPattern
    {
        public PointPattern(List<List<Point>> points)
        {
            Points = points;
        }

        public List<List<Point>> Points { get; set; }
    }
}
