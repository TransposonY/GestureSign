using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public class PointsCapturedEventArgs : EventArgs
    {
        #region Constructors

        public PointsCapturedEventArgs(List<Point> capturePoint)
        {
            this.FirstCapturedPoints = capturePoint;
            this.Points = new List<List<Point>>(capturePoint.Count);
            for (int i = 0; i < capturePoint.Count; i++)
            {
                this.Points.Add(new List<Point>(1));
                this.Points[i].Add(capturePoint[i]);
            }
        }

        public PointsCapturedEventArgs(List<List<Point>> points, List<Point> capturePoint)
        {
            this.Points = points;
            this.FirstCapturedPoints = capturePoint;
        }

        #endregion

        #region Public Properties

        public List<List<Point>> Points { get; set; }
        public List<Point> FirstCapturedPoints { get; set; }
        public bool Cancel { get; set; }
        public int BlockTouchInputThreshold { get; set; }
        public bool Delay { get; set; }
        public bool GestureTimeout { get; set; }

        #endregion
    }
}
