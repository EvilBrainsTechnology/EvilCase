using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class StubMarkdownEditors : IMarkdownEditors
{
    public List<(string Text, bool Editable, string Label)> Opened { get; } = [];

    public int Edits { get; private set; }

    public List<string> Shown { get; } = [];

    public List<string> Commands { get; } = [];

    public List<string> Links { get; } = [];

    public int Reads { get; private set; }

    public int Closed { get; private set; }

    /// <summary>
    /// What the editor answers as its text.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// What the editor answers as the link at the cursor.
    /// </summary>
    public string Link { get; set; } = "";

    /// <summary>
    /// When set, the library does not load.
    /// </summary>
    public bool Fails { get; set; }

    /// <summary>
    /// When set, the shown text does not turn editable.
    /// </summary>
    public bool EditFails { get; set; }

    /// <summary>
    /// When set, the editor's text cannot be read.
    /// </summary>
    public bool ReadFails { get; set; }

    public Task OpenMarkdown(ElementReference host, string text, bool editable, string label, DotNetObjectReference<MarkdownView> owner)
    {
        if (this.Fails)
            throw new JSException("The Markdown library did not load.");

        this.Opened.Add((text, editable, label));

        return Task.CompletedTask;
    }

    public Task<string> ReadMarkdown(ElementReference host)
    {
        if (this.ReadFails)
            throw new JSException("The Markdown view is gone.");

        this.Reads++;

        return Task.FromResult(this.Text);
    }

    public Task EditMarkdown(ElementReference host)
    {
        if (this.EditFails)
            throw new JSException("The Markdown view did not turn editable.");

        this.Edits++;

        return Task.CompletedTask;
    }

    public Task ShowMarkdown(ElementReference host, string text)
    {
        this.Shown.Add(text);

        return Task.CompletedTask;
    }

    public Task RunMarkdownCommand(ElementReference host, string command)
    {
        this.Commands.Add(command);

        return Task.CompletedTask;
    }

    public Task<string> ReadMarkdownLink(ElementReference host)
    {
        return Task.FromResult(this.Link);
    }

    public Task SetMarkdownLink(ElementReference host, string href)
    {
        this.Links.Add(href);

        return Task.CompletedTask;
    }

    public Task CloseMarkdown(ElementReference host)
    {
        this.Closed++;

        return Task.CompletedTask;
    }
}
