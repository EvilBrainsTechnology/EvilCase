using EvilBrains.EvilCase.App.Navigation;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class StubBrowserHistory : IBrowserHistory
{
    public int Tracked { get; private set; }

    public List<string> Taken { get; } = [];

    public List<int> Moves { get; } = [];

    /// <summary>
    /// The distance every remembered step answers; 0 when none is known.
    /// </summary>
    public int Distance { get; set; }

    public Task TrackSteps()
    {
        this.Tracked++;

        return Task.CompletedTask;
    }

    public Task<int> TakeStep(string target)
    {
        this.Taken.Add(target);

        return Task.FromResult(this.Distance);
    }

    public Task Go(int distance)
    {
        this.Moves.Add(distance);

        return Task.CompletedTask;
    }
}
