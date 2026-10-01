using System.Collections.Generic;
using System.Drawing;
using GestureSign.Common.Input;
using GestureSign.Tests.Support;
using ManagedWinapi.Hooks;
using Xunit;

namespace GestureSign.Tests.Input
{
    public class MouseCaptureTests
    {
        private static CaptureHarness RightButtonHarness(int initialTimeout = 0)
        {
            return new CaptureHarness(new TestInputSettings { DrawingButton = MouseActions.Right, InitialTimeout = initialTimeout });
        }

        [Fact]
        public void DragWithDrawingButtonIsSwallowedAndRecognized()
        {
            var h = RightButtonHarness();
            h.Capture.BeforePointsCaptured += (o, e) => h.Host.RecognizedGestureName = "L";

            bool downHandled = h.MouseDown(MouseActions.Right, 100, 100);
            h.MouseMove(150, 100);
            h.MouseMove(150, 160);
            bool upHandled = h.MouseUp(MouseActions.Right, 150, 160);

            Assert.True(downHandled);
            Assert.True(upHandled);
            var stroke = Assert.Single(Assert.Single(h.Completed));
            Assert.Equal(new[] { new Point(100, 100), new Point(150, 100), new Point(150, 160) }, stroke);
            var recognized = Assert.Single(h.Recognized);
            Assert.Equal("L", recognized.GestureName);
            Assert.Equal(new List<int> { 1 }, recognized.ContactIdentifiers);
            Assert.Equal(CaptureState.Ready, h.Capture.State);
            Assert.Equal(new[] { "priority:high", "priority:normal" }, h.Host.Effects);
        }

        [Fact]
        public void UnrecognizedGestureRaisesNoRecognitionEvent()
        {
            var h = RightButtonHarness();

            h.MouseDrag(MouseActions.Right, new Point(0, 0), new Point(0, 100));

            Assert.Single(h.Completed);
            Assert.Empty(h.Recognized);
        }

        [Fact]
        public void MovesShorterThanMinimumPointDistanceAreDropped()
        {
            var h = RightButtonHarness();

            h.MouseDrag(MouseActions.Right,
                new Point(0, 0), new Point(10, 0), new Point(25, 0), new Point(30, 0), new Point(45, 0));

            // 10 < 20 from (0,0): dropped; 25 kept; 30 is 5 from 25: dropped; 45 is 20 from 25: kept.
            Assert.Equal(new[] { new Point(0, 0), new Point(25, 0), new Point(45, 0) }, h.Completed[0][0]);
        }

        [Fact]
        public void ClickWithoutMovementIsReplayedAsAClick()
        {
            var h = RightButtonHarness();

            bool downHandled = h.MouseDown(MouseActions.Right, 100, 100);
            h.MouseMove(105, 103);
            bool upHandled = h.MouseUp(MouseActions.Right, 105, 103);

            Assert.True(downHandled);
            Assert.True(upHandled);
            Assert.Empty(h.Completed);
            Assert.Equal(new[] { "priority:high", "click:Right", "priority:normal" }, h.Host.Effects);
            Assert.Equal(CaptureState.Ready, h.Capture.State);
        }

        [Fact]
        public void NonDrawingButtonIsIgnored()
        {
            var h = RightButtonHarness();

            bool downHandled = h.MouseDown(MouseActions.Left, 0, 0);
            h.MouseMove(0, 100);
            bool upHandled = h.MouseUp(MouseActions.Left, 0, 100);

            Assert.False(downHandled);
            Assert.False(upHandled);
            Assert.Empty(h.Completed);
            Assert.Empty(h.Host.Effects);
        }

        [Fact]
        public void DrawingButtonPressedWhileAnotherButtonIsHeldDoesNotStartAGesture()
        {
            var h = RightButtonHarness();

            h.MouseDown(MouseActions.Left, 0, 0);
            bool downHandled = h.MouseDown(MouseActions.Right, 0, 0);
            h.MouseMove(0, 100);
            bool upHandled = h.MouseUp(MouseActions.Right, 0, 100);

            Assert.False(downHandled);
            Assert.False(upHandled);
            Assert.Empty(h.Completed);
            Assert.Empty(h.Host.Effects);
        }

