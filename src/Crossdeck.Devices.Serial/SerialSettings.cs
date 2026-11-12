namespace Crossdeck.Devices.Serial;

public sealed record SerialSettings(
    string PortName,
    int BaudRate,
    bool DtrEnabled,
    bool RtsEnabled,
    int ReadTimeout = 50,
    int WriteTimeout = 1_000);