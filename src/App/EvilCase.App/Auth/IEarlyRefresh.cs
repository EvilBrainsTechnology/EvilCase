namespace EvilBrains.EvilCase.App.Auth;

internal interface IEarlyRefresh
{
    /// <summary>
    /// The refresh index.html started before the download, or null when there is none or it
    /// failed. Returns the result once.
    /// </summary>
    public Task<EarlyRefreshResult?> Take();
}
