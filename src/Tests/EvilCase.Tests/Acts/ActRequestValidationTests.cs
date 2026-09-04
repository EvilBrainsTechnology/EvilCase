using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActRequestValidationTests
{
    [Test]
    public void ADirectionWithNoContactIsRefused()
    {
        var created = Validate(Create(ActDirection.Incoming, contactId: null));
        var edited = Validate(Edit(ActDirection.Incoming, contactId: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Single().MemberNames, Does.Contain(nameof(CreateActRequest.ContactId)));
            Assert.That(edited.Single().MemberNames, Does.Contain(nameof(ActEditRequest.ContactId)));
        }
    }

    [Test]
    public void AContactWithNoDirectionIsRefused()
    {
        var created = Validate(Create(direction: null, Guid.CreateVersion7()));
        var edited = Validate(Edit(direction: null, Guid.CreateVersion7()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Single().MemberNames, Does.Contain(nameof(CreateActRequest.Direction)));
            Assert.That(edited.Single().MemberNames, Does.Contain(nameof(ActEditRequest.Direction)));
        }
    }

    [Test]
    public void ADirectionWithAContactIsValid()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Validate(Create(ActDirection.Outgoing, Guid.CreateVersion7())), Is.Empty);
            Assert.That(Validate(Edit(ActDirection.Outgoing, Guid.CreateVersion7())), Is.Empty);
        }
    }

    [Test]
    public void NeitherADirectionNorAContactIsValid()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Validate(Create(direction: null, contactId: null)), Is.Empty);
            Assert.That(Validate(Edit(direction: null, contactId: null)), Is.Empty);
        }
    }

    private static CreateActRequest Create(ActDirection? direction, Guid? contactId)
    {
        return new()
        {
            Direction = direction,
            ContactId = contactId,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
        };
    }

    private static ActEditRequest Edit(ActDirection? direction, Guid? contactId)
    {
        return new()
        {
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = direction,
            ContactId = contactId,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
        };
    }

    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }
}
