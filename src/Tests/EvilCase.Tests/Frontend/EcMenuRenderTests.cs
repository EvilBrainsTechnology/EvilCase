using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcMenuRenderTests
{
    [Test]
    public async Task TheClosedMenuIsOnlyItsTrigger()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx, []);
        var trigger = component.Find("button[aria-haspopup=menu]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.GetAttribute("aria-label"), Is.EqualTo("Další akce s úkonem"));
            Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(component.FindAll("[role=menu]"), Is.Empty);
        }
    }

    [Test]
    public async Task OpeningShowsTheItemsAndFocusesTheFirst()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx, []);
        var trigger = component.Find("button[aria-haspopup=menu]");

        await trigger.ClickAsync(new MouseEventArgs());

        var items = component.FindAll("[role=menuitem]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("button[aria-haspopup=menu]").GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(
                items.Select(static item => item.TextContent.Trim()),
                Is.EqualTo(["Nový úkon ve spisu", "Zkopírovat odkaz na úkon", "Smazat úkon…"]));
            Assert.That(component.FindAll("[role=separator]"), Has.Count.EqualTo(1));
            Assert.That(items[2].ClassList, Does.Contain("ec-menu-item-danger"));
        }

        ctx.JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(items[0]);
    }

    [Test]
    public async Task ArrowsMoveTheFocusAndWrapAround()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx, []);

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        var focused = ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 4);
        var items = component.FindAll("[role=menuitem]");

        using (Assert.EnterMultipleScope())
        {
            focused[1].Arguments[0].ShouldBeElementReferenceTo(items[1]);
            focused[2].Arguments[0].ShouldBeElementReferenceTo(items[0]);
            focused[3].Arguments[0].ShouldBeElementReferenceTo(items[2]);
        }
    }

    [Test]
    public async Task EscapeClosesTheMenuAndReturnsTheFocusToTheTrigger()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx, []);

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        var trigger = component.Find("button[aria-haspopup=menu]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("[role=menu]"), Is.Empty);
            Assert.That(trigger.GetAttribute("aria-expanded"), Is.EqualTo("false"));
        }

        ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 2)[1].Arguments[0].ShouldBeElementReferenceTo(trigger);
    }

    [Test]
    public async Task AClickOutsideClosesTheMenuAndChoosesNothing()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        List<string> chosen = [];
        var component = Render(ctx, chosen);

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-menu-backdrop").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("[role=menu]"), Is.Empty);
            Assert.That(chosen, Is.Empty, "a click outside is no choice");
        }
    }

    [Test]
    public async Task ClickingAnItemRunsItAndClosesTheMenu()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        List<string> chosen = [];
        var component = Render(ctx, chosen);

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chosen, Is.EqualTo(["delete"]));
            Assert.That(component.FindAll("[role=menu]"), Is.Empty);
        }
    }

    [Test]
    public async Task EnterRunsTheItemTheArrowsReached()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        List<string> chosen = [];
        var component = Render(ctx, chosen);

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(chosen, Is.EqualTo(["link"]));
            Assert.That(component.FindAll("[role=menu]"), Is.Empty);
        }
    }

    private static IRenderedComponent<EcMenu> Render(BunitContext ctx, List<string> chosen)
    {
        EcMenuItem[] items =
        [
            new EcMenuItem { Text = "Nový úkon ve spisu", Icon = AppIcons.Plus, OnSelect = EventCallback.Factory.Create(chosen, () => chosen.Add("new")) },
            new EcMenuItem { Text = "Zkopírovat odkaz na úkon", Icon = AppIcons.Link, OnSelect = EventCallback.Factory.Create(chosen, () => chosen.Add("link")) },
            new EcMenuItem { Text = "Smazat úkon…", Icon = AppIcons.Trash, OnSelect = EventCallback.Factory.Create(chosen, () => chosen.Add("delete")), Danger = true, DividerBefore = true },
        ];

        return ctx.Render<EcMenu>(parameters => parameters
            .Add(static menu => menu.AriaLabel, "Další akce s úkonem")
            .Add(static menu => menu.Items, items));
    }
}
