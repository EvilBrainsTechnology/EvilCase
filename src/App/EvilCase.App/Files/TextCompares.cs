using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class TextCompares(IJSRuntime jsRuntime) : JsModule(jsRuntime, "./js/text-compare.js"), ITextCompares
{
    public async Task<CompareView> OpenCompare(ElementReference host, string older, string newer, CodeLanguage? language, bool format, IReadOnlyList<string> labels, DotNetObjectReference<TextCompare> owner)
    {
        // Never cancelled: a compare the browser made must reach the caller to be closed.
        return await this.Invoke<CompareView>("openCompare", host, older, newer, language is { } value ? FilePreviews.LanguageValue(value) : null, format, labels, owner);
    }

    public async Task<CompareView> OpenMarkdownCompare(ElementReference host, string older, string newer, IReadOnlyList<string> labels, DotNetObjectReference<MarkdownCompare> owner)
    {
        // Never cancelled: a compare the browser made must reach the caller to be closed.
        return await this.Invoke<CompareView>("openMarkdownCompare", host, older, newer, labels, owner);
    }

    public async Task<CompareView> ShowCompareFormatted(ElementReference host, bool formatted)
    {
        return await this.Invoke<CompareView>("showCompareFormatted", host, formatted);
    }

    public async Task StepCompareChange(ElementReference host, bool previous)
    {
        await this.InvokeVoid("stepCompareChange", host, previous);
    }

    public async Task FindInCompare(ElementReference host, string query, PreviewFind find)
    {
        await this.InvokeVoid("findInCompare", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task CloseCompare(ElementReference host)
    {
        await this.InvokeVoid("closeCompare", host);
    }
}
