using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class EarlyRefresh(IJSRuntime jsRuntime, ILogger<EarlyRefresh> logger) : IEarlyRefresh
{
    public async Task<EarlyRefreshResult?> Take()
    {
        try
        {
            await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/early-refresh.js");

            return await module.InvokeAsync<EarlyRefreshResult?>("takeEarlyRefresh");
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            logger.LogWarning(exception, "The early refresh was not available");

            return null;
        }
    }
}
