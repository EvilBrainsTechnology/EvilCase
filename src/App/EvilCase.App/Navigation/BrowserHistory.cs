using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Navigation;

internal sealed class BrowserHistory(IJSRuntime jsRuntime) : IBrowserHistory, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task TrackSteps()
    {
        var history = await this.Module();

        await history.InvokeVoidAsync("trackSteps");
    }

    public async Task<int> TakeStep(string target)
    {
        var history = await this.Module();

        return await history.InvokeAsync<int>("takeStep", target);
    }

    public async Task Go(int distance)
    {
        var history = await this.Module();

        await history.InvokeVoidAsync("go", distance);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    private async Task<IJSObjectReference> Module()
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/browser-history.js");

        return this.module;
    }
}
