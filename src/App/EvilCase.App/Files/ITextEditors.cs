using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The source editors of text files, run by a library the browser loads when the first one opens.
/// </summary>
internal interface ITextEditors
{
    /// <summary>
    /// Opens an editor of <paramref name="text"/> in <paramref name="host"/>, highlighted in <paramref name="language"/>
    /// or plain where it is null; Tab types and indents by <paramref name="indentUnit"/>. <paramref name="searchInput"/>
    /// drives its search and <paramref name="owner"/> hears of its changes and of Ctrl+S. A <paramref name="readOnly"/>
    /// editor shows the text without taking input and without Ctrl+S. A library that does not load throws a
    /// <c>JSException</c>.
    /// </summary>
    public Task OpenEditor(ElementReference host, ElementReference searchInput, string text, CodeLanguage? language, string indentUnit, bool readOnly, string label, DotNetObjectReference<TextEditor> owner);

    /// <summary>
    /// The edited text, its lines joined with "\n".
    /// </summary>
    public Task<string> ReadEditorText(ElementReference host);

    public Task UndoEdit(ElementReference host);

    public Task RedoEdit(ElementReference host);

    public Task CloseEditor(ElementReference host);
}
