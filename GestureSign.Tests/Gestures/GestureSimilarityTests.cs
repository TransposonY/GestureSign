using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GestureSign.Common.Gestures;
using Xunit;

namespace GestureSign.Tests.Gestures
{
    /// <summary>Ranking of the gestures shipped in GestureSign.ControlPanel/Defaults/Gestures.gest against a drawn stroke.</summary>
    public class GestureSimilarityTests
    {
        private static readonly Lazy<List<IGesture>> DefaultGestures = new Lazy<List<IGesture>>(() =>
            GestureManager.LoadGesturesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Gestures", "Defaults.gest"), throwException: true));

        private static IGesture Default(string name) => DefaultGestures.Value.Single(g => g.Name == name);

        private static PointF[] MovingStroke(IGesture gesture)
        {
            return gesture.PointPatterns[0].Points.First(s => s.Length > 1).Select(p => new PointF(p.X, p.Y)).ToArray();
        }

        public static IEnumerable<object[]> SingleLevelDefaultGestures()
        {
            return DefaultGestures.Value
                .Where(g => g.PointPatterns.Length == 1 && g.PointPatterns[0].Points.Any(s => s.Length > 1))
                .Select(g => new object[] { g.Name });
        }

        [Theory]
        [MemberData(nameof(SingleLevelDefaultGestures))]
        public void DrawingAGestureScoresItNearlyPerfect(string name)
        {
            var drawn = GestureSimilarity.Prepare(MovingStroke(Default(name)));

            Assert.True(GestureSimilarity.Score(Default(name), drawn) >= 95);
        }

        [Fact]
        public void ScoreIgnoresScaleAndPosition()
        {
            var gesture = Default("S");
            var moved = MovingStroke(gesture).Select(p => new PointF(p.X * 0.5f + 300, p.Y * 0.5f - 120)).ToArray();

            Assert.True(GestureSimilarity.Score(gesture, GestureSimilarity.Prepare(moved)) >= 90);
        }

        [Fact]
        public void ReversedStrokeScoresLowerThanTheOriginal()
        {
            var gesture = Default("2Left");
            var original = MovingStroke(gesture);

            double forwards = GestureSimilarity.Score(gesture, GestureSimilarity.Prepare(original));
            double backwards = GestureSimilarity.Score(gesture, GestureSimilarity.Prepare(original.Reverse()));

            Assert.True(forwards > backwards + 20);
        }

        [Fact]
        public void GesturesWithoutAMovingStrokeScoreZero()
        {
            var dotOnly = DefaultGestures.Value.FirstOrDefault(g => g.PointPatterns[0].Points.All(s => s.Length < 2));
            if (dotOnly == null)
                return;

            var line = Enumerable.Range(0, 20).Select(i => new PointF(i * 10, 0)).ToArray();

            Assert.Equal(0, GestureSimilarity.Score(dotOnly, GestureSimilarity.Prepare(line)));
        }

        [Fact]
        public void TapsAndTinyStrokesAreNotSearchable()
        {
            Assert.Null(GestureSimilarity.Prepare(null));
            Assert.Null(GestureSimilarity.Prepare(new[] { new PointF(5, 5) }));
            Assert.Null(GestureSimilarity.Prepare(new[] { new PointF(5, 5), new PointF(6, 6), new PointF(7, 5) }));
        }

        [Fact]
        public void RepeatedPointsAreCollapsed()
        {
            var stroke = new[] { new PointF(0, 0), new PointF(0, 0), new PointF(40, 0), new PointF(40, 0), new PointF(80, 0) };

            Assert.Equal(3, GestureSimilarity.Prepare(stroke).Length);
        }
    }
}
