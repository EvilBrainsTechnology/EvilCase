using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App;

/// <summary>
/// A JavaScript module imported on the first call and disposed with its service.
/// </summary>
internal abstract class JsModule(IJSRuntime jsRuntime, string path) : IAsyncDisposable
{
    private IJSObjectReference? module;

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    protected async Task<IJSObjectReference> Module(CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, path);

        return this.module;
    }

    protected async Task<T> Invoke<T>(string identifier, params object?[] args)
    {
        return await this.Invoke<T>(identifier, CancellationToken.None, args);
    }

    protected async Task<T> Invoke<T>(string identifier, CancellationToken token, params object?[] args)
    {
        var imported = await this.Module(token);

        return await imported.InvokeAsync<T>(identifier, token, args);
    }

    protected async Task InvokeVoid(string identifier, params object?[] args)
    {
        await this.InvokeVoid(identifier, CancellationToken.None, args);
    }

    protected async Task InvokeVoid(string identifier, CancellationToken token, params object?[] args)
    {
        var imported = await this.Module(token);

        await imported.InvokeVoidAsync(identifier, token, args);
    }
}
