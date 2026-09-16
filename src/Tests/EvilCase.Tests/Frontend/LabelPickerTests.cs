using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelPickerTests
{
    [Test]
    public void MenuIsAbsentUntilTheInputIsFocusedAndPresentAfter()
    {
        using var ctx = new BunitContext();

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Urgentní", Color = LabelColor.Red }] }));

        ctx.Services.AddSingleton(labelsClient);

        var component = ctx.Render<LabelPicker>(static parameters => parameters
            .Add(static picker => picker.InputId, "case-labels"));

        Assert.That(component.FindAll("div.ec-picker-menu"), Is.Empty, "the menu stays closed until the input is focused");

        component.Find("#case-labels").Focus();

        Assert.That(component.FindAll("div.ec-picker-menu"), Has.Count.EqualTo(1), "the menu opens once the input is focused");
    }
}
