using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.App.Search;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchResultDisplayTests
{
    [Test]
    public void ACaseNavigatesToItsDetail()
    {
        var caseId = Guid.CreateVersion7();

        var href = SearchResultDisplay.Href(Item(caseId));

        Assert.That(href, Is.EqualTo($"/cases/{caseId}"));
    }

    [Test]
    public void AnActNavigatesToItsDetailUnderItsCase()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var href = SearchResultDisplay.Href(Item(caseId, actId));

        Assert.That(href, Is.EqualTo($"/cases/{caseId}/act/{actId}"), "an act's link runs through the case it belongs to");
    }

    [Test]
    public void EachKindNamesItself()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SearchResultDisplay.KindText(SearchResultKind.Case), Is.EqualTo("Spis"));
            Assert.That(SearchResultDisplay.KindText(SearchResultKind.Act), Is.EqualTo("Úkon"));
        }
    }

    private static SearchResultItem Item(Guid caseId, Guid? actId = null)
    {
        return new()
        {
            Kind = actId is null ? SearchResultKind.Case : SearchResultKind.Act,
            CaseId = caseId,
            ActId = actId,
            Number = "EC/20260821-001",
            Title = "Přestupek",
            Date = new DateOnly(2026, 8, 21),
        };
    }
}
