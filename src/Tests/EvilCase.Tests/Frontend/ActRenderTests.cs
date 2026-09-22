using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActRenderTests
{
    [Test]
    public void TheActDetailRendersBothTheFilesAndTheCommentsCard()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };

        Serve(ctx, caseId, actId, detail);

        // The regression: FilesCard and CommentsCard shared the same @key, so a second render of
        // the batch threw from RenderTreeDiffBuilder and the whole batch was discarded.
        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        var empties = component.FindAll(".ec-empty-text").Select(static node => node.TextContent).ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(empties, Does.Contain("Zatím tu nejsou žádné soubory."), "the files card renders under its own @key");
            Assert.That(empties, Does.Contain("Zatím tu nejsou žádné komentáře."), "the comments card renders under its own @key");
        }
    }

    [Test]
    public void TheHeadCardCarriesTheNameTheDirectionTheDescriptionAndTheRowOfData()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var contactId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20250528-001/20250902-001",
            Date = new DateOnly(2025, 9, 2),
            ExternalActNumber = "MUVZ/2025/93547",
            Direction = ActDirection.Incoming,
            Title = "Rozhodnutí o přestupku",
            Description = "Vina a pokuta.",
            Contact = new ContactListItem { ContactId = contactId, Name = "Jan Novák", Kind = ContactKind.Person },
            Labels = [new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue }],
        };

        Serve(ctx, caseId, actId, detail);

        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("h1.ec-page-title").TextContent, Is.EqualTo("Rozhodnutí o přestupku"));
            Assert.That(component.Find(".ec-badge-incoming").TextContent, Is.EqualTo("Příchozí"));
            Assert.That(component.Find(".ec-detail-text").TextContent, Is.EqualTo("Vina a pokuta."));
            Assert.That(
                component.FindAll(".ec-detail-fact-name").Select(static node => node.TextContent),
                Is.EqualTo(["Datum úkonu", "Číslo jednací", "Externí číslo jednací", "Kontakt", "Štítky"]),
                "the row of data follows the design");
            Assert.That(component.Find(".ec-crumb[aria-current=page]").TextContent, Is.EqualTo("EC/20250528-001/20250902-001"));
        }
    }

    [Test]
    public void TheFilesAndCommentsCardsNameTheAct()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };

        Serve(ctx, caseId, actId, detail);

        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        var titles = component.FindAll(".ec-card-title").Select(static node => node.TextContent).ToArray();

        Assert.That(
            titles,
            Does.Contain("Soubory úkonu").And.Contain("Komentáře úkonu"),
            "the act's own files and comments say whose they are (SDD-020)");
    }

    [Test]
    public void ALoadFailureOffersToTryAgain()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };

        var (actsClient, _, _) = Serve(ctx, caseId, actId, detail);
        actsClient
            .GetAct(caseId, actId, Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<ActDetail>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("nepodařilo načíst"));
            Assert.That(component.Find(".ec-card-error .ec-button-secondary").TextContent, Does.Contain("Zkusit znovu"));
        }
    }

    [Test]
    public async Task ThePickerPutsALabelOnTheActWithoutLeavingThePage()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var label = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };

        var (actsClient, actLabelsClient, labelsClient) = Serve(ctx, caseId, actId, detail);

        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [label] }));
        actsClient
            .GetAct(caseId, actId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(detail), Task.FromResult(detail with { Labels = [label] }));

        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        await component.Find(".ec-detail-fact-actions .ec-button-ghost").ClickAsync(new MouseEventArgs());
        await component.Find("#act-labels").InputAsync(new ChangeEventArgs { Value = "inf" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        await actLabelsClient
            .Received(1)
            .SetActLabels(
                caseId,
                actId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { label.LabelId })),
                Arg.Any<CancellationToken>());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-chip").TextContent, Does.Contain("InfZ")),
            TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task AFailedLabelWriteIsReportedBesideTheLabels()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var label = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };

        var (_, actLabelsClient, labelsClient) = Serve(ctx, caseId, actId, detail);

        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [label] }));
        actLabelsClient
            .SetActLabels(caseId, actId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        await component.Find(".ec-detail-fact-actions .ec-button-ghost").ClickAsync(new MouseEventArgs());
        await component.Find("#act-labels").InputAsync(new ChangeEventArgs { Value = "inf" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-detail-fact .ec-alert").TextContent, Does.Contain("nepodařilo uložit")),
            TimeSpan.FromSeconds(2));
    }

    private static (IActsClient Acts, IActLabelsClient ActLabels, ILabelsClient Labels) Serve(BunitContext ctx, Guid caseId, Guid actId, ActDetail detail)
    {
        // FilesCard's drop zone imports its own module on first render from a path carrying a
        // version query string, which no SetupModule can name, and reads a property bUnit's
        // runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.GetAct(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));

        var actLabelsClient = Substitute.For<IActLabelsClient>();

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        var actFilesClient = Substitute.For<IActFilesClient>();
        actFilesClient.ListActFiles(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var actCommentsClient = Substitute.For<IActCommentsClient>();
        actCommentsClient.ListActComments(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(actLabelsClient);
        ctx.Services.AddSingleton(actCommentsClient);
        ctx.Services.AddSingleton(actFilesClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(Substitute.For<IModalService>());
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return (actsClient, actLabelsClient, labelsClient);
    }
}
