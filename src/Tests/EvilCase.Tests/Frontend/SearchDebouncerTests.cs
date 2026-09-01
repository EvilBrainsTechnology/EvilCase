using System.Collections.Concurrent;
using EvilBrains.EvilCase.App.Search;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchDebouncerTests
{
    [Test]
    public async Task TheNewestSearchWinsWhenTwoStartsOverlap()
    {
        using var debouncer = new SearchDebouncer();

        var first = await debouncer.Start(debounce: false);

        // A live registration forces the next CancelAsync to yield, opening the re-entrancy window.
        await using var registration = first!.Value.Register(static () =>
        {
        });

        var second = debouncer.Start(debounce: true);
        var third = debouncer.Start(debounce: true);

        var secondToken = await second;
        var thirdToken = await third;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondToken, Is.Null, "a start superseded before its delay returns null");
            Assert.That(thirdToken, Is.Not.Null, "the newest start keeps its token");
            Assert.That(thirdToken!.Value.IsCancellationRequested, Is.False, "the newest start's token stays live");
        }

        var next = await debouncer.Start(debounce: false);

        Assert.That(next!.Value.IsCancellationRequested, Is.False, "the debouncer stays usable after an overlap");
    }

    [Test]
    public async Task AStartSupersededAfterItsDelayElapsedReturnsNull()
    {
        using var debouncer = new SearchDebouncer();
        var pump = new QueueingContext();
        var original = SynchronizationContext.Current;

        // The pump holds the resumption after the delay, so a newer start can run in between.
        SynchronizationContext.SetSynchronizationContext(pump);
        Task<CancellationToken?> parked;
        try
        {
            parked = debouncer.Start(debounce: true);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        await pump.FirstPost;

        var newer = await debouncer.Start(debounce: false);
        pump.RunAll();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await parked, Is.Null, "a start superseded after its delay elapsed returns null");
            Assert.That(newer!.Value.IsCancellationRequested, Is.False, "the newest start's token stays live");
        }
    }

    private sealed class QueueingContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> queue = new();

        private readonly TaskCompletionSource posted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task FirstPost => this.posted.Task;

        public override void Post(SendOrPostCallback d, object? state)
        {
            this.queue.Enqueue((d, state));
            this.posted.TrySetResult();
        }

        public void RunAll()
        {
            while (this.queue.TryDequeue(out var item))
                item.Callback(item.State);
        }
    }
}
