using System.Drawing;

namespace GestureSign.PointPatterns
{
    public class PointsPatternSet
    {
        private double[] _angularMargins;

        public string Name;
        public Point[] Points;

        public PointsPatternSet(string name, Point[] points)
        {
            Name = name;
            Points = points;
            _angularMargins = null;
        }

        public double[] GetAngularMargins(int precision)
        {
            if (_angularMargins == null)
            {
                _angularMargins = PointPatternMath.GetPointArrayAngularMargins(PointPatternMath.GetInterpolatedPointArray(Points, precision));
            }
            return _angularMargins;
        }

    }
}
