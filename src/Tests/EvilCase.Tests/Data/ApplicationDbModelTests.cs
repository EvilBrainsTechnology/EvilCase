using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Builds the model without touching a server — the design-time factory names no connection string,
/// and nothing here opens one. What it pins are the conventions in the domain model section of
/// <c>src/Data/CLAUDE.md</c>, which a new entity is otherwise free to forget silently.
/// </summary>
public class ApplicationDbModelTests
{
    [Test]
    public void EveryEnumIsStoredAsAName()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var enumProperties = context.Model.GetEntityTypes()
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

    [Test]
    public void EveryAggregateRootCarriesItsOwner()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var party = context.Model.FindEntityType(typeof(Party));

        Assert.That(party, Is.Not.Null);

        var owner = party.FindProperty(nameof(Party.OwnerId));
        var ownerForeignKey = party.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(User));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(owner, Is.Not.Null, "the column ships before anything filters on it");
            Assert.That(owner?.IsNullable, Is.False, "a party without an owner is unreachable once M8 filters");
            Assert.That(ownerForeignKey, Is.Not.Null, "and it points at a real user");
            Assert.That(IsIndexed(party, nameof(Party.OwnerId)), Is.True, "every owner-scoped query reads this index");
        }
    }

    [Test]
    public void APartyIsFlatAndItsAddressIsOneBlock()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var party = context.Model.FindEntityType(typeof(Party));

        Assert.That(party, Is.Not.Null);

        var columns = party.GetProperties().Select(property => property.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(party.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Party)), Is.False, "an official carries no link to its authority");
            Assert.That(columns, Has.Member(nameof(Party.Address)), "the address is one free-text block");
            Assert.That(columns, Does.Not.Contain("Town").And.Not.Contains("PostCode"), "and is never split into parts");
            Assert.That(party.FindProperty(nameof(Party.Kind))?.ClrType, Is.EqualTo(typeof(PartyKind)));
            Assert.That(IsIndexed(party, nameof(Party.DataBoxId)), Is.True, "looking a party up by data box is the one unambiguous lookup");
        }
    }

    [Test]
    public void TheInternalMarkIsAColumnOnTheCase()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var @case = context.Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        var mark = @case.FindProperty(nameof(Case.InternalCaseReference));
        var unique = @case.GetIndexes().SingleOrDefault(index => index.IsUnique);
        string[] expected = [nameof(Case.OwnerId), nameof(Case.InternalCaseReference)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mark, Is.Not.Null, "a case always has exactly one of its own, so it is a column and not a row");
            Assert.That(mark?.IsNullable, Is.False, "it is generated with the case and never absent");
            Assert.That(unique?.Properties.Select(property => property.Name), Is.EqualTo(expected), "and a generated series must not repeat within one owner");
        }
    }

    [Test]
    public void TwoActsMayShareOneOrdinal()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var overOrdinal = act.GetIndexes().Where(index => index.Properties.Any(property => string.Equals(property.Name, nameof(Act.Ordinal), StringComparison.Ordinal)));

        Assert.That(
            overOrdinal.Any(index => index.IsUnique),
            Is.False,
            "a real case file has two unrelated submissions filed under one number, so the ordinal orders acts and does not identify them");
    }

    [Test]
    public void ActDatesAreCalendarDatesRatherThanInstants()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        string[] dates = [nameof(Act.Drafted), nameof(Act.Sent), nameof(Act.Delivered), nameof(Act.Received)];

        using (Assert.EnterMultipleScope())
        {
            foreach (var name in dates)
            {
                var property = act.FindProperty(name);

                Assert.That(property?.ClrType, Is.EqualTo(typeof(DateOnly?)), $"{name} starts a statutory period, and the hour never enters that arithmetic");
                Assert.That(property?.IsNullable, Is.True, $"{name} does not apply to every direction");
            }

            Assert.That(act.FindProperty(nameof(Act.Summary))?.GetMaxLength(), Is.Null, "the summary is long-form and lives on the act alone");
        }
    }

    [Test]
    public void EveryExternalMarkNamesWhoAssignedIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var reference = context.Model.FindEntityType(typeof(CaseReference));

        Assert.That(reference, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                reference.FindProperty(nameof(CaseReference.AssignedByPartyId))?.IsNullable,
                Is.False,
                "a mark nobody assigned is the case's own, and that one lives on the case");

            Assert.That(
                reference.GetIndexes().Any(index => index.GetFilter() is not null),
                Is.False,
                "nothing here is conditional any more — this table is external marks and only those");
        }
    }

    [Test]
    public void AMarkNeverTakesAPartyDownWithIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var reference = context.Model.FindEntityType(typeof(CaseReference));

        Assert.That(reference, Is.Not.Null);

        var toParty = reference.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Party));
        var toCase = reference.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toParty?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict), "a party accumulates history across cases and outlives any one mark naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "a mark has no meaning without its case");
            Assert.That(reference.GetProperties().Any(property => string.Equals(property.Name, "OwnerId", StringComparison.Ordinal)), Is.False, "only aggregate roots carry an owner, and a mark is not one");
        }
    }

    [Test]
    public void AnActNeverTakesAPartyDownWithIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var toParties = act.GetForeignKeys().Where(key => key.PrincipalEntityType.ClrType == typeof(Party)).ToList();
        var toCase = act.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toParties, Has.Count.EqualTo(2), "an act references who issued it and who it was addressed to");
            Assert.That(toParties.TrueForAll(key => key.DeleteBehavior == DeleteBehavior.Restrict), Is.True, "a party outlives any one act naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "an act has no meaning without its case");
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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var oneToMany = context.Model.GetEntityTypes()
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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var eager = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetNavigations())
            .Where(navigation => navigation.IsEagerLoaded)
            .Select(navigation => $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}")
            .ToList();

        Assert.That(eager, Is.Empty, "auto-include is off, and an AutoInclude() would turn one read of the case list into a read of everything under it");
    }

    private static bool IsIndexed(IReadOnlyEntityType entityType, string propertyName) =>
        entityType.GetIndexes().Any(index => index.Properties.Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal)));
}
