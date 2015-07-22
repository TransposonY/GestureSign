using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using GestureSign.Common.Plugins;
using System.Windows.Controls;
using GestureSign.Common.Localization;

namespace GestureSign.CorePlugins
{
    public class TemporarilyDisable : IPlugin
	{
		#region Private Variables

		IHostControl _HostControl = null;

		#endregion

		#region IAction Properties

		public string Name
		{
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.TemporarilyDisable.Name"); }
		}

		public string Description
		{
            get { return LocalizationProvider.Instance.GetTextValue("CorePlugins.TemporarilyDisable.Description"); }
		}

		public UserControl GUI
		{
			get { return null; }
		}

		public string Category
		{
            get { return "GestureSign"; }
		}

		public bool IsAction
		{
			get { return true; }
		}

		#endregion

		#region IAction Methods

		public void Initialize()
		{

		}

		public bool Gestured(PointInfo ActionPoint)
		{
		    _HostControl.TouchCapture.TemporarilyDisableCapture = true;
			_HostControl.TrayManager.ToggleDisableGestures();
			return true;
		}

		public bool Deserialize(string SerializedData)
		{
            return true;
			// Nothing to deserialize
		}

		public string Serialize()
		{
			// Nothing to serialize, send empty string
			return "";
		}
        
		#endregion

		#region Host Control

		public IHostControl HostControl
		{
			get { return _HostControl; }
			set { _HostControl = value; }
		}

		#endregion
	}
}