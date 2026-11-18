using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Routing;

public sealed record ScreenRenderedEventArgs(DeviceIdentity Identity, DisplayRole Role, TextScreen Screen);