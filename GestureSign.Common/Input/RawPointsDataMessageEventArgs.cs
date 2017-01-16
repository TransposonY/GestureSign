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
        public RawPointsDataMessageEventArgs(List<RawData> rawData)
        {
            this.RawData = rawData;
        }


        #endregion

        #region Public Properties

        public List<RawData> RawData { get; set; }

        #endregion
    }
}
