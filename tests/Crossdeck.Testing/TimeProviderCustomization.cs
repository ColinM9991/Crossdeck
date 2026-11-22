using AutoFixture;

namespace Crossdeck.Testing;

/// <summary>
/// Registers one stubbed <see cref="TimeProvider"/> per fixture so every resolution in that
/// fixture returns the same deterministic instance.
/// </summary>
public sealed class TimeProviderCustomization : ICustomization
{
    private readonly TestTimeProvider _timeProvider = new();

    public void Customize(IFixture fixture)
    {
        fixture.Register<TimeProvider>(() => _timeProvider);
        fixture.Register(() => _timeProvider);
    }
}
