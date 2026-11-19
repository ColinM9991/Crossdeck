namespace Crossdeck.Core.Simulators.Models;

public closed record SimValue
{
    public sealed record None : SimValue;

    public sealed record String(string Value) : SimValue;

    public sealed record Number(double Value) : SimValue;

    public sealed record Array(IReadOnlyList<SimValue> Values) : SimValue;

    public sealed record NumberArray(IReadOnlyList<double> Values) : SimValue;
}