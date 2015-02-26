using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public class PointsMessageEventArgs : EventArgs
    {
        #region Constructors
        public PointsMessageEventArgs(RawTouchData[] rawTouchsData, ushort timeStamp)
        {
            this.RawTouchsData = rawTouchsData;
            this.TimeStamp = timeStamp;
        }


        #endregion

        #region Public Properties

        public RawTouchData[] RawTouchsData { get; set; }
        public ushort TimeStamp { get; set; }

        #endregion
    }
}
