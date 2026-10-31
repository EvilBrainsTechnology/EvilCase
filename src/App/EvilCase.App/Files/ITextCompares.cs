using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The read-only compares of two texts, run by the text editor's library and for a formatted Markdown text by the
/// Markdown library besides, which the browser loads when the first one opens.
/// </summary>
internal interface ITextCompares
{
    /// <summary>
    /// Opens the compare of <paramref name="older"/> and <paramref name="newer"/> in <paramref name="host"/>, highlighted
    /// in <paramref name="language"/> or plain where it is null. With <paramref name="format"/> both texts are formatted
    /// first and compare formatted when both format and differ formatted. <paramref name="labels"/> names the older, the newer and the
    /// one-column text; <paramref name="owner"/> hears of the search matches and of Ctrl+F. A library that does not load
    /// throws a <c>JSException</c>.
    /// </summary>
    public Task<CompareView> OpenCompare(ElementReference host, string older, string newer, CodeLanguage? language, bool format, IReadOnlyList<string> labels, DotNetObjectReference<TextCompare> owner);

    /// <summary>
    /// Opens the compare of the Markdown texts <paramref name="older"/> and <paramref name="newer"/> formatted in
    /// <paramref name="host"/>; its changes count blocks. <paramref name="labels"/> names the older, the newer and the
    /// one-column text; <paramref name="owner"/> hears of the search matches and of Ctrl+F. A library that does not load
    /// throws a <c>JSException</c>.
    /// </summary>
    public Task<CompareView> OpenMarkdownCompare(ElementReference host, string older, string newer, IReadOnlyList<string> labels, DotNetObjectReference<MarkdownCompare> owner);

    /// <summary>
    /// Compares the formatted or the original texts; an open search runs again. Answers the changes of the texts shown.
    /// </summary>
    public Task<CompareView> ShowCompareFormatted(ElementReference host, bool formatted);

    /// <summary>
    /// Scrolls to the next or, with <paramref name="previous"/>, the previous change; both wrap around.
    /// </summary>
    public Task StepCompareChange(ElementReference host, bool previous);

    /// <summary>
    /// Highlights every match of <paramref name="query"/> in the texts the compare shows and scrolls to the one
    /// <paramref name="find"/> names; an empty query clears the highlights.
    /// </summary>
    public Task FindInCompare(ElementReference host, string query, PreviewFind find);

    /// <summary>
    /// Closes a compare; a formatting under way stops.
    /// </summary>
    public Task CloseCompare(ElementReference host);
}
