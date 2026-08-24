using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The delete guard on the rows a real PostgreSQL returns: a contact is referenced from each of the four
/// places, and from none. Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ContactReferencesTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    private TestTenant tenant = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task AContactNothingPointsAtIsNotReferenced()
    {
        var contact = await this.tenant.AddContact("Městský úřad");

        // A case and an act of their own, naming another contact: the guard answers for its contact alone.
        var other = await this.tenant.AddContact("Krajský soud");
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddAct(@case, Day, issuedBy: other);

        Assert.That(await this.IsReferenced(contact), Is.False, "nothing points at the contact, so nothing stands between it and deletion");
    }

    [Test]
    public async Task AnIssuedActReferencesTheContact()
    {
        var contact = await this.tenant.AddContact("Městský úřad");
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddAct(@case, Day, issuedBy: contact);

        Assert.That(await this.IsReferenced(contact), Is.True, "an act the contact issued still points at it");
    }

    [Test]
    public async Task AnAddressedActReferencesTheContact()
    {
        var contact = await this.tenant.AddContact("Jan Novák", ContactKind.Person);
        var issuer = await this.tenant.AddContact("Městský úřad");
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddAct(@case, Day, issuedBy: issuer, addressedTo: contact);

        Assert.That(await this.IsReferenced(contact), Is.True, "an act addressed to the contact still points at it");
    }

    [Test]
    public async Task AnAssignedExternalCaseNumberReferencesTheContact()
    {
        var contact = await this.tenant.AddContact("Městský úřad");
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddExternalCaseNumber(@case, "MUB/2026/117", contact);

        Assert.That(await this.IsReferenced(contact), Is.True, "a case mark the contact assigned still points at it");
    }

    [Test]
    public async Task AnAssignedExternalActNumberReferencesTheContact()
    {
        var contact = await this.tenant.AddContact("Městský úřad");
        var issuer = await this.tenant.AddContact("Krajský soud");
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day, issuedBy: issuer);
        await this.tenant.AddExternalActNumber(act, "MUB/2026/117-3", contact);

        Assert.That(await this.IsReferenced(contact), Is.True, "an act reference number the contact assigned still points at it");
    }

    /// <summary>
    /// What a returned row cannot show.
    /// </summary>
    [Test]
    public void TheGuardCountsNothing()
    {
        var sql = this.tenant.Context.Contacts
            .WithId(Guid.CreateVersion7())
            .Referenced()
            .ToQueryString();

        Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "the guard asks whether a row exists and counts nothing");
    }

    private async Task<bool> IsReferenced(Contact contact)
    {
        return await this.tenant.Context.Contacts
            .WithId(contact.Id)
            .Referenced()
            .AnyAsync();
    }
}
