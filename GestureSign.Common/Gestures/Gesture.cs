using System;
using System.Runtime.Serialization;
using ManagedWinapi;
using ManagedWinapi.Hooks;

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

        [DataMember]
        public Hotkey Hotkey { get; set; }

        [DataMember]
        public MouseActions MouseAction { get; set; }

        public bool Equals(Gesture other)
        {
            if (other == null) return false;
            return Name == other.Name && Hotkey != null && Hotkey.Equals(other.Hotkey) && MouseAction == other.MouseAction;
        }

        #endregion
    }
}
