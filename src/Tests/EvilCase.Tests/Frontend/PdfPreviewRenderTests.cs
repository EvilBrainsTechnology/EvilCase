using Bunit;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class PdfPreviewRenderTests
{
    private const string Url = "/api/files/0f8fad5b-d9cb-469f-a165-70867728950e/content";

    [Test]
    public void TheToolbarStandsAboveTheOneContainerThePdfIsShownIn()
    {
        using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.RenderedPdfs, Is.EqualTo([Url]));
            Assert.That(component.Find(".ec-preview-pdf-toolbar").GetAttribute("role"), Is.EqualTo("toolbar"));
            Assert.That(component.FindAll(".ec-preview-pdf-frame > .ec-preview-pdf"), Has.Count.EqualTo(1));
            Assert.That(component.Find(".ec-preview-pdf").GetAttribute("aria-label"), Is.EqualTo("Obsah souboru Rozhodnutí.pdf"));
            Assert.That(component.Find(".ec-preview-pdf").ChildElementCount, Is.Zero, "the library fills the container");
            Assert.That(component.Find(".ec-preview-pdf-page-input").GetAttribute("value"), Is.EqualTo("1"));
            Assert.That(component.Find(".ec-preview-pdf-page-count").TextContent, Is.EqualTo("z 3"));
            Assert.That(component.FindAll(".ec-preview-pdf-loading"), Is.Empty);
            Assert.That(component.FindAll("iframe, object, embed"), Is.Empty);
        }
    }

    [Test]
    public void EveryIconButtonHasAName()
    {
        using var ctx = NewContext(out _);

        var component = Render(ctx);

        Assert.That(
            component.FindAll(".ec-preview-pdf-toolbar button").Select(static button => button.GetAttribute("aria-label")),
            Is.EqualTo(["Zmenšit", "Zvětšit", "Na šířku", "Celá stránka", "Hledat v dokumentu", "Předchozí výskyt", "Další výskyt"]));
    }

    [Test]
    public void TheZoomButtonsStepAndFitThroughTheLibrary()
    {
        using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        Button(component, "Zvětšit").Click();
        Button(component, "Zmenšit").Click();
        Button(component, "Na šířku").Click();
        Button(component, "Celá stránka").Click();

        Assert.That(previews.Zooms, Is.EqualTo([PdfZoom.In, PdfZoom.Out, PdfZoom.PageWidth, PdfZoom.PageFit]));
    }

    [Test]
    public async Task TheReportedZoomShowsAsPercentAndMarksTheFit()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await previews.PdfView!.Value.ZoomChanged(137, "page-width");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-pdf-zoom-value").TextContent, Is.EqualTo("137 %"));
            Assert.That(Button(component, "Na šířku").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(Button(component, "Celá stránka").GetAttribute("aria-pressed"), Is.EqualTo("false"));
        }

        await previews.PdfView.Value.ZoomChanged(150, "");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-pdf-zoom-value").TextContent, Is.EqualTo("150 %"));
            Assert.That(Button(component, "Na šířku").GetAttribute("aria-pressed"), Is.EqualTo("false"));
        }
    }

    [TestCase(400, "Zvětšit")]
    [TestCase(50, "Zmenšit")]
    public async Task TheZoomStopsAtItsBounds(int percent, string blocked)
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await previews.PdfView!.Value.ZoomChanged(percent, "");

        Assert.That(Button(component, blocked).HasAttribute("disabled"), Is.True);
    }

    [Test]
    public async Task ThePageFieldGoesToAPageAndFollowsTheScroll()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await component.Find(".ec-preview-pdf-page-input").ChangeAsync(new() { Value = "2" });
        await component.Find(".ec-preview-pdf-page-input").ChangeAsync(new() { Value = "99" });
        await component.Find(".ec-preview-pdf-page-input").ChangeAsync(new() { Value = "abc" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.PdfPagesGoneTo, Is.EqualTo([2, 3]), "a page past the end is the last one, and a word goes nowhere");
            Assert.That(component.Find(".ec-preview-pdf-page-input").GetAttribute("value"), Is.EqualTo("1"), "the field shows the page in view again");
        }

        await previews.PdfView!.Value.PageChanged(3);

        Assert.That(component.Find(".ec-preview-pdf-page-input").GetAttribute("value"), Is.EqualTo("3"));
    }

    [Test]
    public async Task TypingSearchesAndEnterStepsThroughTheMatches()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);
        var search = component.Find(".ec-preview-pdf-search-input");

        await search.InputAsync(new() { Value = "lhůta" });

        Assert.That(component.Find(".ec-preview-pdf-matches").TextContent, Is.EqualTo("Hledám…"));

        await previews.PdfView!.Value.MatchesChanged(3, 12, pending: false);
        await component.Find(".ec-preview-pdf-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await component.Find(".ec-preview-pdf-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
        await Button(component, "Další výskyt").ClickAsync(new MouseEventArgs());
        await Button(component, "Předchozí výskyt").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-pdf-matches").TextContent, Is.EqualTo("3 z 12"));
            Assert.That(
                previews.Finds,
                Is.EqualTo([("lhůta", PdfFind.New), ("lhůta", PdfFind.Next), ("lhůta", PdfFind.Previous), ("lhůta", PdfFind.Next), ("lhůta", PdfFind.Previous)]));
        }
    }

    [Test]
    public async Task ASearchWithoutMatchesSaysSoAndEscapeClearsIt()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await component.Find(".ec-preview-pdf-search-input").InputAsync(new() { Value = "nic" });
        await previews.PdfView!.Value.MatchesChanged(0, 0, pending: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-pdf-matches").TextContent, Is.EqualTo("Nenalezeno"));
            Assert.That(Button(component, "Další výskyt").HasAttribute("disabled"), Is.True);
            Assert.That(component.Find(".ec-preview-pdf-search").HasAttribute("data-ec-keeps-escape"), Is.True, "Escape clears the search and leaves the dialog open");
        }

        await component.Find(".ec-preview-pdf-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.Finds[^1], Is.EqualTo(("", PdfFind.New)), "an empty query clears the highlights");
            Assert.That(component.Find(".ec-preview-pdf-search-input").GetAttribute("value"), Is.Empty);
            Assert.That(component.Find(".ec-preview-pdf-matches").TextContent, Is.Empty);
        }
    }

    [Test]
    public async Task ACountReportedAfterTheSearchClosedLeavesTheStepButtonsDisabled()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await component.Find(".ec-preview-pdf-search-input").InputAsync(new() { Value = "lhůta" });
        await previews.PdfView!.Value.MatchesChanged(3, 12, pending: false);
        await component.Find(".ec-preview-pdf-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        await previews.PdfView.Value.MatchesChanged(3, 12, pending: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Button(component, "Další výskyt").HasAttribute("disabled"), Is.True);
            Assert.That(Button(component, "Předchozí výskyt").HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public void TheSearchIconOpensTheSearchField()
    {
        using var ctx = NewContext(out _);

        var component = Render(ctx);

        Button(component, "Hledat v dokumentu").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-pdf-search").ClassList, Does.Contain("ec-preview-pdf-search-open"));
            Assert.That(Button(component, "Hledat v dokumentu").GetAttribute("aria-expanded"), Is.EqualTo("true"));
        }
    }

    [Test]
    public void APdfTheLibraryCannotOpenRaisesUnreadableAndKeepsTheToolbarDisabled()
    {
        using var ctx = NewContext(out var previews);

        previews.Fails = true;

        var unreadable = 0;
        var component = ctx.Render<PdfPreview>(parameters => parameters
            .Add(static preview => preview.Url, Url)
            .Add(static preview => preview.FileName, "Chráněný.pdf")
            .Add(static preview => preview.OnUnreadable, () => unreadable++));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unreadable, Is.EqualTo(1));
            Assert.That(Button(component, "Zvětšit").HasAttribute("disabled"), Is.True);
            Assert.That(component.Find(".ec-preview-pdf-page-input").HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public async Task RemovingThePreviewReleasesThePdf()
    {
        var ctx = NewContext(out var previews);

        Render(ctx);
        await ctx.DisposeAsync();

        Assert.That(previews.DisposedPdfs, Is.EqualTo(1));
    }

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<PdfPreview> component, string label)
    {
        return component.Find($".ec-preview-pdf-toolbar button[aria-label='{label}']");
    }

    private static BunitContext NewContext(out StubFilePreviews previews)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        previews = new StubFilePreviews();
        ctx.Services.AddSingleton<IFilePreviews>(previews);

        return ctx;
    }

    private static IRenderedComponent<PdfPreview> Render(BunitContext ctx)
    {
        return ctx.Render<PdfPreview>(static parameters => parameters
            .Add(static preview => preview.Url, Url)
            .Add(static preview => preview.FileName, "Rozhodnutí.pdf"));
    }
}
