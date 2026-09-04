using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ModelConventionTests : ModelFixture
{
    /// <summary>
    /// EB0009 flags a <c>Set&lt;TEntity&gt;()</c> call site; an entity with no typed <c>DbSet</c> at all
    /// leaves none to flag.
    /// </summary>
    [Test]
    public void EveryEntityIsReachedThroughItsOwnDbSet()
    {
        var sets = typeof(ApplicationDbContext).GetProperties()
            .Where(static property => property.PropertyType.IsGenericType)
            .Where(static property => property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(static property => property.PropertyType.GetGenericArguments()[0])
            .ToList();

        var entities = Model.GetEntityTypes().Select(static entityType => entityType.ClrType).ToList();

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
            .SelectMany(static entityType => entityType.GetProperties())
            .Where(static property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            .ToList();

        Assert.That(enumProperties, Is.Not.Empty, "the model has enums at all, or this test passes vacuously");

        using (Assert.EnterMultipleScope())
        {
            foreach (var property in enumProperties)
            {
                var name = $"{property.DeclaringType.ShortName()}.{property.Name}";
                var longest = Enum.GetNames(Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).Max(static value => value.Length);

                Assert.That(property.GetProviderClrType(), Is.EqualTo(typeof(string)), $"{name} is stored by number, so renumbering the enum would silently rewrite every row");
                Assert.That(property.GetColumnType(), Does.StartWith("character varying"), $"{name} is stored as unbounded text rather than a bounded column");
                Assert.That(property.GetMaxLength(), Is.EqualTo(longest), $"{name} is not as wide as its longest value and no wider");
            }
        }
    }

    /// <summary>
    /// <see cref="User"/> is left out at either end: the rows it owns are never read from there.
    /// </summary>
    [Test]
    public void EveryOneToManyIsNavigableFromBothEnds()
    {
        var oneToMany = Model.GetEntityTypes()
            .SelectMany(static entityType => entityType.GetForeignKeys())
            .Where(static key => !key.IsUnique)
            .Where(static key => key.DependentToPrincipal is not null)
            .Where(static key => key.PrincipalEntityType.ClrType != typeof(User))
            .Where(static key => key.DeclaringEntityType.ClrType != typeof(User))
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

    [Test]
    public void NothingIsEagerLoaded()
    {
        var eager = Model.GetEntityTypes()
            .SelectMany(static entityType => entityType.GetNavigations())
            .Where(static navigation => navigation.IsEagerLoaded)
            .Select(static navigation => $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}")
            .ToList();

        Assert.That(eager, Is.Empty, "auto-include is off, and an AutoInclude() would turn one read of the case list into a read of everything under it");
    }

    [Test]
    public void EveryIdentifierIsAUuidTheApplicationGenerates()
    {
        var entities = Model.GetEntityTypes()
            .Where(static entityType => typeof(IEntity).IsAssignableFrom(entityType.ClrType))
            .ToList();

        Assert.That(entities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in entities)
            {
                var idProperty = entityType.FindProperty(nameof(IEntity.Id));

                Assert.That(idProperty?.ClrType, Is.EqualTo(typeof(Guid)), $"{entityType.ShortName()}: a database-generated key would leave a new row without an identifier until it is saved");
                Assert.That(idProperty?.ValueGenerated, Is.EqualTo(ValueGenerated.Never), $"{entityType.ShortName()}: a database-generated key would leave a new row without an identifier until it is saved");
            }
        }
    }

    /// <summary>
    /// The runtime model drops the save behaviours; only the design-time one carries them.
    /// </summary>
    [Test]
    public void TheDatabaseOwnsEveryTimestamp()
    {
        var entities = DesignTimeModel.GetEntityTypes()
            .Where(static entityType => typeof(IEntity).IsAssignableFrom(entityType.ClrType))
            .ToList();

        Assert.That(entities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in entities)
            {
                var name = entityType.ShortName();
                var created = entityType.FindProperty(nameof(IEntity.Created));
                var updated = entityType.FindProperty(nameof(IEntity.Updated));

                Assert.That(created?.ValueGenerated, Is.EqualTo(ValueGenerated.OnAdd), $"{name}.Created is not read back from the database, so a write could leave it wrong");
                Assert.That(created?.GetBeforeSaveBehavior(), Is.EqualTo(PropertySaveBehavior.Ignore), $"{name}.Created is still sent by the write, so a write could leave it wrong");
                Assert.That(created?.GetAfterSaveBehavior(), Is.EqualTo(PropertySaveBehavior.Ignore), $"{name}.Created is still sent by the write, so a write could leave it wrong");

                Assert.That(updated?.ValueGenerated, Is.EqualTo(ValueGenerated.OnAddOrUpdate), $"{name}.Updated is not read back from the database, so a write could leave it wrong");
                Assert.That(updated?.GetBeforeSaveBehavior(), Is.EqualTo(PropertySaveBehavior.Ignore), $"{name}.Updated is still sent by the write, so a write could leave it wrong");
                Assert.That(updated?.GetAfterSaveBehavior(), Is.EqualTo(PropertySaveBehavior.Ignore), $"{name}.Updated is still sent by the write, so a write could leave it wrong");
            }
        }
    }
}
