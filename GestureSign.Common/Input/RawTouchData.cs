using System.Drawing;

namespace GestureSign.Common.Input
{

    public struct RawTouchData
    {
        public RawTouchData(bool status, int num, Point rawPointsData)
        {
            this.Status = status;
            this.Num = num;
            this.RawPoints = rawPointsData;
        }
        public bool Status;
        public int Num;
        public Point RawPoints;
    }
}
