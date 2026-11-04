using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Hid;

public readonly record struct HidInputChange(DeviceControl DeviceControl, double Value);