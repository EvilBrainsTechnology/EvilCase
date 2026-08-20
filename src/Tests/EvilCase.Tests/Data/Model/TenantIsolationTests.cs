using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class TenantIsolationTests : ModelFixture
{
    [Test]
    public void EveryTenantEntityCarriesItsTenantAndItsUser()
    {
        var tenantEntities = TenantEntities();

        Assert.That(tenantEntities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in tenantEntities)
            {
                var tenantId = entityType.FindProperty("TenantId");
                var userId = entityType.FindProperty("UserId");

                Assert.That(tenantId, Is.Not.Null, $"{entityType.ShortName()}: a row without a tenant is invisible to every filter");
                Assert.That(tenantId?.IsNullable, Is.False, $"{entityType.ShortName()}: a row without a tenant is invisible to every filter");
                Assert.That(userId, Is.Not.Null, $"{entityType.ShortName()}: a row without a tenant is invisible to every filter");
                Assert.That(userId?.IsNullable, Is.False, $"{entityType.ShortName()}: a row without a tenant is invisible to every filter");
            }
        }
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

    private static List<IReadOnlyEntityType> TenantEntities() =>
        [.. Model.GetEntityTypes().Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType))];
}
