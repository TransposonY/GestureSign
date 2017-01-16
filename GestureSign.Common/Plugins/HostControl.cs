using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.UI;

namespace GestureSign.Common.Plugins
{
	public class HostControl : IHostControl
	{
		#region Internal Manager Setters

		public IApplicationManager _ApplicationManager;
        public IGestureManager _GestureManager;
        public IPointCapture _PointCapture;
        public IPluginManager _PluginManager;
        public ITrayManager _TrayManager;

		#endregion

		public bool AllowEscapeKey
		{
			get	{ return true; } // Forms.InstanceManager.ActionDefinition.AllowEscapeKey; }
            set { } //Forms.InstanceManager.ActionDefinition.AllowEscapeKey = value; }
		}

		#region IHostControl Members

		public IApplicationManager ApplicationManager
		{
			get { return _ApplicationManager; }
		}

		public IGestureManager GestureManager
		{
			get { return _GestureManager; }
		}
        public IPointCapture PointCapture
        {
            get { return _PointCapture; }
        }
		public IPluginManager PluginManager
		{
			get { return _PluginManager; }
		}


		public ITrayManager TrayManager
		{
			get { return _TrayManager; }
		}

		#endregion
	}
}
