namespace Crossdeck.Core.Devices.Models;

public enum InputKind { Button, Axis, Hat, Encoder, Switch }

public readonly record struct InputId(InputKind Kind, string Name)
{
    public static InputId Parse(string text) =>
        TryParse(text, out var id) ? id : throw new FormatException($"'{text}' is not a valid InputId");

    public static bool TryParse(string? text, out InputId id)
    {
        id = default;
        if (text is null)
            return false;

        var separator = text.IndexOf(':');
        if (separator < 0 || separator == text.Length - 1)
            return false;

        if (!Enum.TryParse<InputKind>(text[..separator], true, out var kind) || !Enum.IsDefined(kind))
            return false;

        id = new InputId(kind, text[(separator + 1)..]);
        return true;
    }

    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Name}";
}
