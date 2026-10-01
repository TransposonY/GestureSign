using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GestureSign.Common.Input;
using GestureSign.InputRecorder.Recording;
using GestureSign.Tests.Hid;

namespace GestureSign.Tests.Replay
{
    /// <summary>
    /// Scripted recordings built from synthetic HID descriptors (real hid.dll encodes the reports), so the
    /// golden corpus covers touchpad, touch screen, pen and mouse paths without hardware.
    /// Written to Fixtures/Replay/synthetic when GESTURESIGN_UPDATE_GOLDEN=1.
    /// </summary>
    internal static class SyntheticRecordings
    {
        private const ushort DigitizerPage = 0x0D;
        private const ushort GenericDesktopPage = 0x01;
        private const ushort TipSwitch = 0x42;
        private const ushort InRange = 0x32;
        private const ushort BarrelSwitch = 0x44;
        private const ushort Invert = 0x3C;
        private const ushort Eraser = 0x45;
        private const ushort ContactId = 0x51;
        private const ushort ContactCount = 0x54;
        private const ushort X = 0x30;
        private const ushort Y = 0x31;

        private const string TouchpadName = @"\\?\HID#VID_06CB&PID_CE7E&Col01#5&1a2b3c4d&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
        private const string TouchScreenName = @"\\?\HID#VID_04F3&PID_2B7C&Col01#5&2b3c4d5e&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
        private const string PenName = @"\\?\HID#VID_04F3&PID_2B7C&Col03#5&2b3c4d5e&0&0002#{4d1e55b2-f16f-11cf-88cb-001111000030}";
        private const string VirtualTouchScreenName = @"\\?\ROOT#HID#0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";

        private static readonly Lazy<byte[]> TouchpadPp = new Lazy<byte[]>(() => HidPreparsedDataBuilder.Build(HidDescriptors.PrecisionTouchpad));
        private static readonly Lazy<byte[]> TouchScreenPp = new Lazy<byte[]>(() => HidPreparsedDataBuilder.Build(HidDescriptors.MultiTouchScreen));
        private static readonly Lazy<byte[]> PenPp = new Lazy<byte[]>(() => HidPreparsedDataBuilder.Build(HidDescriptors.Pen));

        public static void WriteAll(string fixtureDirectory)
        {
            string directory = Path.Combine(fixtureDirectory, "synthetic");
            Directory.CreateDirectory(directory);
            foreach (var scenario in All())
                scenario.Value.Save(Path.Combine(directory, scenario.Key + ".gsrec.json"));
        }

