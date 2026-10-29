using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FilePicker(IJSRuntime jsRuntime) : IFilePicker, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task PickFile(ElementReference input, CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/files.js");

        await this.module.InvokeVoidAsync("pickFile", token, input);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }
}
