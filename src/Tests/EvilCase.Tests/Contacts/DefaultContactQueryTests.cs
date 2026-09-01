using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class DefaultContactQueryTests : TenantFixture
{
    [Test]
    public async Task TheDefaultContactOfTheUserComesBack()
    {
        var contact = await this.Tenant.Context.Users.DefaultContactOf(this.Tenant.UserId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contact.ContactId, Is.EqualTo(this.Tenant.DefaultContact.Id), "the user's default contact is what an act prefills with");
            Assert.That(contact.Kind, Is.EqualTo(this.Tenant.DefaultContact.Kind));
            Assert.That(contact.Name, Is.EqualTo(this.Tenant.DefaultContact.Name));
        }
    }
}
