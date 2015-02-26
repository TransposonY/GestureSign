using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.Common.Plugins
{
	public interface IPluginInfo
	{
		#region Instance Properties

		string DisplayText { get; set; }
		IPlugin Plugin { get; set; }
		string Class { get; set; }
		string Filename { get; set; }

		#endregion

		#region Instance Methods

		string ToString();

		#endregion
	}
}
