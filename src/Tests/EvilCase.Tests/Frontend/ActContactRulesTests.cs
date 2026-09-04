using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActContactRulesTests
{
    private static readonly ContactListItem ActContact = Contact("Krajský soud ve Vzorově");

    private static readonly ContactListItem CaseContact = Contact("Městský úřad Vzorov");

    [Test]
    public void PickingADirectionTakesTheCasesContact()
    {
        var prefilled = ActContactRules.Prefilled(ActDirection.Incoming, actContact: null, CaseContact);

        Assert.That(prefilled, Is.SameAs(CaseContact));
    }

    [Test]
    public void AContactAlreadyPickedStays()
    {
        var prefilled = ActContactRules.Prefilled(ActDirection.Incoming, ActContact, CaseContact);

        Assert.That(prefilled, Is.SameAs(ActContact), "the prefill never overwrites a contact the user chose");
    }

    [Test]
    public void NoDirectionPrefillsNothing()
    {
        Assert.That(ActContactRules.Prefilled(direction: null, actContact: null, CaseContact), Is.Null);
    }

    [Test]
    public void ACaseWithNoContactPrefillsNothing()
    {
        Assert.That(ActContactRules.Prefilled(ActDirection.Incoming, actContact: null, caseContact: null), Is.Null);
    }

    [Test]
    public void ADifferentContactIsWarnedAbout()
    {
        Assert.That(ActContactRules.DifferingCaseContact(ActContact, CaseContact), Is.SameAs(CaseContact));
    }

    [Test]
    public void TheSameContactIsNotWarnedAbout()
    {
        Assert.That(ActContactRules.DifferingCaseContact(CaseContact, CaseContact), Is.Null);
    }

    [Test]
    public void AMissingContactOnEitherSideIsNotWarnedAbout()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActContactRules.DifferingCaseContact(actContact: null, CaseContact), Is.Null);
            Assert.That(ActContactRules.DifferingCaseContact(ActContact, caseContact: null), Is.Null);
            Assert.That(ActContactRules.DifferingCaseContact(actContact: null, caseContact: null), Is.Null);
        }
    }

    private static ContactListItem Contact(string name)
    {
        return new() { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = name };
    }
}
