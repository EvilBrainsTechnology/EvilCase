using EvilBrains.EvilCase.App.Search;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchTextTests
{
    [Test]
    public void MatchesFindsADiacriticTextFromAPlainSearch()
    {
        Assert.That(SearchText.Matches("Žádost o informace", "zadost"), Is.True);
    }

    [Test]
    public void MatchesFindsAPlainTextFromADiacriticSearch()
    {
        Assert.That(SearchText.Matches("Zadost o informace", "žádost"), Is.True);
    }

    [Test]
    public void MatchesIsFalseWhenTheWordIsAbsent()
    {
        Assert.That(SearchText.Matches("Ministerstvo dopravy", "úřad"), Is.False);
    }

    [Test]
    public void AnEmptySearchMatchesEverything()
    {
        Assert.That(SearchText.Matches("cokoli", "   "), Is.True);
    }
}
