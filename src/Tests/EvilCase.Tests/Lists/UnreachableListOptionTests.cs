using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Lists;

public class UnreachableListOptionTests
{
    private static readonly IQueryable<Case> Cases = new List<Case>().AsQueryable();

    private static readonly IQueryable<Act> Acts = new List<Act>().AsQueryable();

    private static readonly IQueryable<Contact> Contacts = new List<Contact>().AsQueryable();

    [Test]
    public void AStatusFilterNoMemberNamesIsUnreachable()
    {
        Assert.That(
            static () => Cases.WithStatus((CaseStatusFilter)99),
            Throws.InstanceOf<UnreachableException>(),
            "a filter value no member names is unreachable, not an argument the caller may pass");
    }

    [Test]
    public void AListScopeNoMemberNamesIsUnreachable()
    {
        Assert.That(
            static () => Cases.UnderParent(parentCaseId: null, (CaseListScope)99),
            Throws.InstanceOf<UnreachableException>(),
            "a scope value no member names is unreachable, not an argument the caller may pass");
    }

    [Test]
    public void ACaseSortKeyNoMemberNamesIsUnreachable()
    {
        Assert.That(
            static () => Cases.InSortOrder((CaseSortKey)99, ListSortDirection.Ascending),
            Throws.InstanceOf<UnreachableException>(),
            "a sort key no member names is unreachable, not an argument the caller may pass");
    }

    [Test]
    public void AnActSortKeyNoMemberNamesIsUnreachable()
    {
        Assert.That(
            static () => Acts.InSortOrder((ActSortKey)99, ListSortDirection.Ascending),
            Throws.InstanceOf<UnreachableException>(),
            "a sort key no member names is unreachable, not an argument the caller may pass");
    }

    [Test]
    public void AContactSortKeyNoMemberNamesIsUnreachable()
    {
        Assert.That(
            static () => Contacts.InSortOrder((ContactSortKey)99, ListSortDirection.Ascending),
            Throws.InstanceOf<UnreachableException>(),
            "a sort key no member names is unreachable, not an argument the caller may pass");
    }
}
