using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session;

public sealed record ActiveSimChangedEventArgs(ISimProducer? Previous, ISimProducer? Current);