        public static IEnumerable<KeyValuePair<string, InputRecording>> All()
        {
            yield return Scenario("touchpad-1finger-l", "Touchpad: one finger draws an L.", b =>
            {
                int pad = b.Touchpad();
                b.TouchpadStroke(pad, Line(new PointF(0.3f, 0.2f), new PointF(0.3f, 0.7f), 6).Concat(Line(new PointF(0.3f, 0.7f), new PointF(0.7f, 0.7f), 6).Skip(1)));
            });

            yield return Scenario("touchpad-2finger-down", "Touchpad: two fingers swipe down together.", b =>
            {
                int pad = b.Touchpad();
                b.TouchpadFrames(pad, Parallel(Line(new PointF(0.4f, 0.2f), new PointF(0.4f, 0.8f), 8), Line(new PointF(0.6f, 0.2f), new PointF(0.6f, 0.8f), 8)));
            });

            yield return Scenario("touchpad-3finger-left", "Touchpad: three fingers swipe left.", b =>
            {
                int pad = b.Touchpad();
                b.TouchpadFrames(pad, Parallel(
                    Line(new PointF(0.8f, 0.3f), new PointF(0.2f, 0.3f), 8),
                    Line(new PointF(0.8f, 0.5f), new PointF(0.2f, 0.5f), 8),
                    Line(new PointF(0.8f, 0.7f), new PointF(0.2f, 0.7f), 8)));
            });

            yield return Scenario("touchpad-second-monitor", "Touchpad with the cursor on a secondary monitor left of the primary.", b =>
            {
                b.Recording.Environment.Screens.Add(new RecordedScreen { DeviceName = @"\\.\DISPLAY2", X = -1280, Y = 0, Width = 1280, Height = 1024 });
                b.Cursor = new Point(-600, 500);
                int pad = b.Touchpad();
                b.TouchpadStroke(pad, Line(new PointF(0.2f, 0.5f), new PointF(0.8f, 0.5f), 6));
            });

            yield return Scenario("touchscreen-1finger-stroke", "Touch screen: one finger draws a diagonal stroke.", b =>
            {
                int screen = b.TouchScreen();
                b.TouchScreenFrames(screen, Parallel(Line(new PointF(0.2f, 0.2f), new PointF(0.6f, 0.6f), 8)), reportsPerMessage: 1);
            });

            yield return Scenario("touchscreen-tap", "Touch screen: a single tap.", b =>
            {
                int screen = b.TouchScreen();
                b.TouchScreenFrames(screen, Parallel(new[] { new PointF(0.5f, 0.5f), new PointF(0.5f, 0.5f) }), reportsPerMessage: 1);
            });

            yield return Scenario("touchscreen-3finger-down-hybrid", "Touch screen: three fingers swipe down; each frame spans two reports (hybrid mode) delivered in one WM_INPUT.", b =>
            {
                int screen = b.TouchScreen();
                b.TouchScreenFrames(screen, Parallel(
                    Line(new PointF(0.3f, 0.2f), new PointF(0.3f, 0.7f), 8),
                    Line(new PointF(0.5f, 0.2f), new PointF(0.5f, 0.7f), 8),
                    Line(new PointF(0.7f, 0.2f), new PointF(0.7f, 0.7f), 8)), reportsPerMessage: 2);
            });

            yield return Scenario("touchscreen-3finger-down-hybrid-split", "Touch screen: same as hybrid, but each report arrives in its own WM_INPUT.", b =>
            {
                int screen = b.TouchScreen();
                b.TouchScreenFrames(screen, Parallel(
                    Line(new PointF(0.3f, 0.2f), new PointF(0.3f, 0.7f), 8),
                    Line(new PointF(0.5f, 0.2f), new PointF(0.5f, 0.7f), 8),
                    Line(new PointF(0.7f, 0.2f), new PointF(0.7f, 0.7f), 8)), reportsPerMessage: 1);
            });

            foreach (int orientation in new[] { 90, 180, 270 })
            {
                int degrees = orientation;
                yield return Scenario($"touchscreen-rotated-{degrees}", $"Touch screen on a display rotated {degrees} degrees: one finger moves along the digitizer X axis.", b =>
                {
                    b.Recording.Environment.Orientation = degrees;
                    int screen = b.TouchScreen();
                    b.TouchScreenFrames(screen, Parallel(Line(new PointF(0.2f, 0.3f), new PointF(0.8f, 0.3f), 6)), reportsPerMessage: 1);
                });
            }

            yield return Scenario("touchscreen-virtual-ignored", "A ROOT-enumerated (virtual) touch screen is ignored.", b =>
            {
                int screen = b.TouchScreen(VirtualTouchScreenName);
                b.TouchScreenFrames(screen, Parallel(Line(new PointF(0.2f, 0.2f), new PointF(0.6f, 0.6f), 6)), reportsPerMessage: 1);
            });

            yield return Scenario("pen-barrel-tip-stroke", "Pen: barrel button held, tip draws a stroke (PenGestureButton = barrel + tip).", b =>
            {
                b.Recording.Settings.PenGestureButton = (int)(DeviceStates.RightClickButton | DeviceStates.Tip);
                int pen = b.Pen();
                b.PenSample(pen, new PointF(0.4f, 0.4f), inRange: true, barrel: true);
                foreach (var p in Line(new PointF(0.4f, 0.4f), new PointF(0.4f, 0.8f), 6))
                    b.PenSample(pen, p, inRange: true, barrel: true, tip: true);
                b.PenSample(pen, new PointF(0.4f, 0.8f), inRange: true, barrel: true);
                b.PenSample(pen, new PointF(0.4f, 0.8f), inRange: false);
            });

            yield return Scenario("pen-hover-barrel-stroke", "Pen: hovering with barrel button draws (PenGestureButton = barrel + in range).", b =>
            {
                b.Recording.Settings.PenGestureButton = (int)(DeviceStates.RightClickButton | DeviceStates.InRange);
                int pen = b.Pen();
                foreach (var p in Line(new PointF(0.2f, 0.5f), new PointF(0.7f, 0.5f), 6))
                    b.PenSample(pen, p, inRange: true, barrel: true);
                b.PenSample(pen, new PointF(0.7f, 0.5f), inRange: true);
                b.PenSample(pen, new PointF(0.7f, 0.5f), inRange: false);
            });

            yield return Scenario("pen-eraser-end", "Pen: inverted (eraser end) hover draws (PenGestureButton = invert + in range).", b =>
            {
                b.Recording.Settings.PenGestureButton = (int)(DeviceStates.Invert | DeviceStates.InRange);
                int pen = b.Pen();
                foreach (var p in Line(new PointF(0.5f, 0.2f), new PointF(0.5f, 0.7f), 6))
                    b.PenSample(pen, p, inRange: true, invert: true);
                b.PenSample(pen, new PointF(0.5f, 0.7f), inRange: false);
            });

            yield return Scenario("pen-suppresses-touch", "Touch within 100 ms of pen activity is ignored when IgnoreTouchInputWhenUsingPen is on.", b =>
            {
                int pen = b.Pen();
                int screen = b.TouchScreen();
                b.PenSample(pen, new PointF(0.5f, 0.5f), inRange: true);
                b.Advance(50);
                b.TouchScreenFrames(screen, Parallel(Line(new PointF(0.2f, 0.2f), new PointF(0.2f, 0.6f), 4)), reportsPerMessage: 1);
                b.Advance(200);
                b.TouchScreenFrames(screen, Parallel(Line(new PointF(0.6f, 0.2f), new PointF(0.6f, 0.6f), 4)), reportsPerMessage: 1);
            });

            yield return Scenario("mouse-right-drag", "Mouse: right-button drag draws a gesture (DrawingButton = Right).", b =>
            {
                b.Recording.Settings.DrawingButton = "Right";
                b.MouseDrag("Right", new Point(800, 300), new Point(800, 700), 8);
            });

            yield return Scenario("mouse-right-click", "Mouse: right click without movement is replayed to the system.", b =>
            {
                b.Recording.Settings.DrawingButton = "Right";
                b.MouseDrag("Right", new Point(800, 300), new Point(805, 302), 2);
            });

            yield return Scenario("mouse-hold-initial-timeout", "Mouse: holding the drawing button still past InitialTimeout hands the press back.", b =>
            {
                b.Recording.Settings.DrawingButton = "Right";
                b.Recording.Settings.InitialTimeout = 300;
                b.Mouse("down", "Right", new Point(800, 300));
                b.Advance(500);
                b.Mouse("up", "Right", new Point(800, 300));
            });
        }

