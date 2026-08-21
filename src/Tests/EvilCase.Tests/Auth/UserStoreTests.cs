using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Auth;

public class UserStoreTests
{
    [Test]
    public async Task AUserAndItsDefaultContactGoInOneWrite()
    {
        var tenantContext = new StubTenantContext();
        var context = FakeApplicationDbContext.Create(tenantContext);
        var tenantId = Guid.CreateVersion7();

        var contact = new Contact { TenantId = tenantId, Kind = ContactKind.Person, Name = "admin@evilcase.test" };
        var user = new User
        {
            TenantId = tenantId,
            Email = "admin@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.Admin,
            DefaultContactId = contact.Id,
        };

        await new UserStore(new FixedDbSession(context), new TestTimeProvider(DateTime.UtcNow)).Add(user, contact, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Saves, Is.EqualTo(1), "a second write would leave a user whose required default contact is not there yet");
            Assert.That(context.Added<Contact>().Single(), Is.SameAs(contact), "the contact the caller passed is written with the user");
            Assert.That(context.Added<User>().Single(), Is.SameAs(user));
        }
    }
}
