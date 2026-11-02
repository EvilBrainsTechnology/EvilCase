using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class TextLinksTests
{
    [Test]
    public void ATextWithoutAnAddressIsOnePlainSegment()
    {
        var segments = TextLinks.Segments("Odvolání podáno 3. 11.");

        Assert.That(segments, Is.EqualTo([new LinkSegment("Odvolání podáno 3. 11.", Link: false)]));
    }

    [Test]
    public void AnAddressInsideATextIsALinkBetweenPlainSegments()
    {
        var segments = TextLinks.Segments("Viz http://example.cz/a?b=1 a dál");

        LinkSegment[] expected =
        [
            new("Viz ", Link: false),
            new("http://example.cz/a?b=1", Link: true),
            new(" a dál", Link: false),
        ];

        Assert.That(segments, Is.EqualTo(expected));
    }

    [Test]
    public void TheDotEndingTheSentenceIsNotPartOfTheAddress()
    {
        var segments = TextLinks.Segments("Rozhodnutí je na https://example.cz/rozhodnuti.");

        LinkSegment[] expected =
        [
            new("Rozhodnutí je na ", Link: false),
            new("https://example.cz/rozhodnuti", Link: true),
            new(".", Link: false),
        ];

        Assert.That(segments, Is.EqualTo(expected));
    }

    [Test]
    public void EveryAddressIsALink()
    {
        var segments = TextLinks.Segments("(https://a.example), https://b.example;");

        LinkSegment[] expected =
        [
            new("(", Link: false),
            new("https://a.example", Link: true),
            new("), ", Link: false),
            new("https://b.example", Link: true),
            new(";", Link: false),
        ];

        Assert.That(segments, Is.EqualTo(expected));
    }

    [Test]
    public void AClosingParenthesisTheAddressOpenedStaysInIt()
    {
        var segments = TextLinks.Segments("Viz https://cs.wikipedia.org/wiki/Brno_(město).");

        Assert.That(segments[1], Is.EqualTo(new LinkSegment("https://cs.wikipedia.org/wiki/Brno_(město)", Link: true)));
    }

    [Test]
    public void TheQuotesAroundAnAddressAreNotPartOfIt()
    {
        var segments = TextLinks.Segments("Odkaz „https://example.cz/a“ a \"https://example.cz/b\"");

        Assert.That(
            segments.Where(static segment => segment.Link).Select(static segment => segment.Text),
            Is.EqualTo(["https://example.cz/a", "https://example.cz/b"]));
    }

    [Test]
    public void TheQuestionMarkEndingTheSentenceIsNotPartOfTheAddress()
    {
        var segments = TextLinks.Segments("Je to na https://example.cz?");

        Assert.That(segments[1], Is.EqualTo(new LinkSegment("https://example.cz", Link: true)));
    }

    [Test]
    public void TheSchemeMatchesInAnyCase()
    {
        var segments = TextLinks.Segments("HTTPS://example.cz a Http://example.cz");

        Assert.That(segments.Count(static segment => segment.Link), Is.EqualTo(2));
    }

    [Test]
    public void OnlyAnHttpOrHttpsAddressIsALink()
    {
        var segments = TextLinks.Segments("ftp://example.cz javascript:alert(1) www.example.cz");

        Assert.That(segments.Any(static segment => segment.Link), Is.False, "another scheme or a bare host stays text");
    }
}
