using System;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins
{
    public class Notifier : IPlugin
    {
        #region IPlugin Members

        public string Name
        {
            get { return "Notifier"; }
        }

        public string Description
        {
            get { return "Notifies user of performed actions"; }
        }

        public object GUI
        {
            get { return null; }
        }

        public bool ActivateWindowDefault
        {
            get { return false; }
        }

        public string Category
        {
            get { return "Common"; }
        }

        public bool IsAction
        {
            get { return false; }
        }

        public object Icon => null;

        public void Initialize()
        {
            //HostControl.GestureManager.GestureRecognized += (o, e) =>
            //    {
            //        //MessageBox.Show(String.Format("You drew a '{0}'", e.GestureName));
            //    };
        }

        public bool Gestured(PointInfo ActionPoint)
        {
            return true;
        }

        public bool Deserialize(string SerializedData)
        {
            return true;
        }

        public string Serialize()
        {
            return String.Empty;
        }

        public IHostControl HostControl { get; set; }

        #endregion
    }
}
