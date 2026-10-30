using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The source editors of text files and their read-only views, run by a library the browser loads when the first one
/// opens.
/// </summary>
internal interface ITextEditors
{
    /// <summary>
    /// Opens an editor of <paramref name="text"/> in <paramref name="host"/>, highlighted in <paramref name="language"/>
    /// or plain where it is null; Tab types and indents by <paramref name="indentUnit"/>. <paramref name="searchInput"/>
    /// drives its search and <paramref name="owner"/> hears of its changes and of Ctrl+S. A library that does not load
    /// throws a <c>JSException</c>.
    /// </summary>
    public Task OpenEditor(ElementReference host, ElementReference searchInput, string text, CodeLanguage? language, string indentUnit, string label, DotNetObjectReference<TextEditor> owner);

    /// <summary>
    /// Opens a read-only view of <paramref name="text"/> in <paramref name="host"/>, highlighted as by
    /// <see cref="OpenEditor"/>. With <paramref name="format"/> the text is formatted in <paramref name="language"/> first
    /// and shows formatted when it formats. <paramref name="view"/> hears of the zoom, the search matches and Ctrl+F.
    /// Answers how the text formatted; a text not asked to format answers neither. A library that does not load throws a
    /// <c>JSException</c>.
    /// </summary>
    public Task<CodeView> OpenViewer(ElementReference host, string text, CodeLanguage? language, bool format, string label, DotNetObjectReference<PreviewToolbar> view);

    /// <summary>
    /// Shows the formatted or the original text in a view; its zoom stays and an open search runs again.
    /// </summary>
    public Task ShowFormatted(ElementReference host, bool formatted);

    public Task ZoomViewer(ElementReference host, PreviewZoom zoom);

    /// <summary>
    /// Highlights every match of <paramref name="query"/> in the view's text and scrolls to the one <paramref name="find"/>
    /// names; an empty query clears the highlights.
    /// </summary>
    public Task FindInViewer(ElementReference host, string query, PreviewFind find);

    /// <summary>
    /// The edited text, its lines joined with "\n".
    /// </summary>
    public Task<string> ReadEditorText(ElementReference host);

    public Task UndoEdit(ElementReference host);

    public Task RedoEdit(ElementReference host);

    /// <summary>
    /// Closes an editor or a view; a formatting under way stops.
    /// </summary>
    public Task CloseEditor(ElementReference host);
}
