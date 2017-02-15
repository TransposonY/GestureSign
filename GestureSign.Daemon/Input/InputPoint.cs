using System.Drawing;

namespace GestureSign.Daemon.Input
{
    public struct InputPoint
    {
        public InputPoint(int contactIdentifier, Point point)
        {
            this.ContactIdentifier = contactIdentifier;
            this.Point = point;
        }

        public int ContactIdentifier;
        public Point Point;
    }
}