        private static KeyValuePair<string, InputRecording> Scenario(string name, string description, Action<Builder> script)
        {
            var builder = new Builder(description);
            script(builder);
            return new KeyValuePair<string, InputRecording>(name, builder.Recording);
        }

        /// <summary>Evenly spaced normalized points from a to b (inclusive).</summary>
        private static IEnumerable<PointF> Line(PointF a, PointF b, int points)
        {
            for (int i = 0; i < points; i++)
            {
                float f = points == 1 ? 0 : (float)i / (points - 1);
                yield return new PointF(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f);
            }
        }

        /// <summary>Zips per-finger paths into frames.</summary>
        private static List<PointF[]> Parallel(params IEnumerable<PointF>[] fingers)
        {
            var paths = fingers.Select(f => f.ToArray()).ToArray();
            return Enumerable.Range(0, paths[0].Length).Select(i => paths.Select(p => p[i]).ToArray()).ToList();
        }

        internal sealed class Builder
        {
            private const int FrameInterval = 8;
            private long _t;

            public Builder(string description)
            {
                Recording = new InputRecording
                {
                    Source = "synthetic",
                    Description = description,
                    Environment = new RecordedEnvironment
                    {
                        Orientation = 0,
                        PointerSize = 8,
                        Screens = { new RecordedScreen { DeviceName = @"\\.\DISPLAY1", Primary = true, X = 0, Y = 0, Width = 1920, Height = 1080 } },
                    },
                };
            }

            public InputRecording Recording { get; }

            public Point Cursor { get; set; } = new Point(960, 540);

            public void Advance(int milliseconds) => _t += milliseconds;

            public int Touchpad(string name = TouchpadName) => AddDevice(0x05, name, TouchpadPp.Value);
            public int TouchScreen(string name = TouchScreenName) => AddDevice(0x04, name, TouchScreenPp.Value);
            public int Pen(string name = PenName) => AddDevice(0x02, name, PenPp.Value);

            private int AddDevice(int usage, string name, byte[] preparsed)
            {
                int id = Recording.Devices.Count + 1;
                Recording.Devices.Add(new RecordedDevice
                {
                    Id = id,
                    Name = name,
                    UsagePage = DigitizerPage,
                    Usage = usage,
                    VendorId = 0x04F3,
                    ProductId = 0x2B7C,
                    VersionNumber = 0x0100,
                    PreparsedData = Convert.ToBase64String(preparsed),
                });
                return id;
            }

            /// <summary>One finger: contact down along the path, then a release report.</summary>
            public void TouchpadStroke(int device, IEnumerable<PointF> path)
            {
                TouchpadFrames(device, path.Select(p => new[] { p }).ToList());
            }

