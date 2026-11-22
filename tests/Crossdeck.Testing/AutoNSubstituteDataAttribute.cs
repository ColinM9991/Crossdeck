using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;

namespace Crossdeck.Testing;

public sealed class AutoNSubstituteDataAttribute() : AutoDataAttribute(CreateFixture)
{
    private static IFixture CreateFixture()
    {
        var fixture = new Fixture { OmitAutoProperties = true }
            .Customize(new AutoNSubstituteCustomization { ConfigureMembers = false })
            .Customize(new ReportDescriptorCustomization())
            .Customize(new TimeProviderCustomization())
            .Customize(new SimValueRouterCollaboratorCustomization())
            .Customize(new ImmutableDictionaryCustomization());

        fixture.Behaviors
            .OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        return fixture;
    }
}
