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
    [TestCase(4, 1, "Štítek nese 4 spisy a 1 úkon. Smazáním z nich zmizí; spisy ani úkony tím nezanikají.")]
    [TestCase(1, 5, "Štítek nese 1 spis a 5 úkonů. Smazáním z nich zmizí; spisy ani úkony tím nezanikají.")]
    [TestCase(3, 0, "Štítek nese 3 spisy. Smazáním z nich zmizí; spisy tím nezanikají.")]
    [TestCase(0, 2, "Štítek nese 2 úkony. Smazáním z nich zmizí; úkony tím nezanikají.")]
    [TestCase(1, 0, "Štítek nese 1 spis. Smazáním z něj zmizí; spis tím nezaniká.")]
    [TestCase(0, 0, "Štítek nenese žádný spis ani úkon.")]
    public void TheSentenceNamesTheLabelAndWhatItCarries(int caseCount, int actCount, string expected)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (_, label) = Serve(ctx, caseCount, actCount);

        var component = ctx.Render<LabelDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Label, label));

        Assert.That(
            string.Join(' ', component.Find(".ec-confirm-text").TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
            Is.EqualTo($"Opravdu smazat štítek InfZ? {expected}"));
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

    private static (ILabelsClient LabelsClient, LabelListItem Label) Serve(BunitContext ctx, int caseCount = 0, int actCount = 0)
    {
        var label = new LabelListItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue, CaseCount = caseCount, ActCount = actCount };

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(labelsClient);

        return (labelsClient, label);
    }
}
