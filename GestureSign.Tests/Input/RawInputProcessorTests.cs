using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.InputRecorder.Recording;
using GestureSign.Tests.Hid;
using GestureSign.Tests.Replay;
using Xunit;

namespace GestureSign.Tests.Input
{
    /// <summary>
    /// RawInputProcessor decoding real hid.dll-encoded reports of synthetic digitizers
    /// (see <see cref="HidDescriptors"/>).
    /// </summary>
    public class RawInputProcessorTests
    {
        private const ushort Digitizer = 0x0D, GenericDesktop = 0x01, ButtonPage = 0x09;
        private const ushort Tip = 0x42, InRange = 0x32, Barrel = 0x44, ContactId = 0x51, ContactCount = 0x54, X = 0x30, Y = 0x31;
        private const int PenId = 1, TouchScreenId = 2, TouchpadId = 3, VirtualTouchScreenId = 4;

        private static readonly byte[] PenPp = HidPreparsedDataBuilder.Build(HidDescriptors.Pen);
        private static readonly byte[] TouchScreenPp = HidPreparsedDataBuilder.Build(HidDescriptors.MultiTouchScreen);
        private static readonly byte[] TouchpadPp = HidPreparsedDataBuilder.Build(HidDescriptors.PrecisionTouchpad);

        private readonly List<RawPointsDataMessageEventArgs> _frames = new List<RawPointsDataMessageEventArgs>();
        private readonly RecordingEnvironment _environment;
        private readonly RawInputProcessor _processor;

        public RawInputProcessorTests()
        {
            _environment = new RecordingEnvironment(new RecordedEnvironment
            {
                Screens =
                {
                    new RecordedScreen { Primary = true, X = 0, Y = 0, Width = 1920, Height = 1080 },
                    new RecordedScreen { X = 1920, Y = -200, Width = 1000, Height = 800 },
                },
            });
            _environment.Cursor = new Point(100, 100);
            _environment.TickCount = 50000;
            var devices = new RecordingDeviceSource(new[]
            {
                Device(PenId, 0x02, @"\\?\HID#VID_04F3&PID_2B7C&Col03#pen", PenPp),
                Device(TouchScreenId, 0x04, @"\\?\HID#VID_04F3&PID_2B7C&Col01#touch", TouchScreenPp),
                Device(TouchpadId, 0x05, @"\\?\HID#VID_06CB&PID_CE7E&Col01#pad", TouchpadPp),
                Device(VirtualTouchScreenId, 0x04, @"\\?\ROOT#HID#0000#virtual", TouchScreenPp),
            });
            _processor = new RawInputProcessor(devices, _environment);
            _processor.UpdateSettings(ignoreTouchInputWhenUsingPen: true, penGestureButton: DeviceStates.RightClickButton);
            _processor.PointsIntercepted += (o, e) => _frames.Add(e);
        }

        private static RecordedDevice Device(int id, int usage, string name, byte[] pp)
        {
            return new RecordedDevice { Id = id, UsagePage = Digitizer, Usage = usage, Name = name, PreparsedData = Convert.ToBase64String(pp) };
        }

