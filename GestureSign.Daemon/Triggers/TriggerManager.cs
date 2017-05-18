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

        private List<Trigger> _triggerList = new List<Trigger>();
        private SynchronizationContext _synchronizationContext;

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

        public void Load(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
            AddTrigger(new HotKeyManager());
            AddTrigger(new MouseTrigger());
            ApplicationManager.OnLoadApplicationsCompleted += (o, e) =>
            {
                _synchronizationContext.Post(state => { LoadConfig((ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApp) && app.Actions != null).SelectMany(app => app.Actions).ToList())); }, null);
            };
        }

        #endregion


        #region Private Methods

        private void LoadConfig(List<IAction> actions)
        {
            foreach (var trigger in _triggerList)
            {
                trigger.LoadConfiguration(actions);
            }
        }

        private void AddTrigger(Trigger newTrigger)
        {
            newTrigger.TriggerFired += Trigger_TriggerFired;
            _triggerList.Add(newTrigger);
        }

        private void Trigger_TriggerFired(object sender, TriggerFiredEventArgs e)
        {
            var point = new List<Point>(new[] { e.FiredPoint });
            var executableActions = ApplicationManager.Instance.GetRecognizedDefinedAction((List<IAction>)e.FiredActions).ToList();
            if (executableActions == null || executableActions.Count == 0) return;
            PluginManager.Instance.ExecuteAction(executableActions, PointCapture.Instance.Mode, new List<int>(new[] { 1 }), point, new List<List<Point>>(new[] { point }));
        }

        #endregion
    }
}
