namespace Crossdeck.Core.Devices.Models;

public sealed record DriverAddedEventArgs(DeviceIdentity Identity, IDeviceDriver Driver);