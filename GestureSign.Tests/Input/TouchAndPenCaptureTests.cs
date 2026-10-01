using System.Drawing;
using System.Linq;
using GestureSign.Common.Input;
using GestureSign.Tests.Support;
using Xunit;
using static GestureSign.Tests.Support.CaptureHarness;

namespace GestureSign.Tests.Input
{
    public class TouchAndPenCaptureTests
    {
        [Fact]
        public void TouchPadGestureAnchorsAtTheCursor()
        {
            var h = new CaptureHarness();
            h.Host.CursorPosition = new Point(500, 400);
            h.Capture.BeforePointsCaptured += (o, e) => h.Host.RecognizedGestureName = "Down";

            h.Frame(Devices.TouchPad, Tip(0, 10, 10));
            h.Frame(Devices.TouchPad, Tip(0, 10, 60));
            h.Frame(Devices.TouchPad, Lifted(0, 10, 60));

            Assert.Equal(new[] { new Point(500, 400) }, Assert.Single(h.Started).FirstCapturedPoints);
            Assert.Equal(new[] { new Point(10, 10), new Point(10, 60) }, Assert.Single(Assert.Single(h.Completed)));
            Assert.Equal(new[] { new Point(500, 400) }, Assert.Single(h.Recognized).FirstCapturedPoints);
        }

        [Fact]
        public void TouchScreenGestureAnchorsAtTheFirstContactPoints()
        {
            var h = new CaptureHarness();
            h.Host.CursorPosition = new Point(500, 400);
            h.Capture.BeforePointsCaptured += (o, e) => h.Host.RecognizedGestureName = "Down";

            h.Frame(Devices.TouchScreen, Tip(7, 10, 10));
            h.Frame(Devices.TouchScreen, Tip(7, 10, 60));
            h.Frame(Devices.TouchScreen, Lifted(7, 10, 60));

            Assert.Equal(new[] { new Point(10, 10) }, Assert.Single(h.Started).FirstCapturedPoints);
            Assert.Equal(new[] { new Point(10, 10) }, Assert.Single(h.Recognized).FirstCapturedPoints);
        }

        [Theory]
        [InlineData(true, new[] { 2, 1 })]
        [InlineData(false, new[] { 1, 2 })]
        public void MultiFingerStrokesAreOrderedByLocationOrContactId(bool orderByLocation, int[] expectedContactOrder)
        {
            var h = new CaptureHarness(new TestInputSettings { IsOrderByLocation = orderByLocation });
            h.Capture.BeforePointsCaptured += (o, e) => h.Host.RecognizedGestureName = "TwoFingerDown";

            h.Frame(Devices.TouchScreen, Tip(1, 300, 100), Tip(2, 100, 100));
            h.Frame(Devices.TouchScreen, Tip(1, 300, 150), Tip(2, 100, 150));
            h.Frame(Devices.TouchScreen, Lifted(1, 300, 150), Lifted(2, 100, 150));

            var recognized = Assert.Single(h.Recognized);
            Assert.Equal(expectedContactOrder, recognized.ContactIdentifiers);
            var strokeStartX = h.Completed[0].Select(s => s[0].X).ToArray();
            Assert.Equal(expectedContactOrder.Select(id => id == 1 ? 300 : 100).ToArray(), strokeStartX);
        }

        [Fact]
        public void TapIsCompletedAsASinglePointGesture()
        {
            var h = new CaptureHarness();

            h.Frame(Devices.TouchScreen, Tip(0, 10, 10));
            h.Frame(Devices.TouchScreen, Lifted(0, 10, 10));

            Assert.Equal(new[] { new Point(10, 10) }, Assert.Single(Assert.Single(h.Completed)));
            Assert.DoesNotContain(h.Host.Effects, e => e.StartsWith("click"));
        }

        [Fact]
        public void TapInTrainingModeIsNotSentAsATrainedGesture()
        {
            var h = new CaptureHarness();
            h.Capture.Mode = CaptureMode.Training;

            h.Frame(Devices.TouchScreen, Tip(0, 10, 10));
            h.Frame(Devices.TouchScreen, Lifted(0, 10, 10));

            Assert.Empty(h.Host.TrainingGestures);
        }

