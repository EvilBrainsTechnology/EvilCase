using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcPageHeaderRenderTests
{
    [Test]
    public async Task TheBackLinkLeadsToTheNearestCrumbWithAnAddress()
    {
        await using var ctx = FrontendContext.Create();
        EcCrumb[] crumbs = [new("Spisy", "/cases"), new("Překročení rychlosti", "/cases/1"), new("Odvolání", "/acts/2"), new("Upravit", Href: null)];

        var component = ctx.Render<EcPageHeader>(parameters => parameters
            .Add(static header => header.Title, "Upravit úkon")
            .Add(static header => header.Breadcrumbs, crumbs));

        var back = component.Find(".ec-page-back");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(back.GetAttribute("href"), Is.EqualTo("/acts/2"), "the link goes to the parent, not the page itself or the root");
            Assert.That(back.TextContent.Trim(), Is.EqualTo("Odvolání"));
        }
    }

    [Test]
    public async Task APageWithoutCrumbsHasNoBackLink()
    {
        await using var ctx = FrontendContext.Create();

        var component = ctx.Render<EcPageHeader>(static parameters => parameters.Add(static header => header.Title, "Spisy"));

        Assert.That(component.FindAll(".ec-page-back"), Is.Empty, "a top-level page has no parent to go back to");
    }
}
