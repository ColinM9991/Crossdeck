using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Core.Devices.Display;

public interface ITextDisplayOutput
{
    void Show(TextScreen screen);
}