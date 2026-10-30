using Bunit;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class PdfPreviewRenderTests
{
    private const string Url = "/api/files/0f8fad5b-d9cb-469f-a165-70867728950e/content";

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
    public async Task ACountReportedAfterTheSearchClosedLeavesTheStepButtonsDisabled()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await component.Find(".ec-preview-toolbar-search-input").InputAsync(new() { Value = "lhůta" });
        await previews.PdfView!.Value.MatchesChanged(3, 12, pending: false);
        await component.Find(".ec-preview-toolbar-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        await previews.PdfView.Value.MatchesChanged(3, 12, pending: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Button(component, "Další výskyt").HasAttribute("disabled"), Is.True);
            Assert.That(Button(component, "Předchozí výskyt").HasAttribute("disabled"), Is.True);
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
            Assert.That(component.Find(".ec-preview-toolbar-page-input").HasAttribute("disabled"), Is.True);
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
        return component.Find($".ec-preview-toolbar button[aria-label='{label}']");
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
