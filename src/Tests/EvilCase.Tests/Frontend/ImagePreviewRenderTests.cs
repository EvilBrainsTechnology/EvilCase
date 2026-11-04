using Bunit;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ImagePreviewRenderTests
{
    [Test]
    public async Task TheZoomWaitsForTheImageToLoad()
    {
        await using var ctx = NewContext(out var previews);

        var component = Render(ctx);

        Assert.That(Button(component, "Zvětšit").HasAttribute("disabled"), Is.True, "an image not yet loaded has no size to zoom");

        await component.Find("img.ec-preview-image").TriggerEventAsync("onload", new ProgressEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.OpenedImageViews, Is.EqualTo(1));
            Assert.That(Button(component, "Zvětšit").HasAttribute("disabled"), Is.False);
        }
    }

    [Test]
    public async Task RemovingThePreviewReleasesTheZoomOfALoadedImage()
    {
        var ctx = NewContext(out var previews);

        var component = Render(ctx);

        await component.Find("img.ec-preview-image").TriggerEventAsync("onload", new ProgressEventArgs());
        await ctx.DisposeAsync();

        Assert.That(previews.DisposedViews, Is.EqualTo(1));
    }

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<ImagePreview> component, string label)
    {
        return component.Find($".ec-preview-toolbar button[aria-label='{label}']");
    }

    private static BunitContext NewContext(out StubFilePreviews previews)
    {
        var ctx = FrontendContext.Create();

        previews = ctx.Services.GetRequiredService<StubFilePreviews>();

        return ctx;
    }

    private static IRenderedComponent<ImagePreview> Render(BunitContext ctx)
    {
        return ctx.Render<ImagePreview>(static parameters => parameters
            .Add(static preview => preview.Url, "/api/files/0f8fad5b-d9cb-469f-a165-70867728950e/content")
            .Add(static preview => preview.FileName, "Fotografie.jpg"));
    }
}
