using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Plugins;
using GestureSign.Daemon.Input;

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
            GestureManager.OnLoadGesturesCompleted += GestureManager_OnLoadGesturesCompleted;
        }

        #endregion


        #region Private Methods

        private void GestureManager_OnLoadGesturesCompleted(object sender, EventArgs e)
        {
            _synchronizationContext.Post(state => { LoadConfig(((GestureManager)sender).Gestures); }, null);
        }

        private void LoadConfig(IGesture[] gestures)
        {
            foreach (var trigger in _triggerList)
            {
                trigger.LoadConfiguration(gestures);
            }
        }

        private void AddTrigger(Trigger newTrigger)
        {
            newTrigger.TriggerFired += Trigger_TriggerFired;
            _triggerList.Add(newTrigger);
        }

        private void Trigger_TriggerFired(object sender, GestureNameEventArgs e)
        {
            var window = ApplicationManager.Instance.GetForegroundApplications();
            var point = new List<Point>(new[] { window.Rectangle.Location });
            foreach (var name in e.GestureName)
            {
                PluginManager.Instance.ExecuteAction(PointCapture.Instance.Mode,
                    name,
                    new List<int>(new[] { 1 }),
                    point,
                    new List<List<Point>>(new[] { point }));
            }
        }

        #endregion
    }
}
