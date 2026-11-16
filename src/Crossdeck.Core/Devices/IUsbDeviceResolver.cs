using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Resolves the USB composite device that owns a HID interface.
/// </summary>
public interface IUsbDeviceResolver
{
    /// <summary>
    /// Resolves the composite device that owns the HID interface at <paramref name="hidPath"/>.
    /// </summary>
    /// <param name="hidPath">The HID device path from the platform HID enumeration.</param>
    /// <returns>The composite device, or <see langword="null"/> when the owning USB device cannot be resolved.</returns>
    UsbDeviceDescriptor? Resolve(string hidPath);
}
