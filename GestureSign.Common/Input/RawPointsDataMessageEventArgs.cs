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
        public RawPointsDataMessageEventArgs(List<RawTouchData> rawTouchsData)
        {
            this.RawTouchsData = rawTouchsData;
        }


        #endregion

        #region Public Properties

        public List<RawTouchData> RawTouchsData { get; set; }

        #endregion
    }
}
