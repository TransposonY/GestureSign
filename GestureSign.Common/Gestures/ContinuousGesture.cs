namespace GestureSign.Common.Gestures
{
    /// <summary>
    /// Named continuous gesture: swiping with <see cref="ContactCount"/> fingers towards <see cref="Direction"/> fires the bound
    /// actions repeatedly while the fingers keep moving. Actions reference it by <see cref="Name"/>
    /// (<see cref="Applications.IAction.ContinuousGestureName"/>), like drawn gestures are referenced by name.
    /// </summary>
    public class ContinuousGesture
    {
        public const int MinContactCount = 2;
        public const int MaxContactCount = 10;

        public ContinuousGesture() { }

        public ContinuousGesture(string name, int contactCount, ContinuousDirection direction)
        {
            Name = name;
            ContactCount = contactCount;
            Direction = direction;
        }

        public string Name { get; set; }
        public int ContactCount { get; set; }
        public ContinuousDirection Direction { get; set; }

        /// <summary>Same fingers and direction, i.e. the daemon cannot tell the two apart.</summary>
        public bool IsSameMotion(ContinuousGesture other)
        {
            return other != null && other.ContactCount == ContactCount && other.Direction == Direction;
        }
    }

    public enum ContinuousDirection
    {
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
    }
}
