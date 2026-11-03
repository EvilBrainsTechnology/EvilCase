using Bunit;
using EvilBrains.EvilCase.App.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class NavLinksRenderTests
{
    [TestCase("", "Přehled")]
    [TestCase("cases", "Spisy")]
    [TestCase("cases/01a1a104-97fe-7957-9e17-ee8259bfcf6b/edit?x=1", "Spisy")]
    [TestCase("acts/01a1a104-d756-7a1c-a597-6a3c04f28bba", "Úkony")]
    [TestCase("contacts/01a1a104-d5ef-7d32-ad6c-363ce48b4625", "Kontakty")]
    [TestCase("settings/api-tokens", "Nastavení")]
    public async Task TheMenuMarksTheAgendaOfTheRouteAndItsSubroutes(string path, string agenda)
    {
        await using var ctx = FrontendContext.Create();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(path);

        var component = ctx.Render<NavLinks>();

        var current = component.FindAll("[aria-current=page]").Select(static link => link.TextContent.Trim());
        string[] expected = [agenda];

        Assert.That(current, Is.EqualTo(expected), "exactly one item, the route's agenda, is current; Přehled only on its own route");
    }

    [Test]
    public async Task TheCurrentItemFollowsANavigation()
    {
        await using var ctx = FrontendContext.Create();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("cases");

        var component = ctx.Render<NavLinks>();
        navigation.NavigateTo("acts");

        await component.WaitForAssertionAsync(() =>
            Assert.That(component.Find("[aria-current=page]").TextContent.Trim(), Is.EqualTo("Úkony")));
    }
}
