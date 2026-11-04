namespace Crossdeck.Core.Devices.Models;

public readonly record struct DeviceIdentity(DeviceTransportKind DeviceKind, string StableId); // "hid", serial or location id

public enum DeviceTransportKind
{
    Hid,
    Serial
}
