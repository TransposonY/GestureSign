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

        public abstract Devices DeviceType { get; }

        /// <summary>Orientation mapping applied by <see cref="GetCoordinate(short, Rectangle, IntPtr)"/>.</summary>
        public ScreenAxisMapping AxisMapping { get; set; }

        /// <param name="preparsedData">RIDI_PREPARSEDDATA of the device; owned (disposed) by this instance.</param>
        protected HidDevice(IntPtr rawInputBuffer, ref RAWINPUT raw, SafeUnmanagedMemoryHandle preparsedData)
        {
            _hPreparsedData = preparsedData;
            _pRawData = GetRawDataPtr(rawInputBuffer, ref raw);
            _dwCount = raw.hid.dwCount;
            _dwSizHid = raw.hid.dwSizHid;
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

        protected virtual Point GetCoordinate(short linkCollection, Rectangle screenBounds, IntPtr pRawDataPacket)
        {
            int physicalX = 0;
            int physicalY = 0;

            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.XCoordinateId, ref physicalX, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);
            HidNativeApi.HidP_GetScaledUsageValue(HidReportType.Input, NativeMethods.GenericDesktopPage, linkCollection, NativeMethods.YCoordinateId, ref physicalY, _hPreparsedData.DangerousGetHandle(), pRawDataPacket, _dwSizHid);

            int x, y;
            if (AxisMapping.AxisCorresponds)
            {
                x = physicalX * screenBounds.Width / _physicalMax.X;
                y = physicalY * screenBounds.Height / _physicalMax.Y;
            }
            else
            {
                x = physicalY * screenBounds.Width / _physicalMax.Y;
                y = physicalX * screenBounds.Height / _physicalMax.X;
            }
            x = AxisMapping.XAxisDirection ? x : screenBounds.Width - x;
            y = AxisMapping.YAxisDirection ? y : screenBounds.Height - y;

            return new Point(x + screenBounds.X, y + screenBounds.Y);
        }

        public virtual Point GetCoordinate(short linkCollection, Rectangle screenBounds)
        {
            return GetCoordinate(linkCollection, screenBounds, _pRawData);
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

        public static ScreenAxisMapping GetScreenAxisMapping(ScreenOrientation orientation)
        {
            switch (orientation)
            {
                case ScreenOrientation.Angle0:
                    return new ScreenAxisMapping(axisCorresponds: true, xAxisDirection: true, yAxisDirection: true);
                case ScreenOrientation.Angle90:
                    return new ScreenAxisMapping(axisCorresponds: false, xAxisDirection: false, yAxisDirection: true);
                case ScreenOrientation.Angle180:
                    return new ScreenAxisMapping(axisCorresponds: true, xAxisDirection: false, yAxisDirection: false);
                case ScreenOrientation.Angle270:
                    return new ScreenAxisMapping(axisCorresponds: false, xAxisDirection: true, yAxisDirection: false);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Devices EnumerateDevices()
        {
            Devices foundDevices = Devices.None;
            uint deviceCount = 0;
            int dwSize = Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

            if (NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                using (new SafeUnmanagedMemoryHandle(pRawInputDeviceList))
                {
                    NativeMethods.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                    for (int i = 0; i < deviceCount; i++)
                    {
                        uint pSize = 0;

                        RAWINPUTDEVICELIST rid = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(
                            IntPtr.Add(pRawInputDeviceList, dwSize * i),
                            typeof(RAWINPUTDEVICELIST));

                        NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICEINFO, IntPtr.Zero, ref pSize);
                        if (pSize <= 0)
                            continue;

                        IntPtr pInfo = Marshal.AllocHGlobal((int)pSize);
                        using (new SafeUnmanagedMemoryHandle(pInfo))
                        {
                            NativeMethods.GetRawInputDeviceInfo(rid.hDevice, NativeMethods.RIDI_DEVICEINFO, pInfo, ref pSize);
                            var info = (RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RID_DEVICE_INFO));
                            switch (info.hid.usUsage)
                            {
                                case NativeMethods.TouchPadUsage:
                                    foundDevices |= Devices.TouchPad;
                                    break;
                                case NativeMethods.TouchScreenUsage:
                                    foundDevices |= Devices.TouchScreen;
                                    break;
                                case NativeMethods.PenUsage:
                                    foundDevices |= Devices.Pen;
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                }
                return foundDevices;
            }
            else
            {
                throw new ApplicationException("Error!");
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

    /// <summary>How digitizer axes map onto the screen for a display orientation.</summary>
    public struct ScreenAxisMapping
    {
        public ScreenAxisMapping(bool axisCorresponds, bool xAxisDirection, bool yAxisDirection)
        {
            AxisCorresponds = axisCorresponds;
            XAxisDirection = xAxisDirection;
            YAxisDirection = yAxisDirection;
        }

        public bool AxisCorresponds { get; }
        public bool XAxisDirection { get; }
        public bool YAxisDirection { get; }
    }
}
