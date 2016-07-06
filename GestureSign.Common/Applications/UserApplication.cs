using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace GestureSign.Common.Applications
{
    public class UserApplication : ApplicationBase
    {
        private int _limitNumberOfFingers;

        public int LimitNumberOfFingers
        {
            get { return _limitNumberOfFingers < 1 ? _limitNumberOfFingers = 2 : _limitNumberOfFingers; }
            set { _limitNumberOfFingers = value; }
        }

        public int BlockTouchInputThreshold { get; set; }
    }
}
