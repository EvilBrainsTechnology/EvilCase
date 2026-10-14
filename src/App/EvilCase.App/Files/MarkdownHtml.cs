using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Markdown as HTML that is safe to put on the page: the pipeline drops raw HTML, so it shows as text, and
/// only a web or mail address stays a link. Images are never loaded; their description shows instead.
/// </summary>
internal static class MarkdownHtml
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UsePipeTables()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .Build();

    public static string Render(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var link in document.Descendants<LinkInline>().ToList())
        {
            if (link.IsImage || !IsSafe(link.Url))
                link.ReplaceBy(new ContainerInline());
            else
                OpenInNewTab(link);
        }

        foreach (var autolink in document.Descendants<AutolinkInline>().ToList())
        {
            if (IsSafe(autolink.Url))
                OpenInNewTab(autolink);
            else
                autolink.ReplaceBy(new LiteralInline(autolink.Url));
        }

        return document.ToHtml(Pipeline);
    }

    private static bool IsSafe(string? url)
    {
        return url is not null
            && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
    }

    private static void OpenInNewTab(Inline link)
    {
        var attributes = link.GetAttributes();

        attributes.AddProperty("target", "_blank");
        attributes.AddProperty("rel", "noopener noreferrer");
    }
}
