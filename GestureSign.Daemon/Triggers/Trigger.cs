using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GestureSign.Common.Gestures;

namespace GestureSign.Daemon.Triggers
{
    public abstract class Trigger
    {
        public event EventHandler<TriggerFiredEventArgs> TriggerFired;
        public abstract bool LoadConfiguration(IGesture[] gestures);

        protected virtual void OnTriggerFired(TriggerFiredEventArgs e)
        {
            TriggerFired?.Invoke(this, e);
        }
    }
}
