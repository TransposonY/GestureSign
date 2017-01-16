using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Daemon.Triggers
{
    public class TriggerFiredEventArgs : EventArgs
    {
        public TriggerFiredEventArgs(List<string> gestureName, Point firedPoint)
        {
            GestureName = gestureName;
            FiredPoint = firedPoint;
        }

        public List<string> GestureName { get; }
        public Point FiredPoint { get; }
    }
}
