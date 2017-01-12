using System;
using System.Collections.Generic;

namespace GestureSign.Daemon.Triggers
{
    public class GestureNameEventArgs : EventArgs
    {
        public GestureNameEventArgs(List<string> gestureName)
        {
            GestureName = gestureName;
        }

        public List<string> GestureName { get; }
    }
}
