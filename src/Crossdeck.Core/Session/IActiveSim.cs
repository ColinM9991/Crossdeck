using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session;

public interface IActiveSim
{
    ISimProducer? Current { get; }

    event EventHandler<ActiveSimChangedEventArgs> Changed;
}