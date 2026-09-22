using System.Diagnostics;
using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcBadgeRenderTests
{
    [Test]
    public void EveryToneCarriesItsOwnClass()
    {
        foreach (var tone in Enum.GetValues<EcBadgeTone>())
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<EcBadge>(parameters => parameters
                .Add(static badge => badge.Text, "Stav")
                .Add(static badge => badge.Tone, tone));

            var toneClass = tone switch
            {
                EcBadgeTone.Active => "ec-badge-active",
                EcBadgeTone.Waiting => "ec-badge-waiting",
                EcBadgeTone.Closed => "ec-badge-closed",
                EcBadgeTone.Incoming => "ec-badge-incoming",
                EcBadgeTone.Outgoing => "ec-badge-outgoing",
                _ => throw new InvalidOperationException($"unexpected tone {tone}"),
            };

            string[] expected = ["ec-badge", toneClass];

            Assert.That(component.Find("span").ClassList, Is.EquivalentTo(expected));
        }
    }

    [Test]
    public void TheBadgeShowsItsText()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcBadge>(static parameters => parameters
            .Add(static badge => badge.Text, "Aktivní")
            .Add(static badge => badge.Tone, EcBadgeTone.Active));

        Assert.That(component.Find("span").TextContent.Trim(), Is.EqualTo("Aktivní"));
    }

    [Test]
    public void AnUnknownToneIsRefused()
    {
        using var ctx = new BunitContext();

        Assert.That(
            () => ctx.Render<EcBadge>(static parameters => parameters
                .Add(static badge => badge.Text, "Stav")
                .Add(static badge => badge.Tone, (EcBadgeTone)(-1))),
            Throws.InstanceOf<UnreachableException>(),
            "an unmapped tone must not fall back to closed");
    }
}
