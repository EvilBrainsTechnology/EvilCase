using Bunit;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class TopBarRenderTests
{
    private static readonly string[] ExpectedHrefs = ["/", "/cases", "/contacts", "/settings"];

    private static readonly string[] ExpectedTexts = ["Přehled", "Spisy", "Kontakty", "Nastavení"];

    [Test]
    public async Task TheBarCarriesTheBrandTheMenuTheSearchAndTheUserMenu()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        var component = ctx.Render<TopBar>();

        var links = component.FindAll(".ec-topbar-nav .ec-nav-link");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-brand"), Is.Not.Empty);
            Assert.That(
                links,
                Has.Count.EqualTo(4),
                "the bar carries Přehled, Spisy, Kontakty a Nastavení (SDD-016)");
            Assert.That(links.Select(static link => link.GetAttribute("href")), Is.EqualTo(ExpectedHrefs));
            Assert.That(links.Select(static link => link.TextContent.Trim()), Is.EqualTo(ExpectedTexts));
            Assert.That(component.FindAll(".ec-search-input"), Is.Not.Empty);
            Assert.That(component.FindAll(".ec-avatar"), Is.Not.Empty);
        }
    }

    [Test]
    public async Task TheMenuMarksTheAgendaThePageBelongsTo()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases/" + Guid.NewGuid());

        var component = ctx.Render<TopBar>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find(".ec-topbar-nav a[href='/cases']").GetAttribute("aria-current"),
                Is.EqualTo("page"),
                "the menu marks its item on a sub-route too (SDD-016)");
            Assert.That(component.Find(".ec-topbar-nav a[href='/']").GetAttribute("aria-current"), Is.Null);
        }
    }

    [Test]
    public async Task SearchingInTheBarOpensTheCaseListNarrowedByTheText()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<TopBar>();

        await component.Find(".ec-search-input").InputAsync(new ChangeEventArgs { Value = "rychlost" });
        await component.Find("form.ec-search").SubmitAsync();

        Assert.That(navigation.Uri, Does.EndWith("/cases?q=rychlost"));
    }

    [Test]
    public async Task AnEmptySearchOpensTheCaseListUnnarrowed()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<TopBar>();

        await component.Find("form.ec-search").SubmitAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(navigation.Uri, Does.EndWith("/cases"));
            Assert.That(navigation.Uri, Does.Not.Contain('?'));
        }
    }

    [Test]
    public async Task TheBarFoldsIntoADrawerAndTheDrawerClosesOnANavigation()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        var component = ctx.Render<TopBar>();

        Assert.That(
            component.Find(".ec-topbar-burger").GetAttribute("aria-expanded"),
            Is.EqualTo("false"),
            "a collapsed control says so, not by leaving the state out (SDD-020)");

        await component.Find(".ec-topbar-burger").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-topbar-burger").GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(component.FindAll(".ec-drawer .ec-nav-link"), Has.Count.EqualTo(4));
        }

        await component.Find(".ec-drawer .ec-nav-link").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-drawer"), Is.Empty, "a link in the drawer closes it (SDD-020)");
    }

    [Test]
    public async Task TheUserMenuOffersSigningOutOfThisDeviceAndOfEveryDevice()
    {
        await using var ctx = new BunitContext();
        var session = new StubAuthSession();
        Arrange(ctx, session);

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<TopBar>();

        Assert.That(
            component.Find(".ec-avatar").GetAttribute("aria-expanded"),
            Is.EqualTo("false"),
            "a collapsed control says so, not by leaving the state out (SDD-020)");

        await component.Find(".ec-avatar").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find(".ec-avatar").GetAttribute("aria-expanded"), Is.EqualTo("true"));

        var items = component.FindAll(".ec-menu-item").Select(static item => item.TextContent.Trim()).ToArray();

        Assert.That(items, Has.Some.Contains("Odhlásit se"));
        Assert.That(items, Has.Some.Contains("Odhlásit všechna zařízení"));

        var signOutEverywhere = component
            .FindAll(".ec-menu-item")
            .Single(static item => item.TextContent.Contains("Odhlásit všechna zařízení", StringComparison.Ordinal));

        await signOutEverywhere.ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.SignOuts, Has.Count.EqualTo(1));
            Assert.That(session.SignOuts[0], Is.True);
            Assert.That(navigation.Uri, Does.EndWith("login"));
        }
    }

    [Test]
    public async Task EveryControlWithoutTextCarriesALabel()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        var component = ctx.Render<TopBar>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find(".ec-topbar-burger").GetAttribute("aria-label"),
                Is.EqualTo("Otevřít menu"),
                "a control the keyboard reaches must say what it does (SDD-020)");
            Assert.That(
                component.Find(".ec-search-input").GetAttribute("aria-label"),
                Is.EqualTo("Hledat spis"),
                "a control the keyboard reaches must say what it does (SDD-020)");
            Assert.That(
                component.Find(".ec-avatar").GetAttribute("aria-label"),
                Does.Contain("spravce@example.cz"),
                "a control the keyboard reaches must say what it does (SDD-020)");
        }
    }

    [Test]
    public async Task TheBarCarriesNoTablerClass()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, new StubAuthSession());

        var component = ctx.Render<TopBar>();

        Assert.That(
            component.FindAll(".navbar, .navbar-nav, .nav-link, .dropdown-menu, .dropdown-item, .btn"),
            Is.Empty,
            "the shell carries no Tabler class (SDD-020)");
    }

    private static void Arrange(BunitContext ctx, StubAuthSession session)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddTabBlazor(static _ => { });
        ctx.Services.AddSingleton<IAuthSession>(session);
        ctx.AddAuthorization().SetAuthorized("spravce@example.cz");
    }
}
