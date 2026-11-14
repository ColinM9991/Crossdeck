using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session.Models;

public sealed record ActiveSimChangedEventArgs(ISimProducer? Current);

public sealed record ActiveSimChangingEventArgs(ISimProducer? Current, ISimProducer? Next);