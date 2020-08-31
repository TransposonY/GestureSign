using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.CorePlugins.Delay
{
    public enum WaitSetting
    {
        NotSet,
        ForegroundWinChanged,
        MenuDisplayed,
        MenuClosed,
        MouseCaptured,
        MouseLost,
    }

    public class DelaySettings
    {
        #region Public Properties

        public WaitSetting WaitType { get; set; }

        public int Timeout { get; set; }

        #endregion
    }
}
