using System.Diagnostics;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActDirectionDisplayTests
{
    [TestCase(ActDirection.Incoming, EcBadgeTone.Incoming)]
    [TestCase(ActDirection.Outgoing, EcBadgeTone.Outgoing)]
    public void EveryDirectionCarriesItsOwnBadgeTone(ActDirection direction, EcBadgeTone expected)
    {
        Assert.That(ActDirectionDisplay.Tone(direction), Is.EqualTo(expected));
    }

    [Test]
    public void EveryDirectionReadsInCzech()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var direction in Enum.GetValues<ActDirection>())
                Assert.That(ActDirectionDisplay.Text(direction), Is.Not.Empty, $"{direction}: every known act direction renders text");
        }
    }

    [Test]
    public void AnActWithNoDirectionShowsADash()
    {
        Assert.That(ActDirectionDisplay.Text(direction: null), Is.EqualTo("—"), "an act without a direction is still a row on the screen");
    }

    [Test]
    public void ADirectionTheAppDoesNotKnowIsNeverDisplayed()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(static () => ActDirectionDisplay.Text((ActDirection)99), Throws.InstanceOf<UnreachableException>(), "a direction the app does not name never renders as a dash");
            Assert.That(static () => ActDirectionDisplay.Tone((ActDirection)99), Throws.InstanceOf<UnreachableException>());
            Assert.That(static () => ActDirectionDisplay.ContactLabel((ActDirection)99), Throws.InstanceOf<UnreachableException>());
            Assert.That(static () => ActDirectionDisplay.Icon((ActDirection)99), Throws.InstanceOf<UnreachableException>());
            Assert.That(static () => ActDirectionDisplay.Css((ActDirection)99), Throws.InstanceOf<UnreachableException>());
        }
    }

    [TestCase(null, "Kontakt")]
    [TestCase(ActDirection.Incoming, "Odesílatel")]
    [TestCase(ActDirection.Outgoing, "Adresát")]
    public void TheContactFactIsNamedByTheDirection(ActDirection? direction, string expected)
    {
        Assert.That(ActDirectionDisplay.ContactLabel(direction), Is.EqualTo(expected));
    }

    [TestCase(ActDirection.Incoming, "incoming")]
    [TestCase(ActDirection.Outgoing, "outgoing")]
    public void EveryDirectionNamesItsOwnTimelineTone(ActDirection direction, string expected)
    {
        Assert.That(ActDirectionDisplay.Css(direction), Is.EqualTo(expected));
    }

    [Test]
    public void EveryDirectionDrawsItsOwnArrow()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActDirectionDisplay.Icon(ActDirection.Incoming), Is.EqualTo(AppIcons.ArrowDownLeft));
            Assert.That(ActDirectionDisplay.Icon(ActDirection.Outgoing), Is.EqualTo(AppIcons.ArrowUpRight));
        }
    }
}
