using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using System;

namespace GestureSign.Daemon.Input
{
    public class PenDevice : HidDevice
    {
        public override Devices DeviceType => Devices.Pen;

        public PenDevice(IntPtr rawInputBuffer, ref RAWINPUT raw) : base(rawInputBuffer, ref raw)
        {
        }

        public DeviceStates GetPenState()
        {
            ushort[] usageList = GetButtonList(_hPreparsedData.DangerousGetHandle(), _pRawData, 0, _dwSizHid);
            DeviceStates state = DeviceStates.None;
            foreach (var u in usageList)
            {
                switch (u)
                {
                    case NativeMethods.TipId:
                        state |= DeviceStates.Tip;
                        break;
                    case NativeMethods.InRangeId:
                        state |= DeviceStates.InRange;
                        break;
                    case NativeMethods.BarrelButtonId:
                        state |= DeviceStates.RightClickButton;
                        break;
                    case NativeMethods.InvertId:
                        state |= DeviceStates.Invert;
                        break;
                    case NativeMethods.EraserId:
                        state |= DeviceStates.Eraser;
                        break;
                    default:
                        break;
                }
            }
            return state;
        }
    }
}
