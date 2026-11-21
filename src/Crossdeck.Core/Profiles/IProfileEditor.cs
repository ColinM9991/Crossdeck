using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Profiles;

public interface IProfileEditor
{
    Profile Draft { get; }

    bool IsDirty { get; }

    bool CanUndo { get; }

    bool CanRedo { get; }

    event EventHandler? Changed;

    void SetAircraftDetails(string name, AircraftMatch aircraftDetails);

    void AddDevice(string slot, DeviceModel model);

    void AssignDevice(string slot, DeviceIdentity? unit);

    void RemoveDevice(string slot);

    void SetInput(string slot, InputId control, InputDefinition input);

    void RemoveInput(string slot, InputId control);

    void SetOutput(string? slot, OutputId output, OutputDefinition definition);

    void RemoveOutput(string? slot, OutputId output);

    void AddVariable(string name, SimValue initial);

    void SetVariableInitial(string name, SimValue initial);

    void RenameVariable(string name, string newName);

    void RemoveVariable(string name);

    IReadOnlyList<string> FindReaders(SimRef reference);

    void Undo();

    void Redo();

    Task SaveAsync(CancellationToken cancellationToken);
}