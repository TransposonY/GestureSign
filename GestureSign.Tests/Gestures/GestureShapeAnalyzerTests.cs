using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GestureSign.Common.Gestures;
using Xunit;

namespace GestureSign.Tests.Gestures
{
    /// <summary>Shape, dot and heading traits that drive the Gesture tab's filter bar.</summary>
    public class GestureShapeAnalyzerTests
    {

        private static readonly Lazy<List<IGesture>> DefaultGestures = new Lazy<List<IGesture>>(() =>
            GestureManager.LoadGesturesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Gestures", "Defaults.gest"), throwException: true));

        private static GestureShapeInfo Analyze(string name) =>
            GestureShapeAnalyzer.Analyze(DefaultGestures.Value.Single(g => g.Name == name).PointPatterns);

        private static GestureShapeInfo Analyze(params Point[][] strokes) =>
            GestureShapeAnalyzer.Analyze(new[] { new PointPattern(strokes) });

        [Theory]
        [InlineData("3Left", 3, false, GestureShape.Line, GestureDirection.Left)]
        [InlineData("2Down", 2, false, GestureShape.Line, GestureDirection.Down)]
        [InlineData("2Left", 2, false, GestureShape.Line, GestureDirection.Left)]
        [InlineData("Up", 2, true, GestureShape.Line, GestureDirection.Up)]
        [InlineData("Point Right", 2, true, GestureShape.Line, GestureDirection.Right)]
        [InlineData("L", 2, true, GestureShape.Curve, GestureDirection.DownRight)]
        [InlineData("S", 2, true, GestureShape.Curve, GestureDirection.DownLeft)]
        [InlineData("ee", 2, false, GestureShape.Loop, GestureDirection.None)]
        [InlineData("3 Finger Double Tap", 3, true, GestureShape.None, GestureDirection.None)]
        public void ClassifiesDefaultGestures(string name, int contacts, bool dot, GestureShape shape, GestureDirection direction)
        {
            var info = Analyze(name);

            Assert.Equal(contacts, info.Contacts);
            Assert.Equal(dot, info.HasDot);
            Assert.Equal(shape, info.Shape);
            Assert.Equal(direction, info.Direction);
        }

        [Fact]
        public void AFingerHeldInPlaceIsADot()
        {
            var info = Analyze(
                new[] { new Point(10, 10) },
                new[] { new Point(100, 100), new Point(160, 100), new Point(220, 100) });

            Assert.True(info.HasDot);
            Assert.Equal(2, info.Contacts);
            Assert.Equal(GestureShape.Line, info.Shape);
            Assert.Equal(GestureDirection.Right, info.Direction);
        }

        [Fact]
        public void TwoMovingFingersHaveNoDot()
        {
            var info = Analyze(
                new[] { new Point(0, 0), new Point(0, 40), new Point(0, 80) },
                new[] { new Point(50, 80), new Point(50, 40), new Point(50, 0) });

            Assert.False(info.HasDot);
            Assert.Equal(GestureShape.Line, info.Shape);
        }

        [Fact]
        public void GestureWithOnlyDotsHasNoShape()
        {
            var info = Analyze(new[] { new Point(0, 0) }, new[] { new Point(60, 0) });

            Assert.True(info.HasDot);
            Assert.Equal(GestureShape.None, info.Shape);
            Assert.Equal(GestureDirection.None, info.Direction);
        }

        [Fact]
        public void ACircleIsALoopWithoutHeading()
        {
            var circle = Enumerable.Range(0, 37)
                .Select(i => new Point((int)(100 + 80 * Math.Cos(i * Math.PI / 18)), (int)(100 + 80 * Math.Sin(i * Math.PI / 18))))
                .ToArray();

            var info = Analyze(circle);

            Assert.Equal(GestureShape.Loop, info.Shape);
            Assert.Equal(GestureDirection.None, info.Direction);
        }

        [Fact]
        public void ACornerIsACurve()
        {
            var info = Analyze(new[] { new Point(0, 0), new Point(0, 50), new Point(0, 100), new Point(50, 100), new Point(100, 100) });

            Assert.Equal(GestureShape.Curve, info.Shape);
        }

        [Fact]
        public void HandJitterOnALineStaysALine()
        {
            var random = new Random(7);
            var line = Enumerable.Range(0, 40).Select(i => new Point(i * 10 + random.Next(-2, 3), 200 + random.Next(-3, 4))).ToArray();

            Assert.Equal(GestureShape.Line, Analyze(line).Shape);
        }

        [Theory]
        [InlineData(100, 0, GestureDirection.Right)]
        [InlineData(100, -100, GestureDirection.UpRight)]
        [InlineData(0, -100, GestureDirection.Up)]
        [InlineData(-100, -100, GestureDirection.UpLeft)]
        [InlineData(-100, 0, GestureDirection.Left)]
        [InlineData(-100, 100, GestureDirection.DownLeft)]
        [InlineData(0, 100, GestureDirection.Down)]
        [InlineData(100, 100, GestureDirection.DownRight)]
        public void HeadingFollowsTheStrokeOnScreen(int dx, int dy, GestureDirection expected)
        {
            var info = Analyze(new[] { new Point(500, 500), new Point(500 + dx / 2, 500 + dy / 2), new Point(500 + dx, 500 + dy) });

            Assert.Equal(expected, info.Direction);
        }

        [Fact]
        public void MissingPatternsAreHandled()
        {
            Assert.Equal(GestureShape.None, GestureShapeAnalyzer.Analyze(null).Shape);
            Assert.Equal(GestureShape.None, GestureShapeAnalyzer.Analyze(new[] { new PointPattern(new Point[0][]) }).Shape);
        }
    }
}
