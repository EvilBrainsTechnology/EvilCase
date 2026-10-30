using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcModalRenderTests
{
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

    [Test]
    public async Task ADialogRemovedWhileOpenLetsThePageScroll()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var scrollLock = ctx.JSInterop
            .SetupModule("./js/ec-modal.js")
            .SetupModule("openModal", static _ => true);
        RenderOpen(ctx, static _ => { });

        await ctx.DisposeComponentsAsync();

        scrollLock.VerifyInvoke("release");
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
