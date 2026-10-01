using System;
using System.Drawing;
using GestureSign.Common.Input;

namespace GestureSign.Daemon.Input
{
    /// <summary>
    /// Replacement for Windows' touchpad tap-to-click: a quick, still, single-finger touchpad tap produces a primary click.
    /// Anything involving a second contact (e.g. one finger resting while another taps) never clicks, so it stays a
    /// pure GestureSign gesture.
    /// </summary>
    internal sealed class TouchPadTapClicker
    {
        /// <summary>Longest touch, in milliseconds, still treated as a tap.</summary>
        internal const int MaxTapDuration = 250;

        /// <summary>
        /// Largest drift still treated as a tap, as a fraction of the touchpad's extent per axis
        /// (touchpad coordinates are mapped onto the whole screen under the cursor).
        /// </summary>
        internal const double MaxTapMovement = 0.025;

        private readonly IRawInputEnvironment _environment;
        private readonly Action _click;

        private bool _active;
        private bool _disqualified;
        private int _startTick;
        private int _contactId;
        private Point _startPoint;
        private Size _screenSize;

        /// <param name="click">Performs the primary click; called on the raw-input thread.</param>
        public TouchPadTapClicker(IRawInputEnvironment environment, Action click)
        {
            _environment = environment;
            _click = click;
        }

        /// <summary>Feeds one frame as emitted by <see cref="RawInputProcessor.PointsIntercepted"/>.</summary>
        public void Process(RawPointsDataMessageEventArgs e)
        {
            if (e.SourceDevice != Devices.TouchPad)
                return;

            bool anyTip = false;
            foreach (RawData contact in e.RawData)
            {
                if ((contact.State & DeviceStates.Tip) == 0)
                    continue;

                if (!anyTip && !_active)
                    Begin(contact);
                anyTip = true;

                if (contact.ContactIdentifier != _contactId || HasMoved(contact.RawPoints))
                    _disqualified = true;
            }

            if (!_active)
                return;

            if (e.ButtonDown)
                _disqualified = true;

            if (anyTip)
                return;

            _active = false;
            if (!_disqualified && _environment.TickCount - _startTick <= MaxTapDuration)
                _click();
        }

        private void Begin(RawData contact)
        {
            _active = true;
            _startTick = _environment.TickCount;
            _contactId = contact.ContactIdentifier;
            _startPoint = contact.RawPoints;

            Rectangle? screen = _environment.CurrentScreenBounds;
            _disqualified = screen == null;
            _screenSize = screen?.Size ?? Size.Empty;
        }

        private bool HasMoved(Point point)
        {
            return Math.Abs(point.X - _startPoint.X) > _screenSize.Width * MaxTapMovement
                || Math.Abs(point.Y - _startPoint.Y) > _screenSize.Height * MaxTapMovement;
        }
    }
}
