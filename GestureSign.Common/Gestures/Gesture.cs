using System;

namespace GestureSign.Common.Gestures
{
    [Serializable]
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

        public string Name { get; set; }

        public PointPattern[] PointPatterns { get; set; }

        #endregion
    }
}
