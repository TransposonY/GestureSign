using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public class PointEventArgs : EventArgs
    {
        #region Constructors
        public PointEventArgs(IEnumerable<KeyValuePair<int, Point>> points)
        {
            this.Points = points;
        }


        #endregion

        #region Public Properties
        public IEnumerable<KeyValuePair<int, Point>> Points { get; set; }

        #endregion
    }
}
