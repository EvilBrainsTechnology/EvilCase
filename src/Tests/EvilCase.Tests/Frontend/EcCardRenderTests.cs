using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcCardRenderTests
{
    [Test]
    public void ACardWithoutATitleOrAFooterRendersNeither()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcCard>(static parameters => parameters.AddChildContent("Obsah"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-card-header"), Is.Empty);
            Assert.That(component.FindAll(".ec-card-footer"), Is.Empty);
            Assert.That(component.Find(".ec-card-body").TextContent, Does.Contain("Obsah"));
        }
    }

    [Test]
    public void TheTitleIsTheCardsHeadingAndTheFooterItsOwnRow()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcCard>(static parameters => parameters
            .Add(static card => card.Title, "Úkony")
            .Add(static card => card.Footer, "Patička")
            .AddChildContent("Obsah"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-card-header h2").TextContent.Trim(), Is.EqualTo("Úkony"));
            Assert.That(component.Find(".ec-card-footer").TextContent, Does.Contain("Patička"));
        }
    }

    [Test]
    public void TheCardCarriesNoForeignClass()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcCard>(static parameters => parameters
            .Add(static card => card.Title, "Úkony")
            .Add(static card => card.Footer, "Patička")
            .AddChildContent("Obsah"));

        Assert.That(
            component.FindAll("*").All(static element => element.ClassList.All(static name => name.StartsWith("ec-", StringComparison.Ordinal))),
            Is.True,
            "a new component carries no bootstrap or Tabler class");
    }
}
