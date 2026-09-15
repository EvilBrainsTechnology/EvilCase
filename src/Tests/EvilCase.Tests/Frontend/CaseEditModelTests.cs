using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseEditModelTests
{
    [Test]
    public void FromCopiesEveryFieldIncludingTheParentCaseId()
    {
        var parentId = Guid.CreateVersion7();
        var contact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Úřad" };

        var detail = new CaseDetail
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            ExternalCaseNumber = "VV41/2026/1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Spis",
            Description = "Popis",
            Status = CaseStatus.WaitingOnAuthority,
            Contact = contact,
            ParentCase = new CaseListItem
            {
                CaseId = parentId,
                CaseNumber = "EC/20260101-001",
                Title = "Nadřízený",
                Date = new DateOnly(2026, 1, 1),
                Status = CaseStatus.Active,
                Changed = DateTime.UtcNow,
            },
        };

        var model = CaseEditModel.From(detail);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.ParentCaseId, Is.EqualTo(parentId), "the sidebar's parent select preselects the case's actual parent");
            Assert.That(model.CaseNumber, Is.EqualTo(detail.CaseNumber));
            Assert.That(model.ExternalCaseNumber, Is.EqualTo(detail.ExternalCaseNumber));
            Assert.That(model.Date, Is.EqualTo(detail.Date));
            Assert.That(model.Title, Is.EqualTo(detail.Title));
            Assert.That(model.Description, Is.EqualTo(detail.Description));
            Assert.That(model.Contact, Is.SameAs(contact));
            Assert.That(model.Status, Is.EqualTo(detail.Status));
        }
    }

    [Test]
    public void FromLeavesParentCaseIdNullForARootCase()
    {
        var model = CaseEditModel.From(RootCase());

        Assert.That(model.ParentCaseId, Is.Null);
    }

    [Test]
    public void CopyFromReplacesEveryFieldOnTheExistingInstance()
    {
        var model = new CaseEditModel { CaseNumber = "EC/20260101-001", Title = "Starý" };

        model.CopyFrom(CaseEditModel.From(RootCase()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.CaseNumber, Is.EqualTo("EC/20260807-001"), "a reload has to overwrite stale field values on the same instance");
            Assert.That(model.Title, Is.EqualTo("Spis"));
        }
    }

    private static CaseDetail RootCase()
    {
        return new CaseDetail
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            Date = new DateOnly(2026, 8, 7),
            Title = "Spis",
            Status = CaseStatus.Active,
        };
    }
}
