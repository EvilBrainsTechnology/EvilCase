using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ChildCaseCountDisplayTests
{
    [TestCase(6, 9, "6 otevřených·9 uzavřených")]
    [TestCase(1, 2, "1 otevřený·2 uzavřené")]
    [TestCase(0, 1, "0 otevřených·1 uzavřený")]
    [TestCase(4, 5, "4 otevřené·5 uzavřených")]
    [TestCase(2, 0, "2 otevřené·0 uzavřených")]
    public void TheCountsReadInCzechPlural(int openCount, int closedCount, string expected)
    {
        Assert.That(string.Join('·', ChildCaseCountDisplay.Parts(openCount, closedCount)), Is.EqualTo(expected));
    }
}
