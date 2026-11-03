namespace Crossdeck.Devices.Hid;

internal sealed record HidConnection(IHidTransport Transport, IHidReportDecoder Decoder);