        [Fact]
        public void AddingAFingerEarlyRestartsTheGestureWithBothFingers()
        {
            var h = new CaptureHarness();

            h.Frame(Devices.TouchScreen, Tip(1, 100, 100));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 130));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 130), Tip(2, 200, 130));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 180), Tip(2, 200, 180));
            h.Frame(Devices.TouchScreen, Lifted(1, 100, 180), Lifted(2, 200, 180));

            var strokes = Assert.Single(h.Completed);
            Assert.Equal(2, strokes.Count);
            Assert.Equal(new[] { new Point(100, 130), new Point(100, 180) }, strokes[0]);
            Assert.Equal(new[] { new Point(200, 130), new Point(200, 180) }, strokes[1]);
        }

        [Fact]
        public void AddingAFingerLateKeepsTheOriginalSingleStroke()
        {
            var h = new CaptureHarness();

            // 11 captured points make the stroke "long"; a new finger is then treated as a move.
            for (int i = 0; i <= 10; i++)
                h.Frame(Devices.TouchScreen, Tip(1, 100, 100 + i * 20));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 320), Tip(2, 200, 320));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 340), Tip(2, 200, 340));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 360), Lifted(2, 200, 360));
            h.Frame(Devices.TouchScreen, Lifted(1, 100, 360));

            var stroke = Assert.Single(Assert.Single(h.Completed));
            Assert.Equal(13, stroke.Count);
            Assert.All(stroke, p => Assert.Equal(100, p.X));
        }


        [Fact]
        public void FramesFromAnotherTouchDeviceDoNotJoinTheGesture()
        {
            var h = new CaptureHarness();

            h.Frame(Devices.TouchScreen, Tip(1, 100, 100));
            h.Frame(Devices.TouchPad, Tip(1, 900, 900));
            h.Frame(Devices.TouchScreen, Tip(1, 100, 150));
            h.Frame(Devices.TouchScreen, Lifted(1, 100, 150));

            Assert.Equal(new[] { new Point(100, 100), new Point(100, 150) }, Assert.Single(Assert.Single(h.Completed)));
        }

        [Fact]
        public void PenDrawsWithTipWhileBarrelButtonIsHeld()
        {
            var h = new CaptureHarness(new TestInputSettings { PenGestureButton = DeviceStates.RightClickButton | DeviceStates.Tip });
            const DeviceStates hover = DeviceStates.InRange | DeviceStates.RightClickButton;

            h.Frame(Devices.Pen, Pen(hover, 100, 100));
            Assert.Empty(h.Started);
            h.Frame(Devices.Pen, Pen(hover | DeviceStates.Tip, 100, 100));
            h.Frame(Devices.Pen, Pen(hover | DeviceStates.Tip, 100, 160));
            h.Frame(Devices.Pen, Pen(hover, 100, 160));

            Assert.Equal(new[] { new Point(100, 100), new Point(100, 160) }, Assert.Single(Assert.Single(h.Completed)));
        }

        [Fact]
        public void ReleasingTheBarrelButtonEndsThePenGesture()
        {
            var h = new CaptureHarness(new TestInputSettings { PenGestureButton = DeviceStates.RightClickButton | DeviceStates.Tip });
            const DeviceStates drawing = DeviceStates.InRange | DeviceStates.RightClickButton | DeviceStates.Tip;

            h.Frame(Devices.Pen, Pen(drawing, 100, 100));
            h.Frame(Devices.Pen, Pen(drawing, 100, 160));
            h.Frame(Devices.Pen, Pen(DeviceStates.InRange | DeviceStates.Tip, 100, 200));

            Assert.Equal(new[] { new Point(100, 100), new Point(100, 160) }, Assert.Single(Assert.Single(h.Completed)));
        }

        [Fact]
        public void PenDrawsWhileHoveringWithBarrelButton()
        {
            var h = new CaptureHarness(new TestInputSettings { PenGestureButton = DeviceStates.RightClickButton | DeviceStates.InRange });
            const DeviceStates hover = DeviceStates.InRange | DeviceStates.RightClickButton;

            h.Frame(Devices.Pen, Pen(hover, 100, 100));
            h.Frame(Devices.Pen, Pen(hover, 160, 100));
            h.Frame(Devices.Pen, Pen(DeviceStates.InRange, 160, 100));

            Assert.Equal(new[] { new Point(100, 100), new Point(160, 100) }, Assert.Single(Assert.Single(h.Completed)));
        }

    }
}
