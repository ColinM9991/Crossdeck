using Crossdeck.Core.Devices;

namespace Crossdeck.Devices.Hid;

public interface IHidReportDecoder
{
    IReadOnlyList<DeviceControl> Controls { get; }
    
    void Decode(byte[] report, List<HidInputChange> inputChanges);
}