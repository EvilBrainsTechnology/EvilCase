using Bunit;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseCountTileRenderTests
{
    [Test]
    public void TheTileNamesItsStatusAndItsCountAndLeadsToTheCaseList()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<CaseCountTile>(static parameters => parameters
            .Add(static tile => tile.Status, CaseStatus.WaitingOnAuthority)
            .Add(static tile => tile.Count, 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-stat-value").TextContent, Is.EqualTo("3"));
            Assert.That(component.Find(".ec-stat-label").TextContent, Is.EqualTo("Čeká na úřad"));
            Assert.That(component.Find(".ec-stat-icon").ClassList, Does.Contain("ec-stat-icon-waiting"));
            Assert.That(component.Find("a.ec-stat").GetAttribute("href"), Is.EqualTo("/cases?status=WaitingOnAuthority&scope=All"));
            Assert.That(component.Find(".ec-stat-more").TextContent, Is.EqualTo("Zobrazit"));
        }
    }

    [TestCase(CaseStatus.Active, "Active")]
    [TestCase(CaseStatus.WaitingOnAuthority, "WaitingOnAuthority")]
    [TestCase(CaseStatus.Closed, "Closed")]
    public void EveryTileOpensTheListOfTheCasesItCounts(CaseStatus status, string filter)
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<CaseCountTile>(parameters => parameters
            .Add(static tile => tile.Status, status)
            .Add(static tile => tile.Count, 1));

        Assert.That(
            component.Find("a.ec-stat").GetAttribute("href"),
            Is.EqualTo($"/cases?status={filter}&scope=All"),
            "the counts include the sub-cases, so the list does as well");
    }

    [Test]
    public void ACountStillLoadingShowsADashSoTheTileKeepsItsHeight()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<CaseCountTile>(static parameters => parameters
            .Add(static tile => tile.Status, CaseStatus.Active)
            .Add(static tile => tile.Count, value: null));

        Assert.That(component.Find(".ec-stat-value").TextContent, Is.EqualTo("—"));
    }
}
