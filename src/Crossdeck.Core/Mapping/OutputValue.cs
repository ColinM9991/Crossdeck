namespace Crossdeck.Core.Mapping;

public closed record OutputValue
{
    public sealed record Boolean(bool Value) : OutputValue;

    public sealed record Number(double Value) : OutputValue;

    public sealed record Text(string Value) : OutputValue;
};