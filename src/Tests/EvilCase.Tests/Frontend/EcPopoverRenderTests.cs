using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcPopoverRenderTests
{
    [Test]
    public async Task EscapeIsKeptFromADialogAroundThePopover()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx);

        await component.Find("button[aria-haspopup=dialog]").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find("[role=dialog]").HasAttribute("data-ec-keeps-escape"), Is.True, "Escape closes the popover and leaves the preview dialog open");
    }

    [TestCase(0, TestName = "ShiftTabOutOfThePanelClosesIt")]
    [TestCase(1, TestName = "TabOutOfThePanelClosesIt")]
    public async Task LeavingThePanelByKeyboardClosesIt(int guard)
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx);
        var triggerId = component.Find("button[aria-haspopup=dialog]").GetAttribute("blazor:elementReference");

        await component.Find("button[aria-haspopup=dialog]").ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-popover-guard")[guard].FocusAsync(new FocusEventArgs());

        Assert.That(component.FindAll("[role=dialog]"), Is.Empty, "no invisible backdrop stays behind the focus");

        var focused = ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2);

        Assert.That(((ElementReference)focused[1].Arguments[0]!).Id, Is.EqualTo(triggerId));
    }

    private static IRenderedComponent<EcPopover> Render(BunitContext ctx)
    {
        return ctx.Render<EcPopover>(static parameters => parameters
            .Add(static popover => popover.AriaLabel, "Podrobnosti")
            .Add(static popover => popover.Trigger, static builder => builder.AddContent(0, "Stav"))
            .Add(static popover => popover.ChildContent, static builder => builder.AddContent(0, "Obsah")));
    }
}
