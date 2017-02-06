using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GestureSign.Common.Gestures;

namespace GestureSign.ControlPanel.Common
{
    public class GestureImage
    {
        #region  Private Variables

        // static Point Dpi = GetPixelsPerXLogicalInch();
        #endregion

        private static Point[] ScaleGesture(List<System.Drawing.Point> input, double width, double height, out Size scaledSize)
        {
            // Create generic list of points to hold scaled stroke
            List<Point> scaledStroke = new List<Point>();

            // Get total width and height of gesture
            double fGestureOffsetLeft = input.Min(i => i.X);
            double fGestureOffsetTop = input.Min(i => i.Y);
            double fGestureWidth = input.Max(i => i.X) - fGestureOffsetLeft;
            double fGestureHeight = input.Max(i => i.Y) - fGestureOffsetTop;

            // Get each scale ratio
            double dScaleX = width / fGestureWidth;
            double dScaleY = height / fGestureHeight;

            // Scale on the longest axis
            if (fGestureWidth >= fGestureHeight)
            {
                // Scale on X axis
                // Clear current scaled stroke
                scaledStroke.Clear();

                scaledStroke.AddRange(input.Select(currentPoint => new Point(((currentPoint.X - fGestureOffsetLeft) * dScaleX), ((currentPoint.Y - fGestureOffsetTop) * dScaleX))));

                // Calculate new gesture width and height
                scaledSize = new Size(Math.Floor(fGestureWidth * dScaleX), Math.Floor(fGestureHeight * dScaleX));
            }
            else
            {
                // Scale on X axis
                // Clear current scaled stroke
                scaledStroke.Clear();

                scaledStroke.AddRange(input.Select(currentPoint => new Point((currentPoint.X - fGestureOffsetLeft) * dScaleY, (currentPoint.Y - fGestureOffsetTop) * dScaleY)));

                // Calculate new gesture width and height
                scaledSize = new Size(fGestureWidth * dScaleY, fGestureHeight * dScaleY);
            }

            return scaledStroke.ToArray();
        }

        public static DrawingImage CreateImage(PointPattern[] pointPatterns, Size size, Color color)
        {
            if (pointPatterns == null)
                return null;

            DrawingGroup drawingGroup = new DrawingGroup();
            for (int i = 0; i < pointPatterns.Length; i++)
            {
                PathGeometry pathGeometry = new PathGeometry();

                color.A = (byte)(0xFF - i * 0x55);
                SolidColorBrush brush = new SolidColorBrush(color);
                Pen drawingPen = new Pen(brush, size.Height / 20 + i * 1.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

                if (pointPatterns[i].Points == null) return null;
                for (int j = 0; j < pointPatterns[i].Points.Count; j++)
                {
                    if (pointPatterns[i].Points[j].Count == 1)
                    {
                        Geometry ellipse = new EllipseGeometry(new Point(size.Width * j + size.Width / 2, size.Height / 2),
                            drawingPen.Thickness / 2, drawingPen.Thickness / 2);
                        pathGeometry.AddGeometry(ellipse);
                        continue;
                    }
                    StreamGeometry sg = new StreamGeometry { FillRule = FillRule.EvenOdd };
                    using (StreamGeometryContext sgc = sg.Open())
                    {
                        // Create new size object accounting for pen width
                        Size szeAdjusted = new Size(size.Width - drawingPen.Thickness - 1,
                            (size.Height - drawingPen.Thickness - 1));

                        Size scaledSize;
                        Point[] scaledPoints = ScaleGesture(pointPatterns[i].Points[j], szeAdjusted.Width - 10, szeAdjusted.Height - 10,
                            out scaledSize);

                        // Define size that will mark the offset to center the gesture
                        double iLeftOffset = (size.Width / 2) - (scaledSize.Width / 2);
                        double iTopOffset = (size.Height / 2) - (scaledSize.Height / 2);
                        Vector sizOffset = new Vector(iLeftOffset + j * size.Width, iTopOffset);
                        sgc.BeginFigure(Point.Add(scaledPoints[0], sizOffset), false, false);
                        foreach (Point p in scaledPoints)
                        {
                            sgc.LineTo(Point.Add(p, sizOffset), true, true);
                        }
                        DrawArrow(sgc, scaledPoints, sizOffset, drawingPen.Thickness);
                    }
                    sg.Freeze();
                    pathGeometry.AddGeometry(sg);
                }
                pathGeometry.Freeze();
                GeometryDrawing drawing = new GeometryDrawing(null, drawingPen, pathGeometry);
                drawing.Freeze();
                drawingGroup.Children.Add(drawing);
            }
            //  myPath.Data = sg;
            drawingGroup.Freeze();
            DrawingImage drawingImage = new DrawingImage(drawingGroup);
            drawingImage.Freeze();

            return drawingImage;

        }
        private static void DrawArrow(StreamGeometryContext streamGeometryContext, Point[] points, Vector sizeOffset, double thickness)
        {
            double headWidth = thickness;
            double headHeight = thickness * 0.8;

            Point pt1 = Point.Add(points[points.Length - 2], sizeOffset);
            Point pt2 = Point.Add(points[points.Length - 1], sizeOffset);

            double theta = Math.Atan2(pt1.Y - pt2.Y, pt1.X - pt2.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);


            Point pt3 = new Point(
                pt2.X + (headWidth * cost - headHeight * sint),
                pt2.Y + (headWidth * sint + headHeight * cost));

            Point pt4 = new Point(
                pt2.X + (headWidth * cost + headHeight * sint),
                pt2.Y - (headHeight * cost - headWidth * sint));

            streamGeometryContext.BeginFigure(pt1, true, false);
            streamGeometryContext.LineTo(pt2, true, true);
            streamGeometryContext.LineTo(pt3, true, true);
            streamGeometryContext.LineTo(pt2, true, true);
            streamGeometryContext.LineTo(pt4, true, true);
        }

    }
}
