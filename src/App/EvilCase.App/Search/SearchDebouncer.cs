namespace EvilBrains.EvilCase.App.Search;

/// <summary>
/// One search request at a time: starting another cancels the one before it.
/// </summary>
internal sealed class SearchDebouncer : IDisposable
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(300);

    private CancellationTokenSource? pending;

    /// <summary>
    /// Returns the token to run this search with, or <see langword="null"/> when a newer search
    /// arrived while this one waited out the delay.
    /// </summary>
    public async Task<CancellationToken?> Start(bool debounce)
    {
        if (this.pending is not null)
        {
            await this.pending.CancelAsync();
            this.pending.Dispose();
        }

        var current = new CancellationTokenSource();
        this.pending = current;

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
        }

        return current.Token;
    }

    public void Dispose()
    {
        this.pending?.Cancel();
        this.pending?.Dispose();
    }
}
