using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class EarlyRefresh(IJSRuntime jsRuntime, ILogger<EarlyRefresh> logger) : JsModule(jsRuntime, "./js/early-refresh.js"), IEarlyRefresh
{
    public async Task<EarlyRefreshResult?> Take()
    {
        return await this.InvokeRefresh("takeEarlyRefresh", CancellationToken.None);
    }

    public async Task<EarlyRefreshResult?> Refresh(CancellationToken token)
    {
        return await this.InvokeRefresh("refreshSession", token);
    }

    private async Task<EarlyRefreshResult?> InvokeRefresh(string identifier, CancellationToken token)
    {
        try
        {
            return await this.Invoke<EarlyRefreshResult?>(identifier, token);
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            logger.LogWarning(exception, "The refresh module was not available");

            return null;
        }
    }
}
