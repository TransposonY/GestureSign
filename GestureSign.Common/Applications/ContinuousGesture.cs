using System;

namespace GestureSign.Common.Applications
{
    public class ContinuousGesture : IEquatable<ContinuousGesture>
    {
        public ContinuousGesture(int contactCount, Gestures gesture)
        {
            ContactCount = contactCount;
            Gesture = gesture;
        }

        public int ContactCount { get; set; }
        public Gestures Gesture { get; set; }

        public bool Equals(ContinuousGesture other)
        {
            return other != null && other.ContactCount == ContactCount && other.Gesture == Gesture;
        }
    }

    [Flags]
    public enum Gestures
    {
        None = 0,
        Left = (1 << 0),
        Right = (1 << 1),
        Up = (1 << 2),
        Down = (1 << 3),
        Horizontal = (Left | Right),
        Vertical = (Up | Down),
        All = (Horizontal | Vertical)
    }
}
