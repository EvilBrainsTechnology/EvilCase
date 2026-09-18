using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcPageHeaderRenderTests
{
    [Test]
    public void TheTitleIsTheHeadingAndNothingElseRenders()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcPageHeader>(static parameters => parameters.Add(static header => header.Title, "Spisy"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("h1").TextContent.Trim(), Is.EqualTo("Spisy"));
            Assert.That(component.FindAll("nav"), Is.Empty);
            Assert.That(component.FindAll(".ec-page-actions"), Is.Empty);
        }
    }

    [Test]
    public void TheCrumbsRenderInOrderAndTheLastOneIsCurrent()
    {
        using var ctx = new BunitContext();

        EcCrumb[] crumbs =
        [
            new EcCrumb("Spisy", "/spisy"),
            new EcCrumb("EC/20260821-001", "/spisy/1"),
            new EcCrumb("Úkon", Href: null),
        ];

        var component = ctx.Render<EcPageHeader>(parameters => parameters
            .Add(static header => header.Title, "Úkon")
            .Add(static header => header.Breadcrumbs, crumbs));

        var texts = component.FindAll(".ec-crumb").Select(static element => element.TextContent.Trim());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(texts, Is.EqualTo(crumbs.Select(static crumb => crumb.Text)));
            Assert.That(component.FindAll("a.ec-crumb"), Has.Count.EqualTo(2));
            Assert.That(component.FindAll(".ec-crumb")[^1].GetAttribute("aria-current"), Is.EqualTo("page"));
            Assert.That(component.FindAll(".ec-crumb-separator"), Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void TheStatusAndTheActionsStandBesideTheTitle()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcPageHeader>(static parameters => parameters
            .Add(static header => header.Title, "Spisy")
            .Add(static header => header.Status, "Aktivní")
            .Add(static header => header.Actions, "Nový spis"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-page-header-row").TextContent, Does.Contain("Aktivní"));
            Assert.That(component.Find(".ec-page-header-row").TextContent, Does.Contain("Nový spis"));
            Assert.That(component.Find(".ec-page-actions").TextContent, Does.Contain("Nový spis"));
        }
    }
}
