using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class NewActModelTests
{
    [Test]
    public void SwitchingTheDirectionMovesTheDefaultContactToTheOtherSide()
    {
        var contact = new ContactListItem { Id = Guid.CreateVersion7(), Kind = ContactKind.Person, Name = "Výchozí kontakt" };
        var model = new NewActModel { AddressedToContact = contact, IssuedByContact = null };

        model.SwapContacts();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.IssuedByContact, Is.SameAs(contact), "an outgoing act prefills the sender and an incoming one the recipient");
            Assert.That(model.AddressedToContact, Is.Null);
        }

        model.SwapContacts();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.AddressedToContact, Is.SameAs(contact));
            Assert.That(model.IssuedByContact, Is.Null);
        }
    }
}