        [Fact]
        public void UserDisabledModeCapturesButDoesNotSwallowInput()
        {
            var h = RightButtonHarness();
            h.Capture.ToggleUserDisablePointCapture();

            bool downHandled = h.MouseDown(MouseActions.Right, 0, 0);
            h.MouseMove(0, 100);
            bool upHandled = h.MouseUp(MouseActions.Right, 0, 100);

            Assert.Equal(CaptureMode.UserDisabled, h.Capture.Mode);
            Assert.False(downHandled);
            Assert.False(upHandled);
            Assert.Single(h.Completed);
        }

        [Fact]
        public void UserDisabledClickIsNotReplayed()
        {
            var h = RightButtonHarness();
            h.Capture.ToggleUserDisablePointCapture();

            h.MouseDown(MouseActions.Right, 0, 0);
            bool upHandled = h.MouseUp(MouseActions.Right, 0, 0);

            Assert.False(upHandled);
            Assert.DoesNotContain("click:Right", h.Host.Effects);
            Assert.Equal(CaptureState.Ready, h.Capture.State);
        }

        [Fact]
        public void HoldingStillPastInitialTimeoutReleasesTheButtonToTheSystem()
        {
            var h = RightButtonHarness(initialTimeout: 300);

            h.MouseDown(MouseActions.Right, 100, 100);
            h.Host.Advance(299);
            Assert.DoesNotContain("buttonDown:Right", h.Host.Effects);
            h.Host.Advance(1);
            bool upHandled = h.MouseUp(MouseActions.Right, 100, 100);

            Assert.Contains("buttonDown:Right", h.Host.Effects);
            Assert.DoesNotContain("click:Right", h.Host.Effects);
            Assert.False(upHandled);
            Assert.Empty(h.Completed);
            Assert.Equal(CaptureState.Ready, h.Capture.State);
        }

        [Fact]
        public void InitialTimeoutDoesNotInterruptAValidGesture()
        {
            var h = RightButtonHarness(initialTimeout: 300);

            h.MouseDown(MouseActions.Right, 0, 0);
            h.MouseMove(0, 100);
            h.Host.Advance(1000);
            bool upHandled = h.MouseUp(MouseActions.Right, 0, 100);

            Assert.True(upHandled);
            Assert.DoesNotContain("buttonDown:Right", h.Host.Effects);
            Assert.Single(h.Completed);
        }

        [Fact]
        public void CanceledCaptureStartLetsInputThrough()
        {
            var h = RightButtonHarness();
            h.Capture.CaptureStarted += (o, e) => e.Cancel = true;

            bool downHandled = h.MouseDown(MouseActions.Right, 0, 0);
            h.MouseMove(0, 100);
            bool upHandled = h.MouseUp(MouseActions.Right, 0, 100);

            Assert.False(downHandled);
            Assert.False(upHandled);
            Assert.Empty(h.Completed);
            Assert.Equal(new[] { "priority:high", "priority:normal" }, h.Host.Effects);
        }

        [Fact]
        public void ReleaseAfterTriggerFiredIsSwallowedWithoutRecognition()
        {
            var h = RightButtonHarness();

            h.MouseDown(MouseActions.Right, 0, 0);
            h.MouseMove(0, 100);
            h.Capture.State = CaptureState.TriggerFired;
            bool upHandled = h.MouseUp(MouseActions.Right, 0, 100);

            Assert.True(upHandled);
            Assert.Empty(h.Completed);
            Assert.Equal(CaptureState.Ready, h.Capture.State);
        }

        [Fact]
        public void TrainingModeSendsTheGestureToTheControlPanel()
        {
            var h = RightButtonHarness();
            h.Capture.Mode = CaptureMode.Training;

            h.MouseDrag(MouseActions.Right, new Point(0, 0), new Point(0, 100));

            var patterns = Assert.Single(h.Host.TrainingGestures);
            var pattern = Assert.Single(patterns);
            Assert.Equal(new[] { new Point(0, 0), new Point(0, 100) }, Assert.Single(pattern));
            Assert.Equal(CaptureMode.Training, h.Capture.Mode);
        }

        [Fact]
        public void TrainingModeEndsWhenControlPanelIsUnreachable()
        {
            var h = RightButtonHarness();
            h.Capture.Mode = CaptureMode.Training;
            h.Host.ControlPanelReachable = false;

            h.MouseDrag(MouseActions.Right, new Point(0, 0), new Point(0, 100));

            Assert.Equal(CaptureMode.Normal, h.Capture.Mode);
        }
    }
}
