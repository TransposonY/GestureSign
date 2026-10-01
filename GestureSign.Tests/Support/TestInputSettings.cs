using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Tests.Support
{
    /// <summary>Input settings with the daemon's AppConfig defaults, freely overridable per test.</summary>
    internal sealed class TestInputSettings : IInputSettings
    {
        public MouseActions DrawingButton { get; set; } = MouseActions.None;
        public DeviceStates PenGestureButton { get; set; } = DeviceStates.None;
        public int MinimumPointDistance { get; set; } = 20;
        public bool IsOrderByLocation { get; set; } = true;
        public int InitialTimeout { get; set; }
        public bool UiAccess { get; set; }
    }
}
