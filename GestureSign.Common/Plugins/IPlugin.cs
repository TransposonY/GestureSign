using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GestureSign.Common.Plugins;

using System.Windows.Controls;
using System.Xaml;

namespace GestureSign.Common.Plugins
{
	public interface IPlugin
	{
		#region Properties

		string Name { get; }
		string Category { get; }
		string Description { get; }
		bool IsAction { get; }
        UserControl GUI { get; }

		#endregion

		#region Methods

		void Initialize();
		bool Gestured(PointInfo ActionPoint);
		void Deserialize(string SerializedData);
		string Serialize();

		#endregion

		#region Host Controls

		IHostControl HostControl { get; set; }

		#endregion
	}
}
