using Bunit;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

// The ⋯ menu of the preview's header above lg.
internal static class FilePreviewHeaderMenu
{
    // Opens the menu and leaves it open.
    public static async Task<string[]> Texts(IRenderedComponent<FilePreviewModal> component)
    {
        await component.Find(".ec-preview-actions [aria-label='Další akce se souborem']").ClickAsync(new MouseEventArgs());

        return [.. component.FindAll("[role=menuitem]").Select(static item => item.TextContent.Trim())];
    }

    public static async Task Select(IRenderedComponent<FilePreviewModal> component, string text)
    {
        await component.Find(".ec-preview-actions [aria-label='Další akce se souborem']").ClickAsync(new MouseEventArgs());
        await component
            .FindAll("[role=menuitem]")
            .Single(item => string.Equals(item.TextContent.Trim(), text, StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());
    }
}
