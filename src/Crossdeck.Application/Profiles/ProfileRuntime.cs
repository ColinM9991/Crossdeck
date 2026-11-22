using System.Collections.Immutable;
using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Mapping.Output;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Profiles;

internal sealed class ProfileRuntime : IProfileRuntime
{
    private readonly ISimValueRouter _simValueRouter;
    private readonly IInputDispatcher _inputDispatcher;

    public ProfileRuntime(
        ISimValueRouter simValueRouter,
        IInputDispatcher inputDispatcher)
    {
        _simValueRouter = simValueRouter ?? throw new ArgumentNullException(nameof(simValueRouter));
        _inputDispatcher = inputDispatcher ?? throw new ArgumentNullException(nameof(inputDispatcher));
    }

    public Profile? Active { get; private set; }

    public event EventHandler<Profile?>? ActiveChanged;

    public IReadOnlyList<ProfileProblem> Activate(Profile profile)
    {
        var outputMappings = GetOutputMappings(profile);
        var inputMappings = GetInputMappings(profile);

        _inputDispatcher.SetBindings(inputMappings);
        _simValueRouter.SetBindings(outputMappings);

        Active = profile;
        ActiveChanged?.Invoke(this, profile);

        return [];
    }

    public void Deactivate()
    {
        Active = null;

        _inputDispatcher.SetBindings([]);
        _simValueRouter.SetBindings([]);

        ActiveChanged?.Invoke(this, null);
    }

    private static List<InputMapping> GetInputMappings(Profile profile)
    {
        var inputMappings = new List<InputMapping>();
        foreach (var (_, deviceSlot) in profile.Devices)
        {
            if (deviceSlot.Assigned is not { } device)
                continue;

            foreach (var (inputId, definition) in deviceSlot.Inputs)
            {
                inputMappings.Add(new InputMapping(device, inputId, definition.Sources,
                    definition.Events, definition.Sync));
            }
        }

        return inputMappings;
    }

    private static List<OutputMapping> GetOutputMappings(Profile profile)
    {
        List<OutputMapping> outputMappings =
        [
            .. profile.Outputs.Select(x => new OutputMapping(
                x.Value.Id,
                x.Value.Sources,
                new OutputSink.Computed(x.Key),
                x.Value.Transform,
                x.Value.When,
                x.Value.Fallback))
        ];

        foreach (var (slotKey, deviceSlot) in profile.Devices)
        {
            if (deviceSlot.Assigned is not { } device)
                continue;

            foreach (var (outputId, definition) in deviceSlot.Outputs)
            {
                outputMappings.Add(new OutputMapping(definition.Id, definition.Sources,
                    new OutputSink.Indicator(device, outputId), definition.Transform,
                    definition.When, definition.Fallback));
            }

            if (deviceSlot.Screen is not null)
            {
                outputMappings.Add(new OutputMapping($"{slotKey}:screen",
                    ImmutableDictionary<string, SimRef>.Empty,
                    new OutputSink.Screen(device, deviceSlot.Screen)));
            }
        }

        return outputMappings;
    }
}