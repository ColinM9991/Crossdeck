namespace Crossdeck.Testing;

/// <summary>
/// A stubbed <see cref="TimeProvider"/> with a fixed clock that tests can advance explicitly.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private long _utcTicks;

    public TestTimeProvider()
        : this(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
    {
    }

    public TestTimeProvider(DateTimeOffset utcNow)
    {
        _utcTicks = utcNow.UtcDateTime.Ticks;
    }

    public void Advance(TimeSpan by) => _utcTicks += by.Ticks;

    public void AdvanceTo(DateTimeOffset utcNow) => _utcTicks = utcNow.UtcDateTime.Ticks;

    public override DateTimeOffset GetUtcNow() => new DateTimeOffset(_utcTicks, TimeSpan.Zero);

    public override long GetTimestamp() => _utcTicks;
}
