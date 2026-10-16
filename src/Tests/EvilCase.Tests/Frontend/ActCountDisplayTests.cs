using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActCountDisplayTests
{
    [TestCase(0, "0 úkonů")]
    [TestCase(1, "1 úkon")]
    [TestCase(2, "2 úkony")]
    [TestCase(4, "4 úkony")]
    [TestCase(5, "5 úkonů")]
    [TestCase(23, "23 úkonů")]
    public void TheCountReadsInCzechPlural(int count, string expected)
    {
        Assert.That(ActCountDisplay.Text(count), Is.EqualTo(expected));
    }

    [TestCase(1, "Všechny úkony spisu")]
    [TestCase(3, "Všechny 3 úkony spisu")]
    [TestCase(23, "Všech 23 úkonů spisu")]
    public void TheLinkToTheWholeCaseReadsInCzechPlural(int count, string expected)
    {
        Assert.That(ActCountDisplay.AllText(count), Is.EqualTo(expected));
    }
}
