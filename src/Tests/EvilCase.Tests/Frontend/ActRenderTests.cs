using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Clipboard;
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
                Is.EqualTo(["Datum úkonu", "Číslo jednací", "Externí číslo jednací", "Odesílatel", "Štítky"]),
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

        await component.Find(".ec-detail-fact-labels .ec-button-ghost").ClickAsync(new MouseEventArgs());
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
    public async Task TheOpenPickerCarriesTheLabelsInsteadOfTheRowOfData()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
            Labels = [new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue }],
        };

        Serve(ctx, caseId, actId, detail);

        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));

        await component.Find(".ec-detail-fact-labels .ec-button-ghost").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find(".ec-label-picker .ec-chip").TextContent,
                Does.Contain("InfZ"),
                "the open picker shows the labels it can take off the act");
            Assert.That(
                component.FindAll(".ec-detail-fact-row .ec-chip"),
                Is.Empty,
                "the row of data does not repeat the labels the open picker carries");
            Assert.That(component.FindAll(".ec-label-picker #act-labels"), Has.Count.EqualTo(1));
        }
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

        await component.Find(".ec-detail-fact-labels .ec-button-ghost").ClickAsync(new MouseEventArgs());
        await component.Find("#act-labels").InputAsync(new ChangeEventArgs { Value = "inf" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-detail-fact .ec-alert").TextContent, Does.Contain("nepodařilo uložit")),
            TimeSpan.FromSeconds(2));
    }

    [Test]
    public void ThePagerNamesThePositionAndLinksTheNeighbours()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var previousId = Guid.CreateVersion7();
        var nextId = Guid.CreateVersion7();

        Serve(ctx, caseId, actId, MiddleOfThree(caseId, actId, previousId, nextId));

        var component = RenderAct(ctx, caseId, actId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-page-header-top .ec-act-pager-text").TextContent, Is.EqualTo("Úkon 2 z 3"));
            Assert.That(component.Find("a[aria-label='Předchozí úkon: Výzva']").GetAttribute("href"), Is.EqualTo($"/cases/{caseId}/act/{previousId}"));
            Assert.That(component.Find("a[aria-label='Další úkon: Odvolání']").GetAttribute("href"), Is.EqualTo($"/cases/{caseId}/act/{nextId}"));
        }
    }

    [Test]
    public void TheFirstActOffersNoPreviousAct()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var nextId = Guid.CreateVersion7();

        var detail = Detail(caseId, actId) with
        {
            ActPosition = 1,
            CaseActCount = 2,
            CaseTimeline =
            [
                new ActTimelineItem { ActId = actId, Title = "Úkon", Date = new DateOnly(2026, 8, 7), IsCurrent = true },
                new ActTimelineItem { ActId = nextId, Title = "Odvolání", Date = new DateOnly(2026, 8, 9), IsCurrent = false },
            ],
        };

        Serve(ctx, caseId, actId, detail);

        var component = RenderAct(ctx, caseId, actId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find("button[aria-label='Předchozí úkon']").HasAttribute("disabled"),
                Is.True,
                "a missing neighbour leaves its button disabled");
            Assert.That(component.FindAll("a[aria-label='Další úkon: Odvolání']"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void TheTimelineLinksTheNeighboursAndMarksThisAct()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var previousId = Guid.CreateVersion7();
        var nextId = Guid.CreateVersion7();

        Serve(ctx, caseId, actId, MiddleOfThree(caseId, actId, previousId, nextId));

        var component = RenderAct(ctx, caseId, actId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll("a.ec-timeline-item").Select(static link => link.GetAttribute("href")),
                Is.EqualTo([$"/cases/{caseId}/act/{previousId}", $"/cases/{caseId}/act/{actId}", $"/cases/{caseId}/act/{nextId}"]));
            Assert.That(component.Find("a.ec-timeline-item[aria-current=page] .ec-timeline-title").TextContent, Is.EqualTo("Úkon"));
            Assert.That(component.Find(".ec-timeline-direction-outgoing").TextContent.Trim(), Is.EqualTo("Odchozí"));
            Assert.That(
                component.FindAll("a.ec-timeline-item[aria-current=page] .ec-timeline-direction"),
                Is.Empty,
                "an act without a direction shows no direction");
            Assert.That(component.Find("a.ec-timeline-all").GetAttribute("href"), Is.EqualTo($"/cases/{caseId}"));
            Assert.That(component.Find("a.ec-timeline-all").TextContent.Trim(), Is.EqualTo("Všechny 3 úkony spisu"));
        }
    }

    [Test]
    public void ADifferingCaseContactIsFlaggedAtTheActsContact()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        var detail = Detail(caseId, actId) with
        {
            Direction = ActDirection.Incoming,
            Contact = new ContactListItem { ContactId = Guid.CreateVersion7(), Name = "Jan Novák", Kind = ContactKind.Person },
            CaseContact = new ContactListItem { ContactId = Guid.CreateVersion7(), Name = "Městský úřad Vzorov", Kind = ContactKind.Authority },
        };

        Serve(ctx, caseId, actId, detail);

        var component = RenderAct(ctx, caseId, actId);

        var warning = component.Find(".ec-detail-fact-warning[role=alert]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(warning.TextContent.Trim(), Is.EqualTo("Spis vede protistranu Městský úřad Vzorov"));
            Assert.That(
                warning.ParentElement!.QuerySelector(".ec-detail-fact-name")!.TextContent,
                Is.EqualTo("Odesílatel"),
                "the warning sits in the contact fact");
            Assert.That(component.FindAll(".ec-warning"), Is.Empty);
        }
    }

    [Test]
    public void TheCaseCardNamesTheCaseContactAndTheActCount()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var caseContactId = Guid.CreateVersion7();

        var detail = Detail(caseId, actId) with
        {
            CaseActCount = 3,
            CaseContact = new ContactListItem { ContactId = caseContactId, Name = "Městský úřad Vzorov", Kind = ContactKind.Authority },
        };

        Serve(ctx, caseId, actId, detail);

        var component = RenderAct(ctx, caseId, actId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-case-contact a").GetAttribute("href"), Is.EqualTo($"/contacts/{caseContactId}"));
            Assert.That(component.Find(".ec-case-contact a").TextContent, Is.EqualTo("Městský úřad Vzorov"));
            Assert.That(component.Find(".ec-case-meta").TextContent, Does.Contain("Aktivní · 3 úkony"));
        }
    }

    [Test]
    public async Task TheActNumberCopiesAndAMissingExternalNumberOffersNoCopy()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var detail = Detail(caseId, actId);

        Serve(ctx, caseId, actId, detail);

        var clipboard = (StubClipboardWriter)ctx.Services.GetRequiredService<IClipboardWriter>();
        var component = RenderAct(ctx, caseId, actId);

        Assert.That(
            component.FindAll("button[aria-label='Zkopírovat externí číslo jednací']"),
            Is.Empty,
            "an act with no external number has nothing to copy");

        await component.Find("button[aria-label='Zkopírovat číslo jednací']").ClickAsync(new MouseEventArgs());

        Assert.That(clipboard.Written, Is.EqualTo([detail.ActNumber]));
    }

    [Test]
    public async Task TheMenuOffersTheActActionsAndDeleteNoLongerStandsBesideEdit()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        Serve(ctx, caseId, actId, Detail(caseId, actId));

        var component = RenderAct(ctx, caseId, actId);

        await component.Find("button[aria-label='Další akce s úkonem']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll("[role=menuitem]").Select(static item => item.TextContent.Trim()),
                Is.EqualTo(["Nový úkon ve spisu", "Zkopírovat odkaz na úkon", "Smazat úkon…"]));
            Assert.That(component.FindAll(".ec-page-actions .ec-button-danger"), Is.Empty, "delete lives in the menu");
        }
    }

    [Test]
    public async Task TheMenuCopiesAnAbsoluteLinkToTheAct()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        Serve(ctx, caseId, actId, Detail(caseId, actId));

        var clipboard = (StubClipboardWriter)ctx.Services.GetRequiredService<IClipboardWriter>();
        var component = RenderAct(ctx, caseId, actId);

        await component.Find("button[aria-label='Další akce s úkonem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                clipboard.Written,
                Is.EqualTo([$"http://localhost/cases/{caseId}/act/{actId}"]),
                "the link is absolute, so it opens outside the app");
            Assert.That(component.Find(".ec-page > span[role=status]").TextContent, Is.EqualTo("Odkaz na úkon zkopírován."));
        }
    }

    [Test]
    public async Task TheMenuOpensTheDeleteConfirmation()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var detail = Detail(caseId, actId);

        Serve(ctx, caseId, actId, detail);

        var component = RenderAct(ctx, caseId, actId);

        await component.Find("button[aria-label='Další akce s úkonem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());

        Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain(detail.ActNumber));
    }

    [Test]
    public async Task TheMenuStartsANewActInTheCase()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();

        Serve(ctx, caseId, actId, Detail(caseId, actId));

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = RenderAct(ctx, caseId, actId);

        await component.Find("button[aria-label='Další akce s úkonem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        Assert.That(navigation.Uri, Does.EndWith($"/cases/{caseId}/act/new"));
    }

    private static IRenderedComponent<Act> RenderAct(BunitContext ctx, Guid caseId, Guid actId)
    {
        return ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));
    }

    private static ActDetail Detail(Guid caseId, Guid actId)
    {
        return new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260807-002",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };
    }

    private static ActDetail MiddleOfThree(Guid caseId, Guid actId, Guid previousId, Guid nextId)
    {
        return Detail(caseId, actId) with
        {
            ActPosition = 2,
            CaseActCount = 3,
            CaseTimeline =
            [
                new ActTimelineItem { ActId = previousId, Title = "Výzva", Date = new DateOnly(2026, 8, 1), Direction = ActDirection.Incoming, IsCurrent = false },
                new ActTimelineItem { ActId = actId, Title = "Úkon", Date = new DateOnly(2026, 8, 7), IsCurrent = true },
                new ActTimelineItem { ActId = nextId, Title = "Odvolání", Date = new DateOnly(2026, 8, 9), Direction = ActDirection.Outgoing, IsCurrent = false },
            ],
        };
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
        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return (actsClient, actLabelsClient, labelsClient);
    }
}
