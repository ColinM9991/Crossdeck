using Crossdeck.Core.Devices;
using HidSharp.Reports;
using HidSharp.Reports.Input;

namespace Crossdeck.Devices.Hid;

internal sealed class HidReportDecoder : IHidReportDecoder
{
    private static readonly string[] AxisNames = ["X", "Y", "Z", "RX", "RY", "RZ", "Slider", "Dial", "Wheel"];

    private readonly ReportDescriptor _reportDescriptor;
    private readonly IReadOnlyCollection<DeviceItemInputParserWithControls> _inputParsers;

    public HidReportDecoder(
        ReportDescriptor reportDescriptor)
    {
        var seen = new Dictionary<InputKind, int>();
        var controls = new List<DeviceControl>();
        
        _reportDescriptor = reportDescriptor;
        _inputParsers = [.. _reportDescriptor.DeviceItems.Select(x => CreateDeviceInputParser(x, controls, seen))];

        Controls = controls;
    }

    public IReadOnlyList<DeviceControl> Controls { get; }

    public void Decode(byte[] report, List<HidInputChange> inputChanges)
    {
        if (!_reportDescriptor.TryGetReport(ReportType.Input, report[0], out var r)) return;

        foreach (var (parser, controls) in _inputParsers)
        {
            if (!parser.TryParseReport(report, 0, r)) continue;

            while (parser.HasChanged)
            {
                var index = parser.GetNextChangedIndex();
                if (controls[index] is not { } control)
                    continue;

                var val = parser.GetValue(index);
                var usage = val.Usages.FirstOrDefault();

                var inputChange = new HidInputChange(control.Id, val.GetPhysicalValue());
                inputChanges.Add(inputChange);
            }
        }
    }

    private static DeviceItemInputParserWithControls CreateDeviceInputParser(
        DeviceItem item,
        List<DeviceControl> controls,
        Dictionary<InputKind, int> seen)
    {
        var parser = item.CreateDeviceItemInputParser();
        var map = new DeviceControl?[parser.ValueCount];

        for (var x = 0; x < parser.ValueCount; x++)
        {
            var value = parser.GetValue(x);
            var usage = value.Usages.FirstOrDefault();
            var control = CreateControl(usage, value.DataItem, seen);

            map[x] = control;
            if (control != null) controls.Add(control);
        }

        return new DeviceItemInputParserWithControls(parser, map);
    }

    private static DeviceControl? CreateControl(uint usage, DataItem item, Dictionary<InputKind, int> seen)
    {
        var page = usage >> 16;
        var id = usage & 0xFFFF;

        return (page, id) switch
        {
            (0x09, > 0) => new ButtonControl(Next(InputKind.Button, seen), $"Button {id}"),
            (0x01, 0x39) => new HatControl(Next(InputKind.Hat, seen), $"Hat {id}",
                item.LogicalMaximum - item.LogicalMinimum + 1),
            (0x01, >= 0x30 and <= 0x38) => new AxisControl(Next(InputKind.Axis, seen),
                AxisNames[id - 0x30], item.PhysicalMinimum, item.PhysicalMaximum),
            _ => null,
        };
    }

    private static InputId Next(InputKind kind, Dictionary<InputKind, int> seen)
    {
        var n = seen[kind] = seen.GetValueOrDefault(kind) + 1;
        return new InputId(kind, $"#{n}");
    }

    private sealed record DeviceItemInputParserWithControls(DeviceItemInputParser Parser, DeviceControl?[] Controls);
}