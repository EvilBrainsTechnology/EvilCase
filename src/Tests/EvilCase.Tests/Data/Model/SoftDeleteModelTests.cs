using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class SoftDeleteModelTests : ModelFixture
{
    [Test]
    public void TheOnlyEntityADeleteStillRemovesIsTheRefreshToken()
    {
        var hardDeleted = Model.GetEntityTypes()
            .Where(static entityType => !typeof(ISoftDeleteEntity).IsAssignableFrom(entityType.ClrType))
            .Select(static entityType => entityType.ShortName())
            .ToList();

        Assert.That(
            hardDeleted,
            Is.EquivalentTo([nameof(RefreshToken)]),
            "a new entity a delete removes outright loses its rows for good");
    }

    [Test]
    public void EverySoftDeleteEntityIsFilteredByItsStamp()
    {
        var entities = SoftDeleteEntities();

        Assert.That(entities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in entities)
            {
                Assert.That(
                    entityType.FindDeclaredQueryFilter(ApplicationDbContext.SoftDeleteFilter),
                    Is.Not.Null,
                    $"{entityType.ShortName()}: a stamped row without the filter stays in every read");
            }
        }
    }

    /// <summary>
    /// EF refuses a model mixing an anonymous filter with a named one, and
    /// <c>IgnoreQueryFilters([key])</c> cannot drop an anonymous filter at all.
    /// </summary>
    [Test]
    public void NoQueryFilterIsAnonymous()
    {
        var anonymous = Model.GetEntityTypes()
            .Where(static entityType => entityType.GetDeclaredQueryFilters().Any(static filter => filter.IsAnonymous))
            .Select(static entityType => entityType.ShortName())
            .ToList();

        Assert.That(anonymous, Is.Empty, "an anonymous filter cannot be dropped on its own, so a read has to drop the tenant with it");
    }

    /// <summary>
    /// The stamp is a value the write sends, unlike <see cref="IEntity.Created"/> and
    /// <see cref="IEntity.Updated"/>, which a trigger owns (SDD-018).
    /// </summary>
    [Test]
    public void TheWriteOwnsTheStamp()
    {
        var entities = SoftDeleteEntities();

        Assert.That(entities, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var entityType in entities)
            {
                var deleted = entityType.FindProperty(nameof(ISoftDeleteEntity.Deleted));

                Assert.That(deleted, Is.Not.Null, $"{entityType.ShortName()}: no stamp to write");
                Assert.That(deleted?.IsNullable, Is.True, $"{entityType.ShortName()}: a stamp that cannot be null cannot say the row is alive");
                Assert.That(deleted?.ValueGenerated, Is.EqualTo(ValueGenerated.Never), $"{entityType.ShortName()}: the stamp comes from the delete, not from a trigger");
            }
        }
    }

    private static List<IReadOnlyEntityType> SoftDeleteEntities()
    {
        return [.. Model.GetEntityTypes().Where(static type => typeof(ISoftDeleteEntity).IsAssignableFrom(type.ClrType))];
    }
}
