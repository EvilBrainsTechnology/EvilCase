using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactKindDisplayTests
{
    [Test]
    public void EveryKindReadsInCzech()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var kind in Enum.GetValues<ContactKind>())
                Assert.That(ContactKindDisplay.Text(kind), Is.Not.Empty, $"{kind}: every known contact kind renders text");
        }
    }

    [Test]
    public void AKindTheAppDoesNotKnowIsNeverDisplayed()
    {
        Assert.That(static () => ContactKindDisplay.Text((ContactKind)99), Throws.InstanceOf<ArgumentOutOfRangeException>(), "a kind the app does not name never renders as an empty label");
    }
}
