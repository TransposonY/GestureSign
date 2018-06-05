using System;
using System.Collections.Generic;
using System.Linq;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Daemon.Input
{
    public class PointEventTranslator
    {
        private int _lastPointsCount;
        private HashSet<MouseActions> _pressedMouseButton;

        internal Devices SourceDevice { get; private set; }

        internal PointEventTranslator(InputProvider inputProvider)
        {
            _pressedMouseButton = new HashSet<MouseActions>();
            inputProvider.PointsIntercepted += TranslateTouchEvent;
            inputProvider.LowLevelMouseHook.MouseDown += LowLevelMouseHook_MouseDown;
            inputProvider.LowLevelMouseHook.MouseMove += LowLevelMouseHook_MouseMove;
            inputProvider.LowLevelMouseHook.MouseUp += LowLevelMouseHook_MouseUp;
        }

        #region Custom Events

        public event EventHandler<InputPointsEventArgs> PointDown;

        protected virtual void OnPointDown(InputPointsEventArgs args)
        {
            if (SourceDevice != Devices.None && SourceDevice != args.PointSource && args.PointSource != Devices.Pen) return;
            SourceDevice = args.PointSource;
            PointDown?.Invoke(this, args);
        }

        public event EventHandler<InputPointsEventArgs> PointUp;

        protected virtual void OnPointUp(InputPointsEventArgs args)
        {
            if (SourceDevice != Devices.None && SourceDevice != args.PointSource) return;

            PointUp?.Invoke(this, args);

            SourceDevice = Devices.None;
        }

        public event EventHandler<InputPointsEventArgs> PointMove;

        protected virtual void OnPointMove(InputPointsEventArgs args)
        {
            if (SourceDevice != args.PointSource) return;
            PointMove?.Invoke(this, args);
        }

        #endregion

        #region Private Methods

        private void LowLevelMouseHook_MouseUp(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            if ((MouseActions)mouseMessage.Button == AppConfig.DrawingButton)
            {
                var args = new InputPointsEventArgs(new List<InputPoint>(new[] { new InputPoint(1, mouseMessage.Point) }), Devices.Mouse);
                OnPointUp(args);
                handled = args.Handled;
            }
            _pressedMouseButton.Remove((MouseActions)mouseMessage.Button);
        }

        private void LowLevelMouseHook_MouseMove(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            var args = new InputPointsEventArgs(new List<InputPoint>(new[] { new InputPoint(1, mouseMessage.Point) }), Devices.Mouse);
            OnPointMove(args);
        }

        private void LowLevelMouseHook_MouseDown(LowLevelMouseMessage mouseMessage, ref bool handled)
        {
            if ((MouseActions)mouseMessage.Button == AppConfig.DrawingButton && _pressedMouseButton.Count == 0)
            {
                var args = new InputPointsEventArgs(new List<InputPoint>(new[] { new InputPoint(1, mouseMessage.Point) }), Devices.Mouse);
                OnPointDown(args);
                handled = args.Handled;
            }
            _pressedMouseButton.Add((MouseActions)mouseMessage.Button);
        }

        private void TranslateTouchEvent(object sender, RawPointsDataMessageEventArgs e)
        {
            if ((e.SourceDevice & Devices.TouchDevice) != 0)
            {
                int releaseCount = e.RawData.Count(rtd => rtd.State == 0);
                if (releaseCount != 0)
                {
                    if (e.RawData.Count <= _lastPointsCount)
                    {
                        OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                        _lastPointsCount = _lastPointsCount - releaseCount;
                    }
                    return;
                }

                if (e.RawData.Count > _lastPointsCount)
                {
                    if (PointCapture.Instance.InputPoints.Any(p => p.Count > 10))
                    {
                        OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                        return;
                    }
                    _lastPointsCount = e.RawData.Count;
                    OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                }
                else if (e.RawData.Count == _lastPointsCount)
                {
                    OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                }
            }
            else if (e.SourceDevice == Devices.Pen)
            {
                bool release = (e.RawData[0].State & (DeviceStates.Invert | DeviceStates.RightClickButton)) == 0 || (e.RawData[0].State & DeviceStates.InRange) == 0;
                bool tip = (e.RawData[0].State & (DeviceStates.Eraser | DeviceStates.Tip)) != 0;

                if (release)
                {
                    OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                    _lastPointsCount = 0;
                    return;
                }

                var penSetting = AppConfig.PenGestureButton;
                bool drawByTip = (penSetting & DeviceStates.Tip) != 0;
                bool drawByHover = (penSetting & DeviceStates.InRange) != 0;

                if (drawByHover && drawByTip)
                {
                    if (_lastPointsCount == 1 && SourceDevice == Devices.Pen)
                    {
                        OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                    }
                    else if (_lastPointsCount >= 0)
                    {
                        _lastPointsCount = 1;
                        OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                    }
                }
                else if (drawByTip)
                {
                    if (!tip)
                    {
                        if (SourceDevice == Devices.Pen)
                        {
                            OnPointUp(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                            _lastPointsCount = 0;
                        }
                        return;
                    }

                    if (_lastPointsCount == 1 && SourceDevice == Devices.Pen)
                    {
                        OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                    }
                    else if (_lastPointsCount >= 0)
                    {
                        _lastPointsCount = 1;
                        OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                    }
                }
                else if (drawByHover)
                {
                    if (_lastPointsCount == 1 && SourceDevice == Devices.Pen)
                    {
                        if (tip)
                        {
                            OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                            _lastPointsCount = -1;
                        }
                        else
                        {
                            OnPointMove(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                        }
                    }
                    else if (_lastPointsCount >= 0)
                    {
                        if (tip)
                        {
                            _lastPointsCount = -1;
                            return;
                        }
                        _lastPointsCount = 1;
                        OnPointDown(new InputPointsEventArgs(e.RawData, e.SourceDevice));
                    }
                }
            }
        }

        #endregion
    }
}
