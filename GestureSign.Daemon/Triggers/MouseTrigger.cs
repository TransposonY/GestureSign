using System.Collections.Generic;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Daemon.Triggers
{
    class MouseTrigger : Trigger
    {
        private Dictionary<MouseActions, List<string>> _actionMap = new Dictionary<MouseActions, List<string>>();

        public MouseTrigger()
        {
            PointCapture.Instance.MouseHook.MouseUp += MouseHook_MouseUp;
            PointCapture.Instance.MouseHook.MouseWheel += MouseHook_MouseWheel;
        }

        private void MouseHook_MouseWheel(LowLevelMouseMessage e, ref bool handled)
        {
            if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
            {
                MouseActions wheelAction = e.MouseData > 0 ? MouseActions.WheelForward : e.MouseData < 0 ? MouseActions.WheelBackward : MouseActions.None;
                if (_actionMap.ContainsKey(wheelAction))
                {
                    OnTriggerFired(new TriggerFiredEventArgs(_actionMap[wheelAction], e.Point));
                    PointCapture.Instance.State = CaptureState.TriggerFired;
                    handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                }
            }
        }

        private void MouseHook_MouseUp(LowLevelMouseMessage e, ref bool handled)
        {
            if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                if (_actionMap.ContainsKey((MouseActions)e.Button))
                {
                    OnTriggerFired(new TriggerFiredEventArgs(_actionMap[(MouseActions)e.Button], e.Point));
                    PointCapture.Instance.State = CaptureState.TriggerFired;
                    handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                }
        }

        public override bool LoadConfiguration(IGesture[] gestures)
        {
            _actionMap.Clear();
            if (gestures == null || gestures.Length == 0) return false;

            foreach (var g in gestures)
            {
                var action = ((Gesture)g).MouseAction;

                if (action != MouseActions.None)
                {
                    if (_actionMap.ContainsKey(action))
                    {
                        var gestureNameList = _actionMap[action] ?? new List<string>();
                        if (!gestureNameList.Contains(g.Name))
                            gestureNameList.Add(g.Name);
                    }
                    else
                    {
                        _actionMap.Add(action, new List<string>(new[] { g.Name }));
                    }
                }
            }
            return true;
        }


    }
}
