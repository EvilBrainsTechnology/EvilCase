using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class DefaultContactQueryTests
{
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
    public async Task TheDefaultContactOfTheUserComesBack()
    {
        var contact = await this.tenant.Context.Users.DefaultContactOf(this.tenant.UserId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contact.Id, Is.EqualTo(this.tenant.DefaultContact.Id), "the user's default contact is what an act prefills with");
            Assert.That(contact.Kind, Is.EqualTo(this.tenant.DefaultContact.Kind));
            Assert.That(contact.Name, Is.EqualTo(this.tenant.DefaultContact.Name));
        }
    }
}
