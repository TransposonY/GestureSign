using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestureSign.Daemon.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Tests.Support
{
    /// <summary>
    /// Deterministic <see cref="ICaptureHost"/>: records side effects, runs background work inline and
    /// drives timers from a virtual clock (<see cref="Advance"/>).
    /// </summary>
    internal sealed class FakeCaptureHost : ICaptureHost
    {
        private readonly List<ManualTimer> _timers = new List<ManualTimer>();

        public long Now { get; private set; }

        /// <summary>Side effects in order, e.g. "priority:high", "click:Right", "buttonDown:Right", "training:1".</summary>
        public List<string> Effects { get; } = new List<string>();

        /// <summary>Virtual time (<see cref="Now"/>) at which each entry of <see cref="Effects"/> happened.</summary>
        public List<long> EffectTimes { get; } = new List<long>();

        public List<Point[][][]> TrainingGestures { get; } = new List<Point[][][]>();

        public Point CursorPosition { get; set; }

        public bool ControlPanelReachable { get; set; } = true;

        /// <summary>Returned as the recognized gesture; may be set from a BeforePointsCaptured handler.</summary>
        public string RecognizedGestureName { get; set; }

        public void SetHighPriority(bool high)
        {
            AddEffect(high ? "priority:high" : "priority:normal");
        }

        public void SimulateMouseClick(MouseActions button)
        {
            AddEffect("click:" + button);
        }

        public void SimulateMouseButtonDown(MouseActions button)
        {
            AddEffect("buttonDown:" + button);
        }

        private void AddEffect(string effect)
        {
            Effects.Add(effect);
            EffectTimes.Add(Now);
        }

        public Task RunInBackground(Action action)
        {
            try
            {
                action();
                return Task.FromResult(0);
            }
            catch (Exception e)
            {
                var failed = new TaskCompletionSource<int>();
                failed.SetException(e);
                return failed.Task;
            }
        }

        public bool SendTrainingGesture(Point[][][] patterns)
        {
            TrainingGestures.Add(patterns);
            AddEffect("training:" + patterns.Length);
            return ControlPanelReachable;
        }

        public ICaptureTimer CreateTimer(Action callback)
        {
            var timer = new ManualTimer(this, callback);
            _timers.Add(timer);
            return timer;
        }

        /// <summary>Moves the virtual clock forward, firing due timers in due-time order.</summary>
        public void Advance(long milliseconds)
        {
            long target = Now + milliseconds;
            while (true)
            {
                var next = _timers.Where(t => t.DueAt.HasValue && t.DueAt.Value <= target).OrderBy(t => t.DueAt.Value).FirstOrDefault();
                if (next == null) break;
                Now = next.DueAt.Value;
                next.DueAt = null;
                next.Callback();
            }
            Now = target;
        }

        private sealed class ManualTimer : ICaptureTimer
        {
            private readonly FakeCaptureHost _host;

            public ManualTimer(FakeCaptureHost host, Action callback)
            {
                _host = host;
                Callback = callback;
            }

            public Action Callback { get; }

            public long? DueAt { get; set; }

            public void Change(int dueTime)
            {
                DueAt = dueTime == Timeout.Infinite ? (long?)null : _host.Now + dueTime;
            }

            public void Dispose()
            {
                DueAt = null;
            }
        }
    }
}
