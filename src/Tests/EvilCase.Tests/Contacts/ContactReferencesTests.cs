using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The delete guard on the rows a real PostgreSQL returns: a contact is referenced from each of the two
/// places, and from none. Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ContactReferencesTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    [Test]
    public async Task AContactNothingPointsAtIsNotReferenced()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");

        // A case and an act of their own, naming another contact: the guard answers for its contact alone.
        var other = await this.Tenant.AddContact("Krajský soud");
        var @case = await this.Tenant.AddCase(Day, contact: other);
        await this.Tenant.AddAct(@case, Day, contact: other);

        Assert.That(await this.IsReferenced(contact), Is.False, "nothing points at the contact, so nothing stands between it and deletion");
    }

    [Test]
    public async Task AnActReferencesTheContact()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddAct(@case, Day, contact: contact);

        Assert.That(await this.IsReferenced(contact), Is.True, "an act naming the contact still points at it");
    }

    [Test]
    public async Task ACaseReferencesTheContact()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");
        await this.Tenant.AddCase(Day, contact: contact);

        Assert.That(await this.IsReferenced(contact), Is.True, "a case naming the contact still points at it");
    }

    /// <summary>
    /// What a returned row cannot show.
    /// </summary>
    [Test]
    public void TheGuardCountsNothing()
    {
        var sql = this.Tenant.Context.Contacts
            .WithId(Guid.CreateVersion7())
            .Referenced()
            .ToQueryString();

        Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "the guard asks whether a row exists and counts nothing");
    }

    private async Task<bool> IsReferenced(Contact contact)
    {
        return await this.Tenant.Context.Contacts
            .WithId(contact.Id)
            .Referenced()
            .AnyAsync();
    }
}
