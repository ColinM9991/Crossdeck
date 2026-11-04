using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.Hid.Models;

internal sealed record HidConnection(IHidTransport Transport, IHidReportDecoder Decoder);