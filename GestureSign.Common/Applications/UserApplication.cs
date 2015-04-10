using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace GestureSign.Common.Applications
{
    public class UserApplication : ApplicationBase
    {
        public bool AllowSingleStroke { get; set; }
        public bool InterceptTouchInput { get; set; }
    }
}
