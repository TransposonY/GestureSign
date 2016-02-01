using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.Common.Input
{
    public class RecognitionEventArgs : EventArgs
    {
        #region Constructors

        public RecognitionEventArgs(List<List<Point>> Points, Point[] CapturePoints)
        {
            this.Points = Points;
            this.CapturePoints = CapturePoints;
        }

        public RecognitionEventArgs(string GestureName, List<List<Point>> Points, Point[] CapturePoints)
        {
            this.GestureName = GestureName;
            this.Points = Points;
            this.CapturePoints = CapturePoints;
        }

        #endregion

        #region Public Instance Properties

        public string GestureName { get; set; }
        public List<List<Point>> Points { get; set; }
        public Point[] CapturePoints { get; set; }
        public CaptureMode Mode { get; set; }

        #endregion
    }
}
