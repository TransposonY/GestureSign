using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.UI;

namespace GestureSign.Plugins
{
	public class HostControl : IHostControl
	{
		#region Internal Manager Setters

		internal IApplicationManager _ApplicationManager;
		internal IGestureManager _GestureManager;
        internal ITouchCapture _TouchCapture;
		internal IPluginManager _PluginManager;
		internal IFormManager _FormManager;
		internal ITrayManager _TrayManager;

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
        public ITouchCapture TouchCapture
        {
            get { return _TouchCapture; }
        }
		public IPluginManager PluginManager
		{
			get { return _PluginManager; }
		}

		public IFormManager FormManager
		{
			get { return _FormManager; }
		}

		public ITrayManager TrayManager
		{
			get { return _TrayManager; }
		}

		#endregion
	}
}
