using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcEmptyStateRenderTests
{
    [Test]
    public void TheStateShowsAnIconAndOneSentence()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcEmptyState>(static parameters => parameters
            .Add(static state => state.Icon, AppIcons.Paperclip)
            .Add(static state => state.Text, "Zatím tu nejsou žádné soubory."));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("svg.ec-icon"), Is.Not.Null);
            Assert.That(component.Find(".ec-empty-text").TextContent, Is.EqualTo("Zatím tu nejsou žádné soubory."));
        }
    }
}
