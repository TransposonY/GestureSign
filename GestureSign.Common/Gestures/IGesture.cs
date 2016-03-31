namespace GestureSign.Common.Gestures
{
    public interface IGesture
    {
        string Name { get; set; }

        PointPattern[] PointPatterns { get; set; }
    }
}
