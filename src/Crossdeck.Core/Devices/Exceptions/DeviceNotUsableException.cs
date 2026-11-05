namespace Crossdeck.Core.Devices.Exceptions;

public sealed class DeviceNotUsableException : Exception
{
    public DeviceNotUsableException(string devicePath) : base($"Device at path: {devicePath} is not usable")
    {
    }
}