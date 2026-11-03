namespace Crossdeck.Core.Devices.Exceptions;

public sealed class DeviceNotFoundException : Exception
{
    public DeviceNotFoundException(string devicePath) : base($"Device not found at path: {devicePath}")
    {
    }
}

public sealed class DeviceNotUsableException : Exception
{
    public DeviceNotUsableException(string devicePath) : base($"Device at path: {devicePath} is not usable")
    {
    }
}