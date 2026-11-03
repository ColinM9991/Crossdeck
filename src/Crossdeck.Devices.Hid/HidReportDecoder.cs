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
        var controls = new List<DeviceControl>();
        var factory = new HidControlFactory();

        _reportDescriptor = reportDescriptor;
        _inputParsers = [.. _reportDescriptor.DeviceItems.Select(x => CreateDeviceInputParser(x, controls, factory))];

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

                var inputChange = new HidInputChange(control.Id, val.GetPhysicalValue());
                inputChanges.Add(inputChange);
            }
        }
    }

    private static DeviceItemInputParserWithControls CreateDeviceInputParser(
        DeviceItem item,
        List<DeviceControl> controls,
        HidControlFactory factory)
    {
        var parser = item.CreateDeviceItemInputParser();
        var map = new DeviceControl?[parser.ValueCount];

        for (var x = 0; x < parser.ValueCount; x++)
        {
            var value = parser.GetValue(x);
            var usage = value.Usages.FirstOrDefault();
            var control = factory.Create(usage, value.DataItem);

            map[x] = control;
            if (control != null) controls.Add(control);
        }

        return new DeviceItemInputParserWithControls(parser, map);
    }

    private sealed record DeviceItemInputParserWithControls(DeviceItemInputParser Parser, DeviceControl?[] Controls);
}