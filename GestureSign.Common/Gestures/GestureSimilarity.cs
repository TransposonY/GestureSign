using System;
using System.Collections.Generic;
using System.Drawing;
using GestureSign.PointPatterns;

namespace GestureSign.Common.Gestures
{
    /// <summary>
    /// Rates how closely a stroke the user drew from memory resembles the moving strokes of a recorded gesture.
    /// Uses the same angle comparison as live recognition, so the ranking agrees with what the daemon will do.
    /// </summary>
    public static class GestureSimilarity
    {
        // Drawn strokes are scaled up before they become integer points so short strokes keep their angles.
        private const float DrawingScale = 4;
        // Strokes shorter than this (in drawn units, before scaling) are taps, not shapes.
        private const double MinimumLength = 16;

        private static readonly PointPatternAnalyzer Analyzer = new PointPatternAnalyzer();

        /// <summary>
        /// Converts a drawn stroke to the integer points the matcher expects. Returns null when the stroke is too short to compare.
        /// </summary>
        public static Point[] Prepare(IEnumerable<PointF> stroke)
        {
            if (stroke == null)
                return null;

            var points = new List<Point>();
            double length = 0;
            PointF? previous = null;
            foreach (var p in stroke)
            {
                if (previous.HasValue)
                {
                    double dx = p.X - previous.Value.X, dy = p.Y - previous.Value.Y;
                    length += Math.Sqrt(dx * dx + dy * dy);
                }
                previous = p;

                var scaled = new Point((int)Math.Round(p.X * DrawingScale), (int)Math.Round(p.Y * DrawingScale));
                // Repeated points would give the interpolation a zero-length step.
                if (points.Count == 0 || points[points.Count - 1] != scaled)
                    points.Add(scaled);
            }

            return points.Count >= 2 && length >= MinimumLength ? points.ToArray() : null;
        }

        /// <summary>
        /// 0-100: how well <paramref name="drawn"/> (from <see cref="Prepare"/>) matches the best moving stroke of the
        /// gesture's first step. Gestures without a moving stroke score 0.
        /// </summary>
        public static double Score(IGesture gesture, Point[] drawn)
        {
            var patterns = gesture?.PointPatterns;
            if (drawn == null || patterns == null || patterns.Length == 0 || patterns[0]?.Points == null)
                return 0;

            double best = 0;
            var target = new PointsPatternSet(null, drawn);
            foreach (var stroke in patterns[0].Points)
            {
                if (stroke == null || stroke.Length < 2)
                    continue;

                double probability;
                try
                {
                    probability = Analyzer.GetPointPatternMatchResult(new PointsPatternSet(gesture.Name, stroke), target).Probability;
                }
                catch (IndexOutOfRangeException)
                {
                    // Degenerate stroke that interpolates to fewer points than the precision asks for.
                    continue;
                }

                if (double.IsNaN(probability))
                    continue;
                if (probability > best)
                    best = probability;
            }

            return Math.Max(0, Math.Min(100, best));
        }
    }
}
