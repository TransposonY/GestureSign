using System.Drawing;

namespace GestureSign.Common.Input
{

    public struct RawTouchData
    {
        public RawTouchData(bool status, int num, Point rawPointsData)
        {
            Status = status;
            Num = num;
            RawPointsData = rawPointsData;
        }
        public bool Status;
        public int Num;
        public Point RawPointsData;
    }
}
