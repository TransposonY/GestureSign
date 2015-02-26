using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;

namespace GestureSign.CorePlugins.HotKey
{
	[DataContract]
	public class HotKeySettings
	{
		#region Public Properties

		[DataMember]
		public bool Windows { get; set; }

		[DataMember]
		public bool Control { get; set; }

		[DataMember]
		public bool Shift { get; set; }

		[DataMember]
		public bool Alt { get; set; }

		[DataMember]
		public Keys KeyCode { get; set; }

		#endregion
	}
}
