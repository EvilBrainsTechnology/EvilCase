using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// The pairing rule both act forms carry. Every model names a title, so property validation passes and
/// the object-level rule is the one that answers.
/// </summary>
public class NewActModelTests
{
    [Test]
    public void ADirectionWithoutAContactIsRefused()
    {
        var results = Validate(NewAct(ActDirection.Incoming, contact: null));
        var editResults = Validate(EditAct(ActDirection.Incoming, contact: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Single().MemberNames, Does.Contain(nameof(NewActModel.Contact)), "a direction names a contact");
            Assert.That(editResults.Single().MemberNames, Does.Contain(nameof(ActEditModel.Contact)), "a direction names a contact");
        }
    }

    [Test]
    public void AContactWithoutADirectionIsRefused()
    {
        var results = Validate(NewAct(direction: null, Contact()));
        var editResults = Validate(EditAct(direction: null, Contact()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Single().MemberNames, Does.Contain(nameof(NewActModel.Direction)), "a contact names a direction");
            Assert.That(editResults.Single().MemberNames, Does.Contain(nameof(ActEditModel.Direction)), "a contact names a direction");
        }
    }

    [Test]
    public void NeitherADirectionNorAContactIsValid()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Validate(NewAct(direction: null, contact: null)), Is.Empty);
            Assert.That(Validate(EditAct(direction: null, contact: null)), Is.Empty);
        }
    }

    [Test]
    public void ADirectionWithAContactIsValid()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Validate(NewAct(ActDirection.Outgoing, Contact())), Is.Empty);
            Assert.That(Validate(EditAct(ActDirection.Outgoing, Contact())), Is.Empty);
        }
    }

    private static ContactListItem Contact()
    {
        return new() { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Městský úřad Vzorov" };
    }

    private static NewActModel NewAct(ActDirection? direction, ContactListItem? contact)
    {
        return new() { Direction = direction, Contact = contact, Title = "Podání" };
    }

    private static ActEditModel EditAct(ActDirection? direction, ContactListItem? contact)
    {
        return new()
        {
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = direction,
            Contact = contact,
            Title = "Podání",
        };
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results;
    }
}
