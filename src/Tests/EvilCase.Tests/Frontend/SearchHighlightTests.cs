using EvilBrains.EvilCase.App.Search;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchHighlightTests
{
    [Test]
    public void TheMatchIgnoresCaseAndDiacriticsAndKeepsTheOriginalText()
    {
        var segments = SearchHighlight.Segments("Uplatnění POJIŠTĚNÍ právní ochrany", "pojisteni");

        HighlightSegment[] expected =
        [
            new("Uplatnění ", Match: false),
            new("POJIŠTĚNÍ", Match: true),
            new(" právní ochrany", Match: false),
        ];

        Assert.That(segments, Is.EqualTo(expected), "the marked stretch is the original text, not the folded one");
    }

    [Test]
    public void EveryOccurrenceIsMarked()
    {
        var segments = SearchHighlight.Segments("ab ab", "ab");

        Assert.That(segments.Count(static segment => segment.Match), Is.EqualTo(2));
    }

    [Test]
    public void ATextWithoutTheTermIsOneUnmarkedSegment()
    {
        var segments = SearchHighlight.Segments("Odvolání", "xyz");

        Assert.That(segments, Is.EqualTo([new HighlightSegment("Odvolání", Match: false)]));
    }

    [Test]
    public void ABlankTermMarksNothing()
    {
        var segments = SearchHighlight.Segments("Odvolání", "  ");

        Assert.That(segments, Is.EqualTo([new HighlightSegment("Odvolání", Match: false)]));
    }

    [Test]
    public void ASnippetStartsInFrontOfAMatchThatSitsFarFromTheStart()
    {
        const string body = "Pojišťovna potvrdila přijetí výzvy telefonicky, lhůta k plnění běží do 6. 11. 2025.";

        var snippet = SearchHighlight.Snippet(body, "lhuta");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(snippet, Does.StartWith("…"));
            Assert.That(snippet, Does.Contain("lhůta k plnění"));
            Assert.That(snippet, Does.Not.Contain("Pojišťovna"));
        }
    }

    [Test]
    public void ASnippetOfAnEarlyMatchIsTheWholeTextOnOneLine()
    {
        var snippet = SearchHighlight.Snippet("Pojišťovna\npotvrdila", "poj");

        Assert.That(snippet, Is.EqualTo("Pojišťovna potvrdila"));
    }
}
