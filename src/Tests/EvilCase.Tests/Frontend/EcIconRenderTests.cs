using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcIconRenderTests
{
    [Test]
    public void TheIconDrawsThePathsItWasGiven()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcIcon>(static parameters => parameters.Add(static icon => icon.Paths, AppIcons.Plus));

        Assert.That(component.Find("svg").InnerHtml, Does.Contain("M12 5l0 14"));
    }

    [Test]
    public void TheDefaultSizeIsTheOneAButtonUses()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcIcon>(static parameters => parameters.Add(static icon => icon.Paths, AppIcons.Plus));

        string[] expected = ["ec-icon", "ec-icon-md"];

        Assert.That(component.Find("svg").ClassList, Is.EquivalentTo(expected));
    }

    [Test]
    public void TheSizeIsATokenClassAndNeverAnAttribute()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcIcon>(static parameters => parameters
            .Add(static icon => icon.Paths, AppIcons.Plus)
            .Add(static icon => icon.Size, EcIconSize.Large));

        var svg = component.Find("svg");

        string[] expected = ["ec-icon", "ec-icon-lg"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(svg.ClassList, Is.EquivalentTo(expected));
            Assert.That(svg.HasAttribute("width"), Is.False, "the edge length is a token, not an attribute");
            Assert.That(svg.HasAttribute("stroke-width"), Is.False, "the stroke is a rule of the stylesheet, not an attribute");
        }
    }

    [Test]
    public void AnUnknownSizeIsRefused()
    {
        using var ctx = new BunitContext();

        Assert.That(
            () => ctx.Render<EcIcon>(static parameters => parameters
                .Add(static icon => icon.Paths, AppIcons.Plus)
                .Add(static icon => icon.Size, (EcIconSize)(-1))),
            Throws.InstanceOf<InvalidOperationException>(),
            "an unmapped size must not fall back to the button size");
    }
}
