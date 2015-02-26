using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Input;

namespace GestureSign.Common.Input
{
    public class StateChangedEventArgs : EventArgs
    {
        #region Constructors

        public StateChangedEventArgs(CaptureState State)
        {
            this.State = State;
        }

        #endregion

        #region Public Properties

        public CaptureState State { get; set; }

        #endregion
    }
}
