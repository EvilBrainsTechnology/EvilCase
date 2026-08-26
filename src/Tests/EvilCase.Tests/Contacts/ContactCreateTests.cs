using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The create rules on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself.
/// </summary>
public class ContactCreateTests
{
    private TestTenant tenant = null!;

    private ContactWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create(asHost: true);
        this.writer = new ContactWriter(new FixedDbSession(this.tenant.Context));
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task ACreatedContactCarriesItsNameKindDataBoxIdAndAddress()
    {
        var request = new ContactEditRequest
        {
            Name = "Krajský soud ve Vzorově",
            Kind = ContactKind.Authority,
            DataBoxId = "abcde12",
            Address = "Náměstí 1, Vzorov",
        };

        var created = await this.writer.CreateContact(request, CancellationToken.None);

        var reloaded = await this.tenant.Context.Contacts.SingleAsync(contact => contact.Id == created.ContactId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.Name, Is.EqualTo(request.Name));
            Assert.That(created.Kind, Is.EqualTo(request.Kind));
            Assert.That(created.DataBoxId, Is.EqualTo(request.DataBoxId));
            Assert.That(created.Address, Is.EqualTo(request.Address));
            Assert.That(reloaded.Name, Is.EqualTo(request.Name));
            Assert.That(reloaded.Kind, Is.EqualTo(request.Kind));
            Assert.That(reloaded.DataBoxId, Is.EqualTo(request.DataBoxId));
            Assert.That(reloaded.Address, Is.EqualTo(request.Address));
        }
    }

    [Test]
    public async Task ABlankOptionalFieldIsFiledAsNothing()
    {
        var request = new ContactEditRequest
        {
            Name = "  Krajský soud ve Vzorově  ",
            Kind = ContactKind.Authority,
            DataBoxId = "  ",
            Address = "\n ",
        };

        var created = await this.writer.CreateContact(request, CancellationToken.None);

        var reloaded = await this.tenant.Context.Contacts.SingleAsync(contact => contact.Id == created.ContactId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reloaded.Name, Is.EqualTo("Krajský soud ve Vzorově"));
            Assert.That(reloaded.DataBoxId, Is.Null);
            Assert.That(reloaded.Address, Is.Null);
        }
    }

    [Test]
    public async Task ACreatedContactBelongsToTheWritersTenant()
    {
        var request = new ContactEditRequest { Name = "Krajský soud ve Vzorově", Kind = ContactKind.Authority };

        var created = await this.writer.CreateContact(request, CancellationToken.None);

        var visible = await this.tenant.Context.Contacts.AnyAsync(contact => contact.Id == created.ContactId);

        Assert.That(visible, Is.True, "the write stamps the tenant, so the tenant-filtered set sees the row");
    }
}
