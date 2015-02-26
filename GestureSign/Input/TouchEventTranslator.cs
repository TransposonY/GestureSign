using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using GestureSign.Common.Input;

namespace GestureSign.Input
{
    public class TouchEventTranslator
    {
        #region Public Properties


        #endregion

        #region Custom Events

        public event EventHandler<PointsMessageEventArgs> TouchDown;

        protected virtual void OnTouchDown(PointsMessageEventArgs args)
        {
            if (TouchDown != null) TouchDown(this, args);
        }

        public event EventHandler<PointsMessageEventArgs> TouchUp;

        protected virtual void OnTouchUp(PointsMessageEventArgs args)
        {
            if (TouchUp != null) TouchUp(this, args);
        }

        public event EventHandler<PointsMessageEventArgs> TouchMove;

        protected virtual void OnTouchMove(PointsMessageEventArgs args)
        {
            if (TouchMove != null) TouchMove(this, args);
        }

        #endregion

        #region Public Methods

        int lastPointsCount = 1;

        public void TranslateTouchEvent(object sender, PointsMessageEventArgs e)
        {
            if (e.RawTouchsData.Length < 2 || GestureSign.Configuration.AppConfig.XRatio == 0) return;

            foreach (RawTouchData rtd in e.RawTouchsData)
            {
                if (!rtd.Status)
                {
                    if (e.RawTouchsData.Length <= lastPointsCount)
                    {
                        OnTouchUp(e);
                        lastPointsCount = 1;
                    }
                    return;
                }
            }
            if (e.RawTouchsData.Length > lastPointsCount)
            {
                if (TouchCapture.Instance.InputPoints.Any(p => p.Count > 10))
                {
                    OnTouchMove(e);
                    return;
                }
                lastPointsCount = e.RawTouchsData.Length;
                OnTouchDown(e);
            }
            else if (e.RawTouchsData.Length == lastPointsCount)
            {
                OnTouchMove(e);
            }

        }

        #endregion
    }
}
