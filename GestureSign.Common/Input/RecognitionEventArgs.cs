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

        public RecognitionEventArgs(List<List<Point>> points, List<Point> capturePoints)
        {
            this.Points = points;
            this.LastCapturedPoints = capturePoints;
        }

        public RecognitionEventArgs(string gestureName, List<List<Point>> points, List<Point> capturePoints)
        {
            this.GestureName = gestureName;
            this.Points = points;
            this.LastCapturedPoints = capturePoints;
        }

        #endregion

        #region Public Instance Properties

        public string GestureName { get; set; }
        public List<List<Point>> Points { get; set; }
        public List<Point> LastCapturedPoints { get; set; }

        #endregion
    }
}
