using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class MarkdownEditors(IJSRuntime jsRuntime) : JsModule(jsRuntime, "./js/markdown-editor.js"), IMarkdownEditors
{
    public async Task OpenMarkdown(ElementReference host, string text, bool editable, string label, DotNetObjectReference<MarkdownView> owner)
    {
        // Never cancelled: an editor the browser made must reach the caller to be closed.
        await this.InvokeVoid("openMarkdown", host, text, editable, label, owner);
    }

    public async Task<string> ReadMarkdown(ElementReference host)
    {
        return await this.Invoke<string>("readMarkdown", host);
    }

    public async Task EditMarkdown(ElementReference host)
    {
        await this.InvokeVoid("editMarkdown", host);
    }

    public async Task ShowMarkdown(ElementReference host, string text)
    {
        await this.InvokeVoid("showMarkdown", host, text);
    }

    public async Task RunMarkdownCommand(ElementReference host, string command)
    {
        await this.InvokeVoid("runMarkdownCommand", host, command);
    }

    public async Task<string> ReadMarkdownLink(ElementReference host)
    {
        return await this.Invoke<string>("readMarkdownLink", host);
    }

    public async Task SetMarkdownLink(ElementReference host, string href)
    {
        await this.InvokeVoid("setMarkdownLink", host, href);
    }

    public async Task FindInMarkdown(ElementReference host, string query, PreviewFind find)
    {
        await this.InvokeVoid("findInMarkdown", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task FocusMarkdown(ElementReference host)
    {
        await this.InvokeVoid("focusMarkdown", host);
    }

    public async Task CloseMarkdown(ElementReference host)
    {
        await this.InvokeVoid("closeMarkdown", host);
    }
}
