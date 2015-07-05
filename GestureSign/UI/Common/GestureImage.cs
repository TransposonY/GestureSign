using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

using System.Management;

namespace GestureSign.UI.Common
{
    public class GestureImage
    {
        #region  Private Variables

        // static Point Dpi = GetPixelsPerXLogicalInch();
        #endregion

        private static Point[] ScaleGesture(List<System.Drawing.Point> Input, double Width, double Height, out Size _ScaledSize)
        {
            // Create generic list of points to hold scaled stroke
            List<Point> ScaledStroke = new List<Point>();

            // Get total width and height of gesture
            double fGestureOffsetLeft = Input.Min(i => i.X);
            double fGestureOffsetTop = Input.Min(i => i.Y);
            double fGestureWidth = Input.Max(i => i.X) - fGestureOffsetLeft;
            double fGestureHeight = Input.Max(i => i.Y) - fGestureOffsetTop;

            // Get each scale ratio
            double dScaleX = Width / fGestureWidth;
            double dScaleY = Height / fGestureHeight;

            // Scale on the longest axis
            if (fGestureWidth >= fGestureHeight)
            {
                // Scale on X axis
                // Clear current scaled stroke
                ScaledStroke.Clear();

                foreach (System.Drawing.Point currentPoint in Input)
                    ScaledStroke.Add(new Point(((currentPoint.X - fGestureOffsetLeft) * dScaleX), ((currentPoint.Y - fGestureOffsetTop) * dScaleX)));

                // Calculate new gesture width and height
                _ScaledSize = new Size(Math.Floor(fGestureWidth * dScaleX), Math.Floor(fGestureHeight * dScaleX));
            }
            else
            {
                // Scale on X axis
                // Clear current scaled stroke
                ScaledStroke.Clear();

                foreach (System.Drawing.Point currentPoint in Input)
                    ScaledStroke.Add(new Point((currentPoint.X - fGestureOffsetLeft) * dScaleY, (currentPoint.Y - fGestureOffsetTop) * dScaleY));

                // Calculate new gesture width and height
                _ScaledSize = new Size(fGestureWidth * dScaleY, fGestureHeight * dScaleY);
            }

            return ScaledStroke.ToArray();
        }

        public static DrawingImage CreateImage(List<List<System.Drawing.Point>> points, Size size, Brush color)
        {
            if (points == null)
                throw new Exception("You must provide a gesture before trying to generate a thumbnail");
            //  System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            Pen _DrawingPen = new Pen(color, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

            Size _ScaledSize;
            PathGeometry pathGeometry = new PathGeometry();

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Count == 1)
                {
                    Geometry ellipse = new EllipseGeometry(new Point(size.Width * i + size.Width / 2, size.Height / 2), _DrawingPen.Thickness / 2, _DrawingPen.Thickness / 2);
                    pathGeometry.AddGeometry(ellipse);
                    continue;
                }
                StreamGeometry sg = new StreamGeometry();
                sg.FillRule = FillRule.EvenOdd;
                using (StreamGeometryContext sgc = sg.Open())
                {
                    // Create new size object accounting for pen width
                    Size szeAdjusted = new Size(size.Width - _DrawingPen.Thickness - 1, (size.Height - _DrawingPen.Thickness - 1));

                    Point[] _ScaledPoints = ScaleGesture(points[i], szeAdjusted.Width - 10, szeAdjusted.Height - 10, out _ScaledSize);

                    // Define size that will mark the offset to center the gesture
                    double iLeftOffset = (size.Width / 2) - (_ScaledSize.Width / 2);
                    double iTopOffset = (size.Height / 2) - (_ScaledSize.Height / 2);
                    Vector sizOffset = new Vector(iLeftOffset + i * size.Width, iTopOffset);
                    sgc.BeginFigure(Point.Add(_ScaledPoints[0], sizOffset), false, false);
                    for (int j = 0; j < _ScaledPoints.Length; j++)
                    {
                        sgc.LineTo(Point.Add(_ScaledPoints[j], sizOffset), true, true);
                    }
                    DrawArrow(sgc, _ScaledPoints, sizOffset, _DrawingPen.Thickness);
                }
                sg.Freeze();
                pathGeometry.AddGeometry(sg);
            }
            //  myPath.Data = sg;
            pathGeometry.Freeze();
            GeometryDrawing drawing = new GeometryDrawing(null, _DrawingPen, pathGeometry);
            drawing.Freeze();
            DrawingImage drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();
            // System.Diagnostics.Debug.WriteLine(sw.ElapsedMilliseconds.ToString());
            return drawingImage;

        }
        private static void DrawArrow(StreamGeometryContext streamGeometryContext, Point[] points, Vector sizeOffset, double thickness)
        {
            double HeadWidth = thickness;
            double HeadHeight = thickness * 0.8;

            Point pt1 = Point.Add(points[points.Length - 2], sizeOffset);
            Point pt2 = Point.Add(points[points.Length - 1], sizeOffset);

            double theta = Math.Atan2(pt1.Y - pt2.Y, pt1.X - pt2.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);


            Point pt3 = new Point(
                pt2.X + (HeadWidth * cost - HeadHeight * sint),
                pt2.Y + (HeadWidth * sint + HeadHeight * cost));

            Point pt4 = new Point(
                pt2.X + (HeadWidth * cost + HeadHeight * sint),
                pt2.Y - (HeadHeight * cost - HeadWidth * sint));

            streamGeometryContext.BeginFigure(pt1, true, false);
            streamGeometryContext.LineTo(pt2, true, true);
            streamGeometryContext.LineTo(pt3, true, true);
            streamGeometryContext.LineTo(pt2, true, true);
            streamGeometryContext.LineTo(pt4, true, true);
        }

    }
}
