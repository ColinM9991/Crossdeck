using AutoFixture;
using Crossdeck.Application.Routing;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Routing;

namespace Crossdeck.Testing;

/// <summary>
/// Registers concrete router collaborators for fixtures: a
/// <see cref="NotifyingSimValueCache"/> over the real <see cref="SimValueCache"/> (built on the
/// fixture's stubbed <see cref="TimeProvider"/>), and a <see cref="FakeDeviceRegistry"/> for
/// both its concrete type and <see cref="IDeviceRegistry"/>. NSubstitute cannot set the
/// <c>out</c> parameters of <see cref="ISimValueCache.TryGet"/> and
/// <see cref="IDeviceRegistry.TryGet"/>, so substitutes stand in for these two collaborators.
/// </summary>
public sealed class SimValueRouterCollaboratorCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register<NotifyingSimValueCache>(() => new NotifyingSimValueCache(
            new SimValueCache(fixture.Create<TimeProvider>())));
        fixture.Register<ISimValueCache>(() => fixture.Create<NotifyingSimValueCache>());
        fixture.Register<FakeDeviceRegistry>(() => new FakeDeviceRegistry());
        fixture.Register<IDeviceRegistry>(() => fixture.Create<FakeDeviceRegistry>());
    }
}
