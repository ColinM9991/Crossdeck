namespace Crossdeck.Devices.Hid;

public readonly record struct HidUsage(uint Page, uint Id)
{
    public static HidUsage FromRaw(uint raw) => new(raw >> 16, raw & 0xFFFF);
    public uint Raw => ((uint)Page << 16) | Id;

    public static implicit operator HidUsage(uint raw) => FromRaw(raw);

    public static readonly HidUsage Joystick   = new(0x01, 0x04);
    public static readonly HidUsage Gamepad    = new(0x01, 0x05);
    public static readonly HidUsage MultiAxis  = new(0x01, 0x08);
    public static readonly HidUsage FlightSimDevice = new(0x2, 0x1);
    
    public override string ToString() => $"0x{Page:X4}_{Id:X4}";
}