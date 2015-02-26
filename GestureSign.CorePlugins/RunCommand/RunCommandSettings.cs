using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace GestureSign.CorePlugins.RunCommand
{
	[DataContract]
	public class RunCommandSettings
	{
		#region Constructors

		public RunCommandSettings()
		{

		}

		#endregion

		#region Public Properties

		[DataMember]
		public string Command { get; set; }

		[DataMember]
		public string Arguments { get; set; }

		#endregion
	}
}
