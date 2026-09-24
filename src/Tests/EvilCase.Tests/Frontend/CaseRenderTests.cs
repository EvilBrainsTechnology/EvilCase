using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseRenderTests
{
    [Test]
    public void TheHeadCardCarriesTheNameTheStatusTheDescriptionAndTheRowOfData()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var contactId = Guid.CreateVersion7();

        var detail = new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Description = "Řízení o přestupku.",
            Status = CaseStatus.Active,
            Contact = new ContactListItem { ContactId = contactId, Name = "Jan Novák", Kind = ContactKind.Person },
        };

        Serve(ctx, caseId, detail);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("h1.ec-page-title").TextContent, Is.EqualTo("Přestupek"));
            Assert.That(component.Find(".ec-badge-active").TextContent, Is.EqualTo("Aktivní"));
            Assert.That(component.Find(".ec-detail-text").TextContent, Is.EqualTo("Řízení o přestupku."));
            Assert.That(
                component.FindAll(".ec-detail-fact-name").Select(static node => node.TextContent),
                Is.EqualTo(["Datum spisu", "Externí spisová značka", "Protistrana", "Nadřízený spis", "Štítky"]),
                "the row of data follows the design");
            Assert.That(component.Find(".ec-crumb[aria-current=page]").TextContent, Is.EqualTo("EC/20260821-001"));
        }
    }

    [Test]
    public void WhatBelongsToTheCaseSitsInTheRightColumnAndTheActsInTheMainOne()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var detail = new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        Serve(ctx, caseId, detail);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-detail-aside .ec-card-title").Select(static node => node.TextContent),
                Is.EqualTo(["Podřízené spisy", "Soubory spisu", "Komentáře spisu"]),
                "what belongs to the case sits in the side column (SDD-020)");
            Assert.That(component.FindAll(".ec-detail-main .ec-table"), Has.Count.EqualTo(1), "the acts grow in the main column");
        }
    }

    [Test]
    public void ALoadFailureOffersToTryAgain()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var detail = new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        var (casesClient, _, _, _) = Serve(ctx, caseId, detail);
        casesClient
            .GetCase(caseId, Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<CaseDetail>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("nepodařilo načíst"));
            Assert.That(component.Find(".ec-card-error .ec-button-secondary").TextContent, Does.Contain("Zkusit znovu"));
        }
    }

    [Test]
    public async Task DeletingIsConfirmedAndTheSentenceNamesWhatTheCascadeTakes()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var detail = new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        Serve(ctx, caseId, detail);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-page-actions .ec-button-danger").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain("EC/20260821-001"));
            Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain("podřízené spisy, úkony, komentáře a soubory"));
        }
    }

    [Test]
    public async Task ThePickerPutsALabelOnTheCaseWithoutLeavingThePage()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var label = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var detail = new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        var (casesClient, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, detail);

        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [label] }));
        casesClient
            .GetCase(caseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(detail), Task.FromResult(detail with { Labels = [label] }));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-detail-fact-labels .ec-button-ghost").ClickAsync(new MouseEventArgs());
        await component.Find("#case-labels").InputAsync(new ChangeEventArgs { Value = "inf" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { label.LabelId })),
                Arg.Any<CancellationToken>());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-chip").TextContent, Does.Contain("InfZ")),
            TimeSpan.FromSeconds(2));
    }

    private static (ICasesClient Cases, ICaseLabelsClient CaseLabels, ICaseCommentsClient CaseComments, ILabelsClient Labels) Serve(
        BunitContext ctx,
        Guid caseId,
        CaseDetail detail)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.GetCase(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var caseLabelsClient = Substitute.For<ICaseLabelsClient>();

        var caseCommentsClient = Substitute.For<ICaseCommentsClient>();
        caseCommentsClient
            .ListCaseComments(caseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(caseLabelsClient);
        ctx.Services.AddSingleton(caseCommentsClient);
        ctx.Services.AddSingleton(caseFilesClient);
        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return (casesClient, caseLabelsClient, caseCommentsClient, labelsClient);
    }
}
