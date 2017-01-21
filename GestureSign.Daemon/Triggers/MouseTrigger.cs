using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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

        private void MouseHook_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!IsCapturing()) return;

            MouseActions wheelAction = e.Delta > 0 ? MouseActions.WheelForward :
                e.Delta < 0 ? MouseActions.WheelBackward : MouseActions.None;
            if (_actionMap.ContainsKey(wheelAction))
            {
                OnTriggerFired(new TriggerFiredEventArgs(_actionMap[wheelAction], e.Location));
            }
        }

        private void MouseHook_MouseUp(object sender, MouseEventArgs e)
        {
            if (!IsCapturing()) return;

            if (_actionMap.ContainsKey((MouseActions)e.Button))
            {
                OnTriggerFired(new TriggerFiredEventArgs(_actionMap[(MouseActions)e.Button], e.Location));
            }
        }

        private bool IsCapturing()
        {
            return PointCapture.Instance.State == CaptureState.Capturing &&
                   PointCapture.Instance.InputPoints.FirstOrDefault()?.Count == 1;
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
