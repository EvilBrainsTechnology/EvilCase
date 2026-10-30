using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class StubTextCompares : ITextCompares
{
    /// <summary>
    /// Every compare opened, in order.
    /// </summary>
    public List<(string Older, string Newer, CodeLanguage? Language, bool Format)> Opened { get; } = [];

    public int Closed { get; private set; }

    /// <summary>
    /// What a compare answers when it opens; by default it shows formatted what it was asked to format.
    /// </summary>
    public CompareView? Shown { get; set; }

    public Task<CompareView> OpenCompare(ElementReference host, string older, string newer, CodeLanguage? language, bool format, IReadOnlyList<string> labels, DotNetObjectReference<TextCompare> owner)
    {
        // Hands the owner to the script as the interop would, which a disposed one refuses.
        _ = owner.Value;
        this.Opened.Add((older, newer, language, format));

        return Task.FromResult(this.Shown ?? new CompareView(Formatted: format, Formattable: format, Added: 1, Removed: 1, Changes: 1));
    }

    public Task<CompareView> ShowCompareFormatted(ElementReference host, bool formatted)
    {
        return Task.FromResult(new CompareView(formatted, Formattable: true, Added: 1, Removed: 1, Changes: 1));
    }

    public Task StepCompareChange(ElementReference host, bool previous)
    {
        return Task.CompletedTask;
    }

    public Task FindInCompare(ElementReference host, string query, PreviewFind find)
    {
        return Task.CompletedTask;
    }

    public Task CloseCompare(ElementReference host)
    {
        this.Closed++;

        return Task.CompletedTask;
    }
}
