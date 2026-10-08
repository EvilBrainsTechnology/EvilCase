using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Clipboard;

internal sealed class ClipboardWriter(IJSRuntime jsRuntime) : IClipboardWriter, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task WriteText(string text, CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/clipboard.js");

        await this.module.InvokeVoidAsync("writeText", token, text);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }
}
