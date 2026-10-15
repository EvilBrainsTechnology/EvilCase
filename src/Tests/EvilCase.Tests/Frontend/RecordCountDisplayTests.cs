using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class RecordCountDisplayTests
{
    [TestCase(0, "0\u00A0záznamů")]
    [TestCase(1, "1\u00A0záznam")]
    [TestCase(2, "2\u00A0záznamy")]
    [TestCase(4, "4\u00A0záznamy")]
    [TestCase(5, "5\u00A0záznamů")]
    [TestCase(16, "16\u00A0záznamů")]
    [TestCase(21, "21\u00A0záznamů")]
    public void TextMatchesTheCzechPluralRules(int count, string expected)
    {
        Assert.That(RecordCountDisplay.Text(count), Is.EqualTo(expected));
    }
}
