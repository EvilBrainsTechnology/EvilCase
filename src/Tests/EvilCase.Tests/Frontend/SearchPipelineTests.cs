using EvilBrains.EvilCase.App.Search;
using Microsoft.Reactive.Testing;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchPipelineTests
{
    private static readonly long Debounce = TimeSpan.FromMilliseconds(300).Ticks;

    [Test]
    public void ADebouncedSearchWaitsForTheDelay()
    {
        var search = new FakeSearch();
        var scheduler = new TestScheduler();

        using var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        pipeline.Start(debounce: true);
        scheduler.AdvanceBy(Debounce - 1);

        Assert.That(search.Tokens, Is.Empty, "a debounced search does not run before its delay elapsed");

        scheduler.AdvanceBy(1);

        Assert.That(search.Tokens, Has.Count.EqualTo(1), "a debounced search runs once its delay elapsed");
    }

    [Test]
    public void AnImmediateSearchDoesNotWait()
    {
        var search = new FakeSearch();
        var scheduler = new TestScheduler();

        using var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);

        Assert.That(search.Tokens, Has.Count.EqualTo(1), "an immediate search does not wait for the debounce delay");
    }

    [Test]
    public void ANewerTriggerReplacesTheOneStillWaiting()
    {
        var search = new FakeSearch();
        var scheduler = new TestScheduler();

        using var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        pipeline.Start(debounce: true);
        scheduler.AdvanceBy(Debounce - 1);
        pipeline.Start(debounce: true);
        scheduler.AdvanceBy(Debounce);

        Assert.That(search.Tokens, Has.Count.EqualTo(1), "only the newest of two overlapping triggers runs");
    }

    [Test]
    public void ANewerTriggerCancelsTheSearchInFlight()
    {
        var search = new FakeSearch { Hold = true };
        var scheduler = new TestScheduler();

        using var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);
        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(search.Tokens[0].IsCancellationRequested, Is.True, "a superseded search is cancelled so its answer cannot land");
            Assert.That(search.Tokens[1].IsCancellationRequested, Is.False, "the newest search keeps running");
            Assert.That(search.Reported, Is.Empty, "a cancelled search is not a failure");
        }
    }

    [Test]
    public void DisposalCancelsTheSearchInFlight()
    {
        var search = new FakeSearch { Hold = true };
        var scheduler = new TestScheduler();

        var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);

        pipeline.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(search.Tokens[0].IsCancellationRequested, Is.True, "disposal cancels the search in flight");
            Assert.That(search.Reported, Is.Empty, "a search cancelled by disposal is not a failure");
        }
    }

    [Test]
    public void DisposalDropsTheDelayThatHadNotElapsed()
    {
        var search = new FakeSearch();
        var scheduler = new TestScheduler();

        var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        pipeline.Start(debounce: true);
        pipeline.Dispose();
        scheduler.AdvanceBy(Debounce);

        Assert.That(search.Tokens, Is.Empty, "disposal drops the delay that had not elapsed");
    }

    [Test]
    public void AFailedSearchLeavesTheNextOneRunning()
    {
        var search = new FakeSearch();
        var scheduler = new TestScheduler();
        Exception[] failures = [new InvalidOperationException("the first failure"), new HttpRequestException("the second failure")];

        using var pipeline = new SearchPipeline(search.Load, search.Report, scheduler);

        foreach (var failure in failures)
            search.FailNext(failure);

        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);
        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);
        pipeline.Start(debounce: false);
        scheduler.AdvanceBy(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(search.Reported, Is.EqualTo(failures), "every failure is reported");
            Assert.That(search.Completed, Is.EqualTo(1), "a search after two failures still runs");
        }
    }
}
