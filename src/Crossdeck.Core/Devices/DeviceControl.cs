namespace Crossdeck.Core.Devices;

public abstract record DeviceControl(InputId Id, string Name);

public sealed record ButtonControl(InputId Id, string Name) : DeviceControl(Id, Name);

public sealed record AxisControl(InputId Id, string Name, double Min, double Max) : DeviceControl(Id, Name);

public sealed record HatControl(InputId Id, string Name, int Positions) : DeviceControl(Id, Name);