namespace Crossdeck.Core.Routing;

public interface ISimValueRouter : IRunnable
{
    ISimValueCache Cache { get; }

    event EventHandler<string> AircraftChanged;
}