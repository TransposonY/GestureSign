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
            this.LastCapturedPoints = capturePoint;
            this.Points = new List<List<Point>>(capturePoint.Count);
            for (int i = 0; i < capturePoint.Count; i++)
            {
                this.Points.Add(new List<Point>(1));
                this.Points[i].Add(capturePoint[i]);
            }
        }
        public PointsCapturedEventArgs(List<List<Point>> points)
        {
            this.Points = points;
            this.LastCapturedPoints = points.Select(p => p.FirstOrDefault()).ToList();
        }

        public PointsCapturedEventArgs(List<List<Point>> points, List<Point> capturePoint)
            : this(points)
        {
            this.LastCapturedPoints = capturePoint;
        }

        #endregion

        #region Public Properties

        public List<List<Point>> Points { get; set; }
        public List<Point> LastCapturedPoints { get; set; }
        public bool Cancel { get; set; }
        public bool InterceptTouchInput { get; set; }
        public bool Delay { get; set; }
        public bool GestureTimeout { get; set; }

        #endregion
    }
}
