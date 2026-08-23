namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A search that records the token it ran with and either completes at once, fails, or stays in flight.
/// </summary>
internal sealed class FakeSearch
{
    private readonly Queue<Exception> failures = new();

    public List<CancellationToken> Tokens { get; } = [];

    public List<Exception> Reported { get; } = [];

    public int Completed { get; private set; }

    /// <summary>
    /// Keeps every following search in flight instead of completing it.
    /// </summary>
    public bool Hold { get; set; }

    public void FailNext(Exception exception)
    {
        this.failures.Enqueue(exception);
    }

    public Task Load(CancellationToken token)
    {
        this.Tokens.Add(token);

        if (this.failures.TryDequeue(out var exception))
            return Task.FromException(exception);

        if (this.Hold)
        {
            // A held search ends the way a cancelled request does.
            var completion = new TaskCompletionSource();
            token.Register(() => completion.TrySetCanceled(token));

            return completion.Task;
        }

        this.Completed++;

        return Task.CompletedTask;
    }

    public Task Report(Exception exception)
    {
        this.Reported.Add(exception);

        return Task.CompletedTask;
    }
}
