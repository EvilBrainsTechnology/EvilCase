using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Parties;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class PartyModelTests : ModelFixture
{
    [Test]
    public void EveryAggregateRootCarriesItsOwner()
    {
        var party = Model.FindEntityType(typeof(Party));

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
        var party = Model.FindEntityType(typeof(Party));

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
}
