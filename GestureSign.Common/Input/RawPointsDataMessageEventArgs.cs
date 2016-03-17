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
        public RawPointsDataMessageEventArgs(RawTouchData[] rawTouchsData)
        {
            this.RawTouchsData = rawTouchsData;
        }


        #endregion

        #region Public Properties

        public RawTouchData[] RawTouchsData { get; set; }

        #endregion
    }
}
