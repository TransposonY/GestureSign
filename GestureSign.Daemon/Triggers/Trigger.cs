using GestureSign.Common.Applications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureSign.Daemon.Triggers
{
    public abstract class Trigger
    {
        public event EventHandler<TriggerFiredEventArgs> TriggerFired;

        protected virtual void OnTriggerFired(TriggerFiredEventArgs e)
        {
            TriggerFired?.Invoke(this, e);
        }
    }
}
