using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class TextEditors(IJSRuntime jsRuntime) : ITextEditors, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task OpenEditor(ElementReference host, string text, CodeLanguage? language, string indentUnit, string label, DotNetObjectReference<TextEditor> owner)
    {
        var editors = await this.Module();

        // Never cancelled: an editor the browser made must reach the caller to be closed.
        await editors.InvokeVoidAsync("openEditor", host, text, language is { } value ? FilePreviews.LanguageValue(value) : null, indentUnit, label, owner);
    }

    public async Task<CodeView> OpenViewer(ElementReference host, string text, CodeLanguage? language, bool format, string label, DotNetObjectReference<PreviewToolbar> view)
    {
        var editors = await this.Module();

        // Never cancelled: a view the browser made must reach the caller to be closed.
        return await editors.InvokeAsync<CodeView>("openViewer", host, text, language is { } value ? FilePreviews.LanguageValue(value) : null, format, label, view);
    }

    public async Task ShowFormatted(ElementReference host, bool formatted)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("showFormatted", host, formatted);
    }

    public async Task ZoomViewer(ElementReference host, PreviewZoom zoom)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("zoomViewer", host, FilePreviews.ZoomValue(zoom));
    }

    public async Task FindInViewer(ElementReference host, string query, PreviewFind find)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("findInViewer", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task FindInEditor(ElementReference host, string query, PreviewFind find)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("findInEditor", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task FocusEditor(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("focusEditor", host);
    }

    public async Task<string> ReadEditorText(ElementReference host)
    {
        var editors = await this.Module();

        return await editors.InvokeAsync<string>("readEditorText", host);
    }

    public async Task UndoEdit(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("undoEdit", host);
    }

    public async Task RedoEdit(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("redoEdit", host);
    }

    public async Task CloseEditor(ElementReference host)
    {
        var editors = await this.Module();

        await editors.InvokeVoidAsync("closeEditor", host);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    private async Task<IJSObjectReference> Module()
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/text-editor.js");

        return this.module;
    }
}
