using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GestureSign.CorePlugins.HotKey
{
    public class HotKeySettings
    {
        #region Public Properties

        public bool Windows { get; set; }

        public bool Control { get; set; }

        public bool Shift { get; set; }

        public bool Alt { get; set; }

        public List<Keys> KeyCode { get; set; }

        #endregion
    }
}
