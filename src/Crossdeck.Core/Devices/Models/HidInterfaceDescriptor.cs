namespace Crossdeck.Core.Devices.Models;

/// <summary>
/// Describes one HID interface of a composite device.
/// </summary>
/// <param name="Key">
/// A stable per-interface identifier: the USB interface number when the device resolver supplies it,
/// otherwise a deterministic ordinal assigned by the device watcher.
/// </param>
/// <param name="Path">The platform HID device path of the interface.</param>
public sealed record HidInterfaceDescriptor(int Key, string Path);
