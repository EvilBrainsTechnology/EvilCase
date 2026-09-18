using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class AuthorInitialsDisplayTests
{
    [Test]
    public void ADottedLocalPartGivesTwoInitials()
    {
        Assert.That(AuthorInitialsDisplay.Text("martin.volek@vzorov.cz"), Is.EqualTo("MV"));
    }

    [Test]
    public void ASingleWordLocalPartGivesOneInitial()
    {
        Assert.That(AuthorInitialsDisplay.Text("admin@evilcase.local"), Is.EqualTo("A"));
    }

    [Test]
    public void AnEmptyLocalPartGivesAQuestionMark()
    {
        Assert.That(AuthorInitialsDisplay.Text("@evilcase.local"), Is.EqualTo("?"));
    }
}
