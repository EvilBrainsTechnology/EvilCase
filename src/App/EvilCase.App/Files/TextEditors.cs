using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class TextEditors(IJSRuntime jsRuntime) : ITextEditors, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task OpenEditor(ElementReference host, ElementReference searchInput, string text, CodeLanguage? language, string indentUnit, bool readOnly, string label, DotNetObjectReference<TextEditor> owner)
    {
        var editors = await this.Module();

        // Never cancelled: an editor the browser made must reach the caller to be closed.
        await editors.InvokeVoidAsync("openEditor", host, searchInput, text, language is { } value ? FilePreviews.LanguageValue(value) : null, indentUnit, readOnly, label, owner);
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
