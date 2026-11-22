using Crossdeck.Testing.Profiles;

namespace Crossdeck.Application.Tests.Profiles.Validation;

internal static class ValidationPaths
{
    public static readonly string Slot = $"Devices/{ProfileBuilder.SlotKey}.Value";

    public static readonly string Input = $"{Slot}.Inputs/{ProfileBuilder.InputKey}.Value";

    public static readonly string DeviceOutput = $"{Slot}.Outputs/{ProfileBuilder.DeviceOutputKey}.Value";

    public static readonly string RootOutput = $"Outputs/{ProfileBuilder.RootOutputKey}.Value";

    public static string Event(string name) => $"{Input}.Events/{name}.Value";

    public static string Action(string eventName, int index) => $"{Event(eventName)}.Actions[{index}]";
}
