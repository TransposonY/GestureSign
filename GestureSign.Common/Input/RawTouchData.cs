using System.Drawing;

namespace GestureSign.Common.Input
{

    public struct RawTouchData
    {
        public RawTouchData(bool tip, int contactIdentifier, Point rawPointsData)
        {
            this.Tip = tip;
            this.ContactIdentifier = contactIdentifier;
            this.RawPoints = rawPointsData;
        }
        public bool Tip;
        public int ContactIdentifier;
        public Point RawPoints;
    }
}
