using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Daemon.Input
{
    public class TouchScreenDevice : HidDevice
    {
        public override Devices DeviceType => Devices.TouchScreen;

        public TouchScreenDevice(IntPtr rawInputBuffer, ref RAWINPUT raw, SafeUnmanagedMemoryHandle preparsedData) : base(rawInputBuffer, ref raw, preparsedData)
        {
        }

        public void GetRawDatas(short numberOfChildren, Rectangle screenBounds, ref int requiringContactCount, ref List<RawData> _outputTouchs)
        {
            for (int dwIndex = 0; dwIndex < _dwCount; dwIndex++)
            {
                IntPtr pRawDataPacket = new IntPtr(_pRawData.ToInt64() + dwIndex * _dwSizHid);
                for (short nodeIndex = 1; nodeIndex <= numberOfChildren; nodeIndex++)
                {
                    int contactIdentifier = GetContactId(nodeIndex, pRawDataPacket);
                    Point point = GetCoordinate(nodeIndex, screenBounds, pRawDataPacket);

                    ushort[] usageList = GetButtonList(_hPreparsedData.DangerousGetHandle(), _pRawData, nodeIndex, _dwSizHid);
                    bool tip = usageList.Length != 0 && usageList[0] == NativeMethods.TipId;

                    _outputTouchs.Add(new RawData(tip ? DeviceStates.Tip : DeviceStates.None, contactIdentifier, point));

                    if (--requiringContactCount == 0) break;
                }
                if (requiringContactCount == 0) break;
            }
        }
    }
}
