using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using static EvilBrains.EvilCase.Tests.Data.Model.ModelFixture;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ModelConventionTests
{
    [Test]
    public void EveryEnumIsStoredAsAName()
    {
        var enumProperties = Runtime.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            .ToList();

        Assert.That(enumProperties, Is.Not.Empty, "the model has enums at all, or this test passes vacuously");

        using (Assert.EnterMultipleScope())
        {
            foreach (var property in enumProperties)
            {
                var name = $"{property.DeclaringType.ShortName()}.{property.Name}";

                Assert.That(property.GetProviderClrType(), Is.EqualTo(typeof(string)), $"{name} is stored by number, so renumbering the enum would silently rewrite every row");
                Assert.That(property.GetColumnType(), Does.StartWith("character varying"), $"{name} is stored as unbounded text rather than a bounded column");
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
        var oneToMany = Runtime.GetEntityTypes()
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
        var eager = Runtime.GetEntityTypes()
            .SelectMany(entityType => entityType.GetNavigations())
            .Where(navigation => navigation.IsEagerLoaded)
            .Select(navigation => $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}")
            .ToList();

        Assert.That(eager, Is.Empty, "auto-include is off, and an AutoInclude() would turn one read of the case list into a read of everything under it");
    }
}
