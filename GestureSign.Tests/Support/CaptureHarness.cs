using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Tests.Support
{
    /// <summary>A <see cref="PointCapture"/> wired to fakes, with helpers to feed mouse/touch/pen input.</summary>
    internal sealed class CaptureHarness
    {
        public CaptureHarness(TestInputSettings settings = null)
        {
            Settings = settings ?? new TestInputSettings();
            Host = new FakeCaptureHost();
            Capture = new PointCapture(Settings, Host);
            Capture.BeforePointsCaptured += (o, e) => Completed.Add(e.Points.Select(s => s.ToList()).ToList());
            Capture.GestureRecognized += (o, e) => Recognized.Add(e);
            Capture.CaptureStarted += (o, e) => Started.Add(e);
        }

        public TestInputSettings Settings { get; }
        public FakeCaptureHost Host { get; }
        public PointCapture Capture { get; }
        public PointEventTranslator Translator => Capture.Translator;

        /// <summary>Strokes delivered to BeforePointsCaptured, one entry per finished gesture.</summary>
        public List<List<List<Point>>> Completed { get; } = new List<List<List<Point>>>();

        public List<RecognitionEventArgs> Recognized { get; } = new List<RecognitionEventArgs>();

        public List<PointsCapturedEventArgs> Started { get; } = new List<PointsCapturedEventArgs>();

        /// <returns>Whether the hook would swallow the message.</returns>
        public bool MouseDown(MouseActions button, int x, int y)
        {
            bool handled = false;
            Translator.LowLevelMouseHook_MouseDown(Mouse.Down(button, x, y), ref handled);
            return handled;
        }

        public bool MouseMove(int x, int y)
        {
            bool handled = false;
            Translator.LowLevelMouseHook_MouseMove(Mouse.Move(x, y), ref handled);
            return handled;
        }

        public bool MouseUp(MouseActions button, int x, int y)
        {
            bool handled = false;
            Translator.LowLevelMouseHook_MouseUp(Mouse.Up(button, x, y), ref handled);
            return handled;
        }

        public void MouseDrag(MouseActions button, params Point[] path)
        {
            MouseDown(button, path[0].X, path[0].Y);
            foreach (var p in path.Skip(1))
                MouseMove(p.X, p.Y);
            MouseUp(button, path[path.Length - 1].X, path[path.Length - 1].Y);
        }

        /// <summary>Delivers one decoded digitizer frame, as RawInputProcessor would.</summary>
        public void Frame(Devices device, params RawData[] contacts)
        {
            Translator.TranslateTouchEvent(this, new RawPointsDataMessageEventArgs(contacts.ToList(), device));
        }

        public static RawData Tip(int id, int x, int y) => new RawData(DeviceStates.Tip, id, new Point(x, y));

        public static RawData Lifted(int id, int x, int y) => new RawData(DeviceStates.None, id, new Point(x, y));

        public static RawData Pen(DeviceStates state, int x, int y) => new RawData(state, 0, new Point(x, y));
    }
}
