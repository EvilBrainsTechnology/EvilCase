using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class TextEditors(IJSRuntime jsRuntime) : JsModule(jsRuntime, "./js/text-editor.js"), ITextEditors
{
    public async Task OpenEditor(ElementReference host, string text, CodeLanguage? language, string indentUnit, string label, DotNetObjectReference<TextEditor> owner)
    {
        // Never cancelled: an editor the browser made must reach the caller to be closed.
        await this.InvokeVoid("openEditor", host, text, language is { } value ? FilePreviews.LanguageValue(value) : null, indentUnit, label, owner);
    }

    public async Task<CodeView> OpenViewer(ElementReference host, string text, CodeLanguage? language, bool format, string label, DotNetObjectReference<PreviewToolbar> view)
    {
        // Never cancelled: a view the browser made must reach the caller to be closed.
        return await this.Invoke<CodeView>("openViewer", host, text, language is { } value ? FilePreviews.LanguageValue(value) : null, format, label, view);
    }

    public async Task ShowFormatted(ElementReference host, bool formatted)
    {
        await this.InvokeVoid("showFormatted", host, formatted);
    }

    public async Task ZoomViewer(ElementReference host, PreviewZoom zoom)
    {
        await this.InvokeVoid("zoomViewer", host, FilePreviews.ZoomValue(zoom));
    }

    public async Task FindInViewer(ElementReference host, string query, PreviewFind find)
    {
        await this.InvokeVoid("findInViewer", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task FindInEditor(ElementReference host, string query, PreviewFind find)
    {
        await this.InvokeVoid("findInEditor", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task<CompareView> ShowEditorChanges(ElementReference host, string? original)
    {
        return await this.Invoke<CompareView>("showEditorChanges", host, original);
    }

    public async Task StepEditorChange(ElementReference host, bool previous)
    {
        await this.InvokeVoid("stepEditorChange", host, previous);
    }

    public async Task FocusEditor(ElementReference host)
    {
        await this.InvokeVoid("focusEditor", host);
    }

    public async Task<string> ReadEditorText(ElementReference host)
    {
        return await this.Invoke<string>("readEditorText", host);
    }

    public async Task UndoEdit(ElementReference host)
    {
        await this.InvokeVoid("undoEdit", host);
    }

    public async Task RedoEdit(ElementReference host)
    {
        await this.InvokeVoid("redoEdit", host);
    }

    public async Task CloseEditor(ElementReference host)
    {
        await this.InvokeVoid("closeEditor", host);
    }
}
