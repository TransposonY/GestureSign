using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using GestureSign.Common.Input;

namespace GestureSignDaemon.Input
{
    public class TouchEventTranslator
    {
        #region Public Properties


        #endregion

        #region Custom Events

        public event EventHandler<PointEventArgs> TouchDown;

        protected virtual void OnTouchDown(PointEventArgs args)
        {
            if (TouchDown != null) TouchDown(this, args);
        }

        public event EventHandler<PointEventArgs> TouchUp;

        protected virtual void OnTouchUp(PointEventArgs args)
        {
            if (TouchUp != null) TouchUp(this, args);
        }

        public event EventHandler<PointEventArgs> TouchMove;

        protected virtual void OnTouchMove(PointEventArgs args)
        {
            if (TouchMove != null) TouchMove(this, args);
        }

        #endregion

        #region Public Methods

        int lastPointsCount = 0;

        public void TranslateTouchEvent(object sender, RawPointsDataMessageEventArgs e)
        {
            var pointEventArgs = new PointEventArgs(e.RawTouchsData.Select(r => (new KeyValuePair<int, Point>(r.ContactIdentifier, r.RawPoints))));
            foreach (RawTouchData rtd in e.RawTouchsData)
            {
                if (!rtd.Tip)
                {
                    if (e.RawTouchsData.Length <= lastPointsCount)
                    {
                        OnTouchUp(pointEventArgs);
                        lastPointsCount = 0;
                    }
                    return;
                }
            }
            if (e.RawTouchsData.Length > lastPointsCount)
            {
                if (TouchCapture.Instance.InputPoints.Any(p => p.Count > 10))
                {
                    OnTouchMove(pointEventArgs);
                    return;
                }
                lastPointsCount = e.RawTouchsData.Length;
                OnTouchDown(pointEventArgs);
            }
            else if (e.RawTouchsData.Length == lastPointsCount)
            {
                OnTouchMove(pointEventArgs);
            }

        }
        
        #endregion
    }
}
