namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// Lets a test say "fifteen minutes later" without waiting for them. Everything in the authentication
/// layer reads the time through <see cref="TimeProvider"/> for exactly this reason.
/// </summary>
internal sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
{
    private DateTimeOffset now = new(utcNow, TimeSpan.Zero);

    public DateTime UtcNow => this.now.UtcDateTime;

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }

    public void Advance(in TimeSpan by)
    {
        this.now = this.now.Add(by);
    }
}
