using System;
using System.Collections.Generic;
using ManagedWinapi.Hooks;

namespace GestureSign.Common.Input
{
    public class RawPointsDataMessageEventArgs : EventArgs
    {
        #region Constructors

        public RawPointsDataMessageEventArgs(List<RawData> rawData, LowLevelMouseMessage mouseMessage = null)
        {
            this.RawData = rawData;
            MouseMessage = mouseMessage;
        }


        #endregion

        #region Public Properties

        public List<RawData> RawData { get; set; }

        public LowLevelMouseMessage MouseMessage { get; set; }

        public bool Handled { get; set; }

        #endregion
    }
}
