using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GestureSign.Common.Gestures
{
	public class GestureEventArgs : EventArgs
	{
		#region Public Instance Properties

		public string GestureName { get; set; }
        public string NewGestureName { get; set; }
		#endregion

		#region Constructors

		public GestureEventArgs(string GestureName)
		{
			this.GestureName = GestureName;
		}
        public GestureEventArgs(string GestureName, string newGestureName)
            : this(GestureName)
        {
            this.NewGestureName = newGestureName;
        }
		#endregion
	}
}
