using System.Globalization;

namespace Crossdeck.Core.Devices.Models;

public enum DeviceIdentitySource { Serial, Usage, Location }

public enum DeviceKind
{
    Hid,
    Serial
}

public readonly record struct DeviceIdentity(DeviceIdentitySource Kind, int VendorId, int ProductId, string Value)
{
    public static DeviceIdentity Parse(string text) =>
        TryParse(text, out var identity) ? identity : throw new FormatException($"'{text}' is not a valid DeviceIdentity");

    public static bool TryParse(string? text, out DeviceIdentity identity)
    {
        identity = default;
        if (text is null)
            return false;

        var parts = text.Split(':', 3);
        if (parts.Length != 3 || parts[0].Length != 4 || parts[1].Length != 4)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var vendorId)
            || !int.TryParse(parts[1], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var productId))
            return false;

        var separator = parts[2].IndexOf('=');
        if (separator < 0 || separator == parts[2].Length - 1)
            return false;

        if (!Enum.TryParse<DeviceIdentitySource>(parts[2][..separator], true, out var kind) || !Enum.IsDefined(kind))
            return false;

        identity = new DeviceIdentity(kind, vendorId, productId, parts[2][(separator + 1)..]);
        return true;
    }

    public override string ToString() => $"{VendorId:X4}:{ProductId:X4}:{Kind.ToString().ToLowerInvariant()}={Value}";
}
