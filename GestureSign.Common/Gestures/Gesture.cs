using System;
using System.Runtime.Serialization;

namespace GestureSign.Common.Gestures
{
    [DataContract]
    [Serializable]
    [KnownType(typeof(Gesture))]
    public class Gesture : IGesture
    {
        #region Constructors
        public Gesture()
        { }
        public Gesture(string name, PointPattern[] pointPatterns)
        {
            this.Name = name;
            this.PointPatterns = pointPatterns;
        }

        #endregion

        #region IPointPattern Instance Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public PointPattern[] PointPatterns { get; set; }

        #endregion
    }
}
