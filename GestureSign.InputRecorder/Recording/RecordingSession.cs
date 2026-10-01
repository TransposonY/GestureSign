using GestureSign.Common.Configuration;
using ManagedWinapi.Hooks;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>
    /// Accumulates one recording. All callbacks arrive on the thread owning the raw input window and the
    /// low-level mouse hook (the UI thread), so no locking is needed.
    /// </summary>
    internal sealed class RecordingSession
    {
        private const int WM_MOUSEMOVE = 0x200,
            WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202,
            WM_RBUTTONDOWN = 0x204, WM_RBUTTONUP = 0x205,
            WM_MBUTTONDOWN = 0x207, WM_MBUTTONUP = 0x208,
            WM_MOUSEWHEEL = 0x20A,
            WM_XBUTTONDOWN = 0x20B, WM_XBUTTONUP = 0x20C;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Dictionary<IntPtr, RecordedDevice> _devices = new Dictionary<IntPtr, RecordedDevice>();
        private readonly Dictionary<int, int> _hidCounts = new Dictionary<int, int>();

        public InputRecording Recording { get; }

        public bool RecordMouseMoves { get; set; }

        public int MouseEventCount { get; private set; }

        public int DeviceChangeCount { get; private set; }

        /// <summary>WM_INPUT buffers that could not be converted (inconsistent sizes).</summary>
        public int ErrorCount { get; private set; }

        /// <summary>Warnings collected while capturing environment/settings.</summary>
        public List<string> Warnings { get; } = new List<string>();

        public RecordingSession(string description, bool recordMouseMoves)
        {
            RecordMouseMoves = recordMouseMoves;
            Recording = new InputRecording
            {
                Source = "recorded",
                Description = description,
                RecordedAtUtc = DateTime.UtcNow,
                OsVersion = GetOsVersion(),
                Environment = CaptureEnvironment(),
                Settings = CaptureSettings(Warnings),
            };
        }

        public IEnumerable<RecordedDevice> Devices => Recording.Devices;

        public int GetHidCount(int deviceId)
        {
            int count;
            return _hidCounts.TryGetValue(deviceId, out count) ? count : 0;
        }

        /// <summary>Restarts the event clock and <see cref="InputRecording.RecordedAtUtc"/>; for sessions created ahead of their first event.</summary>
        public void RestartClock()
        {
            _stopwatch.Restart();
            Recording.RecordedAtUtc = DateTime.UtcNow;
        }

        public void OnRawInput(byte[] buffer)
        {
            Point cursor = Cursor.Position;
            try
            {
                if (RawInputConverter.GetInputType(buffer) != RawInputConverter.RimTypeHid)
                    return;
                RecordedDevice device = GetOrAddDevice(RawInputConverter.GetDeviceHandle(buffer));
                RecordedEvent e = RawInputConverter.ToHidEvent(buffer, device.Id, _stopwatch.ElapsedMilliseconds,
                    System.Environment.TickCount, cursor.X, cursor.Y);
                Recording.Events.Add(e);
                _hidCounts[device.Id] = GetHidCount(device.Id) + 1;
            }
            catch (InvalidDataException)
            {
                ErrorCount++;
            }
        }

        public void OnDeviceChange(int change, IntPtr hDevice)
        {
            Point cursor = Cursor.Position;
            RecordedDevice known;
            Recording.Events.Add(new RecordedEvent
            {
                T = _stopwatch.ElapsedMilliseconds,
                TickCount = System.Environment.TickCount,
                Type = RecordedEventTypes.DeviceChange,
                CursorX = cursor.X,
                CursorY = cursor.Y,
                Device = _devices.TryGetValue(hDevice, out known) ? known.Id : 0,
            });
            DeviceChangeCount++;
        }

        /// <summary>LowLevelMouseHook.MouseCallback payload. Never marks the event handled.</summary>
        public void OnMouse(int msg, int x, int y, int mouseData)
        {
            string message;
            MouseActions button = MouseActions.None;
            switch (msg)
            {
                case WM_MOUSEMOVE:
                    if (!RecordMouseMoves) return;
                    message = RecordedMouseMessages.Move;
                    break;
                case WM_LBUTTONDOWN: message = RecordedMouseMessages.Down; button = MouseActions.Left; break;
                case WM_LBUTTONUP: message = RecordedMouseMessages.Up; button = MouseActions.Left; break;
                case WM_RBUTTONDOWN: message = RecordedMouseMessages.Down; button = MouseActions.Right; break;
                case WM_RBUTTONUP: message = RecordedMouseMessages.Up; button = MouseActions.Right; break;
                case WM_MBUTTONDOWN: message = RecordedMouseMessages.Down; button = MouseActions.Middle; break;
                case WM_MBUTTONUP: message = RecordedMouseMessages.Up; button = MouseActions.Middle; break;
                case WM_XBUTTONDOWN:
                    message = RecordedMouseMessages.Down;
                    button = HiWord(mouseData) == 1 ? MouseActions.XButton1 : MouseActions.XButton2;
                    break;
                case WM_XBUTTONUP:
                    message = RecordedMouseMessages.Up;
                    button = HiWord(mouseData) == 1 ? MouseActions.XButton1 : MouseActions.XButton2;
                    break;
                case WM_MOUSEWHEEL:
                    message = RecordedMouseMessages.Wheel;
                    button = HiWord(mouseData) > 0 ? MouseActions.WheelForward : MouseActions.WheelBackward;
                    break;
                default:
                    return;
            }

            Point cursor = Cursor.Position;
            Recording.Events.Add(new RecordedEvent
            {
                T = _stopwatch.ElapsedMilliseconds,
                TickCount = System.Environment.TickCount,
                Type = RecordedEventTypes.Mouse,
                CursorX = cursor.X,
                CursorY = cursor.Y,
                MouseMessage = message,
                Button = button == MouseActions.None ? null : button.ToString(),
                X = x,
                Y = y,
                MouseData = mouseData,
            });
            MouseEventCount++;
        }

        private RecordedDevice GetOrAddDevice(IntPtr hDevice)
        {
            RecordedDevice device;
            if (!_devices.TryGetValue(hDevice, out device))
            {
                device = DeviceCatalog.Describe(hDevice, Recording.Devices.Count + 1);
                _devices.Add(hDevice, device);
                Recording.Devices.Add(device);
            }
            return device;
        }

        private static short HiWord(int n)
        {
            return (short)((n >> 16) & 0xFFFF);
        }

        private static RecordedEnvironment CaptureEnvironment()
        {
            var environment = new RecordedEnvironment { PointerSize = IntPtr.Size };
            foreach (Screen screen in Screen.AllScreens)
            {
                environment.Screens.Add(new RecordedScreen
                {
                    DeviceName = screen.DeviceName,
                    Primary = screen.Primary,
                    X = screen.Bounds.X,
                    Y = screen.Bounds.Y,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                });
            }
            switch (SystemInformation.ScreenOrientation)
            {
                case ScreenOrientation.Angle90: environment.Orientation = 90; break;
                case ScreenOrientation.Angle180: environment.Orientation = 180; break;
                case ScreenOrientation.Angle270: environment.Orientation = 270; break;
                default: environment.Orientation = 0; break;
            }
            return environment;
        }

        /// <summary>Reads (never writes) the daemon's current input settings.</summary>
        private static RecordedSettings CaptureSettings(List<string> warnings)
        {
            var settings = new RecordedSettings();
            try
            {
                settings.DrawingButton = AppConfig.DrawingButton.ToString();
                settings.PenGestureButton = (int)AppConfig.PenGestureButton;
                settings.IgnoreTouchInputWhenUsingPen = AppConfig.IgnoreTouchInputWhenUsingPen;
                settings.RegisterTouchPad = AppConfig.RegisterTouchPad;
                settings.RegisterTouchScreen = AppConfig.RegisterTouchScreen;
                settings.MinimumPointDistance = AppConfig.MinimumPointDistance;
                settings.IsOrderByLocation = AppConfig.IsOrderByLocation;
                settings.InitialTimeout = AppConfig.InitialTimeout;
            }
            catch (Exception ex)
            {
                warnings.Add("Could not read GestureSign settings, defaults recorded: " + ex.Message);
            }
            return settings;
        }

        private static string GetOsVersion()
        {
            string version = System.Environment.OSVersion.VersionString;
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key == null) return version;
                    string release = (key.GetValue("DisplayVersion") ?? key.GetValue("ReleaseId")) as string;
                    object ubr = key.GetValue("UBR");
                    if (release != null) version += " " + release;
                    if (ubr != null) version += " UBR " + ubr;
                }
            }
            catch (Exception)
            {
                // Release id is informational only.
            }
            return version;
        }
    }
}
