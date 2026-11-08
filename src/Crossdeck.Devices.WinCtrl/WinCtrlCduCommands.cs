namespace Crossdeck.Devices.WinCtrl;

internal static class WinCtrlCduCommands
{
    public const int RefreshLcd = 0x0103;
    public const int TestPattern = 0x0104;
    public const int Fence = 0x0105;
    public const int FontHead = 0x0106;
    public const int FontData = 0x0107;
    public const int ClearFonts = 0x0108;
    public const int ListFontVersions = 0x0109;
    public const int ListFontsFlash = 0x010A;
    public const int FillRectangle = 0x0110;
    public const int SelectFont = 0x0111;
    public const int FontColour = 0x0112;
    public const int BackgroundColour = 0x0113;
    public const int DrawString = 0x0114;
    public const int TextGrid = 0x0118;
    public const int AddStyle = 0x0119;
    public const int StyleIndexWidth = 0x011A;
    public const int ScreenLogicVersion = 0x011B;
    public const int BuildStyleTable = 0x011C;
    public const int DrawDebugGrid = 0x011D;
    public const int ClearStyleTable = 0x011E;
}

internal static class WinCtrlCduStyleFeatures
{
    public const ushort Font = 1;
    public const ushort TextColour = 2;
    public const ushort BackgroundColour = 3;
    public const ushort Operations = 4;
}

internal static class WinCtrlCduReports
{
    public const int Setup = 0xF0;
    public const int Screen = 0xF2;
}