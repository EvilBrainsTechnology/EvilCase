using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActDeleteModalRenderTests
{
    [Test]
    public void TheSentenceNamesTheActAndWhatTheCascadeTakes()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (_, act) = Serve(ctx);

        var component = ctx.Render<ActDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Act, act));

        Assert.That(
            component.Find(".ec-confirm-text").TextContent,
            Does.Contain(act.ActNumber).And.Contain("komentáře a soubory"));
    }

    [Test]
    public async Task ConfirmingCallsTheApiAndRaisesOnDeleted()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (actsClient, act) = Serve(ctx);

        var deleted = false;

        var component = ctx.Render<ActDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Act, act)
            .Add(static modal => modal.OnDeleted, () => deleted = true));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await actsClient.Received(1).DeleteAct(act.CaseId, act.ActId, Arg.Any<CancellationToken>());
        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task AFailedDeleteIsReportedInTheModal()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (actsClient, act) = Serve(ctx);
        actsClient
            .DeleteAct(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = ctx.Render<ActDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Act, act));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("nepodařilo smazat")));
    }

    private static (IActsClient ActsClient, ActDetail Act) Serve(BunitContext ctx)
    {
        var act = new ActDetail
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20250528-001/20250902-001",
            Date = new DateOnly(2025, 9, 2),
            Title = "Rozhodnutí o přestupku",
        };

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .DeleteAct(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(actsClient);

        return (actsClient, act);
    }
}