            /// <summary>All fingers down for every frame, then one frame releasing all of them (PTP parallel mode).</summary>
            public void TouchpadFrames(int device, List<PointF[]> frames)
            {
                var pp = TouchpadPp.Value;
                foreach (var frame in frames.Concat(new[] { (PointF[])null }))
                {
                    var fingers = frame ?? frames[frames.Count - 1];
                    bool tip = frame != null;
                    var report = HidReport.Create(pp, HidDescriptors.TouchpadReportId);
                    report.SetValue(DigitizerPage, 0, ContactCount, fingers.Length);
                    for (int i = 0; i < fingers.Length; i++)
                    {
                        short link = (short)(i + 1);
                        report.SetValue(DigitizerPage, link, ContactId, i);
                        report.SetValue(GenericDesktopPage, link, X, Scale(fingers[i].X, HidDescriptors.TouchpadLogicalMaxX));
                        report.SetValue(GenericDesktopPage, link, Y, Scale(fingers[i].Y, HidDescriptors.TouchpadLogicalMaxY));
                        if (tip)
                            report.SetButtons(DigitizerPage, link, TipSwitch);
                    }
                    Hid(device, report.ToArray());
                }
            }

            /// <summary>
            /// Touch screen frames with <see cref="HidDescriptors.TouchScreenContactsPerReport"/> contacts per report;
            /// larger frames span several reports and only the first carries the contact count (hybrid mode).
            /// The last frame is repeated with tips released.
            /// </summary>
            public void TouchScreenFrames(int device, List<PointF[]> frames, int reportsPerMessage)
            {
                var pp = TouchScreenPp.Value;
                int perReport = HidDescriptors.TouchScreenContactsPerReport;
                foreach (var frame in frames.Concat(new[] { (PointF[])null }))
                {
                    var contacts = frame ?? frames[frames.Count - 1];
                    bool tip = frame != null;
                    var reports = new List<byte[]>();
                    for (int first = 0; first < contacts.Length; first += perReport)
                    {
                        var report = HidReport.Create(pp, HidDescriptors.TouchScreenReportId);
                        report.SetValue(DigitizerPage, 0, ContactCount, first == 0 ? contacts.Length : 0);
                        for (int slot = 0; slot < perReport && first + slot < contacts.Length; slot++)
                        {
                            short link = (short)(slot + 1);
                            var p = contacts[first + slot];
                            report.SetValue(DigitizerPage, link, ContactId, first + slot + 10);
                            report.SetValue(GenericDesktopPage, link, X, Scale(p.X, HidDescriptors.TouchScreenLogicalMaxX));
                            report.SetValue(GenericDesktopPage, link, Y, Scale(p.Y, HidDescriptors.TouchScreenLogicalMaxY));
                            if (tip)
                                report.SetButtons(DigitizerPage, link, TipSwitch);
                        }
                        reports.Add(report.ToArray());
                    }
                    for (int i = 0; i < reports.Count; i += reportsPerMessage)
                        Hid(device, reports.Skip(i).Take(reportsPerMessage).ToArray());
                }
            }

            public void PenSample(int device, PointF p, bool inRange, bool tip = false, bool barrel = false, bool invert = false, bool eraser = false)
            {
                var report = HidReport.Create(PenPp.Value, HidDescriptors.PenReportId);
                report.SetValue(GenericDesktopPage, 0, X, Scale(p.X, HidDescriptors.PenLogicalMaxX));
                report.SetValue(GenericDesktopPage, 0, Y, Scale(p.Y, HidDescriptors.PenLogicalMaxY));
                var buttons = new List<ushort>();
                if (inRange) buttons.Add(InRange);
                if (tip) buttons.Add(TipSwitch);
                if (barrel) buttons.Add(BarrelSwitch);
                if (invert) buttons.Add(Invert);
                if (eraser) buttons.Add(Eraser);
                if (buttons.Count != 0)
                    report.SetButtons(DigitizerPage, 0, buttons.ToArray());
                Hid(device, report.ToArray());
            }

            public void Mouse(string message, string button, Point p)
            {
                Cursor = p;
                Add(new RecordedEvent { Type = RecordedEventTypes.Mouse, MouseMessage = message, Button = button, X = p.X, Y = p.Y });
            }

            public void MouseDrag(string button, Point from, Point to, int points)
            {
                Mouse("down", button, from);
                for (int i = 1; i < points; i++)
                {
                    var p = new Point(from.X + (to.X - from.X) * i / (points - 1), from.Y + (to.Y - from.Y) * i / (points - 1));
                    Mouse("move", null, p);
                }
                Mouse("up", button, to);
            }

            private void Hid(int device, params byte[][] reports)
            {
                Add(new RecordedEvent
                {
                    Type = RecordedEventTypes.Hid,
                    Device = device,
                    SizeHid = reports[0].Length,
                    Count = reports.Length,
                    Data = Convert.ToBase64String(reports.SelectMany(r => r).ToArray()),
                });
            }

            private void Add(RecordedEvent e)
            {
                e.T = _t;
                e.TickCount = 1000000 + (int)_t;
                e.CursorX = Cursor.X;
                e.CursorY = Cursor.Y;
                Recording.Events.Add(e);
                _t += FrameInterval;
            }

            private static int Scale(float fraction, int logicalMax) => (int)Math.Round(fraction * logicalMax);
        }
    }
}
