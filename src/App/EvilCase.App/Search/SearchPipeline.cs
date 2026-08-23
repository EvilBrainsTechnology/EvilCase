using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EvilBrains.EvilCase.App.Search;

/// <summary>
/// One search at a time: a new trigger drops a pending delay and cancels the request in flight.
/// </summary>
internal sealed class SearchPipeline : IDisposable
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private readonly Subject<bool> triggers = new();

    private readonly Func<CancellationToken, Task> load;

    private readonly Func<Exception, Task> failed;

    private readonly IDisposable subscription;

    public SearchPipeline(Func<CancellationToken, Task> load, Func<Exception, Task> failed, IScheduler? scheduler = null)
    {
        this.load = load;
        this.failed = failed;

        var clock = scheduler ?? Scheduler.Default;

        this.subscription = this.triggers
            .Select(debounce => Observable
                .FromAsync(this.Run)
                .DelaySubscription(debounce ? Delay : TimeSpan.Zero, clock))
            .Switch()
            .Subscribe();
    }

    /// <summary>
    /// Runs a search, after the debounce delay when <paramref name="debounce"/> is set.
    /// </summary>
    public void Start(bool debounce)
    {
        this.triggers.OnNext(debounce);
    }

    public void Dispose()
    {
        this.subscription.Dispose();
        this.triggers.Dispose();
    }

    // A fault ends the sequence for good and leaves the search dead, so nothing escapes here.
    private async Task Run(CancellationToken token)
    {
        try
        {
            await this.load(token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer search.
        }
        catch (Exception exception)
        {
            await this.failed(exception);
        }
    }
}
