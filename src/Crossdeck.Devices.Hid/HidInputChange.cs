using Crossdeck.Core.Devices;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid;

public readonly record struct HidInputChange(InputId Id, double Value);