using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public class RawPointsDataMessageEventArgs : EventArgs
    {
        #region Constructors
        public RawPointsDataMessageEventArgs(RawTouchData[] rawTouchsData, int scanTime)
        {
            this.RawTouchsData = rawTouchsData;
            this.ScanTime = scanTime;
        }


        #endregion

        #region Public Properties

        public RawTouchData[] RawTouchsData { get; set; }
        public int ScanTime { get; set; }

        #endregion
    }
}
