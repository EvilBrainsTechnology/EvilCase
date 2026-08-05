namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// Lets a test say "fifteen minutes later" without waiting for them, and name the zone the application
/// runs in. Both come from <see cref="TimeProvider"/> everywhere, so neither reaches for the machine.
/// </summary>
internal sealed class TestTimeProvider(DateTime utcNow, TimeZoneInfo? localTimeZone = null) : TimeProvider
{
    private DateTimeOffset now = new(utcNow, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone { get; } = localTimeZone ?? TimeZoneInfo.Utc;

    public DateTime UtcNow => this.now.UtcDateTime;

    public override DateTimeOffset GetUtcNow() => this.now;

    public void Advance(in TimeSpan by) => this.now = this.now.Add(by);
}
