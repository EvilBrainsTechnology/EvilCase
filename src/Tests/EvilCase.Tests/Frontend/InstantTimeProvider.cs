namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Records every requested delay and lets it elapse at once.
/// </summary>
internal sealed class InstantTimeProvider : TimeProvider
{
    public List<TimeSpan> Delays { get; } = [];

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        this.Delays.Add(dueTime);

        return System.CreateTimer(callback, state, TimeSpan.Zero, period);
    }
}
