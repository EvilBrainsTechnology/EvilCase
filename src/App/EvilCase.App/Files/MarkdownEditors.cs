using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class MarkdownEditors(IJSRuntime jsRuntime) : IMarkdownEditors, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task OpenMarkdown(ElementReference host, string text, bool editable, string label, DotNetObjectReference<MarkdownView> owner)
    {
        var editors = await this.Module();

        // Never cancelled: an editor the browser made must reach the caller to be closed.
        await editors.InvokeVoidAsync("openMarkdown", host, text, editable, label, owner);
    }

    public async Task<string> ReadMarkdown(ElementReference host)
    {
        var editors = await this.Module();

        return await editors.InvokeAsync<string>("readMarkdown", host);
    }

    public async Task EditMarkdown(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("editMarkdown", host);
    }

    public async Task ShowMarkdown(ElementReference host, string text)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("showMarkdown", host, text);
    }

    public async Task RunMarkdownCommand(ElementReference host, string command)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("runMarkdownCommand", host, command);
    }

    public async Task<string> ReadMarkdownLink(ElementReference host)
    {
        var editors = await this.Module();

        return await editors.InvokeAsync<string>("readMarkdownLink", host);
    }

    public async Task SetMarkdownLink(ElementReference host, string href)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("setMarkdownLink", host, href);
    }

    public async Task FindInMarkdown(ElementReference host, string query, PreviewFind find)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("findInMarkdown", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task FocusMarkdown(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("focusMarkdown", host);
    }

    public async Task CloseMarkdown(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("closeMarkdown", host);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    private async Task<IJSObjectReference> Module()
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/markdown-editor.js");

        return this.module;
    }
}
