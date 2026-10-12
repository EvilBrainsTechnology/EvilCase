using System.Net;
using AngleSharp.Dom;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Clipboard;
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
                Is.EqualTo(["Datum spisu", "Spisová značka", "Externí spisová značka", "Protistrana", "Nadřízený spis", "Štítky"]),
                "the row of data follows the design");
            Assert.That(
                component.Find(".ec-detail-facts").ChildNodes.OfType<IText>().Select(static node => node.Data.Trim()),
                Is.All.Empty,
                "the row of data holds only facts");
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

        await component.Find("button[aria-label='Další akce se spisem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[3].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain("EC/20260821-001"));
            Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain("podřízené spisy, úkony, komentáře a soubory"));
        }
    }

    [Test]
    public async Task TheMenuOffersTheCaseActionsAndDeleteNoLongerStandsBesideEdit()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Další akce se spisem']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll("[role=menuitem]").Select(static item => item.TextContent.Trim()),
                Is.EqualTo(["Nový úkon", "Nový podřízený spis", "Zkopírovat odkaz na spis", "Smazat spis…"]));
            Assert.That(component.FindAll(".ec-page-actions .ec-button-danger"), Is.Empty, "delete lives in the menu");
        }
    }

    [Test]
    public async Task TheMenuCopiesAnAbsoluteLinkToTheCase()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var clipboard = (StubClipboardWriter)ctx.Services.GetRequiredService<IClipboardWriter>();
        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Další akce se spisem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                clipboard.Written,
                Is.EqualTo([$"http://localhost/cases/{caseId}"]),
                "the link is absolute, so it opens outside the app");
            Assert.That(component.Find(".ec-page > span[role=status]").TextContent, Is.EqualTo("Odkaz na spis zkopírován."));
        }
    }

    [Test]
    public async Task TheMenuStartsAChildCase()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Další akce se spisem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());

        Assert.That(navigation.Uri, Does.EndWith($"/cases/new?parent={caseId}"));
    }

    [Test]
    public async Task TheMenuStartsANewActInTheCase()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Další akce se spisem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        Assert.That(navigation.Uri, Does.EndWith($"/cases/{caseId}/act/new"));
    }

    [Test]
    public async Task TheCaseNumberCopiesAndAMissingExternalNumberOffersNoCopy()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var clipboard = (StubClipboardWriter)ctx.Services.GetRequiredService<IClipboardWriter>();
        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        Assert.That(
            component.FindAll("button[aria-label='Zkopírovat externí spisovou značku']"),
            Is.Empty,
            "a case with no external number has nothing to copy");

        await component.Find("button[aria-label='Zkopírovat spisovou značku']").ClickAsync(new MouseEventArgs());

        Assert.That(clipboard.Written, Is.EqualTo(["EC/20260821-001"]));
    }

    [Test]
    public async Task TheActsShowTheShortNumberAndMuteTheCaseCounterparty()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var contactId = Guid.CreateVersion7();

        var detail = Detail(caseId) with
        {
            Contact = new ContactListItem { ContactId = contactId, Name = "Jan Novák", Kind = ContactKind.Person },
        };

        Serve(ctx, caseId, detail);

        var act = new ActListItem
        {
            ActId = Guid.CreateVersion7(),
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            CaseTitle = "Přestupek",
            ActNumber = "EC/20260821-001/20260822-001",
            Title = "Výzva",
            Date = new DateOnly(2026, 8, 22),
            Changed = new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc),
            ContactId = contactId,
            ContactName = "Jan Novák",
        };

        var actsClient = ctx.Services.GetRequiredService<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListResponse { Items = [act], TotalCount = 1 }));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.WaitForElementAsync("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-detail-main .ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
                Is.EqualTo(["Datum", "Úkon", "Směr", "Kontakt", "Číslo jednací"]));
            Assert.That(component.Find(".ec-detail-main .ec-cell-mono .ec-cell-text").TextContent, Is.EqualTo("20260822-001"));
            Assert.That(component.Find(".ec-detail-main .ec-cell-muted").TextContent, Is.EqualTo("Jan Novák"));
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

    private static CaseDetail Detail(Guid caseId)
    {
        return new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
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
        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return (casesClient, caseLabelsClient, caseCommentsClient, labelsClient);
    }
}
