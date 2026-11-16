namespace Crossdeck.Core.Simulators.Models;

public closed record SimStatus
{
    public sealed record Connected : SimStatus;

    public sealed record Disconnected : SimStatus;

    public sealed record Gone : SimStatus;

    public sealed record Ready : SimStatus;
}