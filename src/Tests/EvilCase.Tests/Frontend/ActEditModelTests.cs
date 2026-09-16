using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActEditModelTests
{
    [Test]
    public void FromCopiesEveryField()
    {
        var contact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Úřad" };

        var detail = new ActDetail
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260812-001",
            ExternalActNumber = "MUVZ/2026/1",
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 12),
            Title = "Úkon",
            Description = "Popis",
            Contact = contact,
        };

        var model = ActEditModel.From(detail);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.ActNumber, Is.EqualTo(detail.ActNumber));
            Assert.That(model.ExternalActNumber, Is.EqualTo(detail.ExternalActNumber));
            Assert.That(model.Direction, Is.EqualTo(detail.Direction));
            Assert.That(model.Date, Is.EqualTo(detail.Date));
            Assert.That(model.Title, Is.EqualTo(detail.Title));
            Assert.That(model.Description, Is.EqualTo(detail.Description));
            Assert.That(model.Contact, Is.SameAs(contact));
        }
    }

    [Test]
    public void CopyFromReplacesEveryFieldOnTheExistingInstance()
    {
        var model = new ActEditModel { ActNumber = "EC/20260101-001/20260101-001", Title = "Starý" };

        model.CopyFrom(ActEditModel.From(new ActDetail
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260812-001",
            Date = new DateOnly(2026, 8, 12),
            Title = "Úkon",
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.ActNumber, Is.EqualTo("EC/20260807-001/20260812-001"), "a reload has to overwrite stale field values on the same instance");
            Assert.That(model.Title, Is.EqualTo("Úkon"));
        }
    }
}
