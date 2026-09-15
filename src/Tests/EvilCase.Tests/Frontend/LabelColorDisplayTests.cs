using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelColorDisplayTests
{
    [Test]
    public void ThePaletteOffersEveryColourOnce()
    {
        var palette = LabelColorDisplay.Palette;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(palette, Is.EquivalentTo(Enum.GetValues<LabelColor>()), "the form offers exactly the colours a label can carry");
            Assert.That(palette.Distinct().Count(), Is.EqualTo(palette.Count), "the same colour is never offered twice");
        }
    }

    [Test]
    public void EveryColourNamesATablerClassAndReadsInCzech()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var color in Enum.GetValues<LabelColor>())
            {
                Assert.That(LabelColorDisplay.Css(color), Is.Not.Empty, $"{color}: bg-{{name}} is what paints the badge and the dot");
                Assert.That(LabelColorDisplay.Text(color), Is.Not.Empty, $"{color}: the swatch is a colour square, so its name reaches the screen reader only from here");
            }
        }
    }

    [Test]
    public void NoTwoColoursSharePaintOrName()
    {
        var css = Enum.GetValues<LabelColor>().Select(LabelColorDisplay.Css).ToList();
        var texts = Enum.GetValues<LabelColor>().Select(LabelColorDisplay.Text).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(css.Count), "two colours painting the same are one colour to the reader");
            Assert.That(texts.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(texts.Count), "two colours named the same cannot be told apart in the form");
        }
    }
}
