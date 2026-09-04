namespace EvilBrains.EvilCase.App.Search;

internal sealed class SearchDebouncer : IDisposable
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private CancellationTokenSource? pending;

    /// <summary>
    /// Null when a newer search superseded this one.
    /// </summary>
    public async Task<CancellationToken?> Start(bool debounce)
    {
        var previous = this.pending;
        var current = new CancellationTokenSource();
        this.pending = current;

        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }

        if (!ReferenceEquals(this.pending, current))
            return null;

        if (debounce)
        {
            try
            {
                await Task.Delay(Delay, current.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            // The delay can complete before a superseding call cancels it; its source is disposed then.
            if (!ReferenceEquals(this.pending, current))
                return null;
        }

        return current.Token;
    }

    public void Dispose()
    {
        this.pending?.Cancel();
        this.pending?.Dispose();
        this.pending = null;
    }
}
