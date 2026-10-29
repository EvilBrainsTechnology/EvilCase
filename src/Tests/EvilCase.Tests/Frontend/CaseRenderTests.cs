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
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
            Assert.That(component.FindAll(".ec-badge-active .ec-badge-dot"), Has.Count.EqualTo(1), "a status carries its dot (SDD-020)");
            Assert.That(component.Find(".ec-detail-text").TextContent, Is.EqualTo("Řízení o přestupku."));
            Assert.That(
                component.FindAll(".ec-detail-fact-name").Select(static node => node.FirstChild!.TextContent),
                Is.EqualTo(["Datum spisu", "Spisová značka", "Externí spisová značka", "Protistrana", "Nadřízený spis", "Štítky"]),
                "the row of data follows the design");
            Assert.That(
                component.Find(".ec-detail-facts").ChildNodes.OfType<IText>().Select(static node => node.Data.Trim()),
                Is.All.Empty,
                "the row of data holds only facts");
            Assert.That(component.Find(".ec-crumb[aria-current=page]").TextContent, Is.EqualTo("Přestupek"), "the current crumb names the case");
        }
    }

    [Test]
    public void TheCrumbsLinkEveryCaseAboveTheCaseTheRootFirst()
    {
        using var ctx = new BunitContext();

        var rootId = Guid.CreateVersion7();
        var parentId = Guid.CreateVersion7();
        var caseId = Guid.CreateVersion7();

        var detail = new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Žádost o informace",
            Status = CaseStatus.Active,
            Ancestors = [new CaseCrumb { CaseId = rootId, Title = "Přestupek" }, new CaseCrumb { CaseId = parentId, Title = "Náhrada újmy" }],
        };

        Serve(ctx, caseId, detail);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-crumb").Select(static crumb => crumb.TextContent),
                Is.EqualTo(["Spisy", "Přestupek", "Náhrada újmy", "Žádost o informace"]));
            Assert.That(
                component.FindAll("a.ec-crumb").Select(static crumb => crumb.GetAttribute("href")),
                Is.EqualTo(["/cases", $"/cases/{rootId}", $"/cases/{parentId}"]));
        }
    }

    [Test]
    public void ACaseWithoutADescriptionShowsNothingUnderTheTitle()
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

        Assert.That(component.FindAll(".ec-detail-text"), Is.Empty, "the row of data follows the title when there is no description");
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
    public void AMissingCaseSaysSoAndLeadsBackToTheCases()
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
            .Returns(static _ => Task.FromException<CaseDetail>(new ApiException(HttpStatusCode.NotFound, responseBody: null)));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-empty .ec-icon"), Is.Not.Null, "a not-found state is an empty state with its icon (SDD-020)");
            Assert.That(component.Find(".ec-empty-text").TextContent, Is.EqualTo("Spis nenalezen."));
            Assert.That(component.Find(".ec-empty a.ec-button").GetAttribute("href"), Is.EqualTo("/cases"));
            Assert.That(component.Find(".ec-empty a.ec-button").TextContent.Trim(), Is.EqualTo("Zpět na spisy"));
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
                Is.EqualTo(["Nový úkon", "Nový podřízený spis", "Zkopírovat odkaz na spis", "Smazat spis"]));
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

        Assert.That(navigation.Uri, Does.EndWith($"/acts/new?case={caseId}"));
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
                Is.EqualTo(["Datum", "Úkon", "Směr", "Kontakt", "Změněno"]));
            Assert.That(component.Find(".ec-detail-main .ec-cell-muted").TextContent, Is.EqualTo("Jan Novák"));
        }
    }

    [Test]
    public async Task TheAddButtonOpensTheBoxInPlaceAndAnOfferedLabelIsSavedAtOnce()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var carried = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };
        var offered = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var detail = Detail(caseId) with { Labels = [carried] };

        var (_, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, detail);

        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [LabelListItems.Of(carried), LabelListItems.Of(offered)] }));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        Assert.That(component.FindAll(".ec-label-field-inline .ec-chip-remove"), Has.Count.EqualTo(1), "the chip carries its cross");

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".ec-label-add"), Is.Empty, "the button gives way to the box");
                    Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1), "only a label the case lacks is offered");
                    Assert.That(component.Find(".ec-label-option").TextContent, Does.Contain("InfZ"));
                }
            },
            TimeSpan.FromSeconds(2));

        await component.Find(".ec-label-option").ClickAsync(new MouseEventArgs());

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { carried.LabelId, offered.LabelId })),
                Arg.Any<CancellationToken>());

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".ec-label-field-inline > .ec-chip"), Has.Count.EqualTo(2));
                    Assert.That(component.FindAll(".ec-label-menu"), Is.Empty, "picking a label closes the box");
                    Assert.That(component.FindAll(".ec-label-add"), Has.Count.EqualTo(1), "the add button returns");
                }
            },
            TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task ALabelAddedWhileTheLastOneIsSavingKeepsIt()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var first = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };
        var second = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };

        var detail = Detail(caseId);

        var (_, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, detail);

        var saving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [LabelListItems.Of(first), LabelListItems.Of(second)] }));
        caseLabelsClient
            .SetCaseLabels(caseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(saving.Task, Task.CompletedTask);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(2));

        var addingFirst = component.FindAll(".ec-label-option")[0].ClickAsync(new MouseEventArgs());

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1), "the added label leaves the offer before its save answers"),
            TimeSpan.FromSeconds(2));

        var addingSecond = component.Find(".ec-label-option").ClickAsync(new MouseEventArgs());

        saving.SetResult();

        await Task.WhenAll(addingFirst, addingSecond);

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { first.LabelId, second.LabelId })),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LeavingTheCaseMidSaveWritesTheLatestLabelsToThatCaseOnly()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var nextCaseId = Guid.CreateVersion7();

        var first = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };
        var second = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };
        var carried = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Hlídat lhůtu", Color = LabelColor.Orange };

        var (casesClient, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, Detail(caseId));

        var saving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [LabelListItems.Of(first), LabelListItems.Of(second)] }));
        caseLabelsClient
            .SetCaseLabels(Arg.Any<Guid>(), Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(saving.Task, Task.CompletedTask);
        casesClient
            .GetCase(nextCaseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Detail(nextCaseId) with { Labels = [carried] }));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(2));

        _ = component.FindAll(".ec-label-option")[0].ClickAsync(new MouseEventArgs());

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        var addingSecond = component.Find(".ec-label-option").ClickAsync(new MouseEventArgs());

        component.Render(parameters => parameters.Add(static page => page.CaseId, nextCaseId));

        saving.SetResult();
        await addingSecond;

        var chips = await component.WaitForElementsAsync(".ec-label-field-inline > .ec-chip", 1, TimeSpan.FromSeconds(2));

        Assert.That(chips.Single().TextContent, Does.Contain(carried.Name), "the next case shows its own labels");

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { first.LabelId, second.LabelId })),
                Arg.Any<CancellationToken>());
        await caseLabelsClient
            .DidNotReceive()
            .SetCaseLabels(nextCaseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnAddedLabelShowsBeforeTheSaveAnswers()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var added = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var (_, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, Detail(caseId));

        var saving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [LabelListItems.Of(added)] }));
        caseLabelsClient
            .SetCaseLabels(caseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(saving.Task);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        var adding = component.Find(".ec-label-option").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".ec-label-field-inline > .ec-chip"), Has.Count.EqualTo(1), "the chip shows while the save runs");
                    Assert.That(component.FindAll(".ec-label-menu"), Is.Empty, "the box closes without waiting for the save");
                }
            },
            TimeSpan.FromSeconds(2));

        saving.SetResult();

        await adding;
    }

    [Test]
    public async Task AFailedSaveTakesTheChangeBackAndSaysSo()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var kept = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };

        var (_, caseLabelsClient, _, _) = Serve(ctx, caseId, Detail(caseId) with { Labels = [kept] });

        caseLabelsClient
            .SetCaseLabels(caseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException()));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Odebrat štítek Priorita']").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".ec-label-field-inline > .ec-chip"), Has.Count.EqualTo(1), "the label the save did not take off returns");
                    Assert.That(component.Find(".ec-detail-fact-labels .ec-alert").TextContent, Does.Contain("Štítky se nepodařilo uložit"));
                }
            },
            TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task TheCrossOnAChipTakesTheLabelOffAtOnce()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var kept = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };
        var removed = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };

        var detail = Detail(caseId) with { Labels = [kept, removed] };

        var (_, caseLabelsClient, _, _) = Serve(ctx, caseId, detail);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Odebrat štítek InfZ']").ClickAsync(new MouseEventArgs());

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { kept.LabelId })),
                Arg.Any<CancellationToken>());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-field-inline > .ec-chip"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task TheShortcutOpensTheBox()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.InvokeAsync(component.FindComponent<LabelField>().Instance.OpenFromShortcut);

        Assert.That(component.FindAll("#case-labels"), Has.Count.EqualTo(1));
    }

    [Test]
    public void ACaseWithAFileShowsItsFilesWideAboveTheActs()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));
        ServeFiles(ctx, [File("a.pdf")]);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-file-card"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-detail-main .ec-card-title").Select(static node => node.TextContent), Is.EqualTo(["Soubory spisu", "Úkony"]));
            Assert.That(component.FindAll(".ec-detail-aside .ec-card-title").Select(static node => node.TextContent), Is.EqualTo(["Podřízené spisy", "Komentáře spisu"]));
            Assert.That(component.FindAll(".ec-file-card-compact"), Is.Empty);
        }
    }

    [Test]
    public async Task TheFilesCardShowsInNeitherColumnBeforeItsListIsKnown()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var list = new TaskCompletionSource<FileListResponse>();

        Serve(ctx, caseId, Detail(caseId));
        ctx.Services.GetRequiredService<ICaseFilesClient>().ListCaseFiles(caseId, Arg.Any<CancellationToken>()).Returns(list.Task);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        Assert.That(component.FindAll(".ec-file-card"), Is.Empty, "the card never jumps between the columns on load");

        list.SetResult(new FileListResponse { Items = [File("a.pdf")] });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-main .ec-file-card"), Has.Count.EqualTo(1)));
    }

    [Test]
    public void AListThatFailsToLoadShowsCompactWithTheRetry()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));
        ctx.Services.GetRequiredService<ICaseFilesClient>()
            .ListCaseFiles(caseId, Arg.Any<CancellationToken>())
            .Returns<Task<FileListResponse>>(static _ => throw new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-detail-aside .ec-file-card-compact .ec-card-error"), Has.Count.EqualTo(1)));
    }

    [Test]
    public async Task TheFirstUploadMovesTheCardToTheMainColumnAndTheLastDeleteBack()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var transfer = (StubFileTransferClient)ctx.Services.GetRequiredService<IFileTransferClient>();
        var files = ServeFiles(ctx, transfer.Uploaded);

        files.DeleteCaseFile(caseId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            transfer.Uploaded.Clear();

            return Task.CompletedTask;
        });

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-aside .ec-file-card-compact"), Has.Count.EqualTo(1)));

        component.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("a", "a.pdf"));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-main .ec-file-card"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-detail-aside .ec-file-card"), Is.Empty);
            Assert.That(component.Find(".ec-detail-main .ec-file-row-fresh .ec-badge").TextContent, Is.EqualTo("nový"), "the moved card keeps the highlight");
            Assert.That(files.ReceivedCalls().Count(static call => string.Equals(call.GetMethodInfo().Name, nameof(ICaseFilesClient.ListCaseFiles), StringComparison.Ordinal)), Is.EqualTo(2), "the move fetches nothing");
        }

        await component.Find(".ec-file-actions button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]").Single(static item => string.Equals(item.TextContent.Trim(), "Smazat", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
        await component.Find("dialog .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-aside .ec-file-card-compact"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-detail-main .ec-file-card"), Is.Empty);
            Assert.That(component.FindComponents<FileDeleteModal>(), Is.Empty);
        }
    }

    [Test]
    public async Task TheCardStaysPutWhileAnUploadRuns()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var transfer = (StubFileTransferClient)ctx.Services.GetRequiredService<IFileTransferClient>();
        var gate = new TaskCompletionSource();
        var stored = new List<FileListItem> { File("a.pdf") };
        var files = ServeFiles(ctx, stored);

        transfer.Gate = gate.Task;
        files.DeleteCaseFile(caseId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            stored.Clear();

            return Task.CompletedTask;
        });

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-main .ec-file-card"), Has.Count.EqualTo(1)));

        var card = component.FindComponent<FilesCard>().Instance;

        var picked = Substitute.For<IBrowserFile>();
        picked.Name.Returns("b.pdf");
        picked.Size.Returns(1);

        // UploadFiles blocks until the upload ends, so the change is raised without waiting for it.
        var input = component.FindComponent<InputFile>().Instance;
        var upload = component.InvokeAsync(async () => await input.OnChange.InvokeAsync(new InputFileChangeEventArgs([picked])));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-uploading"), Has.Count.EqualTo(1)));

        await component.Find(".ec-file-actions button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]").Single(static item => string.Equals(item.TextContent.Trim(), "Smazat", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());
        await component.Find("dialog .ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-row"), Is.Empty));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindComponent<FilesCard>().Instance, Is.SameAs(card), "an empty list mid-upload keeps the card and its inputs");
            Assert.That(component.FindAll(".ec-detail-main .ec-file-uploading"), Has.Count.EqualTo(1));
        }

        stored.Add(File("b.pdf"));
        gate.SetResult();
        await upload;

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-uploading"), Is.Empty));

        Assert.That(component.FindComponent<FilesCard>().Instance, Is.SameAs(card), "the list holds a file again, so the card stays wide");
    }

    [Test]
    public async Task AnotherCaseGetsItsOwnFilesCard()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();

        var (cases, _, _, _) = Serve(ctx, caseId, Detail(caseId));
        cases.GetCase(otherId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Detail(otherId)));

        var files = ctx.Services.GetRequiredService<ICaseFilesClient>();
        files.ListCaseFiles(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [File("a.pdf")] }));
        files.ListCaseFiles(otherId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-main .ec-file-card"), Has.Count.EqualTo(1)));

        component.Render(parameters => parameters.Add(static page => page.CaseId, otherId));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-detail-aside .ec-file-card-compact"), Has.Count.EqualTo(1)));

        Assert.That(component.FindAll(".ec-detail-main .ec-file-card"), Is.Empty);
    }

    private static ICaseFilesClient ServeFiles(BunitContext ctx, IReadOnlyList<FileListItem> files)
    {
        var client = ctx.Services.GetRequiredService<ICaseFilesClient>();

        client.ListCaseFiles(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new FileListResponse { Items = [.. files] }));

        return client;
    }

    private static FileListItem File(string fileName)
    {
        return new FileListItem { FileId = Guid.CreateVersion7(), FileName = fileName, SizeBytes = 1, Created = DateTime.UtcNow, Version = 1, VersionCount = 1 };
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
            .ListCaseComments(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

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
        ctx.Services.AddSingleton<IFilePicker>(new StubFilePicker());
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return (casesClient, caseLabelsClient, caseCommentsClient, labelsClient);
    }
}
