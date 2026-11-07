using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid;

public interface IHidReportEncoder
{
    IReadOnlyList<HidOutputReport> Encode(IReadOnlyDictionary<OutputId, int> state);

    void Reset();
}