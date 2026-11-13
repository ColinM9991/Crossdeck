using Crossdeck.Core.Devices.Display.Models;

namespace Crossdeck.Devices.Moza;

internal record SessionEvent;

internal sealed record ScreenChangedEvent(TextScreen Screen) : SessionEvent;

internal sealed record ButtonChangedEvent(int Button, bool Pressed) : SessionEvent;

internal sealed record ReaderStoppedEvent(Exception? Error) : SessionEvent;