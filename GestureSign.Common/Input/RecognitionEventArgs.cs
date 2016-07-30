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

        public RecognitionEventArgs(List<List<Point>> points, List<Point> capturePoints, List<int> contactIdentifiers)
        {
            this.Points = points;
            this.FirstCapturedPoints = capturePoints;
            ContactIdentifiers = contactIdentifiers;
        }

        public RecognitionEventArgs(string gestureName, List<List<Point>> points, List<Point> capturePoints, List<int> contactIdentifiers)
            : this(points, capturePoints, contactIdentifiers)
        {
            this.GestureName = gestureName;
        }

        #endregion

        #region Public Instance Properties

        public string GestureName { get; set; }
        public List<List<Point>> Points { get; set; }
        public List<Point> FirstCapturedPoints { get; set; }
        public List<int> ContactIdentifiers { get; set; }

        #endregion
    }
}
