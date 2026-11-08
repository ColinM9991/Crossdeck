namespace Crossdeck.Devices.WinCtrl.Models;

public sealed record SegmentPanel(
    string Name,
    ushort Target,
    int BufferSize,
    IReadOnlyDictionary<string, SegmentWindow> Windows,
    IReadOnlyDictionary<string, BitAddr[]> Labels,
    SegmentFraming Framing);