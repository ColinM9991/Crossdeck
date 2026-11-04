using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session.Models;

public sealed record ActiveSimChangedEventArgs(ISimProducer? Previous, ISimProducer? Current);