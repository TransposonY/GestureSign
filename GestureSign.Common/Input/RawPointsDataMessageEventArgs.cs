using System;
using System.Collections.Generic;

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
