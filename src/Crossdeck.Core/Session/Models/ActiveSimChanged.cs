using Crossdeck.Core.Simulators;

namespace Crossdeck.Core.Session.Models;

public sealed record ActiveSimChanged(ISimProducer? Current);

public sealed record ActiveSimChanging(ISimProducer? Current, ISimProducer? Next);