using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.Common.Applications
{
	public class ApplicationChangedEventArgs : EventArgs
	{
		#region Constructors

		public ApplicationChangedEventArgs()
		{

		}

		public ApplicationChangedEventArgs(IApplication Application)
		{
			this.Application = Application;
		}

		#endregion

		#region Public Properties

		public IApplication Application { get; set; }

		#endregion
	}
}
