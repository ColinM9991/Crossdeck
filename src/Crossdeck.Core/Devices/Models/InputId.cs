namespace Crossdeck.Core.Devices.Models;

public enum InputKind { Button, Axis, Hat, Encoder, Switch }

public readonly record struct InputId(InputKind Kind, string Name)
{
    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Name}";
}
