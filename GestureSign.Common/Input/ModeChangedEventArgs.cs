using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Input;

namespace GestureSign.Common.Input
{
    public class ModeChangedEventArgs : EventArgs
    {
        #region Constructors

        public ModeChangedEventArgs(CaptureMode newMode)
        {
            this.Mode = newMode;
        }

        #endregion

        #region Public Properties

        public CaptureMode Mode { get; set; }

        #endregion
    }
}
