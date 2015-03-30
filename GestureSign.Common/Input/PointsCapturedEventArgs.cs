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

        public PointsCapturedEventArgs(Point[] capturePoint)
        {
            this.CapturePoint = capturePoint;
            this.Points = new List<List<Point>>(capturePoint.Length);
            for (int i = 0; i < capturePoint.Length; i++)
            {
                this.Points.Add(new List<Point>(1));
                this.Points[i].Add(capturePoint[i]);
            }
        }
        public PointsCapturedEventArgs(List<List<Point>> Points)
        {
            this.Points = Points;
            this.CapturePoint = Points.Select(p => p.FirstOrDefault()).ToArray();
        }

        public PointsCapturedEventArgs(List<List<Point>> Points, Point[] CapturePoint)
            : this(Points)
        {
            this.CapturePoint = CapturePoint;
        }

        public PointsCapturedEventArgs(List<List<Point>> Points, CaptureState State)
            : this(Points)
        {
            this.State = State;
        }

        public PointsCapturedEventArgs(List<List<Point>> Points, Point[] CapturePoint, CaptureState State)
            : this(Points, CapturePoint)
        {
            this.State = State;
        }

        #endregion

        #region Public Properties

        public List<List<Point>> Points { get; set; }
        public Point[] CapturePoint { get; set; }
        public bool Cancel { get; set; }
        public CaptureState State { get; set; }
        public bool InterceptTouchInput { get; set; }
        #endregion
    }
}
