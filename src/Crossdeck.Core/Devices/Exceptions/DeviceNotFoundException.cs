namespace Crossdeck.Core.Devices.Exceptions;

public sealed class DeviceNotFoundException : Exception
{
    public DeviceNotFoundException(string devicePath) : base($"Device not found at path: {devicePath}")
    {
    }
}