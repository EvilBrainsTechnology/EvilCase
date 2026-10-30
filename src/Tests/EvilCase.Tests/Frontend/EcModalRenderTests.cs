using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using Microsoft.AspNetCore.Components.Web;
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
    public void ASheetCarriesItsClass()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = ctx.Render<EcModal>(static parameters => parameters
            .Add(static modal => modal.Title, "Verze souboru")
            .Add(static modal => modal.Sheet, value: true)
            .Add(static modal => modal.Open, value: false));

        Assert.That(component.Find("dialog").ClassList, Does.Contain("ec-modal-sheet"));
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

    [Test]
    public async Task AClickOnTheBackdropRaisesOpenChangedWithFalse()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;
        var component = RenderOpen(ctx, value => raised = value);

        await component.Find("dialog").TriggerEventAsync("onpointerdown", new PointerEventArgs());
        await component.Find("dialog").ClickAsync(new MouseEventArgs());

        Assert.That(raised, Is.False);
    }

    [Test]
    public async Task AClickInTheContentKeepsTheDialogOpen()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;
        var component = RenderOpen(ctx, value => raised = value);

        await component.Find("button.content").TriggerEventAsync("onpointerdown", new PointerEventArgs());
        await component.Find("button.content").ClickAsync(new MouseEventArgs());

        Assert.That(raised, Is.Null);
    }

    [Test]
    public async Task ADragFromTheContentToTheBackdropKeepsTheDialogOpen()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;
        var component = RenderOpen(ctx, value => raised = value);

        await component.Find("button.content").TriggerEventAsync("onpointerdown", new PointerEventArgs());
        await component.Find("dialog").ClickAsync(new MouseEventArgs());

        Assert.That(raised, Is.Null);
    }

    [Test]
    public void OnlyAGuardedDialogIsMarked()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var guarded = RenderGuarded(ctx, static _ => { }, static () => { });
        var plain = RenderOpen(ctx, static _ => { });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guarded.Find("dialog").HasAttribute("data-ec-guarded"), Is.True);
            Assert.That(plain.Find("dialog").HasAttribute("data-ec-guarded"), Is.False);
        }
    }

    [Test]
    public async Task TheCloseButtonOfAGuardedDialogAsksItsOwnerAndLeavesItOpen()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;
        var requested = 0;
        var component = RenderGuarded(ctx, value => raised = value, () => requested++);

        await component.Find(".ec-modal-header button[aria-label=Zavřít]").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(raised, Is.Null, "the owner closes a guarded dialog");
        }
    }

    [Test]
    public async Task AClickOnTheBackdropOfAGuardedDialogAsksItsOwner()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;
        var requested = 0;
        var component = RenderGuarded(ctx, value => raised = value, () => requested++);

        await component.Find("dialog").TriggerEventAsync("onpointerdown", new PointerEventArgs());
        await component.Find("dialog").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(raised, Is.Null, "the owner closes a guarded dialog");
        }
    }

    [Test]
    public async Task EscapeOnAGuardedDialogAsksItsOwner()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        bool? raised = null;
        var requested = 0;
        var component = RenderGuarded(ctx, value => raised = value, () => requested++);

        await component.InvokeAsync(component.Instance.NotifyCloseRequested);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(raised, Is.Null, "the owner closes a guarded dialog");
        }
    }

    [Test]
    public async Task AGuardedDialogTheBrowserClosedAnywayAsksItsOwnerAndOpensAgain()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var module = ctx.JSInterop.SetupModule("./js/ec-modal.js");
        bool? raised = null;
        var requested = 0;
        var component = RenderGuarded(ctx, value => raised = value, () => requested++);

        await component.InvokeAsync(component.Instance.NotifyClosed);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requested, Is.EqualTo(1));
            Assert.That(raised, Is.Null, "the owner closes a guarded dialog");
            Assert.That(module.Invocations["openModal"], Has.Count.EqualTo(2), "the dialog opens again until its owner closes it");
        }
    }

    private static IRenderedComponent<EcModal> RenderGuarded(BunitContext ctx, Action<bool> openChanged, Action closeRequest)
    {
        return ctx.Render<EcModal>(parameters => parameters
            .Add(static modal => modal.Title, "Poznámky.txt")
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.OpenChanged, openChanged)
            .Add(static modal => modal.OnCloseRequest, closeRequest));
    }

    private static IRenderedComponent<EcModal> RenderOpen(BunitContext ctx, Action<bool> openChanged)
    {
        return ctx.Render<EcModal>(parameters => parameters
            .Add(static modal => modal.Title, "Nový kontakt")
            .Add(static modal => modal.Open, value: true)
            .Add(
                static modal => modal.ChildContent,
                static builder =>
                {
                    builder.OpenElement(0, "button");
                    builder.AddAttribute(1, "class", "content");
                    builder.AddAttribute(2, "onpointerdown", static () => { });
                    builder.AddAttribute(3, "onclick", static () => { });
                    builder.CloseElement();
                })
            .Add(static modal => modal.OpenChanged, openChanged));
    }
}
