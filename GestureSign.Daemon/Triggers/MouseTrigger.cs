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
                    var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == wheelAction);
                    if (actions != null && actions.Count != 0)
                    {
                        OnTriggerFired(new TriggerFiredEventArgs(actions, e.Point));
                        PointCapture.Instance.State = CaptureState.TriggerFired;
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
                }
        }

        private void MouseHook_MouseDown(LowLevelMouseMessage evt, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
                if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                {
                    var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == (MouseActions)evt.Button);
                    if (actions != null && actions.Count != 0)
                    {
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
                }
        }

        private void MouseHook_MouseUp(LowLevelMouseMessage e, ref bool handled)
        {
            if (PointCapture.Instance.SourceDevice == Devices.Mouse)
                if (PointCapture.Instance.State == CaptureState.CapturingInvalid || PointCapture.Instance.State == CaptureState.TriggerFired)
                {
                    var actions = ApplicationManager.Instance.GetRecognizedDefinedAction(a => a.MouseHotkey == (MouseActions)e.Button);
                    if (actions != null && actions.Count != 0)
                    {
                        OnTriggerFired(new TriggerFiredEventArgs(actions, e.Point));
                        PointCapture.Instance.State = CaptureState.TriggerFired;
                        handled = PointCapture.Instance.Mode != CaptureMode.UserDisabled;
                    }
                }
        }
    }
}
