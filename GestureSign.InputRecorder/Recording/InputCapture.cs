using ManagedWinapi.Hooks;
using System;

namespace GestureSign.InputRecorder.Recording
{
    /// <summary>
    /// Owns the raw input window and the low-level mouse hook and forwards their events to the active
    /// <see cref="RecordingSession"/>. Must be created on a thread that pumps messages.
    /// </summary>
    internal sealed class InputCapture : IDisposable
    {
        private readonly RawInputWindow _window;
        private readonly LowLevelMouseHook _mouseHook;

        public RecordingSession Session { get; private set; }

        /// <summary>
        /// Every WM_INPUT buffer, raised before it is forwarded to <see cref="Session"/>, so a handler may start
        /// a session that then receives this very buffer.
        /// </summary>
        public event Action<byte[]> RawInput;

        /// <summary>WM_INPUT_DEVICE_CHANGE (wParam, hDevice), raised before it is forwarded to <see cref="Session"/>.</summary>
        public event Action<int, IntPtr> DeviceChanged;

        /// <summary>Low-level mouse hook (message, x, y), raised before it is forwarded to <see cref="Session"/>.</summary>
        public event Action<int, int, int> Mouse;

        public InputCapture()
        {
            _window = new RawInputWindow();
            _window.RawInput += buffer =>
            {
                RawInput?.Invoke(buffer);
                Session?.OnRawInput(buffer);
            };
            _window.DeviceChanged += (change, hDevice) =>
            {
                DeviceChanged?.Invoke(change, hDevice);
                Session?.OnDeviceChange(change, hDevice);
            };

            // The recorder must be transparent: the callback never sets handled.
            _mouseHook = new LowLevelMouseHook(OnMouse);
        }

        private void OnMouse(int msg, ManagedWinapi.Windows.POINT pt, int mouseData, int flags, int time, IntPtr dwExtraInfo, ref bool handled)
        {
            Mouse?.Invoke(msg, pt.X, pt.Y);
            Session?.OnMouse(msg, pt.X, pt.Y, mouseData);
        }

        public RecordingSession Start(string description, bool recordMouseMoves)
        {
            return Start(new RecordingSession(description, recordMouseMoves));
        }

        /// <summary>
        /// Starts capturing into a session prepared in advance (environment and settings are read in its
        /// constructor); its clock and timestamp restart now.
        /// </summary>
        public RecordingSession Start(RecordingSession session)
        {
            session.RestartClock();
            Session = session;
            return session;
        }

        public RecordingSession Stop()
        {
            RecordingSession session = Session;
            Session = null;
            return session;
        }

        public void Dispose()
        {
            Session = null;
            _mouseHook.Dispose();
            _window.Dispose();
        }
    }
}
