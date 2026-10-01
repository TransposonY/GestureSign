using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GestureSign.Daemon.Input
{
    public class TouchPadDevice : HidDevice
    {
        public override Devices DeviceType => Devices.TouchPad;

        public TouchPadDevice(IntPtr rawInputBuffer, ref RAWINPUT raw, SafeUnmanagedMemoryHandle preparsedData) : base(rawInputBuffer, ref raw, preparsedData)
        {
        }

        protected override Point GetCoordinate(short linkCollection, Rectangle screenBounds, IntPtr pRawDataPacket)
        {
            int physicalX = 0;
            int physicalY = 0;

            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.XCoordinateId, ref physicalX, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.YCoordinateId, ref physicalY, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);

            int x, y;
            x = physicalX * screenBounds.Width / _physicalMax.X;
            y = physicalY * screenBounds.Height / _physicalMax.Y;

            return new Point(x + screenBounds.X, y + screenBounds.Y);
        }

        /// <param name="buttonDown">Set when any packet reports a pressed physical button (Button page, top-level collection).</param>
        public void GetRawDatas(short numberOfChildren, Rectangle screenBounds, ref int requiringContactCount, ref List<RawData> _outputTouchs, ref bool buttonDown)
        {
            for (int dwIndex = 0; dwIndex < _dwCount; dwIndex++)
            {
                IntPtr pRawDataPacket = new IntPtr(_pRawData.ToInt64() + dwIndex * _dwSizHid);
                buttonDown |= IsButtonDown(pRawDataPacket);
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

        private readonly ushort[] _buttonUsages = new ushort[8];

        private bool IsButtonDown(IntPtr pRawDataPacket)
        {
            int usageLength = _buttonUsages.Length;
            int status = HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.ButtonUsagePage, 0, _buttonUsages, ref usageLength, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
            return status == HidNativeApi.HIDP_STATUS_BUFFER_TOO_SMALL
                || status == HidNativeApi.HIDP_STATUS_SUCCESS && usageLength > 0;
        }
    }
}
