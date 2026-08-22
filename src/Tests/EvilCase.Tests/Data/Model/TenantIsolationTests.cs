using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class TenantIsolationTests : ModelFixture
{
    [Test]
    public void EveryTenantEntityCarriesItsTenant()
    {
        var tenantEntities = TenantEntities();

        Assert.That(tenantEntities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in tenantEntities)
            {
                var tenantId = entityType.FindProperty("TenantId");

                Assert.That(tenantId, Is.Not.Null, $"{entityType.ShortName()}: a row without a tenant is invisible to every filter");
                Assert.That(tenantId?.IsNullable, Is.False, $"{entityType.ShortName()}: a row without a tenant is invisible to every filter");
            }
        }
    }

    [Test]
    public void EveryUserOwnedEntityCarriesItsUser()
    {
        var userOwnedEntities = Model.GetEntityTypes().Where(type => typeof(IUserOwnedEntity).IsAssignableFrom(type.ClrType)).ToList();

        Assert.That(userOwnedEntities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in userOwnedEntities)
            {
                var userId = entityType.FindProperty("UserId");

                Assert.That(userId, Is.Not.Null, $"{entityType.ShortName()}: a user-owned row without its user cannot say who wrote it");
                Assert.That(userId?.IsNullable, Is.False, $"{entityType.ShortName()}: a user-owned row without its user cannot say who wrote it");
            }
        }
    }

    [Test]
    public void AContactCarriesNoUser()
    {
        var contact = Model.FindEntityType(typeof(Contact));

        Assert.That(contact, Is.Not.Null);

        var isUserOwned = typeof(IUserOwnedEntity).IsAssignableFrom(contact.ClrType);

        Assert.That(isUserOwned, Is.False, "a contact is shared across the tenant rather than owned by whoever typed it in");
    }

    [Test]
    public void EveryTenantEntityIsFilteredByItsTenant()
    {
        var tenantEntities = TenantEntities();

        Assert.That(tenantEntities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in tenantEntities)
            {
                Assert.That(
                    entityType.GetDeclaredQueryFilters(),
                    Is.Not.Empty,
                    $"{entityType.ShortName()}: a tenant entity without a query filter leaks across tenants");
            }
        }
    }

    [Test]
    public void EveryUniqueIndexOfATenantEntityLeadsWithTheTenant()
    {
        var tenantEntities = TenantEntities();

        Assert.That(tenantEntities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in tenantEntities)
            {
                foreach (var index in entityType.GetIndexes().Where(index => index.IsUnique))
                {
                    Assert.That(
                        index.Properties[0].Name,
                        Is.EqualTo("TenantId"),
                        $"{entityType.ShortName()}: a unique index without the tenant makes one tenant's value refuse another's");
                }
            }
        }
    }

    [Test]
    public void TheOnlyRowsOutsideATenantAreAccountsTenantsUsersAndTheirTokens()
    {
        var untenanted = Model.GetEntityTypes()
            .Where(entityType => !typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.ShortName())
            .ToList();

        Assert.That(
            untenanted,
            Is.EquivalentTo(["Account", "Tenant", "User", "RefreshToken"]),
            "a new entity outside the tenant is a leak until someone says otherwise");
    }

    private static List<IReadOnlyEntityType> TenantEntities()
    {
        return [.. Model.GetEntityTypes().Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType))];
    }
}
