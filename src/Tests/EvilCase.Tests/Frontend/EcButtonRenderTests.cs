using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcButtonRenderTests
{
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
}
