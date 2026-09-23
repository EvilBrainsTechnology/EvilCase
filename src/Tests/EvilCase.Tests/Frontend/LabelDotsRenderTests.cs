using Bunit;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelDotsRenderTests
{
    [Test]
    public void EveryLabelCarriesItsColourAndItsName()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx, ("Lhůta", LabelColor.Red), ("Odvolání", LabelColor.Teal));

        var marks = component.FindAll(".ec-label-mark");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(marks, Has.Count.EqualTo(2));
            Assert.That(marks[0].QuerySelector(".ec-dot")!.ClassList, Does.Contain("ec-dot-red"));
            Assert.That(marks[1].QuerySelector(".ec-dot")!.ClassList, Does.Contain("ec-dot-teal"));
        }
    }

    [Test]
    public void TheNameRidesAlongAsTextSoNoTooltipCarriesItAlone()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx, ("Lhůta", LabelColor.Red));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find(".ec-label-name").TextContent.Trim(),
                Is.EqualTo("Lhůta"),
                "the reflowed narrow row shows the name, where no tooltip opens");
            Assert.That(component.Find(".ec-label-mark").GetAttribute("title"), Is.EqualTo("Lhůta"));
            Assert.That(component.Find(".ec-dot").GetAttribute("aria-label"), Is.EqualTo("Lhůta"));
        }
    }

    [Test]
    public void NoLabelLeavesNoMarkup()
    {
        using var ctx = new BunitContext();

        var component = Render(ctx);

        Assert.That(component.Markup.Trim(), Is.Empty);
    }

    private static IRenderedComponent<LabelDots> Render(BunitContext ctx, params (string Name, LabelColor Color)[] labels)
    {
        var items = labels
            .Select(static label => new LabelItem { LabelId = Guid.CreateVersion7(), Name = label.Name, Color = label.Color })
            .ToArray();

        return ctx.Render<LabelDots>(parameters => parameters.Add(static dots => dots.Labels, items));
    }
}
