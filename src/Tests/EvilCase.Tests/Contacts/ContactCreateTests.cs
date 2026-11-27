using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class ContactCreateTests : TenantFixture
{
    private ContactWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new ContactWriter(this.Tenant.Session, NullLogger<ContactWriter>.Instance);
    }

    [Test]
    public async Task ACreatedContactBelongsToTheWritersTenant()
    {
        var request = new ContactEditRequest { Name = "Krajský soud ve Vzorově", Kind = ContactKind.Authority };

        var created = await this.writer.CreateContact(request, CancellationToken.None);

        var visible = await this.Tenant.Context.Contacts.AnyAsync(contact => contact.Id == created.ContactId);

        Assert.That(visible, Is.True, "the write stamps the tenant, so the tenant-filtered set sees the row");
    }
}
