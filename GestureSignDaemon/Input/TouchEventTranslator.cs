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

        public void TranslatePointerMessage(object sender, PointerMessageEventArgs e)
        {
            var pointEventArgs = new PointEventArgs(e.PointerInfo.Select(
                pointer => (new KeyValuePair<int, Point>(pointer.PointerID, new Point(pointer.PtPixelLocation.X, pointer.PtPixelLocation.Y)))));
            foreach (POINTER_INFO pointer in e.PointerInfo)
            {
                if (pointer.PointerFlags.HasFlag(POINTER_FLAGS.UP))
                {
                    OnTouchUp(pointEventArgs);
                    return;
                }
                if (pointer.PointerFlags.HasFlag(POINTER_FLAGS.DOWN))
                {
                    if (TouchCapture.Instance.InputPoints.Any(p => p.Count > 10))
                    {
                        OnTouchMove(pointEventArgs);

                    }
                    else OnTouchDown(pointEventArgs);
                    return;
                }
            }
            OnTouchMove(pointEventArgs);


        }

        #endregion
    }
}
