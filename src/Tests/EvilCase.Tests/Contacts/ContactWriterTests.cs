using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class ContactWriterTests
{
    [Test]
    public void ABlankOptionalFieldIsFiledAsNothing()
    {
        var request = new ContactEditRequest { Name = "  Krajský soud  ", Kind = ContactKind.Authority, DataBoxId = "   ", Address = "\n " };

        var normalized = ContactWriter.Normalize(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(normalized.Name, Is.EqualTo("Krajský soud"), "a name is stored without its surrounding space");
            Assert.That(normalized.DataBoxId, Is.Null);
            Assert.That(normalized.Address, Is.Null);
            Assert.That(normalized.Kind, Is.EqualTo(request.Kind), "Normalize returns the request record, not a tuple");
        }
    }

    [Test]
    public void AFilledOptionalFieldIsTrimmed()
    {
        var request = new ContactEditRequest { Name = "Krajský soud", Kind = ContactKind.Authority, DataBoxId = " ksvz456 ", Address = " Soudní 3 " };

        var normalized = ContactWriter.Normalize(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(normalized.DataBoxId, Is.EqualTo("ksvz456"));
            Assert.That(normalized.Address, Is.EqualTo("Soudní 3"));
        }
    }
}
