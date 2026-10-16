using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseDeleteModalRenderTests
{
    [Test]
    public void TheSentenceNamesTheCaseAndWhatTheCascadeTakes()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (_, caseDetail) = Serve(ctx);

        var component = ctx.Render<CaseDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Case, caseDetail));

        Assert.That(
            component.Find(".ec-confirm-text").TextContent,
            Does.Contain(caseDetail.CaseNumber).And.Contain("podřízené spisy, úkony, komentáře a soubory"));
    }

    [Test]
    public async Task ConfirmingCallsTheApiAndRaisesOnDeleted()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (casesClient, caseDetail) = Serve(ctx);

        var deleted = false;

        var component = ctx.Render<CaseDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Case, caseDetail)
            .Add(static modal => modal.OnDeleted, () => deleted = true));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await casesClient.Received(1).DeleteCase(caseDetail.CaseId, Arg.Any<CancellationToken>());
        Assert.That(deleted, Is.True);
    }

    [Test]
    public async Task AFailedDeleteIsReportedInTheModal()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var (casesClient, caseDetail) = Serve(ctx);
        casesClient
            .DeleteCase(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = ctx.Render<CaseDeleteModal>(parameters => parameters
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.Case, caseDetail));

        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("nepodařilo smazat")));
    }

    private static (ICasesClient CasesClient, CaseDetail Case) Serve(BunitContext ctx)
    {
        var caseDetail = new CaseDetail
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .DeleteCase(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(casesClient);

        return (casesClient, caseDetail);
    }
}
