using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class NumberDisplayTests
{
    [Test]
    public void ThousandsAreGroupedByANonBreakingSpace()
    {
        Assert.That(NumberDisplay.Text(1234567), Is.EqualTo("1\u00A0234\u00A0567"));
    }
}
