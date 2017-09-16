using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Native;
using GestureSign.PointPatterns;
using ManagedWinapi.Hooks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GestureSign.Daemon.Triggers
{
    class ContinuousGestureTrigger : Trigger
    {
        private List<KeyValuePair<ContinuousGesture, List<IAction>>> _actionMap = new List<KeyValuePair<ContinuousGesture, List<IAction>>>();

        private Point _startPoint;
        private float _motionThreshold;
        private Stopwatch _stopwatch = new Stopwatch();
        private List<Point> _lastPoints;

        public ContinuousGestureTrigger()
        {
            _motionThreshold = 20f * NativeMethods.GetScreenDpi() / 96f;

            PointCapture.Instance.PointCaptured += PointCapture_PointCaptured;
            PointCapture.Instance.CaptureEnded += PointCapture_CaptureEnded;
        }

        public override bool LoadConfiguration(List<IAction> actions)
        {
            _actionMap.Clear();
            if (actions == null || actions.Count == 0) return false;

            foreach (var action in actions)
            {
                if (action.ContinuousGesture != null)
                {
                    var index = _actionMap.FindIndex(kvp => action.ContinuousGesture.Equals(kvp.Key));
                    if (index >= 0)
                    {
                        var actionConfig = _actionMap[index].Value;
                        if (!actionConfig.Contains(action))
                            actionConfig.Add(action);
                    }
                    else
                    {
                        _actionMap.Add(new KeyValuePair<ContinuousGesture, List<IAction>>(action.ContinuousGesture, new List<IAction>(new[] { action })));
                    }
                }
            }
            return true;
        }

        private void PointCapture_CaptureEnded(object sender, System.EventArgs e)
        {
            _stopwatch.Stop();
            _lastPoints = null;
        }

        private void PointCapture_PointCaptured(object sender, PointsCapturedEventArgs e)
        {
            if (PointCapture.Instance.State != CaptureState.Capturing || _actionMap.Count == 0 || e.Points.Count < 2)
                return;
            if (_lastPoints == null || _lastPoints.Count != e.FirstCapturedPoints.Count)
            {
                _startPoint = e.FirstCapturedPoints[0];
                _lastPoints = e.FirstCapturedPoints;
                _stopwatch.Restart();
                return;
            }

            int deltaX = 0, deltaY = 0;
            for (int i = 0; i < _lastPoints.Count; i++)
            {
                deltaX += e.FirstCapturedPoints[i].X - _lastPoints[i].X;
                deltaY += e.FirstCapturedPoints[i].Y - _lastPoints[i].Y;
            }
            deltaX /= _lastPoints.Count;
            deltaY /= _lastPoints.Count;
            int deltaXAbs = Math.Abs(deltaX);
            int deltaYAbs = Math.Abs(deltaY);
            bool isHorizontal = deltaXAbs > deltaYAbs;
            if (isHorizontal)
            {
                var rate = GetRateOfFire(deltaXAbs);
                if (rate >= 1)
                {
                    for (int i = 1; i < rate; i++)
                    {
                        OnGesturerRecognized(_lastPoints.Count, deltaX > 0 ? Gestures.Right : Gestures.Left);
                    }
                    _stopwatch.Restart();
                    _lastPoints = e.FirstCapturedPoints;
                }
            }
            else
            {
                var rate = GetRateOfFire(deltaYAbs);
                if (rate >= 1)
                {
                    for (int i = 1; i < rate; i++)
                    {
                        OnGesturerRecognized(_lastPoints.Count, deltaY > 0 ? Gestures.Down : Gestures.Up);
                    }
                    _stopwatch.Restart();
                    _lastPoints = e.FirstCapturedPoints;
                }
            }
        }

        private void OnGesturerRecognized(int contactCount, Gestures gesture)
        {
            int index = _actionMap.FindIndex(cg => cg.Key.ContactCount == contactCount && cg.Key.Gesture == gesture);
            if (index >= 0)
                OnTriggerFired(new TriggerFiredEventArgs(_actionMap[index].Value, _startPoint));
        }

        private double GetRateOfFire(int distance)
        {
            var deltaTime = _stopwatch.ElapsedMilliseconds;
            if (deltaTime < 2)
                return 0;

            var velocity = distance / (double)deltaTime;
            if (velocity < 3)
            {
                return distance / _motionThreshold;
            }
            else
            {
                if (velocity > 16)
                    velocity = 16;
                return distance / ((0.0039 * velocity * velocity - 0.105 * velocity + 1.195) * _motionThreshold);
            }
        }
    }
}
