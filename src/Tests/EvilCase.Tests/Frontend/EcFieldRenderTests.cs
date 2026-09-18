using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcFieldRenderTests
{
    [Test]
    public void TheLabelPointsAtTheControl()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcField>(static parameters => parameters
            .Add(static field => field.Label, "Název spisu")
            .Add(static field => field.ControlId, "case-title")
            .AddChildContent("<input id=\"case-title\" />"));

        var label = component.Find("label");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(label.GetAttribute("for"), Is.EqualTo("case-title"));
            Assert.That(label.TextContent.TrimStart(), Does.StartWith("Název spisu"));
        }
    }

    [Test]
    public void AFieldWithoutAControlIdNamesNothing()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcField>(static parameters => parameters
            .Add(static field => field.Label, "Směr")
            .AddChildContent("<div role=\"group\"><button id=\"act-direction-incoming\" type=\"button\">Příchozí</button></div>"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("label"), Is.Empty, "a label would forward its click to a control the name does not belong to");
            Assert.That(component.Find(".ec-field-name").LocalName, Is.EqualTo("span"));
        }
    }

    [Test]
    public void ARequiredFieldCarriesTheAsteriskAndAnOptionalOneDoesNot()
    {
        using var required = new BunitContext();

        var requiredField = required.Render<EcField>(static parameters => parameters
            .Add(static field => field.Label, "Název spisu")
            .Add(static field => field.ControlId, "case-title")
            .Add(static field => field.Required, value: true));

        using var optional = new BunitContext();

        var optionalField = optional.Render<EcField>(static parameters => parameters
            .Add(static field => field.Label, "Popis")
            .Add(static field => field.ControlId, "case-description"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requiredField.FindAll(".ec-field-mark"), Has.Count.EqualTo(1));
            Assert.That(optionalField.FindAll(".ec-field-mark"), Has.Count.EqualTo(0));
        }
    }

    [Test]
    public void TheHintAndTheErrorStandBelowTheControl()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcField>(static parameters => parameters
            .Add(static field => field.Label, "Název spisu")
            .Add(static field => field.ControlId, "case-title")
            .Add(static field => field.Hint, "Nejvýše 256 znaků")
            .AddChildContent("<input id=\"case-title\" />")
            .Add(static field => field.Error, "Zadejte název spisu"));

        var field = component.Find(".ec-field");
        var children = field.Children;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(children[0].LocalName, Is.EqualTo("label"));
            Assert.That(children[1].LocalName, Is.EqualTo("input"));
            Assert.That(children[2].ClassList, Does.Contain("ec-field-hint"));
            Assert.That(children[3].ClassList, Does.Contain("ec-field-error"));
            Assert.That(children[2].TextContent, Is.EqualTo("Nejvýše 256 znaků"));
            Assert.That(children[3].TextContent, Is.EqualTo("Zadejte název spisu"));
        }
    }
}
