using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Common.Gestures
{
    public enum GestureShape
    {
        /// <summary>No finger moves (only dots).</summary>
        None,
        Line,
        Curve,
        Loop
    }

    public enum GestureDirection
    {
        None,
        Right,
        UpRight,
        Up,
        UpLeft,
        Left,
        DownLeft,
        Down,
        DownRight
    }

    public sealed class GestureShapeInfo
    {
        /// <summary>Most simultaneous contacts used by any step of the gesture.</summary>
        public int Contacts { get; set; }

        /// <summary>A finger that is held in place (drawn as a dot) while the others move.</summary>
        public bool HasDot { get; set; }

        public GestureShape Shape { get; set; }

        /// <summary>Overall heading of the first moving stroke; None for loops and dot-only gestures.</summary>
        public GestureDirection Direction { get; set; }
    }

    /// <summary>Derives coarse, searchable traits (dot, line/curve/loop, heading) from the recorded point patterns.</summary>
    public static class GestureShapeAnalyzer
    {
        private const int ResampleCount = 24;
        // A stroke whose heading turns by this much overall is read as a loop (a full "0" is 360°, a "6" more).
        private const double LoopTurning = 270 * Math.PI / 180;
        // Largest bow, relative to the drawn length, that is still a straight line.
        private const double LineDeviation = 0.08;
        // Below this chord-to-length ratio the stroke has no meaningful heading.
        private const double MinHeadingChord = 0.2;

        public static GestureShapeInfo Analyze(PointPattern[] patterns)
        {
            var info = new GestureShapeInfo();
            if (patterns == null)
                return info;

            bool anyLoop = false, anyCurve = false, anyLine = false;
            bool directionSet = false;

            foreach (var pattern in patterns)
            {
                var strokes = pattern?.Points;
                if (strokes == null)
                    continue;
                if (strokes.Length > info.Contacts)
                    info.Contacts = strokes.Length;

                foreach (var stroke in strokes)
                {
                    if (stroke == null || stroke.Length < 2)
                    {
                        info.HasDot = true;
                        continue;
                    }

                    var resampled = Resample(stroke, ResampleCount, out double length);
                    if (length <= 0)
                    {
                        info.HasDot = true;
                        continue;
                    }

                    var shape = Classify(resampled, length);
                    switch (shape)
                    {
                        case GestureShape.Loop: anyLoop = true; break;
                        case GestureShape.Curve: anyCurve = true; break;
                        default: anyLine = true; break;
                    }

                    if (!directionSet && shape != GestureShape.Loop)
                    {
                        info.Direction = Heading(resampled[0], resampled[resampled.Length - 1], length);
                        directionSet = true;
                    }
                }
            }

            info.Shape = anyLoop ? GestureShape.Loop
                : anyCurve ? GestureShape.Curve
                : anyLine ? GestureShape.Line
                : GestureShape.None;
            return info;
        }

        private static GestureShape Classify(PointF[] points, double length)
        {
            double turning = 0;
            double previous = 0;
            bool havePrevious = false;
            for (int i = 1; i < points.Length; i++)
            {
                double dx = points[i].X - points[i - 1].X;
                double dy = points[i].Y - points[i - 1].Y;
                if (dx == 0 && dy == 0)
                    continue;
                double angle = Math.Atan2(dy, dx);
                if (havePrevious)
                {
                    double delta = angle - previous;
                    while (delta > Math.PI) delta -= 2 * Math.PI;
                    while (delta <= -Math.PI) delta += 2 * Math.PI;
                    turning += delta;
                }
                previous = angle;
                havePrevious = true;
            }

            if (Math.Abs(turning) >= LoopTurning)
                return GestureShape.Loop;

            var first = points[0];
            var last = points[points.Length - 1];
            double chordX = last.X - first.X;
            double chordY = last.Y - first.Y;
            double chord = Math.Sqrt(chordX * chordX + chordY * chordY);
            double maxDeviation = 0;
            for (int i = 1; i < points.Length - 1; i++)
            {
                double deviation = chord > 0
                    ? Math.Abs(chordX * (first.Y - points[i].Y) - (first.X - points[i].X) * chordY) / chord
                    : Distance(first, points[i]);
                if (deviation > maxDeviation)
                    maxDeviation = deviation;
            }

            return maxDeviation <= LineDeviation * length ? GestureShape.Line : GestureShape.Curve;
        }

        private static GestureDirection Heading(PointF from, PointF to, double length)
        {
            double dx = to.X - from.X;
            double dy = from.Y - to.Y; // screen Y grows downwards
            if (Math.Sqrt(dx * dx + dy * dy) < MinHeadingChord * length)
                return GestureDirection.None;

            double degrees = Math.Atan2(dy, dx) * 180 / Math.PI;
            int sector = (int)Math.Round(degrees / 45.0);
            sector = ((sector % 8) + 8) % 8;
            return (GestureDirection)(sector + 1);
        }

        private static PointF[] Resample(Point[] stroke, int count, out double length)
        {
            length = 0;
            for (int i = 1; i < stroke.Length; i++)
                length += Distance(stroke[i - 1], stroke[i]);
            if (length <= 0)
                return new PointF[0];

            var result = new List<PointF>(count) { new PointF(stroke[0].X, stroke[0].Y) };
            double step = length / (count - 1);
            double accumulated = 0;
            PointF current = result[0];

            for (int i = 1; i < stroke.Length && result.Count < count; i++)
            {
                var target = new PointF(stroke[i].X, stroke[i].Y);
                double segment = Distance(current, target);
                while (segment > 0 && accumulated + segment >= step && result.Count < count)
                {
                    double ratio = (step - accumulated) / segment;
                    current = new PointF(
                        (float)(current.X + ratio * (target.X - current.X)),
                        (float)(current.Y + ratio * (target.Y - current.Y)));
                    result.Add(current);
                    segment = Distance(current, target);
                    accumulated = 0;
                }
                accumulated += segment;
                current = target;
            }

            var end = new PointF(stroke[stroke.Length - 1].X, stroke[stroke.Length - 1].Y);
            while (result.Count < count)
                result.Add(end);
            return result.ToArray();
        }

        private static double Distance(PointF a, PointF b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double Distance(Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
