namespace Crossdeck.Core.Devices.Models;

public enum OutputKind { Led, Level, State, Display, Flag }

public readonly record struct OutputId(OutputKind Kind, string Name)
{
    public static OutputId Parse(string text) =>
        TryParse(text, out var id) ? id : throw new FormatException($"'{text}' is not a valid OutputId");

    public static bool TryParse(string? text, out OutputId id)
    {
        id = default;
        if (text is null)
            return false;

        var separator = text.IndexOf(':');
        if (separator < 0 || separator == text.Length - 1)
            return false;

        if (!Enum.TryParse<OutputKind>(text[..separator], true, out var kind) || !Enum.IsDefined(kind))
            return false;

        id = new OutputId(kind, text[(separator + 1)..]);
        return true;
    }

    public override string ToString() => $"{Kind.ToString().ToLowerInvariant()}:{Name}";
}

public record DeviceOutput(OutputId Id, string Label, int? Min, int? Max);
