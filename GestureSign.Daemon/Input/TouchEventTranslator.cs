using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using GestureSign.Common.Input;

namespace GestureSign.Daemon.Input
{
    public class TouchEventTranslator
    {
        #region Public Properties


        #endregion

        #region Custom Events

        public event EventHandler<RawPointsDataMessageEventArgs> TouchDown;

        protected virtual void OnTouchDown(RawPointsDataMessageEventArgs args)
        {
            if (TouchDown != null) TouchDown(this, args);
        }

        public event EventHandler<RawPointsDataMessageEventArgs> TouchUp;

        protected virtual void OnTouchUp(RawPointsDataMessageEventArgs args)
        {
            if (TouchUp != null) TouchUp(this, args);
        }

        public event EventHandler<RawPointsDataMessageEventArgs> TouchMove;

        protected virtual void OnTouchMove(RawPointsDataMessageEventArgs args)
        {
            if (TouchMove != null) TouchMove(this, args);
        }

        #endregion

        #region Public Methods

        int _lastPointsCount;

        public void TranslateTouchEvent(object sender, RawPointsDataMessageEventArgs e)
        {
            if (e.RawTouchsData.Any(rtd => !rtd.Tip))
            {
                if (e.RawTouchsData.Count <= _lastPointsCount)
                {
                    OnTouchUp(e);
                    _lastPointsCount = 0;
                }
                return;
            }
            if (e.RawTouchsData.Count > _lastPointsCount)
            {
                if (TouchCapture.Instance.InputPoints.Any(p => p.Count > 10))
                {
                    OnTouchMove(e);
                    return;
                }
                _lastPointsCount = e.RawTouchsData.Count;
                OnTouchDown(e);
            }
            else if (e.RawTouchsData.Count == _lastPointsCount)
            {
                OnTouchMove(e);
            }
        }

        #endregion
    }
}
