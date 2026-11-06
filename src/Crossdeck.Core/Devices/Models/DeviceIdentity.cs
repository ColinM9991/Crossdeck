namespace Crossdeck.Core.Devices.Models;

public readonly record struct DeviceIdentity(DeviceIdentitySource Kind, int VendorId, int ProductId, string Value)
{
    public override string ToString() => $"{VendorId:X4}:{ProductId:X4}:{Kind.ToString().ToLowerInvariant()}={Value}";
}

public enum DeviceIdentitySource { Serial, Usage, Location }

public enum DeviceKind
{
    Hid,
    Serial
}
