using System.Collections.Generic;
using System.Drawing;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.InputRecorder.Recording;
using GestureSign.Tests.Replay;
using Xunit;

namespace GestureSign.Tests.Input
{
    public class TouchPadTapClickerTests
    {
        private readonly RecordingEnvironment _environment;
        private readonly TouchPadTapClicker _clicker;
        private int _clicks;

        public TouchPadTapClickerTests()
        {
            _environment = new RecordingEnvironment(new RecordedEnvironment
            {
                Screens = { new RecordedScreen { Primary = true, X = 0, Y = 0, Width = 1920, Height = 1080 } },
            });
            _environment.Cursor = new Point(100, 100);
            _environment.TickCount = 10000;
            _clicker = new TouchPadTapClicker(_environment, () => _clicks++);
        }

        private static RawData Down(int id, int x = 500, int y = 500) => new RawData(DeviceStates.Tip, id, new Point(x, y));

        private static RawData Up(int id, int x = 500, int y = 500) => new RawData(DeviceStates.None, id, new Point(x, y));

        private void Frame(int elapsed, params RawData[] contacts) => Frame(elapsed, false, contacts);

        private void Frame(int elapsed, bool buttonDown, params RawData[] contacts)
        {
            _environment.TickCount += elapsed;
            _clicker.Process(new RawPointsDataMessageEventArgs(new List<RawData>(contacts), Devices.TouchPad) { ButtonDown = buttonDown });
        }

        [Fact]
        public void QuickSingleFingerTapClicksOnRelease()
        {
            Frame(0, Down(0));
            Frame(80, Down(0, 510, 505));
            Assert.Equal(0, _clicks);

            Frame(40, Up(0, 510, 505));
            Assert.Equal(1, _clicks);
        }

        [Fact]
        public void SecondFingerTappedWhileFirstRestsDoesNotClick()
        {
            Frame(0, Down(0));
            Frame(600, Down(0), Down(1, 900, 500));
            Frame(80, Down(0), Up(1, 900, 500));
            Frame(500, Up(0));

            Assert.Equal(0, _clicks);
        }

        [Fact]
        public void TwoFingerTapDoesNotClick()
        {
            Frame(0, Down(0), Down(1, 900, 500));
            Frame(60, Up(0), Up(1, 900, 500));

            Assert.Equal(0, _clicks);
        }

        [Theory]
        [InlineData(TouchPadTapClicker.MaxTapDuration, 1)]
        [InlineData(TouchPadTapClicker.MaxTapDuration + 1, 0)]
        public void TouchHeldLongerThanATapDoesNotClick(int duration, int clicks)
        {
            Frame(0, Down(0));
            Frame(duration, Up(0));

            Assert.Equal(clicks, _clicks);
        }

        [Theory]
        [InlineData(48, 0, 1)]
        [InlineData(49, 0, 0)]
        [InlineData(0, 27, 1)]
        [InlineData(0, 28, 0)]
        public void FingerThatSlidesDoesNotClick(int dx, int dy, int clicks)
        {
            Frame(0, Down(0));
            Frame(30, Down(0, 500 + dx, 500 + dy));
            Frame(30, Down(0));
            Frame(30, Up(0));

            Assert.Equal(clicks, _clicks);
        }

        [Fact]
        public void PhysicalButtonPressDoesNotClickAgain()
        {
            Frame(0, Down(0));
            Frame(50, true, Down(0));
            Frame(50, Up(0));

            Assert.Equal(0, _clicks);
        }

        [Fact]
        public void TapAfterAMultiFingerTouchStillClicks()
        {
            Frame(0, Down(0), Down(1, 900, 500));
            Frame(60, Up(0), Up(1, 900, 500));
            Frame(100, Down(0));
            Frame(60, Up(0));

            Assert.Equal(1, _clicks);
        }
    }
}
