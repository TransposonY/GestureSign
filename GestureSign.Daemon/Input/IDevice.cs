using GestureSign.Common.Input;

namespace GestureSign.Daemon.Input
{
    public interface IDevice
    {
        Devices DeviceType { get; }
    }
}
