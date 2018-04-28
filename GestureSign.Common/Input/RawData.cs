using System.Drawing;

namespace GestureSign.Common.Input
{

    public struct RawData
    {
        public RawData(DeviceStates state, int contactIdentifier, Point rawPointsData)
        {
            this.State = state;
            this.ContactIdentifier = contactIdentifier;
            this.RawPoints = rawPointsData;
        }
        public DeviceStates State;
        public int ContactIdentifier;
        public Point RawPoints;
    }
}
