using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcMenuRenderTests
{
    [Test]
    public async Task ArrowsMoveTheFocusAndWrapAround()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx, []);

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());

        // bUnit drops the element reference from the markup once an element renders again, so the ids are read first.
        var itemIds = component.FindAll("[role=menuitem]").Select(static item => item.GetAttribute("blazor:elementReference")).ToArray();

        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        await component.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        var focused = ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((ElementReference)focused[1].Arguments[0]!).Id, Is.EqualTo(itemIds[1]));
            Assert.That(((ElementReference)focused[2].Arguments[0]!).Id, Is.EqualTo(itemIds[0]));
            Assert.That(((ElementReference)focused[3].Arguments[0]!).Id, Is.EqualTo(itemIds[2]), "the focus wraps around");
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
            new EcMenuItem { Text = "Smazat úkon", Icon = AppIcons.Trash, OnSelect = EventCallback.Factory.Create(chosen, () => chosen.Add("delete")), Danger = true, DividerBefore = true },
        ];

        return ctx.Render<EcMenu>(parameters => parameters
            .Add(static menu => menu.AriaLabel, "Další akce s úkonem")
            .Add(static menu => menu.Items, items));
    }
}
