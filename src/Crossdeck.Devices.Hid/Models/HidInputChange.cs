using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Devices.Hid.Models;

public readonly record struct HidInputChange(DeviceControl DeviceControl, double Value);