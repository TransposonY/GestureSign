using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GestureSign.Common.Gestures;
using Xunit;

namespace GestureSign.Tests.Gestures
{
    /// <summary>Recognition over the gesture set shipped in GestureSign.ControlPanel/Defaults/Gestures.gest.</summary>
    public class GestureMatchingTests
    {
        private static readonly Lazy<List<IGesture>> DefaultGestures = new Lazy<List<IGesture>>(() =>
            GestureManager.LoadGesturesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Gestures", "Defaults.gest"), throwException: true));

        private static IGesture Default(string name) => DefaultGestures.Value.Single(g => g.Name == name);

        private static string Recognize(Point[][] strokes)
        {
            List<IGesture> matching;
            return GestureManager.GetGestureSetNameMatch(strokes, DefaultGestures.Value, 0, out matching);
        }

        public static IEnumerable<object[]> SingleLevelDefaultGestures()
        {
            return DefaultGestures.Value.Where(g => g.PointPatterns.Length == 1).Select(g => new object[] { g.Name });
        }

        [Theory]
        [MemberData(nameof(SingleLevelDefaultGestures))]
        public void EveryDefaultGestureRecognizesItsOwnSample(string name)
        {
            Assert.Equal(name, Recognize(Default(name).PointPatterns[0].Points));
        }

        [Theory]
        [InlineData("L", 0.5, 300, -200)]
        [InlineData("S", 2.0, -400, 100)]
        [InlineData("2UpRight", 0.7, 50, 50)]
        public void RecognitionIsInvariantToScaleAndPosition(string name, double scale, int dx, int dy)
        {
            var transformed = Default(name).PointPatterns[0].Points
                .Select(stroke => stroke.Select(p => new Point((int)(p.X * scale) + dx, (int)(p.Y * scale) + dy)).ToArray())
                .ToArray();

            Assert.Equal(name, Recognize(transformed));
        }

        [Fact]
        public void SmallJitterDoesNotChangeTheResult()
        {
            var random = new Random(1234);
            var jittered = Default("S").PointPatterns[0].Points
                .Select(stroke => stroke.Select(p => new Point(p.X + random.Next(-3, 4), p.Y + random.Next(-3, 4))).ToArray())
                .ToArray();

            Assert.Equal("S", Recognize(jittered));
        }

        [Theory]
        [InlineData("2Left")]
        [InlineData("3Down")]
        public void ReversedStrokesAreNotRecognizedAsTheOriginal(string name)
        {
            var reversed = Default(name).PointPatterns[0].Points.Select(stroke => stroke.Reverse().ToArray()).ToArray();

            Assert.NotEqual(name, Recognize(reversed));
        }

        [Fact]
        public void StrokeCountMustMatch()
        {
            var twoFingerDown = Default("2Down").PointPatterns[0].Points;

            Assert.Null(Recognize(new[] { twoFingerDown[0] }));
        }

        [Fact]
        public void MultiLevelGestureNeedsEveryLevel()
        {
            var doubleTap = Default("3 Finger Double Tap");
            List<IGesture> matching;

            string first = GestureManager.GetGestureSetNameMatch(doubleTap.PointPatterns[0].Points, DefaultGestures.Value, 0, out matching);
            string second = GestureManager.GetGestureSetNameMatch(doubleTap.PointPatterns[1].Points, matching, 1, out matching);

            Assert.NotEqual("3 Finger Double Tap", first);
            Assert.Equal("3 Finger Double Tap", second);
        }
    }
}
