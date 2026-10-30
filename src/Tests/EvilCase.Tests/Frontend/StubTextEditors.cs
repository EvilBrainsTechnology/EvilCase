using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class StubTextEditors : ITextEditors
{
    public List<(string Text, CodeLanguage? Language, string IndentUnit, string Label)> Opened { get; } = [];

    public List<bool> OpenedReadOnly { get; } = [];

    /// <summary>
    /// What the editor answers as its text.
    /// </summary>
    public string Text { get; set; } = "";

    public int Undos { get; private set; }

    public int Redos { get; private set; }

    public int Closed { get; private set; }

    /// <summary>
    /// When set, the library does not load.
    /// </summary>
    public bool Fails { get; set; }

    /// <summary>
    /// When set, the editor's text cannot be read.
    /// </summary>
    public bool ReadFails { get; set; }

    public Task OpenEditor(ElementReference host, ElementReference searchInput, string text, CodeLanguage? language, string indentUnit, bool readOnly, string label, DotNetObjectReference<TextEditor> owner)
    {
        if (this.Fails)
            throw new JSException("The editor did not load.");

        this.Opened.Add((text, language, indentUnit, label));
        this.OpenedReadOnly.Add(readOnly);

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
