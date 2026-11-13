namespace Crossdeck.Devices.Moza.Channels;

internal enum SettingReadResult
{
    Incomplete,
    Frame,
    Skipped,
    BadCrc
}