        private void Deliver(int deviceId, params byte[][] reports)
        {
            byte[] data = reports.SelectMany(r => r).ToArray();
            IntPtr buffer = RawInputBuffer.Allocate(RecordingDeviceSource.HandleOf(deviceId), reports[0].Length, reports.Length, data);
            try
            {
                _processor.Process(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private struct Contact
        {
            public int Id, X, Y;
        }

        /// <summary>A contact with the tip switch down.</summary>
        private static Contact Touch(int id, int x, int y) => new Contact { Id = id, X = x, Y = y };

        private static byte[] TouchScreenReport(int contactCount, params Contact[] contacts)
        {
            var report = HidReport.Create(TouchScreenPp, HidDescriptors.TouchScreenReportId);
            report.SetValue(Digitizer, 0, ContactCount, contactCount);
            for (int i = 0; i < contacts.Length; i++)
            {
                short link = (short)(i + 1);
                report.SetValue(Digitizer, link, ContactId, contacts[i].Id);
                report.SetValue(GenericDesktop, link, X, contacts[i].X);
                report.SetValue(GenericDesktop, link, Y, contacts[i].Y);
                report.SetButtons(Digitizer, link, Tip);
            }
            return report.ToArray();
        }

        private static byte[] PenReport(int x, int y, params ushort[] buttons)
        {
            var report = HidReport.Create(PenPp, HidDescriptors.PenReportId);
            report.SetValue(GenericDesktop, 0, X, x);
            report.SetValue(GenericDesktop, 0, Y, y);
            if (buttons.Length != 0)
                report.SetButtons(Digitizer, 0, buttons);
            return report.ToArray();
        }

        [Fact]
        public void TouchScreenCoordinatesMapOntoTheScreenUnderTheCursor()
        {
            _environment.Cursor = new Point(2500, 100);

            Deliver(TouchScreenId, TouchScreenReport(1, Touch(5, 4095, 0)));

            var frame = Assert.Single(_frames);
            Assert.Equal(Devices.TouchScreen, frame.SourceDevice);
            var contact = Assert.Single(frame.RawData);
            Assert.Equal(5, contact.ContactIdentifier);
            Assert.Equal(DeviceStates.Tip, contact.State);
            Assert.Equal(new Point(1920 + 1000, -200), contact.RawPoints);
        }

        [Fact]
        public void HybridFrameIsEmittedOnlyOnceAllContactsArrived()
        {
            Deliver(TouchScreenId, TouchScreenReport(3, Touch(1, 0, 0), Touch(2, 2048, 0)));
            Assert.Empty(_frames);

            Deliver(TouchScreenId, TouchScreenReport(0, Touch(3, 4095, 4095)));

            var frame = Assert.Single(_frames);
            Assert.Equal(new[] { 1, 2, 3 }, frame.RawData.Select(c => c.ContactIdentifier));
            Assert.Equal(new Point(1920, 1080), frame.RawData[2].RawPoints);
        }

        [Fact]
        public void RootEnumeratedDigitizersAreIgnored()
        {
            Deliver(VirtualTouchScreenId, TouchScreenReport(1, Touch(1, 100, 100)));

            Assert.Empty(_frames);
        }

        [Theory]
        [InlineData(99, false)]
        [InlineData(100, true)]
        public void TouchIsIgnoredForAHundredMillisecondsAfterPenActivity(int delay, bool accepted)
        {
            Deliver(PenId, PenReport(100, 100, InRange));
            _environment.TickCount += delay;

            Deliver(TouchScreenId, TouchScreenReport(1, Touch(1, 100, 100)));

            Assert.Equal(accepted, _frames.Any(f => f.SourceDevice == Devices.TouchScreen));
        }

        [Fact]
        public void PenFramesStartOnlyWithTheGestureButtonAndEndWhenItIsReleased()
        {
            Deliver(PenId, PenReport(21240, 15980, InRange, Tip));
            Assert.Empty(_frames);

            Deliver(PenId, PenReport(21240, 15980, InRange, Barrel));
            Deliver(PenId, PenReport(0, 0, InRange));

            Assert.Equal(2, _frames.Count);
            Assert.Equal(DeviceStates.InRange | DeviceStates.RightClickButton, _frames[0].RawData[0].State);
            Assert.Equal(new Point(1920, 1080), _frames[0].RawData[0].RawPoints);
            Assert.Equal(DeviceStates.None, _frames[1].RawData[0].State);
        }

        [Fact]
        public void TouchpadOwningTheGestureBlocksTheTouchScreenUntilReleased()
        {
            var pad = HidReport.Create(TouchpadPp, HidDescriptors.TouchpadReportId)
                .SetValue(Digitizer, 0, ContactCount, 1)
                .SetValue(Digitizer, 1, ContactId, 0)
                .SetValue(GenericDesktop, 1, X, 0)
                .SetValue(GenericDesktop, 1, Y, 0)
                .SetButtons(Digitizer, 1, Tip);
            var padReleased = HidReport.Create(TouchpadPp, HidDescriptors.TouchpadReportId)
                .SetValue(Digitizer, 0, ContactCount, 1)
                .SetValue(Digitizer, 1, ContactId, 0);

            Deliver(TouchpadId, pad.ToArray());
            Deliver(TouchScreenId, TouchScreenReport(1, Touch(1, 100, 100)));
            Deliver(TouchpadId, padReleased.ToArray());
            Deliver(TouchScreenId, TouchScreenReport(1, Touch(1, 100, 100)));

            Assert.Equal(new[] { Devices.TouchPad, Devices.TouchPad, Devices.TouchScreen }, _frames.Select(f => f.SourceDevice));
        }

        [Fact]
        public void TouchpadFramesReportThePhysicalButton()
        {
            var pressed = HidReport.Create(TouchpadPp, HidDescriptors.TouchpadReportId)
                .SetValue(Digitizer, 0, ContactCount, 1)
                .SetValue(Digitizer, 1, ContactId, 0)
                .SetButtons(Digitizer, 1, Tip)
                .SetButtons(ButtonPage, 0, 1);
            var released = HidReport.Create(TouchpadPp, HidDescriptors.TouchpadReportId)
                .SetValue(Digitizer, 0, ContactCount, 1)
                .SetValue(Digitizer, 1, ContactId, 0)
                .SetButtons(Digitizer, 1, Tip);

            Deliver(TouchpadId, pressed.ToArray());
            Deliver(TouchpadId, released.ToArray());

            Assert.Equal(new[] { true, false }, _frames.Select(f => f.ButtonDown));
        }
    }
}
