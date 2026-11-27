using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseDetailQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheDetailNamesTheCasesContact()
    {
        var contact = await this.Tenant.AddContact("Městský úřad Vzorov");
        var @case = await this.Tenant.AddCase(Day, contact: contact);

        var detail = await this.Tenant.Context.Cases.DetailOf(@case.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.Contact!.ContactId, Is.EqualTo(contact.Id));
            Assert.That(detail.Contact.Name, Is.EqualTo(contact.Name));
            Assert.That(detail.Contact.Kind, Is.EqualTo(contact.Kind));
        }
    }

    [Test]
    public async Task TheDetailNamesTheParentCase()
    {
        var parent = await this.Tenant.AddCase(Day, "Nadřízený");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);

        var detail = await this.Tenant.Context.Cases.DetailOf(child.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.ParentCase!.CaseId, Is.EqualTo(parent.Id), "the detail links to the parent so the flat lists are enough to walk the hierarchy");
            Assert.That(detail.ParentCase.CaseNumber, Is.EqualTo(parent.CaseNumber));
            Assert.That(detail.ParentCase.Title, Is.EqualTo(parent.Title));
        }
    }

    [Test]
    public async Task AnUnknownIdIsNoDetail()
    {
        var detail = await this.Tenant.Context.Cases.DetailOf(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNoDetail()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);

        var detail = await this.Tenant.Context.Cases.DetailOf(otherCase.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "the tenant query filter is what turns another tenant's id into nothing");
    }
}
