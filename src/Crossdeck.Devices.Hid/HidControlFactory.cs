using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using HidSharp.Reports;

namespace Crossdeck.Devices.Hid;

internal sealed class HidControlFactory
{
    private const uint GenericDesktop = 0x01;
    private const uint Simulation = 0x02;
    private const uint Button = 0x09;
    private const uint HatSwitch = 0x39;

    private static readonly string[] DesktopAxes = ["X", "Y", "Z", "RX", "RY", "RZ", "Slider", "Dial", "Wheel"];

    private readonly Dictionary<string, int> _seen = new();

    public DeviceControl? Create(uint usage, DataItem item)
    {
        var id = usage & 0xFFFF;

        return HidUsage.FromRaw(usage) switch
        {
            (Button, > 0) => CreateButton(id),
            (GenericDesktop, HatSwitch) => CreateHat(item),
            (GenericDesktop, >= 0x30 and <= 0x38) => CreateAxis(DesktopAxes[id - 0x30], item),
            (Simulation, 0xB0) => CreateAxis("Aileron", item),
            (Simulation, 0xB8) => CreateAxis("Elevator", item),
            (Simulation, 0xBA) => CreateAxis("Rudder", item),
            (Simulation, 0xBB) => CreateAxis("Throttle", item),
            (Simulation, 0xBF) => CreateAxis("Toe Brake", item),
            _ => null
        };
    }

    private ButtonControl CreateButton(uint id)
    {
        var (inputId, n) = Next(InputKind.Button, id.ToString());
        return new ButtonControl(inputId, id, n == 1 ? $"Button {id}" : $"Button {id} ({n})");
    }

    private HatControl CreateHat(DataItem item)
    {
        var n = _seen["hat"] = _seen.GetValueOrDefault("hat") + 1;
        return new HatControl(new InputId(InputKind.Hat, n.ToString()), $"Hat {n}",
            item.LogicalMaximum - item.LogicalMinimum + 1);
    }

    private AxisControl CreateAxis(string name, DataItem item)
    {
        var (inputId, n) = Next(InputKind.Axis, name.Replace(" ", "").ToLowerInvariant());
        var (min, max) = Range(item);
        return new AxisControl(inputId, n == 1 ? $"{name} axis" : $"{name} axis {n}", min, max);
    }

    private (InputId Id, int N) Next(InputKind kind, string key)
    {
        var slot = $"{kind}:{key}";
        var n = _seen[slot] = _seen.GetValueOrDefault(slot) + 1;
        return (new InputId(kind, n == 1 ? key : $"{key}#{n}"), n);
    }

    private static (double Min, double Max) Range(DataItem item) =>
        item is { PhysicalMinimum: 0, PhysicalMaximum: 0 }
            ? (item.LogicalMinimum, item.LogicalMaximum)
            : (item.PhysicalMinimum, item.PhysicalMaximum);
}