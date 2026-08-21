using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ModelConventionTests : ModelFixture
{
    [Test]
    public void EveryEntityIsReachedThroughItsOwnDbSet()
    {
        var sets = typeof(ApplicationDbContext).GetProperties()
            .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.PropertyType.GetGenericArguments()[0])
            .ToList();

        var entities = Model.GetEntityTypes().Select(entityType => entityType.ClrType).ToList();

        Assert.That(entities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entity in entities)
                Assert.That(sets, Does.Contain(entity), $"{entity.Name}: a read would have to reach for Set<TEntity>() without a typed set");
        }
    }

    [Test]
    public void EveryEnumIsStoredAsAName()
    {
        var enumProperties = Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            .ToList();

        Assert.That(enumProperties, Is.Not.Empty, "the model has enums at all, or this test passes vacuously");

        using (Assert.EnterMultipleScope())
        {
            foreach (var property in enumProperties)
            {
                var name = $"{property.DeclaringType.ShortName()}.{property.Name}";
                var longest = Enum.GetNames(Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).Max(value => value.Length);

                Assert.That(property.GetProviderClrType(), Is.EqualTo(typeof(string)), $"{name} is stored by number, so renumbering the enum would silently rewrite every row");
                Assert.That(property.GetColumnType(), Does.StartWith("character varying"), $"{name} is stored as unbounded text rather than a bounded column");
                Assert.That(property.GetMaxLength(), Is.EqualTo(longest), $"{name} is not as wide as its longest value and no wider");
            }
        }
    }

    /// <summary>
    /// From review on #86: a one-to-many is reachable from both ends, so the principal carries a
    /// collection rather than the dependent carrying the only reference. Without it a party's history
    /// across cases can be reached only by querying the dependent table by hand.
    /// </summary>
    [Test]
    public void EveryOneToManyIsNavigableFromBothEnds()
    {
        var oneToMany = Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys())
            .Where(key => !key.IsUnique && key.DependentToPrincipal is not null && key.PrincipalEntityType.ClrType != typeof(User))
            .ToList();

        Assert.That(oneToMany, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var key in oneToMany)
            {
                var name = $"{key.DeclaringEntityType.ShortName()}.{key.DependentToPrincipal?.Name}";

                Assert.That(key.PrincipalToDependent, Is.Not.Null, $"{name} points at {key.PrincipalEntityType.ShortName()} and nothing points back");
            }
        }
    }

    /// <summary>
    /// From the same review: a navigation is followed because a query asked, never because it exists.
    /// </summary>
    [Test]
    public void NothingIsEagerLoaded()
    {
        var eager = Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetNavigations())
            .Where(navigation => navigation.IsEagerLoaded)
            .Select(navigation => $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}")
            .ToList();

        Assert.That(eager, Is.Empty, "auto-include is off, and an AutoInclude() would turn one read of the case list into a read of everything under it");
    }

    [Test]
    public void EveryIdentifierIsAUuidTheApplicationGenerates()
    {
        var entities = Model.GetEntityTypes()
            .Where(entityType => typeof(IEntity).IsAssignableFrom(entityType.ClrType))
            .ToList();

        Assert.That(entities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in entities)
            {
                var id = entityType.FindProperty(nameof(IEntity.Id));

                Assert.That(id?.ClrType, Is.EqualTo(typeof(Guid)), $"{entityType.ShortName()}: a database-generated key would leave a new row without an identifier until it is saved");
                Assert.That(id?.ValueGenerated, Is.EqualTo(ValueGenerated.Never), $"{entityType.ShortName()}: a database-generated key would leave a new row without an identifier until it is saved");
            }
        }
    }
}
