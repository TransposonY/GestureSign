using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.InputRecorder.Recording;
using GestureSign.Tests.Support;
using ManagedWinapi.Hooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GestureSign.Tests.Replay
{
    /// <summary>
    /// Replays an <see cref="InputRecording"/> through the daemon's real input pipeline
    /// (RawInputProcessor -> PointEventTranslator -> PointCapture, gesture matching against a gesture set)
    /// and produces a language-neutral transcript: one JSON object per observable step.
    /// </summary>
    internal static class InputReplayer
    {
        public static string Replay(InputRecording recording, List<IGesture> gestures)
        {
            var transcript = new List<JObject>();
            long now = 0;

            var settings = new TestInputSettings
            {
                DrawingButton = (MouseActions)Enum.Parse(typeof(MouseActions), recording.Settings.DrawingButton),
                PenGestureButton = (DeviceStates)recording.Settings.PenGestureButton,
                MinimumPointDistance = recording.Settings.MinimumPointDistance,
                IsOrderByLocation = recording.Settings.IsOrderByLocation,
                InitialTimeout = recording.Settings.InitialTimeout,
            };
            var host = new FakeCaptureHost();
            var capture = new PointCapture(settings, host);

            var environment = new RecordingEnvironment(recording.Environment);
            var processor = new RawInputProcessor(new RecordingDeviceSource(recording.Devices), environment);
            var penSetting = settings.PenGestureButton;
            var penGestureButton = penSetting & (DeviceStates.Invert | DeviceStates.RightClickButton);
            processor.UpdateSettings(recording.Settings.IgnoreTouchInputWhenUsingPen, penGestureButton);
            var registeredUsages = RegisteredUsages(recording.Settings, penSetting, penGestureButton);

            // InputProvider forwards only non-empty frames.
            processor.PointsIntercepted += (o, e) =>
            {
                transcript.Add(Entry(now, "raw", new JObject
                {
                    ["device"] = e.SourceDevice.ToString(),
                    ["contacts"] = new JArray(e.RawData.Select(rd => new JArray(rd.ContactIdentifier, (int)rd.State, rd.RawPoints.X, rd.RawPoints.Y))),
                }));
                if (e.RawData.Count == 0)
                    return;
                capture.Translator.TranslateTouchEvent(o, e);
            };

            // Subscribed after PointCapture, so these observe the capture's decision (Handled, State).
            capture.Translator.PointDown += (o, e) => transcript.Add(Translated(now, "down", e, capture));
            capture.Translator.PointMove += (o, e) => transcript.Add(Translated(now, "move", e, capture));
            capture.Translator.PointUp += (o, e) => transcript.Add(Translated(now, "up", e, capture));

            capture.CaptureStarted += (o, e) => transcript.Add(Entry(now, "capture", new JObject
            {
                ["event"] = "started",
                ["firstPoints"] = Points(e.FirstCapturedPoints),
            }));
            capture.BeforePointsCaptured += (o, e) =>
            {
                List<IGesture> matching;
                host.RecognizedGestureName = GestureManager.GetGestureSetNameMatch(e.Points.Select(s => s.ToArray()).ToArray(), gestures, 0, out matching);
                transcript.Add(Entry(now, "capture", new JObject
                {
                    ["event"] = "completed",
                    ["strokes"] = new JArray(e.Points.Select(Points)),
                    ["firstPoints"] = Points(e.FirstCapturedPoints),
                    ["gesture"] = host.RecognizedGestureName,
                }));
            };

            int effectsSeen = 0;
            Action flushEffects = () =>
            {
                for (; effectsSeen < host.Effects.Count; effectsSeen++)
                    transcript.Add(Entry(host.EffectTimes[effectsSeen], "host", new JObject { ["effect"] = host.Effects[effectsSeen] }));
            };

            foreach (var e in recording.Events.OrderBy(ev => ev.T))
            {
                host.Advance(e.T - now);
                now = e.T;
                flushEffects();

                environment.TickCount = e.TickCount;
                environment.Cursor = new Point(e.CursorX, e.CursorY);
                host.CursorPosition = environment.Cursor;

                switch (e.Type)
                {
                    case RecordedEventTypes.Hid:
                        var device = recording.Devices.Single(d => d.Id == e.Device);
                        if (!registeredUsages.Contains(device.Usage))
                            break;
                        IntPtr buffer = RawInputBuffer.Allocate(RecordingDeviceSource.HandleOf(e.Device), e.SizeHid, e.Count, Convert.FromBase64String(e.Data));
                        try
                        {
                            processor.Process(buffer);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buffer);
                        }
                        break;
                    case RecordedEventTypes.DeviceChange:
                        processor.ResetDevices();
                        break;
                    case RecordedEventTypes.Mouse:
                        ReplayMouse(capture.Translator, e, transcript, now);
                        break;
                    default:
                        throw new InvalidDataException("Unknown event type " + e.Type);
                }
                flushEffects();
            }

            return Format(transcript);
        }

        /// <summary>Mirrors MessageWindow.UpdateRegistration.</summary>
        private static HashSet<int> RegisteredUsages(RecordedSettings s, DeviceStates penSetting, DeviceStates penGestureButton)
        {
            var usages = new HashSet<int>();
            if (s.RegisterTouchScreen) usages.Add(0x04);
            if (s.IgnoreTouchInputWhenUsingPen || penGestureButton != 0 && (penSetting & (DeviceStates.InRange | DeviceStates.Tip)) != 0) usages.Add(0x02);
            if (s.RegisterTouchPad) usages.Add(0x05);
            return usages;
        }

        private static void ReplayMouse(PointEventTranslator translator, RecordedEvent e, List<JObject> transcript, long now)
        {
            bool handled = false;
            switch (e.MouseMessage)
            {
                case RecordedMouseMessages.Move:
                    translator.LowLevelMouseHook_MouseMove(Mouse.Move(e.X, e.Y), ref handled);
                    return;
                case RecordedMouseMessages.Down:
                    translator.LowLevelMouseHook_MouseDown(Mouse.Down(ParseButton(e.Button), e.X, e.Y), ref handled);
                    break;
                case RecordedMouseMessages.Up:
                    translator.LowLevelMouseHook_MouseUp(Mouse.Up(ParseButton(e.Button), e.X, e.Y), ref handled);
                    break;
                case RecordedMouseMessages.Wheel:
                    // The capture pipeline does not consume wheel messages (MouseTrigger does).
                    return;
                default:
                    throw new InvalidDataException("Unknown mouse message " + e.MouseMessage);
            }
            transcript.Add(Entry(now, "hook", new JObject { ["message"] = e.MouseMessage, ["button"] = e.Button, ["handled"] = handled }));
        }

        private static MouseActions ParseButton(string button) => (MouseActions)Enum.Parse(typeof(MouseActions), button);

        private static JObject Translated(long t, string kind, InputPointsEventArgs e, PointCapture capture)
        {
            return Entry(t, "translated", new JObject
            {
                ["event"] = kind,
                ["device"] = e.PointSource.ToString(),
                ["points"] = new JArray(e.InputPointList.Select(p => new JArray(p.ContactIdentifier, p.Point.X, p.Point.Y))),
                ["handled"] = e.Handled,
                ["state"] = capture.State.ToString(),
            });
        }

        private static JArray Points(IEnumerable<Point> points)
        {
            return new JArray((points ?? Enumerable.Empty<Point>()).Select(p => new JArray(p.X, p.Y)));
        }

        private static JObject Entry(long t, string stage, JObject body)
        {
            var entry = new JObject { ["t"] = t, ["stage"] = stage };
            foreach (var property in body.Properties())
                entry.Add(property.Name, property.Value);
            return entry;
        }

        /// <summary>A JSON array with one compact entry per line (stable, diff-friendly).</summary>
        private static string Format(List<JObject> transcript)
        {
            var sb = new StringBuilder("[\n");
            for (int i = 0; i < transcript.Count; i++)
            {
                sb.Append("  ").Append(transcript[i].ToString(Formatting.None));
                sb.Append(i + 1 < transcript.Count ? ",\n" : "\n");
            }
            return sb.Append("]\n").ToString();
        }
    }
}
