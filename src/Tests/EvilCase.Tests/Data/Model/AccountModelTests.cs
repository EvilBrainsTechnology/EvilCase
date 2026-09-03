using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class AccountModelTests : ModelFixture
{
    [Test]
    public void AnAccountCoversItsTenantsAndATenantItsUsers()
    {
        var tenant = Model.FindEntityType(typeof(Tenant));
        var user = Model.FindEntityType(typeof(User));

        Assert.That(new[] { tenant, user }, Has.None.Null, "both the tenant and the user are mapped");

        var tenantToAccount = ForeignKeyTo<Account>(tenant!);
        var userToTenant = ForeignKeyTo<Tenant>(user!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tenant!.FindProperty(nameof(Tenant.AccountId))?.IsNullable, Is.False);
            Assert.That(tenantToAccount?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(user!.FindProperty(nameof(User.TenantId))?.IsNullable, Is.False);
            Assert.That(userToTenant?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict));
        }
    }

    [Test]
    public void AUserNamesNoContact()
    {
        var user = Model.FindEntityType(typeof(User));

        Assert.That(user, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.FindProperty("DefaultContactId"), Is.Null, "an act prefills from the case it sits in, so a user names no contact");
            Assert.That(ForeignKeyTo<Contact>(user), Is.Null, "an act prefills from the case it sits in, so a user names no contact");
        }
    }
}
