using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Input;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Input
{
    /// <summary>Raw-input device queries (GetRawInputDeviceInfo) needed to decode WM_INPUT.</summary>
    internal interface IRawInputDeviceSource
    {
        /// <summary>RIDI_DEVICEINFO; false when the device reports no info.</summary>
        bool TryGetDeviceInfo(IntPtr hDevice, out RID_DEVICE_INFO info);

        /// <summary>RIDI_DEVICENAME; false when the device reports no name.</summary>
        bool TryGetDeviceName(IntPtr hDevice, out string name);

        /// <summary>RIDI_PREPARSEDDATA copied into unmanaged memory owned by the caller.</summary>
        SafeUnmanagedMemoryHandle GetPreparsedData(IntPtr hDevice);
    }

    /// <summary>Machine state sampled while decoding WM_INPUT.</summary>
    internal interface IRawInputEnvironment
    {
        int TickCount { get; }

        /// <summary>Bounds of the screen under the cursor, or null when there is none.</summary>
        Rectangle? CurrentScreenBounds { get; }

        ScreenOrientation ScreenOrientation { get; }
    }

    /// <summary>
    /// Decodes digitizer WM_INPUT payloads (touch screen, touch pad, pen) into <see cref="RawData"/> frames
    /// and arbitrates which device currently owns the gesture.
    /// </summary>
    internal sealed class RawInputProcessor
    {
        private readonly IRawInputDeviceSource _deviceSource;
        private readonly IRawInputEnvironment _environment;

        private Rectangle _currentScreenBounds;
        private ScreenAxisMapping _axisMapping;

        private List<RawData> _outputTouchs = new List<RawData>(1);
        private int _requiringContactCount;
        private bool _touchpadButtonDown;
        private readonly Dictionary<IntPtr, ushort> _validDevices = new Dictionary<IntPtr, ushort>();

        private Devices _sourceDevice;
        private int? _penLastActivity;
        private bool _ignoreTouchInputWhenUsingPen;
        private DeviceStates _penGestureButton;

        public event RawPointsDataMessageEventHandler PointsIntercepted;

        public RawInputProcessor(IRawInputDeviceSource deviceSource, IRawInputEnvironment environment)
        {
            _deviceSource = deviceSource;
            _environment = environment;
        }

        /// <summary>Applies pen settings and forgets validated devices (called on registration changes).</summary>
        public void UpdateSettings(bool ignoreTouchInputWhenUsingPen, DeviceStates penGestureButton)
        {
            _ignoreTouchInputWhenUsingPen = ignoreTouchInputWhenUsingPen;
            _penGestureButton = penGestureButton;
            _validDevices.Clear();
        }

        /// <summary>WM_INPUT_DEVICE_CHANGE: devices must be validated again.</summary>
        public void ResetDevices()
        {
            _validDevices.Clear();
        }

        private bool ValidateDevice(IntPtr hDevice, out ushort usage)
        {
            usage = 0;
            RID_DEVICE_INFO info;
            if (!_deviceSource.TryGetDeviceInfo(hDevice, out info))
                return false;

            switch (info.hid.usUsage)
            {
                case NativeMethods.TouchPadUsage:
                case NativeMethods.TouchScreenUsage:
                case NativeMethods.PenUsage:
                    break;
                default:
                    return true;
            }

            string deviceName;
            if (!_deviceSource.TryGetDeviceName(hDevice, out deviceName))
                return false;

            if (string.IsNullOrEmpty(deviceName) || deviceName.IndexOf("VIRTUAL_DIGITIZER", StringComparison.OrdinalIgnoreCase) >= 0 || deviceName.IndexOf("ROOT", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            usage = info.hid.usUsage;
            return true;
        }

        private bool TryUpdateCurrentScreen()
        {
            Rectangle? bounds = _environment.CurrentScreenBounds;
            if (bounds == null)
                return false;
            _currentScreenBounds = bounds.Value;
            return true;
        }

        /// <summary>Processes one RAWINPUT structure as returned by GetRawInputData(RID_INPUT).</summary>
        public void Process(IntPtr buffer)
        {
            RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));

            ushort usage;
            if (!_validDevices.TryGetValue(raw.header.hDevice, out usage))
            {
                if (ValidateDevice(raw.header.hDevice, out usage))
                    _validDevices.Add(raw.header.hDevice, usage);
            }

            if (usage == 0)
                return;
            if (usage == NativeMethods.PenUsage)
            {
                if (_ignoreTouchInputWhenUsingPen)
                    _penLastActivity = _environment.TickCount;
                else
                    _penLastActivity = null;

                if (_penGestureButton == 0)
                    return;

                switch (_sourceDevice)
                {
                    case Devices.TouchScreen:
                    case Devices.None:
                    case Devices.Pen:
                        break;
                    default:
                        return;
                }

                using (PenDevice penDevice = new PenDevice(buffer, ref raw, _deviceSource.GetPreparsedData(raw.header.hDevice)))
                {
                    DeviceStates state = penDevice.GetPenState();

                    if (_sourceDevice == Devices.None || _sourceDevice == Devices.TouchScreen)
                    {
                        if ((state & _penGestureButton) != 0)
                        {
                            if (!TryUpdateCurrentScreen())
                                return;
                            _sourceDevice = Devices.Pen;
                            _axisMapping = HidDevice.GetScreenAxisMapping(_environment.ScreenOrientation);
                        }
                        else
                            return;
                    }
                    else if (_sourceDevice == Devices.Pen)
                    {
                        if ((state & _penGestureButton) == 0 || (state & DeviceStates.InRange) == 0)
                        {
                            state = DeviceStates.None;
                        }
                    }
                    penDevice.GetPhysicalMax(1);
                    penDevice.AxisMapping = _axisMapping;
                    Point point = penDevice.GetCoordinate(0, _currentScreenBounds);
                    _outputTouchs = new List<RawData>(1) { new RawData(state, 0, point) };
                }
            }
            else if (usage == NativeMethods.TouchScreenUsage)
            {
                if (_penLastActivity != null && _environment.TickCount - _penLastActivity < 100)
                    return;
                if (_sourceDevice == Devices.None)
                {
                    if (!TryUpdateCurrentScreen())
                        return;
                    _sourceDevice = Devices.TouchScreen;
                    _axisMapping = HidDevice.GetScreenAxisMapping(_environment.ScreenOrientation);
                }
                else if (_sourceDevice != Devices.TouchScreen)
                    return;

                using (TouchScreenDevice touchScreen = new TouchScreenDevice(buffer, ref raw, _deviceSource.GetPreparsedData(raw.header.hDevice)))
                {
                    int contactCount = touchScreen.GetContactCount();
                    HidNativeApi.HIDP_LINK_COLLECTION_NODE[] linkCollection = touchScreen.GetLinkCollectionNodes();
                    touchScreen.GetPhysicalMax(linkCollection.Length);
                    touchScreen.AxisMapping = _axisMapping;

                    if (contactCount != 0)
                    {
                        _requiringContactCount = contactCount;
                        _outputTouchs = new List<RawData>(contactCount);
                    }
                    if (_requiringContactCount == 0) return;

                    touchScreen.GetRawDatas(linkCollection[0].NumberOfChildren, _currentScreenBounds, ref _requiringContactCount, ref _outputTouchs);
                }
            }
            else if (usage == NativeMethods.TouchPadUsage)
            {
                if (_sourceDevice == Devices.None)
                {
                    if (!TryUpdateCurrentScreen())
                        return;
                    _sourceDevice = Devices.TouchPad;
                }
                else if (_sourceDevice != Devices.TouchPad)
                    return;

                using (TouchPadDevice touchPad = new TouchPadDevice(buffer, ref raw, _deviceSource.GetPreparsedData(raw.header.hDevice)))
                {
                    int contactCount = touchPad.GetContactCount();
                    HidNativeApi.HIDP_LINK_COLLECTION_NODE[] linkCollection = touchPad.GetLinkCollectionNodes();
                    touchPad.GetPhysicalMax(linkCollection.Length);

                    if (contactCount != 0)
                    {
                        _requiringContactCount = contactCount;
                        _outputTouchs = new List<RawData>(contactCount);
                        _touchpadButtonDown = false;
                    }
                    if (_requiringContactCount == 0) return;

                    touchPad.GetRawDatas(linkCollection[0].NumberOfChildren, _currentScreenBounds, ref _requiringContactCount, ref _outputTouchs, ref _touchpadButtonDown);
                }
            }

            if (_requiringContactCount == 0 && PointsIntercepted != null)
            {
                PointsIntercepted(this, new RawPointsDataMessageEventArgs(_outputTouchs, _sourceDevice)
                {
                    ButtonDown = _sourceDevice == Devices.TouchPad && _touchpadButtonDown
                });
                if (_outputTouchs.TrueForAll(rd => rd.State == DeviceStates.None))
                {
                    _sourceDevice = Devices.None;
                }
            }
        }
    }
}
