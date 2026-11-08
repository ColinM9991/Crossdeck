namespace Crossdeck.Core.Devices.Models;

public enum OutputKind { Led, Level, State, Display, Flag }

public readonly record struct OutputId(OutputKind Kind, string Name);

public record DeviceOutput(OutputId Id, string Label, int? Min, int? Max);