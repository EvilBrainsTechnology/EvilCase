namespace EvilBrains.EvilCase.App.Auth;

internal interface IEarlyRefresh
{
    /// <summary>
    /// The refresh index.html started before the download, or null when there is none or it
    /// failed. Returns the result once.
    /// </summary>
    public Task<EarlyRefreshResult?> Take();

    /// <summary>
    /// A refresh serialized with every other tab's, or null when it did not complete.
    /// </summary>
    public Task<EarlyRefreshResult?> Refresh(CancellationToken token);
}
