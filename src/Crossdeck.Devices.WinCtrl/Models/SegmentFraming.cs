namespace Crossdeck.Devices.WinCtrl.Models;

public sealed record SegmentFraming(int PayloadLength, uint WriteId, uint CommitId, bool CommitInOwnPacket);