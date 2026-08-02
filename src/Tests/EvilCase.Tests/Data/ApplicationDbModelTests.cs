using EvilBrains.EvilCase.Api.Contract.Parties;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Builds the model without touching a server — the design-time factory names no connection string,
/// and nothing here opens one. What it pins are the conventions in the domain model section of
/// <c>AGENTS.md</c>, which a new entity is otherwise free to forget silently.
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

    private static bool IsIndexed(IReadOnlyEntityType entityType, string propertyName) =>
        entityType.GetIndexes().Any(index => index.Properties.Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal)));
}
