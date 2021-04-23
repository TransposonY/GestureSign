using GestureSign.Common.Input;
using GestureSign.Daemon.Native;
using SYNCTRLLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GestureSign.Daemon.Input
{
    /// <summary>
    /// Synaptics TouchPad
    /// </summary>
    public class SynTouchPad : IDisposable
    {
        private bool disposedValue;

        private SynAPICtrl _synCtrl;
        private SynDeviceCtrl _device;
        private SynPacketCtrl _packet;

        private SynchronizationContext _synchronizationContext;

        private bool _touching;
        private readonly bool _isRelative = false;
        private Screen _startScr;
        private Point _delta;
        private int _fingerDistance;
        private int _deviceHandle;
        private int _x_Lo;
        private int _x_Hi;
        private int _y_Lo;
        private int _y_Hi;
        private int _xPhysicalWide;
        private int _yPhysicalWide;

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public bool IsAvailable
        {
            get
            {
                return _synCtrl != null;
            }
        }

        public SynTouchPad()
        {
            try
            {
                _synCtrl = new SynAPICtrl();
                _synCtrl.Initialize();
                _synCtrl.Activate();

                int deviceHandle = _synCtrl.FindDevice(SynConnectionType.SE_ConnectionAny, SynDeviceType.SE_DeviceTouchPad, -1);
                if (deviceHandle == -1)
                {
                    _synCtrl.Deactivate();
                    _synCtrl = null;
                    return;
                }

                _device = new SynDeviceCtrl();
                _packet = new SynPacketCtrl();
            }
            catch
            {
                _packet = null;
                _device = null;
                _synCtrl = null;
            }
        }

        public void Initialize()
        {
            _deviceHandle = _synCtrl.FindDevice(SynConnectionType.SE_ConnectionAny, SynDeviceType.SE_DeviceTouchPad, -1);
            if (_deviceHandle == -1)
                return;
            _device.Select(_deviceHandle);
            _device.Activate();
            _device.OnPacket += SynDevice_OnPacket;

            _x_Lo = _device.GetLongProperty(SynDeviceProperty.SP_XLoSensor);
            _x_Hi = _device.GetLongProperty(SynDeviceProperty.SP_XHiSensor);
            _y_Lo = _device.GetLongProperty(SynDeviceProperty.SP_YLoSensor);
            _y_Hi = _device.GetLongProperty(SynDeviceProperty.SP_YHiSensor);
            _xPhysicalWide = _x_Hi - _x_Lo;
            _yPhysicalWide = _y_Hi - _y_Lo;

            _fingerDistance = 50 * DpiHelper.GetSystemDpi() / 96;
            _synchronizationContext = SynchronizationContext.Current;
        }

        private void SynDevice_OnPacket()
        {
            _device.LoadPacket(_packet);
            bool isTouching = (_packet.FingerState & (int)SynFingerFlags.SF_FingerTouch) != 0;
            if (isTouching)
            {
                if (!_touching)
                {
                    Point cur = Cursor.Position;
                    _startScr = Screen.FromPoint(cur);
                    if (_startScr == null)
                        return;
                    if (_isRelative)
                    {
                        Point p = GetPoint();
                        _delta.X = p.X - cur.X;
                        _delta.Y = p.Y - cur.Y;
                    }
                    _touching = true;
                }
                Point point = GetPoint();
                int fingerCount = _packet.GetLongProperty(SynPacketProperty.SP_ExtraFingerState) & 7;
                PostPointMessage(new RawPointsDataMessageEventArgs(SimulateMultiTouchInput(point, fingerCount), Devices.TouchPad));
            }
            else
            {
                if (_touching)
                {
                    Point p = GetPoint();
                    PostPointMessage(new RawPointsDataMessageEventArgs(new List<RawData>() { new RawData(DeviceStates.None, 0, p) }, Devices.TouchPad));
                    _touching = false;
                    _delta = Point.Empty;
                    _startScr = null;
                }
                return;
            }

        }

        private void PostPointMessage(RawPointsDataMessageEventArgs eventArgs)
        {
            _synchronizationContext.Send(o => PointsIntercepted?.Invoke(this, eventArgs), null);
        }

        private Point GetPoint()
        {
            int physicalX = _packet.X - _x_Lo;
            int physicalY = _y_Hi - _packet.Y;

            int x = physicalX * _startScr.Bounds.Width / _xPhysicalWide;
            int y = physicalY * _startScr.Bounds.Height / _yPhysicalWide;
            x += _startScr.Bounds.X - _delta.X;
            y += _startScr.Bounds.Y - _delta.Y;

            return new Point(x, y);
        }

        private List<RawData> SimulateMultiTouchInput(Point p, int fingerCount)
        {
            var rawDataList = new List<RawData>(fingerCount);
            for (int i = 0; i < fingerCount; i++)
            {
                rawDataList.Add(new RawData(DeviceStates.Tip, i, new Point(p.X + i * _fingerDistance, p.Y)));
            }
            return rawDataList;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_device != null)
                    {
                        _device.Deactivate();
                        _device.OnPacket -= SynDevice_OnPacket;
                    }
                    _synCtrl?.Deactivate();
                    //Marshal.ReleaseComObject(_device);
                }
                _packet = null;
                _device = null;
                _synCtrl = null;

                disposedValue = true;
            }
        }

        ~SynTouchPad()
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
