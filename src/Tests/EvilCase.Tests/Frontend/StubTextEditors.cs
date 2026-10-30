using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class StubTextEditors : ITextEditors
{
    /// <summary>
    /// Every editor and view opened, in order; a view records an empty indent unit.
    /// </summary>
    public List<(string Text, CodeLanguage? Language, string IndentUnit, string Label)> Opened { get; } = [];

    /// <summary>
    /// Whether each opened was a read-only view.
    /// </summary>
    public List<bool> OpenedReadOnly { get; } = [];

    /// <summary>
    /// What the editor answers as its text.
    /// </summary>
    public string Text { get; set; } = "";

    public int Undos { get; private set; }

    public int Redos { get; private set; }

    public int Closed { get; private set; }

    /// <summary>
    /// When set, the editor does not start.
    /// </summary>
    public bool Fails { get; set; }

    /// <summary>
    /// When set, a read-only view does not start.
    /// </summary>
    public bool ViewFails { get; set; }

    /// <summary>
    /// What formatting makes of a text a view formats.
    /// </summary>
    public CodeView Code { get; set; } = new(Formatted: true, Invalid: false);

    /// <summary>
    /// Whether each view opened was asked to format.
    /// </summary>
    public List<bool> Formatting { get; } = [];

    public List<bool> ShownFormatted { get; } = [];

    public List<PreviewZoom> Zooms { get; } = [];

    public List<(string Query, PreviewFind Find)> Finds { get; } = [];

    /// <summary>
    /// The toolbar the last view reports its zoom and matches to, as the library would.
    /// </summary>
    public DotNetObjectReference<PreviewToolbar>? View { get; private set; }

    /// <summary>
    /// When set, the editor's text cannot be read.
    /// </summary>
    public bool ReadFails { get; set; }

    public Task OpenEditor(ElementReference host, ElementReference searchInput, string text, CodeLanguage? language, string indentUnit, string label, DotNetObjectReference<TextEditor> owner)
    {
        if (this.Fails)
            throw new JSException("The editor did not load.");

        this.Opened.Add((text, language, indentUnit, label));
        this.OpenedReadOnly.Add(false);

        return Task.CompletedTask;
    }

    public Task<CodeView> OpenViewer(ElementReference host, string text, CodeLanguage? language, bool format, string label, DotNetObjectReference<PreviewToolbar> view)
    {
        if (this.ViewFails)
            throw new JSException("The view did not load.");

        // Hands the toolbar to the script as the interop would, which a disposed one refuses.
        _ = view.Value;
        this.Opened.Add((text, language, "", label));
        this.OpenedReadOnly.Add(true);
        this.Formatting.Add(format);
        this.View = view;

        return Task.FromResult(format ? this.Code : new CodeView(Formatted: false, Invalid: false));
    }

    public Task ShowFormatted(ElementReference host, bool formatted)
    {
        this.ShownFormatted.Add(formatted);

        return Task.CompletedTask;
    }

    public Task ZoomViewer(ElementReference host, PreviewZoom zoom)
    {
        this.Zooms.Add(zoom);

        return Task.CompletedTask;
    }

    public Task FindInViewer(ElementReference host, string query, PreviewFind find)
    {
        this.Finds.Add((query, find));

        return Task.CompletedTask;
    }

    public Task<string> ReadEditorText(ElementReference host)
    {
        if (this.ReadFails)
            throw new JSException("The editor is gone.");

        return Task.FromResult(this.Text);
    }

    public Task UndoEdit(ElementReference host)
    {
        this.Undos++;

        return Task.CompletedTask;
    }

    public Task RedoEdit(ElementReference host)
    {
        this.Redos++;

        return Task.CompletedTask;
    }

    public Task CloseEditor(ElementReference host)
    {
        this.Closed++;

        return Task.CompletedTask;
    }
}
