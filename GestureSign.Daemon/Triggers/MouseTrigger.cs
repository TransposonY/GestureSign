using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using ManagedWinapi.Hooks;
using System.Collections.Generic;

namespace GestureSign.Daemon.Triggers
{
    class MouseTrigger : Trigger
    {
        private Dictionary<MouseActions, List<IAction>> _actionMap = new Dictionary<MouseActions, List<IAction>>();

        public MouseTrigger()
        {
            PointCapture.Instance.MouseHook.MouseDown += MouseHook_MouseDown;
            PointCapture.Instance.MouseHook.MouseUp += MouseHook_MouseUp;
            PointCapture.Instance.MouseHook.MouseWheel += MouseHook_MouseWheel;
        }

        private void MouseHook_MouseWheel(LowLevelMouseMessage e, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
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

        private void MouseHook_MouseDown(LowLevelMouseMessage evt, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
                if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                    if (_actionMap.ContainsKey((MouseActions)evt.Button))
                    {
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
        }

        private void MouseHook_MouseUp(LowLevelMouseMessage e, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
                if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                    if (_actionMap.ContainsKey((MouseActions)e.Button))
                    {
                        OnTriggerFired(new TriggerFiredEventArgs(_actionMap[(MouseActions)e.Button], e.Point));
                        PointCapture.Instance.State = CaptureState.TriggerFired;
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
        }

        public override bool LoadConfiguration(List<IAction> actions)
        {
            _actionMap.Clear();
            if (actions == null || actions.Count == 0) return false;

            foreach (var action in actions)
            {
                if (action.MouseHotkey != MouseActions.None)
                {
                    if (_actionMap.ContainsKey(action.MouseHotkey))
                    {
                        var mouseActionList = _actionMap[action.MouseHotkey];
                        if (!mouseActionList.Contains(action))
                            mouseActionList.Add(action);
                    }
                    else
                    {
                        _actionMap.Add(action.MouseHotkey, new List<IAction>(new[] { action }));
                    }
                }
            }
            return true;
        }


    }
}
