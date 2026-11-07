using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.WinCtrl;

public sealed class WinCtrlHidReportEncoder : IHidReportEncoder
{
    private readonly WinCtrlDefinition _definition;
    private readonly Dictionary<OutputId, byte> _lastSent = new();
    
    public WinCtrlHidReportEncoder(WinCtrlDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
    
    public IReadOnlyList<HidOutputReport> Encode(IReadOnlyDictionary<OutputId, int> state)
    {
        var outputReports = new List<HidOutputReport>();
        
        foreach (var light in _definition.Lights)
        {
            if (!state.TryGetValue(light.Id, out var v)) continue;
            if (_lastSent.TryGetValue(light.Id, out var last) && last == v) continue;

            var data = new byte[14];
            data[0] = 0x02;
            data[1] = _definition.TargetLo;
            data[2] = _definition.TargetHi;
            data[5] = 0x03;
            data[6] = 0x49;
            data[7] = light.Light;
            data[8] = (byte)v;

            outputReports.Add(new HidOutputReport(HidReportType.Output, data));
            _lastSent[light.Id] = (byte)v;
        }

        return outputReports;
    }

    public void Reset() => _lastSent.Clear();
}