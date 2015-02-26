using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GestureSign.PointPatterns;
using GestureSign.Common.Gestures;
using System.Runtime.Serialization;

namespace GestureSign.Gestures
{
    [DataContract]
    [Serializable] 
	[KnownType(typeof(Gesture))]
	public class Gesture : IGesture
	{
		#region Constructors
        public Gesture()
        { }
        public Gesture(string Name, List<List<Point>> Points)
		{
			this.Name = Name;
			this.Points = Points;
		}

		#endregion

		#region IPointPattern Instance Properties

		[DataMember]
		public string Name { get; set; }

		[DataMember]
        public List<List<Point>> Points { get; set; }

		#endregion
	}
}
