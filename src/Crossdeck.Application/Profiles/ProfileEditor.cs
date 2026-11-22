using System.Collections.Immutable;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Application.Profiles;

public sealed class ProfileEditor : IProfileEditor
{
    private readonly IProfileStore _profileStore;
    private readonly ProfileHistory _history;
    private Profile _profile;

    public ProfileEditor(
        IProfileStore profileStore,
        Profile profile)
    {
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _history = new ProfileHistory();

        Draft = profile;
    }

    public Profile Draft { get; private set; }

    public bool IsDirty => !ReferenceEquals(_profile, Draft);

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public event EventHandler? Changed;

    public void SetProfileDetails(string name, AircraftMatch aircraftDetails)
    {
        var next = Draft with { Name = name, Aircraft = aircraftDetails };

        Apply(next);
    }

    public void AddDevice(string slot, DeviceModel model)
    {
        if (Draft.Devices.ContainsKey(slot))
            return; // TODO Union Result

        var deviceSlot = new DeviceSlot(model,
            ImmutableDictionary<InputId, InputDefinition>.Empty,
            ImmutableDictionary<OutputId, OutputDefinition>.Empty);

        var next = Draft with { Devices = Draft.Devices.SetItem(slot, deviceSlot) };

        Apply(next);
    }

    public void AssignDevice(string slot, DeviceIdentity? unit) =>
        UpdateDevice(slot, deviceSlot => deviceSlot with { Assigned = unit });

    public void RemoveDevice(string slot)
    {
        if (!Draft.Devices.ContainsKey(slot))
            return;

        var next = Draft with { Devices = Draft.Devices.Remove(slot) };

        Apply(next);
    }

    public void SetInput(string slot, InputId control, InputDefinition input) => UpdateDevice(slot,
        deviceSlot => deviceSlot with { Inputs = deviceSlot.Inputs.SetItem(control, input) });

    public void RemoveInput(string slot, InputId control) => UpdateDevice(slot,
        deviceSlot => deviceSlot with { Inputs = deviceSlot.Inputs.Remove(control) });

    public void SetOutput(string slot, OutputId output, OutputDefinition definition) => UpdateDevice(slot,
        deviceSlot => deviceSlot with { Outputs = deviceSlot.Outputs.SetItem(output, definition) });

    public void RemoveOutput(string slot, OutputId output) => UpdateDevice(slot,
        deviceSlot => deviceSlot with { Outputs = deviceSlot.Outputs.Remove(output) });

    public void AddVariable(string name, SimValue initial)
    {
        if (Draft.Variables.ContainsKey(name))
            return;

        var next = Draft with
        {
            Variables = Draft.Variables.SetItem(name, new VariableDefinition(initial))
        };

        Apply(next);
    }

    public void SetVariableInitial(string name, SimValue initial)
    {
        if (!Draft.Variables.TryGetValue(name, out _))
            return;

        var next = Draft with
        {
            Variables = Draft.Variables.SetItem(name, new VariableDefinition(Initial: initial))
        };

        Apply(next);
    }

    public void RenameVariable(string name, string newName)
    {
        if (string.Equals(name, newName, StringComparison.OrdinalIgnoreCase))
            return;

        if (!Draft.Variables.TryGetValue(name, out _))
            return;

        if (Draft.Variables.ContainsKey(newName))
            return;

        var next = Draft with
        {
            Variables = Draft.Variables
                .SetItem(newName, Draft.Variables[name])
                .Remove(name)
        };

        Apply(next);
    }

    public void RemoveVariable(string name)
    {
        if (!Draft.Variables.TryGetValue(name, out _))
            return;

        var next = Draft with
        {
            Variables = Draft.Variables.Remove(name)
        };

        Apply(next);
    }

    public IReadOnlyList<string> FindReaders(SimRef reference)
    {
        return [];
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await _profileStore.SaveAsync(Draft, cancellationToken);

        _profile = Draft;
        _history.Clear();
    }

    public void Undo()
    {
        if (!CanUndo)
            return;

        Replace(_history.Undo(Draft));
    }

    public void Redo()
    {
        if (!CanRedo)
            return;

        Replace(_history.Redo(Draft));
    }

    private void Apply(Profile next)
    {
        _history.Push(Draft);

        Replace(next);
    }

    private void Replace(Profile next)
    {
        Draft = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateDevice(string slot, Func<DeviceSlot, DeviceSlot> change)
    {
        if (!Draft.Devices.TryGetValue(slot, out var deviceSlot))
            return;

        var changed = change(deviceSlot);

        Apply(Draft with { Devices = Draft.Devices.SetItem(slot, changed) });
    }
}