using Bunit;
using EvilBrains.EvilCase.App.Components;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchMarkRenderTests
{
    [Test]
    public void TheHighlightedTitleKeepsCzechTypography()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<SearchMark>(static parameters => parameters
            .Add(static mark => mark.Text, "Výzva k úhradě")
            .Add(static mark => mark.Term, "k úh"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("mark.ec-hit").TextContent, Is.EqualTo("k úh"));
            Assert.That(component.Markup, Does.StartWith("Výzva "));
        }
    }
}
