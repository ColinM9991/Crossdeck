namespace Crossdeck.Devices.Moza.Protocol;

public sealed record SerialLinkFrame(byte Command, byte Pair, byte[] Payload);