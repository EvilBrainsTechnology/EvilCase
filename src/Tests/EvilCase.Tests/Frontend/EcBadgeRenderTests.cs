using System.Diagnostics;
using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;

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
                EcBadgeTone.Authority => "ec-badge-authority",
                EcBadgeTone.Official => "ec-badge-official",
                EcBadgeTone.Person => "ec-badge-person",
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
    public void ADotLeadsTheTextOnlyWhereTheHostAsksForIt()
    {
        using var ctx = new BunitContext();

        var plain = ctx.Render<EcBadge>(static parameters => parameters
            .Add(static badge => badge.Text, "Aktivní")
            .Add(static badge => badge.Tone, EcBadgeTone.Active));

        var dotted = ctx.Render<EcBadge>(static parameters => parameters
            .Add(static badge => badge.Text, "Aktivní")
            .Add(static badge => badge.Tone, EcBadgeTone.Active)
            .Add(static badge => badge.ShowDot, value: true));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plain.FindAll(".ec-badge-dot"), Is.Empty, "a badge carries no dot unless the host asks for one");
            Assert.That(dotted.FindAll(".ec-badge-dot"), Has.Count.EqualTo(1));
            Assert.That(dotted.Find(".ec-badge-dot").GetAttribute("aria-hidden"), Is.EqualTo("true"));
            Assert.That(dotted.Find("span").TextContent.Trim(), Is.EqualTo("Aktivní"));
        }
    }

    [Test]
    public void AnIconLeadsTheText()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcBadge>(static parameters => parameters
            .Add(static badge => badge.Text, "Příchozí")
            .Add(static badge => badge.Tone, EcBadgeTone.Incoming)
            .Add(static badge => badge.Icon, AppIcons.ArrowDownLeft));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("svg.ec-icon"), Has.Count.EqualTo(1));
            Assert.That(component.Find("span").TextContent.Trim(), Is.EqualTo("Příchozí"));
        }
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
