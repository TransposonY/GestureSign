using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.PointPatterns
{
	public interface IPointPattern
	{
		#region Interface Properties

        List<List<Point>> Points { get; set; }

		#endregion
	}
}
