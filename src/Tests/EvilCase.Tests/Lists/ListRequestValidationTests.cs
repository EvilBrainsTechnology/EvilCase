using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;

namespace EvilBrains.EvilCase.Tests.Lists;

public class ListRequestValidationTests
{
    [Test]
    public void ATakeAboveTheHundredIsRefused()
    {
        var refused = Validate(new CaseListRequest { Take = 101 });

        Assert.That(refused.Single().MemberNames, Does.Contain(nameof(CaseListRequest.Take)), "a page never holds more than a hundred rows");
    }

    [Test]
    public void ATakeBelowOneIsRefused()
    {
        var refused = Validate(new CaseListRequest { Take = 0 });

        Assert.That(refused.Single().MemberNames, Does.Contain(nameof(CaseListRequest.Take)), "a page holds at least one row");
    }

    [Test]
    public void ANegativeSkipIsRefused()
    {
        var refused = Validate(new CaseListRequest { Skip = -1, Take = 20 });

        Assert.That(refused.Single().MemberNames, Does.Contain(nameof(CaseListRequest.Skip)), "a page starts at the first row or later");
    }

    [Test]
    public void TheLimitsDeclaredOnTheSharedRequestBindTheActListToo()
    {
        var refused = Validate(new ActListRequest { Take = 101 });
        var negative = Validate(new ActListRequest { Skip = -1, Take = 20 });
        var accepted = Validate(new ActListRequest { Skip = 40, Take = 100 });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refused.Single().MemberNames, Does.Contain(nameof(ActListRequest.Take)), "every list takes its page limit from the shared request");
            Assert.That(negative.Single().MemberNames, Does.Contain(nameof(ActListRequest.Skip)), "every list takes its page start from the shared request");
            Assert.That(accepted, Is.Empty);
        }
    }

    [Test]
    public void TheLimitsDeclaredOnTheSharedRequestBindTheContactListToo()
    {
        var refused = Validate(new ContactListRequest { Take = 101 });
        var negative = Validate(new ContactListRequest { Skip = -1, Take = 20 });
        var accepted = Validate(new ContactListRequest { Skip = 40, Take = 100 });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refused.Single().MemberNames, Does.Contain(nameof(ContactListRequest.Take)), "every list takes its page limit from the shared request");
            Assert.That(negative.Single().MemberNames, Does.Contain(nameof(ContactListRequest.Skip)), "every list takes its page start from the shared request");
            Assert.That(accepted, Is.Empty);
        }
    }

    [Test]
    public void TheSharedRequestCarriesThePageAndTheSortAndNoFilter()
    {
        var declared = typeof(ListRequest<CaseSortKey>)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(static property => property.Name);

        Assert.That(
            declared,
            Is.EquivalentTo(["Skip", "Take", "SortDirection", "Sort"]),
            "a list request shares the page, the sort key and its direction; a filter belongs to the list that narrows by it");
    }

    [Test]
    public void TheContactListNarrowsByItsOwnSearchAndKindAlone()
    {
        var declared = typeof(ContactListRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(static property => property.Name);

        Assert.That(
            declared,
            Is.EquivalentTo([nameof(ContactListRequest.Search), nameof(ContactListRequest.Kind), nameof(ContactListRequest.Sort)]),
            "a contact carries no label, no contact and no date, so the contact list never takes such a filter");
    }

    [Test]
    public void EveryListOpensOnTheKeyAndTheDirectionItsOwnRecordCarries()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new CaseListRequest { Take = 20 }.Sort, Is.EqualTo(CaseSortKey.Date));
            Assert.That(
                new CaseListRequest { Take = 20 }.SortDirection,
                Is.EqualTo(ListSortDirection.Descending),
                "a case list reads by the case's own date, newest first");
            Assert.That(new ActListRequest { Take = 20 }.Sort, Is.EqualTo(ActSortKey.Date));
            Assert.That(
                new ActListRequest { Take = 20 }.SortDirection,
                Is.EqualTo(ListSortDirection.Ascending),
                "an act list reads by the act's own date, oldest first, the direction the shared request already holds");
            Assert.That(new ContactListRequest { Take = 20 }.Sort, Is.EqualTo(ContactSortKey.Name));
            Assert.That(
                new ContactListRequest { Take = 20 }.SortDirection,
                Is.EqualTo(ListSortDirection.Ascending),
                "a contact list reads by name, so it starts at A");
        }
    }

    private static List<ValidationResult> Validate<TSortKey>(ListRequest<TSortKey> request)
        where TSortKey : struct, Enum
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }
}
