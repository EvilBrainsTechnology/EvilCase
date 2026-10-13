using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActListRenderTests
{
    [Test]
    public void TheColumnsRenderInTheOrderTheHostGaveThem()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.ActWithLabels, ActColumn.Direction, ActColumn.Date]);

        component.WaitForElement("a.ec-table-row");

        string[] headers = ["Úkon", "Směr", "Datum"];

        Assert.That(
            component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
            Is.EqualTo(headers),
            "the host's order of the columns is the order of the headers");
    }

    [Test]
    public void AColumnRendersItsHeaderAndItsCellFromOneDefinition()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.ActWithLabels]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-header").TextContent.Trim().TrimEnd('↑', '↓').Trim(), Is.EqualTo("Úkon"));
            Assert.That(component.Find(".ec-table-cell").TextContent, Does.Contain("Oznámení"));
        }
    }

    [Test]
    public void EveryRowRendersOnceAndOnlyCssReflowsItOnANarrowWidth()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Date, ActColumn.ActWithLabels]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("a.ec-table-row"), Has.Count.EqualTo(1), "the same row reflows on a narrow width; only CSS chooses how");
            Assert.That(
                component.FindAll("a.ec-table-row")[0].QuerySelectorAll(".ec-table-cell")[1].ClassList,
                Does.Contain("ec-table-cell-primary"),
                "the Act column takes its own line where the row reflows");
        }
    }

    [Test]
    public void ASortableHeaderIsAButtonAndAnUnsortableOneIsPlainText()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Date, ActColumn.Contact]);

        component.WaitForElement("a.ec-table-row");

        var headers = component.FindAll(".ec-table-header");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers[0].QuerySelector("button.ec-sort"), Is.Not.Null, "a column with a sort key sorts");
            Assert.That(headers[1].QuerySelector("button"), Is.Null, "a column without a sort key carries no button");
        }
    }

    [Test]
    public async Task ClickingTheSortedHeaderFlipsTheDirectionAndAsksForTheFirstPageAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [ActColumn.Date], take: 2, paging: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].SortDirection, Is.EqualTo(ListSortDirection.Descending), "a click on the sorted column turns it around");
            Assert.That(requests[^1].Skip, Is.Zero, "a change of the order starts the list over");
        }
    }

    [Test]
    public async Task TheFilterTheHostGaveSurvivesEveryReloadTheToolbarTriggers()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var caseId = Guid.CreateVersion7();
        var component = Render(ctx, [ActColumn.Date], caseId: caseId);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        Assert.That(
            requests.Select(static request => request.CaseId),
            Is.All.EqualTo(caseId),
            "the toolbar narrows what the host's filter left, never widens it");
    }

    [Test]
    public async Task TheRowLinksToTheActDetail()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.ActWithLabels]);

        await component.WaitForElementAsync("a.ec-table-row");

        var row = component.Find("a.ec-table-row");

        Assert.That(row.GetAttribute("href"), Is.EqualTo($"/acts/{ActId}"));
    }

    [Test]
    public async Task TheDirectionFilterNarrowsTheListAndStartsItOver()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [ActColumn.Date], take: 2, paging: true, directionFilter: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("#acts-direction-incoming").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Direction, Is.EqualTo(ActDirection.Incoming));
            Assert.That(requests[^1].Skip, Is.Zero, "a changed direction filter starts the list over");
        }
    }

    [Test]
    public void TheFailureStateReplacesTheTableAndNamesWhatFailed()
    {
        using var ctx = new BunitContext();

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<ActListResponse>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        ctx.Services.AddSingleton(actsClient);

        var component = Render(ctx, [ActColumn.Date]);

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("a.ec-table-row"), Is.Empty, "a failed load leaves no row behind");
                Assert.That(component.Markup, Does.Contain("Seznam úkonů se nepodařilo načíst"));
                Assert.That(component.FindAll(".ec-card-error button"), Has.Count.EqualTo(1), "a failed load offers a retry (SDD-020)");
            }
        });
    }

    [Test]
    public void EveryColumnTrackComesFromAToken()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(
            ctx,
            [ActColumn.Changed, ActColumn.Date, ActColumn.Direction, ActColumn.Contact, ActColumn.ExternalNumber, ActColumn.ActWithLabels, ActColumn.ActNumberInCase, ActColumn.ActAndCase]);

        Assert.That(
            component.Find(".ec-table").GetAttribute("style"),
            Does.Not.Match("[0-9]+px"),
            "a column width is a token in ec-tokens.css, never a size written in the component");
    }

    [Test]
    public void TheActNumberInTheCaseDropsTheCasePrefix()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.ActNumberInCase]);

        component.WaitForElement("a.ec-table-row");

        var cell = component.Find(".ec-table-cell .ec-cell-text");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-header").TextContent.Trim().TrimEnd('↑', '↓').Trim(), Is.EqualTo("Číslo jednací"));
            Assert.That(cell.TextContent, Is.EqualTo("20260821-002"));
            Assert.That(cell.GetAttribute("title"), Is.EqualTo("EC/20260821-001/20260821-002"), "the title keeps the whole number");
        }
    }

    [Test]
    public void TheLabelsSitBesideTheTitleAsSmallChips()
    {
        using var ctx = new BunitContext();

        var labelled = Row with
        {
            Labels =
            [
                new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red },
                new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Hlídat lhůtu", Color = LabelColor.Orange },
            ],
        };

        Serve(ctx, out _, items: [labelled]);

        var component = Render(ctx, [ActColumn.ActWithLabels]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-table-cell-primary .ec-chip-sm").Select(static chip => chip.TextContent.Trim()),
                Is.EqualTo(["Priorita", "Hlídat lhůtu"]));
            Assert.That(component.FindAll(".ec-table-cell-primary .ec-chip-red"), Has.Count.EqualTo(1));
            Assert.That(component.Find(".ec-table-cell-primary .ec-cell-text").GetAttribute("title"), Is.EqualTo("Oznámení"));
        }
    }

    [Test]
    public void TheActAndItsCaseShareOneCellOnTwoLines()
    {
        using var ctx = new BunitContext();

        var labelled = Row with
        {
            Labels = [new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red }],
        };

        Serve(ctx, out _, items: [labelled]);

        var component = Render(ctx, [ActColumn.ActAndCase]);

        component.WaitForElement("a.ec-table-row");

        var cell = component.Find(".ec-table-cell");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cell.ClassList, Does.Contain("ec-table-cell-primary"));
            Assert.That(cell.ClassList, Does.Contain("ec-cell-stack"));
            Assert.That(component.Find(".ec-table-header").TextContent.Trim().TrimEnd('↑', '↓').Trim(), Is.EqualTo("Úkon a spis"));
            Assert.That(cell.QuerySelector(".ec-cell-line .ec-cell-text")!.TextContent, Is.EqualTo("Oznámení"));
            Assert.That(cell.QuerySelector(".ec-cell-line .ec-chip-sm")!.TextContent.Trim(), Is.EqualTo("Priorita"));
            Assert.That(cell.QuerySelector(".ec-cell-sub")!.TextContent, Is.EqualTo("Přestupek"));
            Assert.That(cell.QuerySelector(".ec-cell-sub")!.GetAttribute("title"), Is.EqualTo("Přestupek"));
        }
    }

    [Test]
    public void TheChangeReadsRelativelyAndTheTooltipKeepsTheMoment()
    {
        using var ctx = new BunitContext();

        var today = DateTime.Now.AddMinutes(-1);

        Serve(ctx, out _, items: [Row with { Changed = today }]);

        var component = Render(ctx, [ActColumn.Changed]);

        component.WaitForElement("a.ec-table-row");

        var moment = component.Find(".ec-table-cell span");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(moment.TextContent, Does.StartWith("dnes ").Or.StartWith("včera "), "a change of today reads as a word and the time");
            Assert.That(moment.GetAttribute("title"), Is.EqualTo(MomentDisplay.Text(today)));
        }
    }

    [Test]
    public void TheCaseCounterpartyIsMutedAndAnotherContactStandsOut()
    {
        using var ctx = new BunitContext();

        var counterpartyId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();

        Serve(
            ctx,
            out _,
            total: 2,
            items:
            [
                Row with { ContactId = counterpartyId, ContactName = "Městský úřad Vzorov" },
                Row with { ActId = Guid.CreateVersion7(), ContactId = otherId, ContactName = "Krajský úřad Vzorového kraje" },
            ]);

        var component = Render(ctx, [ActColumn.Contact], caseContactId: counterpartyId);

        component.WaitForElement("a.ec-table-row");

        Assert.That(
            component.FindAll(".ec-table-cell > span").Select(static span => span.ClassName),
            Is.EqualTo(["ec-cell-text ec-cell-muted", "ec-cell-text ec-cell-strong"]));
    }

    [Test]
    public void WithoutACaseContactNoContactIsMarked()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _, items: [Row with { ContactId = Guid.CreateVersion7(), ContactName = "Jan Novák" }]);

        var component = Render(ctx, [ActColumn.Contact]);

        component.WaitForElement("a.ec-table-row");

        Assert.That(component.Find(".ec-table-cell > span").ClassName, Is.EqualTo("ec-cell-text"));
    }

    [Test]
    public async Task TheFiltersStayFoldedUntilTheButtonOpensThem()
    {
        await using var ctx = new BunitContext();
        ServeNoLabels(ctx);
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Date], filterPanel: true);

        await component.WaitForElementAsync("a.ec-table-row");

        var button = component.Find("button[aria-controls='acts-filters']");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(button.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(component.FindAll("#acts-filters"), Is.Empty);
            Assert.That(component.FindAll("input[type=date]"), Is.Empty);
        }

        await button.ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("button[aria-controls='acts-filters']").GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(component.Find("#acts-filters").QuerySelectorAll("input[type=date]"), Has.Length.EqualTo(2));
            Assert.That(component.FindAll("#acts-labels"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task TheButtonCountsTheActiveFiltersAndClearingThemStartsOver()
    {
        await using var ctx = new BunitContext();
        ServeNoLabels(ctx);
        Serve(ctx, out var requests);

        var component = Render(ctx, [ActColumn.Date], filterPanel: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find("button[aria-controls='acts-filters']").ClickAsync(new MouseEventArgs());
        await component.Find("#acts-from").ChangeAsync(new ChangeEventArgs { Value = "2026-08-01" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].From, Is.EqualTo(new DateOnly(2026, 8, 1)));
            Assert.That(component.Find(".ec-filter-count").TextContent, Is.EqualTo("1"));
        }

        await component.Find("#acts-to").ChangeAsync(new ChangeEventArgs { Value = "2026-08-31" });

        Assert.That(component.Find(".ec-filter-count").TextContent, Is.EqualTo("2"));

        await component.Find(".ec-filter-clear").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].From, Is.Null);
            Assert.That(requests[^1].To, Is.Null);
            Assert.That(component.FindAll(".ec-filter-count"), Is.Empty, "no active filter, no count");
            Assert.That(component.FindAll("#acts-filters"), Has.Count.EqualTo(1), "clearing leaves the panel open");
        }
    }

    [Test]
    public async Task TheDirectionFilterOffersAllAndMarksTheChosenOne()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, [ActColumn.Date], directionFilter: true);

        await component.WaitForElementAsync("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-segment").Select(static segment => segment.TextContent.Trim()), Is.EqualTo(["Vše", "Příchozí", "Odchozí"]));
            Assert.That(component.Find("#acts-direction-all").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        }

        await component.Find("#acts-direction-outgoing").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Direction, Is.EqualTo(ActDirection.Outgoing));
            Assert.That(component.Find("#acts-direction-outgoing").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        }

        await component.Find("#acts-direction-all").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Direction, Is.Null);
    }

    [Test]
    public void TheDirectionPillCarriesItsArrow()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Direction]);

        component.WaitForElement("a.ec-table-row");

        var pill = component.Find(".ec-table-cell .ec-badge");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pill.TextContent.Trim(), Is.EqualTo("Příchozí"));
            Assert.That(pill.QuerySelectorAll("svg.ec-icon"), Has.Length.EqualTo(1), "the direction reads from its arrow as well as its word");
        }
    }

    [Test]
    public void AnActWithoutDirectionOrContactShowsALightDash()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _, items: [Row with { Direction = null, ContactId = null, ContactName = null }]);

        var component = Render(ctx, [ActColumn.Direction, ActColumn.Contact]);

        component.WaitForElement("a.ec-table-row");

        Assert.That(
            component.FindAll(".ec-cell-empty").Select(static dash => dash.TextContent),
            Is.EqualTo(["—", "—"]),
            "a missing value reads as a light dash (SDD-020)");
    }

    [Test]
    public async Task TheOpenCasesBoxNarrowsTheListAndStartsItOver()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);
        ServeNoLabels(ctx);

        var component = ctx.Render<ActList>(static parameters => parameters
            .Add(static list => list.Columns, [ActColumn.ActWithLabels])
            .Add(static list => list.Filter, new ActListRequest { CaseStatus = CaseStatusFilter.Open, Take = 20 })
            .Add(static list => list.ShowCaseStatusFilter, value: true));

        await component.WaitForElementAsync("a.ec-table-row");

        Assert.That(component.Find("#acts-open-cases").HasAttribute("checked"), Is.True, "the host's open status checks the box");

        await component.Find("#acts-open-cases").ChangeAsync(new ChangeEventArgs { Value = false });

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(requests[^1].CaseStatus, Is.EqualTo(CaseStatusFilter.All), "clearing the box lists acts of every case");
                Assert.That(requests[^1].Skip, Is.Zero);
            }
        });
    }

    [Test]
    public void TheOpenCasesBoxIsHiddenUnlessTheHostAsksForIt()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.ActWithLabels]);

        component.WaitForElement("a.ec-table-row");

        Assert.That(component.FindAll("#acts-open-cases"), Is.Empty);
    }

    private static readonly Guid CaseId = Guid.CreateVersion7();

    private static readonly Guid ActId = Guid.CreateVersion7();

    private static readonly ActListItem Row = new()
    {
        ActId = ActId,
        CaseId = CaseId,
        CaseNumber = "EC/20260821-001",
        CaseTitle = "Přestupek",
        ActNumber = "EC/20260821-001/20260821-002",
        Direction = ActDirection.Incoming,
        Title = "Oznámení",
        Date = new DateOnly(2026, 8, 21),
        Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
    };

    private static void Serve(BunitContext ctx, out List<ActListRequest> requests, int total = 1, IReadOnlyList<ActListItem>? items = null)
    {
        var captured = new List<ActListRequest>();
        var response = new ActListResponse
        {
            Items = items ?? [Row],
            TotalCount = total,
        };

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<ActListRequest>());

                return Task.FromResult(response);
            });

        ctx.Services.AddSingleton(actsClient);

        requests = captured;
    }

    private static void ServeNoLabels(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(labelsClient);
    }

    private static IRenderedComponent<ActList> Render(
        BunitContext ctx,
        IReadOnlyList<ActColumn> columns,
        int take = 20,
        bool paging = false,
        bool directionFilter = false,
        Guid? caseId = null,
        bool filterPanel = false,
        Guid? caseContactId = null)
    {
        return ctx.Render<ActList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new ActListRequest { CaseId = caseId, SortDirection = ListSortDirection.Ascending, Take = take })
            .Add(static list => list.ShowPaging, paging)
            .Add(static list => list.ShowDirectionFilter, directionFilter)
            .Add(static list => list.ShowDates, filterPanel)
            .Add(static list => list.ShowLabelFilter, filterPanel)
            .Add(static list => list.CaseContactId, caseContactId));
    }
}
