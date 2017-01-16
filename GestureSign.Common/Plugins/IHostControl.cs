using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Input;
using GestureSign.Common.Plugins;
using GestureSign.Common.UI;

namespace GestureSign.Common.Plugins
{
	public interface IHostControl
	{
		IApplicationManager ApplicationManager { get; }
		IGestureManager GestureManager { get; }
		IPluginManager PluginManager { get; }
		ITrayManager TrayManager { get; }

        IPointCapture PointCapture { get; }
		#region Methods

		bool AllowEscapeKey { get; set; }

		#endregion
	}
}
