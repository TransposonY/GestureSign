using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.CorePlugins.ActivateWindow
{
    public class ActivateWindowSettings
    {
        #region Public Properties

        public string ClassName { get; set; }

        public string Caption { get; set; }

        public bool IsRegEx { get; set; }

        public int Timeout { get; set; }
        #endregion
    }
}
