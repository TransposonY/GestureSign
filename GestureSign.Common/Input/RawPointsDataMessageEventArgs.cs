using System;
using System.Collections.Generic;

namespace GestureSign.Common.Input
{
    public class RawPointsDataMessageEventArgs : EventArgs
    {
        #region Constructors

        public RawPointsDataMessageEventArgs(List<RawData> rawData, Devices device)
        {
            this.RawData = rawData;
            SourceDevice = device;
        }


        #endregion

        #region Public Properties

        public List<RawData> RawData { get; set; }
        public Devices SourceDevice { get; set; }

        #endregion
    }
}
