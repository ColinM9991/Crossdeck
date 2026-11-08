using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices.Display;

public interface ISegmentDisplayOutput
{
    void SetText(OutputId id, string text);
}