using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GestureSign.Daemon.Input
{
    public abstract class HidDevice : IDevice, IDisposable
    {
        private bool disposedValue;
        protected SafeUnmanagedMemoryHandle _hPreparsedData;
        protected IntPtr _pRawData;
        protected int _dwCount;
        protected int _dwSizHid;
        protected Point _physicalMax;

        protected static bool _isAxisCorresponds;
        protected static bool _xAxisDirection;
        protected static bool _yAxisDirection;


        public abstract Devices DeviceType { get; }

        protected HidDevice(IntPtr rawInputBuffer, ref RAWINPUT raw)
        {
            _hPreparsedData = GetPreparsedData(raw.header.hDevice);
            _pRawData = GetRawDataPtr(rawInputBuffer, ref raw);
            _dwCount = raw.hid.dwCount;
            _dwSizHid = raw.hid.dwSizHid;
        }

        protected static SafeUnmanagedMemoryHandle GetPreparsedData(IntPtr hDevice)
        {
            uint pcbSize = 0;
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, IntPtr.Zero, ref pcbSize);
            IntPtr pPreparsedData = Marshal.AllocHGlobal((int)pcbSize);
            NativeMethods.GetRawInputDeviceInfo(hDevice, NativeMethods.RIDI_PREPARSEDDATA, pPreparsedData, ref pcbSize);
            return new SafeUnmanagedMemoryHandle(pPreparsedData);
        }

        protected static IntPtr GetRawDataPtr(IntPtr rawInputBuffer, ref RAWINPUT raw)
        {
            return new IntPtr(rawInputBuffer.ToInt64() + (raw.header.dwSize - raw.hid.dwSizHid * raw.hid.dwCount));
        }

        protected static ushort[] GetButtonList(IntPtr pPreparsedData, IntPtr pRawData, short nodeIndex, int rawDateSize)
        {
            int usageLength = 0;
            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, null, ref usageLength, pPreparsedData, pRawData, rawDateSize);
            var usageList = new ushort[usageLength];
            HidNativeApi.HidP_GetUsages(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, usageList, ref usageLength, pPreparsedData, pRawData, rawDateSize);
            return usageList;
        }

        protected virtual Point GetCoordinate(short linkCollection, Screen currentScr, IntPtr pRawDataPacket)
        {
            int physicalX = 0;
            int physicalY = 0;

            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.XCoordinateId, ref physicalX, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.YCoordinateId, ref physicalY, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);

            int x, y;
            if (_isAxisCorresponds)
            {
                x = physicalX * currentScr.Bounds.Width / _physicalMax.X;
                y = physicalY * currentScr.Bounds.Height / _physicalMax.Y;
            }
            else
            {
                x = physicalY * currentScr.Bounds.Width / _physicalMax.Y;
                y = physicalX * currentScr.Bounds.Height / _physicalMax.X;
            }
            x = _xAxisDirection ? x : currentScr.Bounds.Width - x;
            y = _yAxisDirection ? y : currentScr.Bounds.Height - y;

            return new Point(x + currentScr.Bounds.X, y + currentScr.Bounds.Y);
        }

        public virtual Point GetCoordinate(short linkCollection, Screen currentScr)
        {
            return GetCoordinate(linkCollection, currentScr, _pRawData);
        }

        public virtual int GetContactCount()
        {
            int contactCount = 0;
            if (HidNativeApi.HIDP_STATUS_SUCCESS != HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, 0, NativeMethods.ContactCountId,
                ref contactCount, _hPreparsedData.DangerousGetHandle(), _pRawData, _dwSizHid))
            {
                throw new ApplicationException(Common.Localization.LocalizationProvider.Instance.GetTextValue("Messages.ContactCountError"));
            }
            return contactCount;
        }

        public virtual HidNativeApi.HIDP_LINK_COLLECTION_NODE[] GetLinkCollectionNodes()
        {
            int linkCount = 0;
            HidNativeApi.HidP_GetLinkCollectionNodes(null, ref linkCount, _hPreparsedData.DangerousGetHandle());
            HidNativeApi.HIDP_LINK_COLLECTION_NODE[] lcn = new HidNativeApi.HIDP_LINK_COLLECTION_NODE[linkCount];
            HidNativeApi.HidP_GetLinkCollectionNodes(lcn, ref linkCount, _hPreparsedData.DangerousGetHandle());
            return lcn;
        }

        public virtual int GetContactId(short nodeIndex, IntPtr pRawDataPacket)
        {
            int contactIdentifier = 0;
            HidNativeApi.HidP_GetUsageValue(HidReportType.Input, NativeMethods.DigitizerUsagePage, nodeIndex, NativeMethods.ContactIdentifierId, ref contactIdentifier, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
            return contactIdentifier;
        }

        public static void GetCurrentScreenOrientation()
        {
            switch (SystemInformation.ScreenOrientation)
            {
                case ScreenOrientation.Angle0:
                    _xAxisDirection = _yAxisDirection = true;
                    _isAxisCorresponds = true;
                    break;
                case ScreenOrientation.Angle90:
                    _isAxisCorresponds = false;
                    _xAxisDirection = false;
                    _yAxisDirection = true;
                    break;
                case ScreenOrientation.Angle180:
                    _xAxisDirection = _yAxisDirection = false;
                    _isAxisCorresponds = true;
                    break;
                case ScreenOrientation.Angle270:
                    _isAxisCorresponds = false;
                    _xAxisDirection = true;
                    _yAxisDirection = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void GetPhysicalMax(int collectionCount)
        {
            short valueCapsLength = (short)(collectionCount > 0 ? collectionCount : 1);
            HidNativeApi.HidP_Value_Caps[] hvc = new HidNativeApi.HidP_Value_Caps[valueCapsLength];

            HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.XCoordinateId, hvc, ref valueCapsLength, _hPreparsedData.DangerousGetHandle());
            _physicalMax.X = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;

            HidNativeApi.HidP_GetSpecificValueCaps(HidReportType.Input, NativeMethods.GenericDesktopPage, 0, NativeMethods.YCoordinateId, hvc, ref valueCapsLength, _hPreparsedData.DangerousGetHandle());
            _physicalMax.Y = hvc[0].PhysicalMax != 0 ? hvc[0].PhysicalMax : hvc[0].LogicalMax;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                _hPreparsedData.Dispose();
                disposedValue = true;
            }
        }

        ~HidDevice()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
