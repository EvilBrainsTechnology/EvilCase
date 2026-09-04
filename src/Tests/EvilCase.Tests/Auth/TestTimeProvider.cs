namespace EvilBrains.EvilCase.Tests.Auth;

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
