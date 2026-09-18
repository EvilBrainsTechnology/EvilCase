using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcButtonRenderTests
{
    [Test]
    public void EveryVariantCarriesItsOwnClassAndNothingForeign()
    {
        foreach (var variant in Enum.GetValues<EcButtonVariant>())
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<EcButton>(parameters => parameters
                .Add(static button => button.Variant, variant)
                .AddChildContent("Uložit"));

            var variantClass = variant switch
            {
                EcButtonVariant.Primary => "ec-button-primary",
                EcButtonVariant.Secondary => "ec-button-secondary",
                EcButtonVariant.Ghost => "ec-button-ghost",
                EcButtonVariant.Danger => "ec-button-danger",
                _ => throw new InvalidOperationException($"unexpected variant {variant}"),
            };

            string[] expected = ["ec-button", variantClass, "ec-button-standard"];

            Assert.That(component.Find("button").ClassList, Is.EquivalentTo(expected), "a new component carries no class of a foreign library");
        }
    }

    [Test]
    public void TheSizeChoosesTheHeightClass()
    {
        using var ctx = new BunitContext();

        var compact = ctx.Render<EcButton>(static parameters => parameters
            .Add(static button => button.Variant, EcButtonVariant.Secondary)
            .Add(static button => button.Size, EcButtonSize.Compact)
            .AddChildContent("Uložit"));

        using var ctx2 = new BunitContext();

        var form = ctx2.Render<EcButton>(static parameters => parameters
            .Add(static button => button.Variant, EcButtonVariant.Secondary)
            .Add(static button => button.Size, EcButtonSize.Form)
            .AddChildContent("Uložit"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(compact.Find("button").ClassList, Does.Contain("ec-button-compact"));
            Assert.That(form.Find("button").ClassList, Does.Contain("ec-button-form"));
        }
    }

    [Test]
    public void TheIconStandsLeftOfTheTextAtTheButtonSize()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcButton>(static parameters => parameters
            .Add(static button => button.Variant, EcButtonVariant.Secondary)
            .Add(static button => button.Icon, AppIcons.Plus)
            .AddChildContent("Nový spis"));

        var button = component.Find("button");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(button.FirstElementChild!.LocalName, Is.EqualTo("svg"), "the icon stands left of the text");
            Assert.That(button.FirstElementChild!.ClassList, Does.Contain("ec-icon-md"));
        }
    }

    [Test]
    public void AnIconOnlyButtonWithoutALabelIsRefused()
    {
        using var ctx = new BunitContext();

        Assert.That(
            () => ctx.Render<EcButton>(static parameters => parameters
                .Add(static button => button.Variant, EcButtonVariant.Ghost)
                .Add(static button => button.Icon, AppIcons.Plus)),
            Throws.InstanceOf<InvalidOperationException>(),
            "a button with no text needs an aria-label");
    }

    [Test]
    public void AnIconOnlyButtonWithALabelRendersItAndTheSquareClass()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcButton>(static parameters => parameters
            .Add(static button => button.Variant, EcButtonVariant.Ghost)
            .Add(static button => button.Icon, AppIcons.Plus)
            .Add(static button => button.AriaLabel, "Smazat spis"));

        var button = component.Find("button");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(button.GetAttribute("aria-label"), Is.EqualTo("Smazat spis"));
            Assert.That(button.ClassList, Does.Contain("ec-button-icon"));
        }
    }

    [Test]
    public void ClickingTheButtonRaisesItsCallback()
    {
        using var ctx = new BunitContext();

        var clicked = false;

        var component = ctx.Render<EcButton>(parameters => parameters
            .Add(static button => button.Variant, EcButtonVariant.Primary)
            .Add(static button => button.OnClick, () => clicked = true)
            .AddChildContent("Uložit"));

        component.Find("button").Click();

        Assert.That(clicked, Is.True);
    }

    [Test]
    public void ADisabledButtonRendersDisabled()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcButton>(static parameters => parameters
            .Add(static button => button.Variant, EcButtonVariant.Primary)
            .Add(static button => button.Disabled, value: true)
            .AddChildContent("Uložit"));

        Assert.That(component.Find("button").HasAttribute("disabled"), Is.True);
    }

    [Test]
    public void AnUnknownVariantIsRefused()
    {
        using var ctx = new BunitContext();

        Assert.That(
            () => ctx.Render<EcButton>(static parameters => parameters
                .Add(static button => button.Variant, (EcButtonVariant)(-1))
                .AddChildContent("Uložit")),
            Throws.InstanceOf<InvalidOperationException>(),
            "an unmapped variant must not fall back to secondary");
    }

    [Test]
    public void AnUnknownSizeIsRefused()
    {
        using var ctx = new BunitContext();

        Assert.That(
            () => ctx.Render<EcButton>(static parameters => parameters
                .Add(static button => button.Variant, EcButtonVariant.Primary)
                .Add(static button => button.Size, (EcButtonSize)(-1))
                .AddChildContent("Uložit")),
            Throws.InstanceOf<InvalidOperationException>(),
            "an unmapped size must not fall back to standard");
    }
}
