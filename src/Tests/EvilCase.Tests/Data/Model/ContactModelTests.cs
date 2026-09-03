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

        var columns = contact.GetProperties().Select(static property => property.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contact.GetForeignKeys().Any(static key => key.PrincipalEntityType.ClrType == typeof(Contact)), Is.False, "an official carries no link to its authority");
            Assert.That(columns, Has.Member(nameof(Contact.Address)), "the address is one free-text block");
            Assert.That(columns, Does.Not.Contain("Town").And.Not.Contains("PostCode"), "and is never split into parts");
            Assert.That(contact.FindProperty(nameof(Contact.Kind))?.ClrType, Is.EqualTo(typeof(ContactKind)));
            Assert.That(IsIndexed(contact, nameof(Contact.DataBoxId)), Is.False, "nothing looks a contact up by data box yet, and the index is not what makes one unique");
            Assert.That(IsIndexed(contact, nameof(Contact.TenantId)), Is.True, "the tenant filter is on every contact read");
        }
    }

    [Test]
    public void AContactReachesItsCasesAndItsActs()
    {
        var contact = Model.FindEntityType(typeof(Contact));

        Assert.That(contact, Is.Not.Null);

        var navigations = contact.GetNavigations().Select(static navigation => navigation.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(navigations, Has.Member(nameof(Contact.Cases)), "the contact detail lists the cases naming it");
            Assert.That(navigations, Has.Member(nameof(Contact.Acts)), "and the acts naming it");
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
