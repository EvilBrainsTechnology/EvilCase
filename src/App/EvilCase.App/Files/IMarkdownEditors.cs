using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The formatted views and editors of Markdown files, run by a library the browser loads when the first one opens.
/// </summary>
internal interface IMarkdownEditors
{
    /// <summary>
    /// Opens <paramref name="text"/> formatted in <paramref name="host"/>, editable or read-only; <paramref name="owner"/>
    /// hears of changes while editable and of Ctrl+S. A library that does not load throws a <c>JSException</c>.
    /// </summary>
    public Task OpenMarkdown(ElementReference host, string text, bool editable, string label, DotNetObjectReference<MarkdownView> owner);

    /// <summary>
    /// The edited text as Markdown, its lines joined with "\n".
    /// </summary>
    public Task<string> ReadMarkdown(ElementReference host);

    /// <summary>
    /// Turns the shown text editable in place.
    /// </summary>
    public Task EditMarkdown(ElementReference host);

    /// <summary>
    /// Shows <paramref name="text"/> read-only in place of the edited one.
    /// </summary>
    public Task ShowMarkdown(ElementReference host, string text);

    /// <summary>
    /// Runs a toolbar command: undo, redo, heading1 to heading3, strong, emphasis, strike, bulletList, orderedList, taskList,
    /// quote, code or table.
    /// </summary>
    public Task RunMarkdownCommand(ElementReference host, string command);

    /// <summary>
    /// The address of the link at the cursor; empty where there is none.
    /// </summary>
    public Task<string> ReadMarkdownLink(ElementReference host);

    /// <summary>
    /// Links the selection to <paramref name="href"/>; empty removes the link.
    /// </summary>
    public Task SetMarkdownLink(ElementReference host, string href);

    public Task CloseMarkdown(ElementReference host);
}
