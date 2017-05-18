using GestureSign.Common.Applications;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Daemon.Triggers
{
    public class TriggerFiredEventArgs : EventArgs
    {
        public TriggerFiredEventArgs(List<IAction> firedActions, Point firedPoint)
        {
            FiredActions = firedActions;
            FiredPoint = firedPoint;
        }

        public List<IAction> FiredActions { get; }
        public Point FiredPoint { get; }
    }
}
