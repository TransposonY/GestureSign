using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GestureSign.Common.UI
{
	public class InstanceEventArgs : EventArgs
	{
		#region Public Instance Properties

        public System.Windows.Window Instance { get; set; }

		#endregion

		#region Constructors

		public InstanceEventArgs(System.Windows.Window Instance)
		{
			this.Instance = Instance;
		}

		#endregion
	}
}
