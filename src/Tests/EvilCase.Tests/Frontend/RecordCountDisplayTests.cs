using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class RecordCountDisplayTests
{
    [TestCase(0, "0 záznamů")]
    [TestCase(1, "1 záznam")]
    [TestCase(2, "2 záznamy")]
    [TestCase(4, "4 záznamy")]
    [TestCase(5, "5 záznamů")]
    [TestCase(16, "16 záznamů")]
    [TestCase(21, "21 záznamů")]
    public void TextMatchesTheCzechPluralRules(int count, string expected)
    {
        Assert.That(RecordCountDisplay.Text(count), Is.EqualTo(expected));
    }
}
