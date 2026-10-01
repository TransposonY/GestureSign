using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using GestureSign.Common;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using ManagedWinapi.Hooks;
using WindowsInput;

namespace GestureSign.Daemon.Input
{
    /// <summary>One-shot timer whose callback runs on the capture (UI) context.</summary>
    internal interface ICaptureTimer : IDisposable
    {
        /// <summary>(Re)starts the timer; <see cref="Timeout.Infinite"/> stops it.</summary>
        void Change(int dueTime);
    }

    /// <summary>Side effects and machine state used by <see cref="PointCapture"/>.</summary>
    internal interface ICaptureHost
    {
        void SetHighPriority(bool high);

        Point CursorPosition { get; }

        void SimulateMouseClick(MouseActions button);

        void SimulateMouseButtonDown(MouseActions button);

        /// <summary>Runs work off the capture thread (click reinjection).</summary>
        Task RunInBackground(Action action);

        /// <summary>Sends a trained gesture to the ControlPanel; false when it is unreachable.</summary>
        bool SendTrainingGesture(Point[][][] patterns);

        /// <summary>Gesture name resolved by the gesture manager during BeforePointsCaptured.</summary>
        string RecognizedGestureName { get; }

        /// <summary>Must be called on the UI thread: the callback is posted to the caller's synchronization context.</summary>
        ICaptureTimer CreateTimer(Action callback);
    }

    internal sealed class SystemCaptureHost : ICaptureHost
    {
        public void SetHighPriority(bool high)
        {
            Process.GetCurrentProcess().PriorityClass = high ? ProcessPriorityClass.High : ProcessPriorityClass.Normal;
        }

        public Point CursorPosition => System.Windows.Forms.Cursor.Position;

        public void SimulateMouseClick(MouseActions button)
        {
            InputSimulator simulator = new InputSimulator();
            switch (button)
            {
                case MouseActions.Left:
                    simulator.Mouse.LeftButtonClick();
                    break;
                case MouseActions.Middle:
                    simulator.Mouse.MiddleButtonClick();
                    break;
                case MouseActions.Right:
                    simulator.Mouse.RightButtonClick();
                    break;
                case MouseActions.XButton1:
                    simulator.Mouse.XButtonClick(1);
                    break;
                case MouseActions.XButton2:
                    simulator.Mouse.XButtonClick(2);
                    break;
            }
        }

        public void SimulateMouseButtonDown(MouseActions button)
        {
            InputSimulator simulator = new InputSimulator();
            switch (button)
            {
                case MouseActions.Left:
                    simulator.Mouse.LeftButtonDown();
                    break;
                case MouseActions.Middle:
                    simulator.Mouse.MiddleButtonDown();
                    break;
                case MouseActions.Right:
                    simulator.Mouse.RightButtonDown();
                    break;
                case MouseActions.XButton1:
                    simulator.Mouse.XButtonDown(1);
                    break;
                case MouseActions.XButton2:
                    simulator.Mouse.XButtonDown(2);
                    break;
            }
        }

        public Task RunInBackground(Action action)
        {
            return Task.Factory.StartNew(action);
        }

        public bool SendTrainingGesture(Point[][][] patterns)
        {
            return NamedPipe.SendMessageAsync(IpcCommands.GotGesture, Constants.ControlPanel, patterns, false).Result;
        }

        public string RecognizedGestureName => GestureManager.Instance.GestureName;

        public ICaptureTimer CreateTimer(Action callback)
        {
            return new ContextTimer(SynchronizationContext.Current, callback);
        }

        private sealed class ContextTimer : ICaptureTimer
        {
            private readonly System.Threading.Timer _timer;

            public ContextTimer(SynchronizationContext context, Action callback)
            {
                _timer = new System.Threading.Timer(o => context.Post(s => callback(), null), null, Timeout.Infinite, Timeout.Infinite);
            }

            public void Change(int dueTime)
            {
                _timer.Change(dueTime, Timeout.Infinite);
            }

            public void Dispose()
            {
                _timer.Dispose();
            }
        }
    }
}
