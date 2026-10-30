using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class TextCompares(IJSRuntime jsRuntime) : ITextCompares, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task<CompareView> OpenCompare(ElementReference host, string older, string newer, CodeLanguage? language, bool format, IReadOnlyList<string> labels, DotNetObjectReference<TextCompare> owner)
    {
        var compares = await this.Module();

        // Never cancelled: a compare the browser made must reach the caller to be closed.
        return await compares.InvokeAsync<CompareView>("openCompare", host, older, newer, language is { } value ? FilePreviews.LanguageValue(value) : null, format, labels, owner);
    }

    public async Task<CompareView> OpenMarkdownCompare(ElementReference host, string older, string newer, IReadOnlyList<string> labels, DotNetObjectReference<MarkdownCompare> owner)
    {
        var compares = await this.Module();

        // Never cancelled: a compare the browser made must reach the caller to be closed.
        return await compares.InvokeAsync<CompareView>("openMarkdownCompare", host, older, newer, labels, owner);
    }

    public async Task<CompareView> ShowCompareFormatted(ElementReference host, bool formatted)
    {
        var compares = await this.Module();

        return await compares.InvokeAsync<CompareView>("showCompareFormatted", host, formatted);
    }

    public async Task StepCompareChange(ElementReference host, bool previous)
    {
        var compares = await this.Module();

        await compares.InvokeVoidAsync("stepCompareChange", host, previous);
    }

    public async Task FindInCompare(ElementReference host, string query, PreviewFind find)
    {
        var compares = await this.Module();

        await compares.InvokeVoidAsync("findInCompare", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task CloseCompare(ElementReference host)
    {
        var compares = await this.Module();

        await compares.InvokeVoidAsync("closeCompare", host);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    private async Task<IJSObjectReference> Module()
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/text-compare.js");

        return this.module;
    }
}
