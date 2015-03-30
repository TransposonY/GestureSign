using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.Common.Input
{
    public class PointerMessageEventArgs : EventArgs
    {
        #region Constructors
        public PointerMessageEventArgs(POINTER_INFO[] pointerInfo)
        {
            this.PointerInfo = pointerInfo;
        }


        #endregion

        #region Public Properties

        public POINTER_INFO[] PointerInfo { get; set; }

        #endregion
    }
}
