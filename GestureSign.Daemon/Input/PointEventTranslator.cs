using System;
using System.Linq;
using GestureSign.Common.Input;

namespace GestureSign.Daemon.Input
{
    public class PointEventTranslator
    {
        #region Public Properties


        #endregion

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

        #region Public Methods

        int _lastPointsCount;

        public void TranslateTouchEvent(object sender, RawPointsDataMessageEventArgs e)
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
