using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public interface IHidReportDecoder
{
    IReadOnlyList<DeviceControl> Controls { get; }
    
    void Decode(byte[] report, List<HidInputChange> inputChanges);
}