using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ContactModelTests : ModelFixture
{
    [Test]
    public void AContactIsFlatAndItsAddressIsOneBlock()
    {
        var contact = Model.FindEntityType(typeof(Contact));

        Assert.That(contact, Is.Not.Null);

        var columns = contact.GetProperties().Select(property => property.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contact.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Contact)), Is.False, "an official carries no link to its authority");
            Assert.That(columns, Has.Member(nameof(Contact.Address)), "the address is one free-text block");
            Assert.That(columns, Does.Not.Contain("Town").And.Not.Contains("PostCode"), "and is never split into parts");
            Assert.That(contact.FindProperty(nameof(Contact.Kind))?.ClrType, Is.EqualTo(typeof(ContactKind)));
            Assert.That(IsIndexed(contact, nameof(Contact.DataBoxId)), Is.True, "looking a contact up by data box is the one unambiguous lookup");

            var dataBoxIndex = contact.GetIndexes().SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(Contact.TenantId), nameof(Contact.DataBoxId)], StringComparer.Ordinal));
            Assert.That(dataBoxIndex, Is.Not.Null, "the data box lookup stays inside the tenant");
        }
    }

    [Test]
    public void AContactBelongsToTheTenantNotToAUser()
    {
        var contact = Model.FindEntityType(typeof(Contact));

        Assert.That(contact, Is.Not.Null);

        var columns = ColumnsOf(contact);

        Assert.That(columns, Does.Not.Contain("UserId"), "a contact belongs to the tenant, not to a user");
    }
}
