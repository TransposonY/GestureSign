using GestureSign.Common.Applications;
using GestureSign.Common.Plugins;
using GestureSign.Daemon.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace GestureSign.Daemon.Triggers
{
    class TriggerManager
    {
        #region Private Variables

        private List<Trigger> _triggerList = new List<Trigger>(3);

        #endregion

        #region Constructors

        static TriggerManager()
        {
            Instance = new TriggerManager();
        }

        #endregion

        #region Public Instance Properties

        public static TriggerManager Instance { get; }

        #endregion

        #region Public Methods

        public void Load()
        {
            AddTrigger(new HotKeyManager());
            AddTrigger(new MouseTrigger());
            AddTrigger(new ContinuousGestureTrigger());
        }

        #endregion


        #region Private Methods

        private void AddTrigger(Trigger newTrigger)
        {
            newTrigger.TriggerFired += Trigger_TriggerFired;
            _triggerList.Add(newTrigger);
        }

        private void Trigger_TriggerFired(object sender, TriggerFiredEventArgs e)
        {
            if (e.FiredActions == null || e.FiredActions.Count == 0) return;
            var point = new List<Point>(new[] { e.FiredPoint });
            PluginManager.Instance.ExecuteAction(e.FiredActions, PointCapture.Instance.Mode, PointCapture.Instance.SourceDevice, new List<int>(new[] { 1 }), point, new List<List<Point>>(new[] { point }));
        }

        #endregion
    }
}
