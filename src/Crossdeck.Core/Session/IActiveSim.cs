using Crossdeck.Core.Session.Models;
using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session;

public interface IActiveSim
{
    ISimProducer? Current { get; }

    event EventHandler<ActiveSimChangingEventArgs> Changing;

    event EventHandler<ActiveSimChangedEventArgs> Changed;
}