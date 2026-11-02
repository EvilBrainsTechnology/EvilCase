using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class ContactDeleteTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    private ContactWriter writer = null!;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new ContactWriter(new FixedDbSession(this.Tenant.Context), NullLogger<ContactWriter>.Instance);
    }

    [Test]
    public async Task AContactNothingPointsAtIsDeleted()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");
        var other = await this.Tenant.AddContact("Krajský soud");
        var @case = await this.Tenant.AddCase(Day, contact: other);
        await this.Tenant.AddAct(@case, Day, contact: other);

        var outcome = await this.writer.DeleteContact(contact.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ContactDeleteOutcome.Deleted), "nothing points at the contact, so nothing stands between it and deletion");
            Assert.That(await this.Tenant.Context.Contacts.Select(static row => row.Id).ToListAsync(), Is.EqualTo([other.Id]));
        }
    }

    [Test]
    public async Task AContactAnActNamesIsInUse()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddAct(@case, Day, contact: contact);

        var outcome = await this.writer.DeleteContact(contact.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ContactDeleteOutcome.Referenced), "an act naming the contact still points at it");
            Assert.That(await this.Tenant.Context.Contacts.CountAsync(), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task AContactACaseNamesIsInUse()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");
        await this.Tenant.AddCase(Day, contact: contact);

        var outcome = await this.writer.DeleteContact(contact.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ContactDeleteOutcome.Referenced), "a case naming the contact still points at it");
            Assert.That(await this.Tenant.Context.Contacts.CountAsync(), Is.EqualTo(1));
        }
    }
}
