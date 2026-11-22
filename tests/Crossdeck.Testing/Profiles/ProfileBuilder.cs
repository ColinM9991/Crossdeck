using System.Collections.Immutable;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Models;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Testing.Profiles;

/// <summary>
/// Builds a <see cref="Profile"/> that passes every validation rule, plus helpers that change one part of it.
/// </summary>
public static class ProfileBuilder
{
    public const string SlotKey = "slot-1";
    public const string VariableName = "mode";
    public const string RootOutputKey = "busPowered";
    public const string RootOutputBindingId = "binding-bus-powered";
    public const string InputBindingId = "binding-input-25";
    public const string DeviceOutputBindingId = "binding-led-exec";
    public const string PressedEvent = "Pressed";
    public const string ReleasedEvent = "Released";
    public const string SourceAlias = "bus";
    public const string SimCommand = "sim/command/test";
    public const string SimDataRef = "sim/electrical/bus";

    public static readonly InputId InputKey = new(InputKind.Button, "25");
    public static readonly OutputId DeviceOutputKey = new(OutputKind.Led, "exec");

    public static Profile ValidProfile()
    {
        var input = new InputDefinition(
            InputBindingId,
            ImmutableDictionary<string, SimRef>.Empty.Add(VariableName, new SimRef.Variable(VariableName)),
            ImmutableDictionary<string, InputEventBinding>.Empty
                .Add(PressedEvent, new InputEventBinding(
                    [new InputAction.Command(new SimRef.Plain(SimCommand))],
                    new ExpressionSpec.Raw($"[{VariableName}] > 0")))
                .Add(ReleasedEvent, new InputEventBinding(
                    [new InputAction.Command(new SimRef.Plain(SimCommand), CommandPhase.End)])));

        var deviceOutput = new OutputDefinition(
            DeviceOutputBindingId,
            ImmutableDictionary<string, SimRef>.Empty.Add(SourceAlias, new SimRef.Plain(SimDataRef)),
            new ExpressionSpec.Raw($"[{SourceAlias}] * 255"));

        var slot = new DeviceSlot(
            new DeviceModel("4098", "BB35", "PFP-3N"),
            ImmutableDictionary<InputId, InputDefinition>.Empty.Add(InputKey, input),
            ImmutableDictionary<OutputId, OutputDefinition>.Empty.Add(DeviceOutputKey, deviceOutput),
            new DeviceIdentity(DeviceIdentitySource.Serial, 0x4098, 0xBB35, "serial-1"));

        var rootOutput = new OutputDefinition(
            RootOutputBindingId,
            ImmutableDictionary<string, SimRef>.Empty.Add(SourceAlias, new SimRef.Plain(SimDataRef)),
            new ExpressionSpec.Raw($"[{SourceAlias}] > 0"));

        return new Profile(
            Guid.Parse("8f1c5a2e-3b7d-4c0a-9e64-2d5b1f7a8c30"),
            "Test profile",
            Simulator.XPlane,
            new AircraftMatch(["Boeing 737"]),
            ImmutableDictionary<string, VariableDefinition>.Empty.Add(VariableName, new VariableDefinition(new SimValue.Number(0))),
            ImmutableDictionary<string, DeviceSlot>.Empty.Add(SlotKey, slot),
            ImmutableDictionary<string, OutputDefinition>.Empty.Add(RootOutputKey, rootOutput));
    }

    public static OutputDefinition ComputedOutput(string bindingId, params string[] outputKeysRead)
    {
        var sources = ImmutableDictionary<string, SimRef>.Empty.Add(SourceAlias, new SimRef.Plain(SimDataRef));
        foreach (var key in outputKeysRead)
            sources = sources.Add(key, new SimRef.Output(key));

        return new OutputDefinition(bindingId, sources, new ExpressionSpec.Raw($"[{SourceAlias}] > 0"));
    }

    public static Profile WithSlot(this Profile profile, Func<DeviceSlot, DeviceSlot> change) =>
        profile with { Devices = profile.Devices.SetItem(SlotKey, change(profile.Devices[SlotKey])) };

    public static Profile WithInput(this Profile profile, Func<InputDefinition, InputDefinition> change) =>
        profile.WithSlot(slot => slot with { Inputs = slot.Inputs.SetItem(InputKey, change(slot.Inputs[InputKey])) });

    public static Profile WithEvent(this Profile profile, string name, Func<InputEventBinding, InputEventBinding> change) =>
        profile.WithInput(input => input with { Events = input.Events.SetItem(name, change(input.Events[name])) });

    public static Profile WithDeviceOutput(this Profile profile, Func<OutputDefinition, OutputDefinition> change) =>
        profile.WithSlot(slot => slot with { Outputs = slot.Outputs.SetItem(DeviceOutputKey, change(slot.Outputs[DeviceOutputKey])) });

    public static Profile WithRootOutput(this Profile profile, Func<OutputDefinition, OutputDefinition> change) =>
        profile with { Outputs = profile.Outputs.SetItem(RootOutputKey, change(profile.Outputs[RootOutputKey])) };
}
