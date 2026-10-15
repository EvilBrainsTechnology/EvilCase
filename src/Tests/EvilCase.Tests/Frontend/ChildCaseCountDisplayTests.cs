using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ChildCaseCountDisplayTests
{
    [TestCase(6, 9, "6\u00A0otevřených\u00A0· 9\u00A0uzavřených")]
    [TestCase(1, 2, "1\u00A0otevřený\u00A0· 2\u00A0uzavřené")]
    [TestCase(0, 1, "0\u00A0otevřených\u00A0· 1\u00A0uzavřený")]
    [TestCase(4, 5, "4\u00A0otevřené\u00A0· 5\u00A0uzavřených")]
    [TestCase(2, 0, "2\u00A0otevřené\u00A0· 0\u00A0uzavřených")]
    public void TheCountsReadInCzechPlural(int openCount, int closedCount, string expected)
    {
        Assert.That(ChildCaseCountDisplay.Text(openCount, closedCount), Is.EqualTo(expected));
    }
}
