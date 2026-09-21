using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelDeleteModalRenderTests
{
    [Test]
    public void TheSentenceNamesTheLabelAndTheCascade()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (_, label) = Serve(ctx);

        var component = ctx.Render<LabelDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, label));

        Assert.That(
            component.Find(".ec-confirm-text").TextContent,
            Does.Contain("InfZ").And.Contain("ze všech spisů a úkonů"));
    }

    [Test]
    public async Task ConfirmingCallsTheApiAndRaisesOnDeleted()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (labelsClient, label) = Serve(ctx);

        var deleted = false;

        var component = ctx.Render<LabelDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, label)
            .Add(static modal => modal.OnDeleted, () => deleted = true));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await labelsClient.Received(1).DeleteLabel(label.LabelId, Arg.Any<CancellationToken>());
        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task AFailedDeleteIsReportedInTheModal()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (labelsClient, label) = Serve(ctx);
        labelsClient
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = ctx.Render<LabelDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, label));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("nepodařilo smazat")));
    }

    private static (ILabelsClient LabelsClient, LabelItem Label) Serve(BunitContext ctx)
    {
        var label = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(labelsClient);

        return (labelsClient, label);
    }
}
