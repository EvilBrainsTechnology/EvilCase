using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActCountDisplayTests
{
    [TestCase(0, "0\u00A0úkonů")]
    [TestCase(1, "1\u00A0úkon")]
    [TestCase(2, "2\u00A0úkony")]
    [TestCase(4, "4\u00A0úkony")]
    [TestCase(5, "5\u00A0úkonů")]
    [TestCase(23, "23\u00A0úkonů")]
    public void TheCountReadsInCzechPlural(int count, string expected)
    {
        Assert.That(ActCountDisplay.Text(count), Is.EqualTo(expected));
    }

    [TestCase(1, "Všechny úkony spisu")]
    [TestCase(3, "Všechny 3\u00A0úkony spisu")]
    [TestCase(23, "Všech 23\u00A0úkonů spisu")]
    public void TheLinkToTheWholeCaseReadsInCzechPlural(int count, string expected)
    {
        Assert.That(ActCountDisplay.AllText(count), Is.EqualTo(expected));
    }
}
