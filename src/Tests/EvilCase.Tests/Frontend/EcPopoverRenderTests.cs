using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcPopoverRenderTests
{
    [Test]
    public async Task TheClosedPopoverIsOnlyItsTrigger()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx);
        var trigger = component.Find("button[aria-haspopup=dialog]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.TextContent.Trim(), Is.EqualTo("Stav"));
            Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(component.FindAll("[role=dialog]"), Is.Empty);
        }
    }

    [Test]
    public async Task OpeningShowsTheContentAndFocusesIt()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx);

        await component.Find("button[aria-haspopup=dialog]").ClickAsync(new MouseEventArgs());

        var dialog = component.Find("[role=dialog]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dialog.GetAttribute("aria-label"), Is.EqualTo("Podrobnosti"));
            Assert.That(dialog.TextContent.Trim(), Is.EqualTo("Obsah"));
            Assert.That(component.Find("button[aria-haspopup=dialog]").GetAttribute("aria-expanded"), Is.EqualTo("true"));
        }

        ctx.JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(dialog);
    }

    [Test]
    public async Task EscapeClosesThePopoverAndReturnsTheFocusToTheTrigger()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx);

        // bUnit drops the element reference from the markup once an element renders again, so the id is read first.
        var triggerId = component.Find("button[aria-haspopup=dialog]").GetAttribute("blazor:elementReference");

        await component.Find("button[aria-haspopup=dialog]").ClickAsync(new MouseEventArgs());
        await component.Find("[role=dialog]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(component.FindAll("[role=dialog]"), Is.Empty);

        var focused = ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);

        Assert.That(((ElementReference)focused[1].Arguments[0]!).Id, Is.EqualTo(triggerId));
    }

    [Test]
    public async Task AClickOutsideClosesThePopover()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx);

        await component.Find("button[aria-haspopup=dialog]").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-popover-backdrop").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll("[role=dialog]"), Is.Empty);
    }

    private static IRenderedComponent<EcPopover> Render(BunitContext ctx)
    {
        return ctx.Render<EcPopover>(static parameters => parameters
            .Add(static popover => popover.AriaLabel, "Podrobnosti")
            .Add(static popover => popover.Trigger, static builder => builder.AddContent(0, "Stav"))
            .Add(static popover => popover.ChildContent, static builder => builder.AddContent(0, "Obsah")));
    }
}
