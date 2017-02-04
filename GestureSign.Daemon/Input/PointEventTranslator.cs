using System;
using System.Collections.Generic;
using System.Linq;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Daemon.Input
{
    public class PointEventTranslator
    {
        private int _lastPointsCount;

        public LowLevelMouseMessage LastDownMessage;
        public LowLevelMouseMessage LastUpMessage;

        internal PointEventTranslator(InputProvider inputProvider)
        {
            inputProvider.TouchInputProcessor.PointsIntercepted += TranslateTouchEvent;
            inputProvider.LowLevelMouseHook.MouseDown += LowLevelMouseHook_MouseDown;
            inputProvider.LowLevelMouseHook.MouseMove += LowLevelMouseHook_MouseMove;
            inputProvider.LowLevelMouseHook.MouseUp += LowLevelMouseHook_MouseUp;
        }

        #region Custom Events

        public event EventHandler<RawPointsDataMessageEventArgs> PointDown;

        protected virtual void OnPointDown(RawPointsDataMessageEventArgs args)
        {
            if (PointDown != null) PointDown(this, args);
        }

        public event EventHandler<RawPointsDataMessageEventArgs> PointUp;

        protected virtual void OnPointUp(RawPointsDataMessageEventArgs args)
        {
            if (PointUp != null) PointUp(this, args);
        }

        public event EventHandler<RawPointsDataMessageEventArgs> PointMove;

        protected virtual void OnPointMove(RawPointsDataMessageEventArgs args)
        {
            if (PointMove != null) PointMove(this, args);
        }

        #endregion

        #region Private Methods

        private void LowLevelMouseHook_MouseUp(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            if ((MouseActions)mouseMessage.Button == AppConfig.DrawingButton)
            {
                LastUpMessage = mouseMessage;
                var args = new RawPointsDataMessageEventArgs(new List<RawData>(new[] { new RawData(true, 1, mouseMessage.Point) }));
                OnPointUp(args);
                PointCapture.Instance.MouseCaptured = false;
                handled = args.Handled;
            }
        }

        private void LowLevelMouseHook_MouseMove(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            var args = new RawPointsDataMessageEventArgs(new List<RawData>(new[] { new RawData(true, 1, mouseMessage.Point) }));
            OnPointMove(args);
        }

        private void LowLevelMouseHook_MouseDown(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            if ((MouseActions)mouseMessage.Button == AppConfig.DrawingButton)
            {
                LastDownMessage = mouseMessage;
                PointCapture.Instance.MouseCaptured = true;
                var args = new RawPointsDataMessageEventArgs(new List<RawData>(new[] { new RawData(true, 1, mouseMessage.Point) }));
                OnPointDown(args);
                handled = args.Handled;
            }
        }

        private void TranslateTouchEvent(object sender, RawPointsDataMessageEventArgs e)
        {
            int releaseCount = e.RawData.Count(rtd => !rtd.Tip);
            if (releaseCount != 0)
            {
                if (e.RawData.Count <= _lastPointsCount)
                {
                    OnPointUp(e);
                    _lastPointsCount = _lastPointsCount - releaseCount;
                }
                return;
            }

            if (e.RawData.Count > _lastPointsCount)
            {
                if (PointCapture.Instance.InputPoints.Any(p => p.Count > 10))
                {
                    OnPointMove(e);
                    return;
                }
                _lastPointsCount = e.RawData.Count;
                OnPointDown(e);
            }
            else if (e.RawData.Count == _lastPointsCount)
            {
                OnPointMove(e);
            }
        }

        #endregion
    }
}
