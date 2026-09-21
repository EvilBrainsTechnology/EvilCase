using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelFormModalRenderTests
{
    [Test]
    public void TheFormOffersTheWholePalette()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx);

        var component = ctx.Render<LabelFormModal>(static parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, value: null));

        Assert.That(
            component.FindAll(".ec-swatch"),
            Has.Count.EqualTo(LabelColorDisplay.Palette.Count),
            "the colour is a fixed palette, never a free code (SDD-019)");
    }

    [Test]
    public async Task AnEmptyNameIsRefusedAndNothingIsSent()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Serve(ctx);

        var component = ctx.Render<LabelFormModal>(static parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, value: null));

        await component.Find(".ec-modal-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await labelsClient.DidNotReceive().CreateLabel(Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>());
        Assert.That(component.Find(".ec-field-error").TextContent, Does.Contain("Zadejte název štítku"));
    }

    [Test]
    public async Task CreatingSendsTheNameAndTheChosenColour()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Serve(ctx);

        var component = ctx.Render<LabelFormModal>(static parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, value: null));

        await component.Find("#label-name").ChangeAsync(new ChangeEventArgs { Value = "nečinnost" });
        await component.Find(".ec-swatch[aria-label=\"Červená\"]").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-modal-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await labelsClient
            .Received(1)
            .CreateLabel(
                Arg.Is<LabelEditRequest>(static request => request.Name == "nečinnost" && request.Color == LabelColor.Red),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EditingSendsTheLabelId()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Serve(ctx);

        var labelId = Guid.CreateVersion7();
        var label = new LabelItem { LabelId = labelId, Name = "InfZ", Color = LabelColor.Blue };

        var component = ctx.Render<LabelFormModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, label));

        await component.Find("#label-name").ChangeAsync(new ChangeEventArgs { Value = "InfZ – nečinnost" });
        await component.Find(".ec-modal-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await labelsClient
            .Received(1)
            .EditLabel(labelId, Arg.Is<LabelEditRequest>(static request => request.Name == "InfZ – nečinnost"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ADuplicateNameIsReported()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Serve(ctx);
        labelsClient
            .CreateLabel(Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<LabelItem>(new ApiException(HttpStatusCode.Conflict, responseBody: null)));

        var component = ctx.Render<LabelFormModal>(static parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, value: null));

        await component.Find("#label-name").ChangeAsync(new ChangeEventArgs { Value = "InfZ" });
        await component.Find(".ec-modal-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(
                component.Find(".ec-alert").TextContent,
                Does.Contain("už existuje"),
                "a name another label carries is refused (SDD-019)"));
    }

    private static ILabelsClient Serve(BunitContext ctx)
    {
        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .CreateLabel(Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelItem { LabelId = Guid.CreateVersion7(), Name = "", Color = LabelColor.Blue }));
        labelsClient
            .EditLabel(Arg.Any<Guid>(), Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(labelsClient);

        return labelsClient;
    }
}
