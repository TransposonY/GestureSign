using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using ManagedWinapi.Hooks;

namespace GestureSign.Daemon.Input
{
    /// <summary>
    /// Settings read by the input pipeline. Production reads <see cref="AppConfig"/> live on every access
    /// (settings can change at runtime); tests supply fixed values.
    /// </summary>
    internal interface IInputSettings
    {
        MouseActions DrawingButton { get; }
        DeviceStates PenGestureButton { get; }
        int MinimumPointDistance { get; }
        bool IsOrderByLocation { get; }
        int InitialTimeout { get; }
        bool UiAccess { get; }
    }

    internal sealed class AppConfigInputSettings : IInputSettings
    {
        public static readonly AppConfigInputSettings Instance = new AppConfigInputSettings();

        private AppConfigInputSettings() { }

        public MouseActions DrawingButton => AppConfig.DrawingButton;
        public DeviceStates PenGestureButton => AppConfig.PenGestureButton;
        public int MinimumPointDistance => AppConfig.MinimumPointDistance;
        public bool IsOrderByLocation => AppConfig.IsOrderByLocation;
        public int InitialTimeout => AppConfig.InitialTimeout;
        public bool UiAccess => AppConfig.UiAccess;
    }
}
