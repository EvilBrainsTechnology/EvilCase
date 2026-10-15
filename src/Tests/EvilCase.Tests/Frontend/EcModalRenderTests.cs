using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcModalRenderTests
{
    [Test]
    public void TheTitleTheBodyAndTheFooterRenderInTheDialog()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = ctx.Render<EcModal>(static parameters => parameters
            .Add(static modal => modal.Title, "Nový kontakt")
            .Add(static modal => modal.Open, value: false)
            .Add(static modal => modal.ChildContent, "Tělo")
            .Add(static modal => modal.Footer, "Patička"));

        var dialog = component.Find("dialog.ec-modal");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dialog.HasAttribute("open"), Is.False);
            Assert.That(component.Find(".ec-modal-title").TextContent, Is.EqualTo("Nový kontakt"));
            Assert.That(component.Find(".ec-modal-body").TextContent, Does.Contain("Tělo"));
            Assert.That(component.Find(".ec-modal-footer").TextContent, Does.Contain("Patička"));
        }
    }

    [Test]
    public void AHeadingReplacesTheTitleInTheHeaderAndTheTitleStillNamesTheDialog()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = ctx.Render<EcModal>(static parameters => parameters
            .Add(static modal => modal.Title, "Rozhodnutí.pdf")
            .Add(static modal => modal.Heading, "Vlastní hlavička")
            .Add(static modal => modal.Large, value: true));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-modal-header").TextContent, Does.Contain("Vlastní hlavička"));
            Assert.That(component.FindAll(".ec-modal-title"), Is.Empty);
            Assert.That(component.Find("dialog").GetAttribute("aria-label"), Is.EqualTo("Rozhodnutí.pdf"));
            Assert.That(component.Find("dialog").ClassList, Does.Contain("ec-modal-large"));
            Assert.That(component.Find(".ec-modal-header button[aria-label=Zavřít]"), Is.Not.Null, "the close button stays");
        }
    }

    [Test]
    public void TheCloseButtonRaisesOpenChangedWithFalse()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;

        var component = ctx.Render<EcModal>(parameters => parameters
            .Add(static modal => modal.Title, "Nový kontakt")
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.OpenChanged, value => raised = value));

        component.Find(".ec-modal-header button").Click();

        Assert.That(raised, Is.False);
    }

    [Test]
    public async Task ClosingFromTheBrowserRaisesOpenChangedWithFalse()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;

        var component = ctx.Render<EcModal>(parameters => parameters
            .Add(static modal => modal.Title, "Nový kontakt")
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.OpenChanged, value => raised = value));

        await component.InvokeAsync(component.Instance.NotifyClosed);

        Assert.That(raised, Is.False, "Esc closes the dialog through its close event");
    }
}
