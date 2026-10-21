using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class EarlyRefresh(IJSRuntime jsRuntime, ILogger<EarlyRefresh> logger) : IEarlyRefresh
{
    public async Task<EarlyRefreshResult?> Take()
    {
        return await this.Invoke("takeEarlyRefresh", CancellationToken.None);
    }

    public async Task<EarlyRefreshResult?> Refresh(CancellationToken token)
    {
        return await this.Invoke("refreshSession", token);
    }

    private async Task<EarlyRefreshResult?> Invoke(string identifier, CancellationToken token)
    {
        try
        {
            await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/early-refresh.js");

            return await module.InvokeAsync<EarlyRefreshResult?>(identifier, token);
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            logger.LogWarning(exception, "The refresh module was not available");

            return null;
        }
    }
}
