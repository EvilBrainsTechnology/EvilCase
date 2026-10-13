using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseListRenderTests
{
    [Test]
    public void TheColumnsRenderInTheOrderTheHostGaveThem()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Status, CaseColumn.CaseNumber, CaseColumn.Date]);

        component.WaitForElement("a.ec-table-row");

        string[] headers = ["Stav", "Spisová značka", "Datum spisu"];

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

        var component = Render(ctx, [CaseColumn.CaseNumber]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-header").TextContent.Trim(), Is.EqualTo("Spisová značka"));
            Assert.That(component.Find(".ec-table-cell").TextContent, Does.Contain("EC/20260821-001"));
        }
    }

    [Test]
    public void EveryRowRendersOnceAndOnlyCssReflowsItOnANarrowWidth()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Date, CaseColumn.Case]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("a.ec-table-row"), Has.Count.EqualTo(1), "the same row reflows on a narrow width; only CSS chooses how");
            Assert.That(
                component.FindAll("a.ec-table-row")[0].QuerySelectorAll(".ec-table-cell")[1].ClassList,
                Does.Contain("ec-table-cell-primary"),
                "the Case column takes its own line where the row reflows");
        }
    }

    [Test]
    public void ASortableHeaderIsAButtonAndAnUnsortableOneIsPlainText()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Date, CaseColumn.Status]);

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

        var component = Render(ctx, [CaseColumn.Date], take: 2, paging: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].SortDirection, Is.EqualTo(ListSortDirection.Ascending), "a click on the sorted column turns it around");
            Assert.That(requests[^1].Skip, Is.Zero, "a change of the order starts the list over");
        }
    }

    [Test]
    public async Task ChangingTheSearchResetsTheListToItsFirstPage()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [CaseColumn.Date], take: 2, paging: true, search: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "kolo" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Search, Is.EqualTo("kolo"));
            Assert.That(requests[^1].Skip, Is.Zero, "a new search starts the list over");
        }
    }

    [Test]
    public async Task TheFilterTheHostGaveSurvivesEveryReloadTheToolbarTriggers()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var contactId = Guid.CreateVersion7();
        var component = Render(ctx, [CaseColumn.Date], contactId: contactId);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        Assert.That(
            requests.Select(static request => request.ContactId),
            Is.All.EqualTo(contactId),
            "the toolbar narrows what the host's filter left, never widens it");
    }

    [Test]
    public async Task AnEqualFilterHandedAgainLoadsNothingAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, [CaseColumn.Date]);

        await component.WaitForElementAsync("a.ec-table-row");

        // The host builds its filter in a property, so every one of its renders hands over a new
        // record of the same values.
        component.Render(static parameters => parameters.Add(static list => list.Filter, new CaseListRequest { Take = 20 }));

        Assert.That(requests, Has.Count.EqualTo(1), "a filter the list already loaded reloads nothing");
    }

    [Test]
    public void TheFailureStateReplacesTheTableAndNamesWhatFailed()
    {
        using var ctx = new BunitContext();

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<CaseListResponse>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        ctx.Services.AddSingleton(casesClient);

        var component = Render(ctx, [CaseColumn.Date]);

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("a.ec-table-row"), Is.Empty, "a failed load leaves no row behind");
                Assert.That(component.Markup, Does.Contain("Seznam spisů se nepodařilo načíst"));
                Assert.That(component.FindAll(".ec-card-error button"), Has.Count.EqualTo(1), "a failed load offers a retry (SDD-020)");
            }
        });
    }

    [Test]
    public async Task TheScopeCheckboxWidensTheListBeyondTheRootCases()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, [CaseColumn.Date], scopeFilter: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-check-box").ChangeAsync(new ChangeEventArgs { Value = false });

        Assert.That(requests[^1].Scope, Is.EqualTo(CaseListScope.All), "unchecking the box widens the list beyond the root cases");
    }

    [Test]
    public async Task TheStatusSelectSendsTheChosenFilter()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [CaseColumn.Date], take: 2, paging: true, statusFilter: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find(".ec-select").ChangeAsync(new ChangeEventArgs { Value = nameof(CaseStatusFilter.Closed) });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Status, Is.EqualTo(CaseStatusFilter.Closed));
            Assert.That(requests[^1].Skip, Is.Zero, "a changed status starts the list over");
        }
    }

    [Test]
    public async Task TheListTellsTheHostHowManyRecordsTheFilterLeaves()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _, total: 7);

        int? reported = null;

        var component = ctx.Render<CaseList>(parameters => parameters
            .Add(static list => list.Columns, [CaseColumn.Date])
            .Add(static list => list.Filter, new CaseListRequest { Take = 20 })
            .Add(static list => list.TotalChanged, count => reported = count));

        await component.WaitForAssertionAsync(() => Assert.That(reported, Is.EqualTo(7)));
    }

    [Test]
    public async Task TheLabelFilterSendsTheChosenLabel()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Serve(ctx, out var requests);

        var labelId = Guid.CreateVersion7();

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [new LabelListItem { LabelId = labelId, Name = "Urgentní", Color = LabelColor.Red, CaseCount = 0, ActCount = 0 }] }));

        ctx.Services.AddSingleton(labelsClient);

        var component = Render(ctx, [CaseColumn.Date], labelFilter: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find("button[aria-controls='cases-filters']").ClickAsync(new MouseEventArgs());
        await component.Find("#cases-labels").InputAsync(new ChangeEventArgs { Value = "urg" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        IReadOnlyList<Guid> chosen = [labelId];

        await component.WaitForAssertionAsync(
            () => Assert.That(requests[^1].LabelIds, Is.EqualTo(chosen)),
            TimeSpan.FromSeconds(2));
    }

    [Test]
    public void EveryColumnTrackComesFromAToken()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Changed, CaseColumn.Date, CaseColumn.Case, CaseColumn.Status, CaseColumn.CaseNumber, CaseColumn.ExternalNumber]);

        Assert.That(
            component.Find(".ec-table").GetAttribute("style"),
            Does.Not.Match("[0-9]+px"),
            "a column width is a token in ec-tokens.css, never a size written in the component");
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

        var component = Render(ctx, [CaseColumn.Case]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-table-cell-primary .ec-chip-sm").Select(static chip => chip.TextContent.Trim()),
                Is.EqualTo(["Priorita", "Hlídat lhůtu"]));
            Assert.That(component.FindAll(".ec-table-cell-primary .ec-chip-red"), Has.Count.EqualTo(1));
            Assert.That(component.Find(".ec-table-cell-primary .ec-cell-text").GetAttribute("title"), Is.EqualTo("Přestupek"));
        }
    }

    [Test]
    public void TheStatusPillCarriesADot()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Status]);

        component.WaitForElement("a.ec-table-row");

        Assert.That(component.FindAll(".ec-table-cell .ec-badge .ec-badge-dot"), Has.Count.EqualTo(1), "a case status reads from its colour as well as its word");
    }

    [Test]
    public void TheExternalCaseNumberSitsUnderTheCaseNumber()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _, total: 2, items: [Row with { ExternalCaseNumber = "VV41/2025/08464" }, Row with { CaseId = Guid.CreateVersion7() }]);

        var component = Render(ctx, [CaseColumn.CaseNumber]);

        component.WaitForElement("a.ec-table-row");

        var cells = component.FindAll(".ec-table-cell");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cells[0].ClassList, Does.Contain("ec-cell-stack"));
            Assert.That(cells[0].QuerySelector(".ec-cell-sub")!.TextContent, Is.EqualTo("VV41/2025/08464"));
            Assert.That(cells[1].QuerySelector(".ec-cell-sub"), Is.Null, "a case without an external number shows no empty line");
        }
    }

    [Test]
    public void TheChangeOfTodayReadsRelativelyAndTheTooltipKeepsTheMoment()
    {
        using var ctx = new BunitContext();

        var today = DateTime.Now.AddMinutes(-1);

        Serve(ctx, out _, items: [Row with { Changed = today }]);

        var component = Render(ctx, [CaseColumn.Changed]);

        component.WaitForElement("a.ec-table-row");

        var moment = component.Find(".ec-table-cell span");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(moment.TextContent, Does.StartWith("dnes ").Or.StartWith("včera "), "a change of today reads as a word and the time");
            Assert.That(moment.GetAttribute("title"), Is.EqualTo(MomentDisplay.Text(today)));
        }
    }

    [Test]
    public async Task TheStatusSwitchOffersEveryStatusAndSendsTheChosenOne()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [CaseColumn.Date], take: 2, paging: true, statusSwitch: true);

        await component.WaitForElementAsync("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-segment").Select(static segment => segment.TextContent.Trim()),
                Is.EqualTo(["Otevřené", "Aktivní", "Čeká na úřad", "Uzavřené", "Vše"]));
            Assert.That(component.Find("#cases-status-Open").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        }

        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("#cases-status-Closed").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Status, Is.EqualTo(CaseStatusFilter.Closed));
            Assert.That(requests[^1].Skip, Is.Zero, "a changed status starts the list over");
            Assert.That(component.Find("#cases-status-Closed").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(component.Find("#cases-status-Open").GetAttribute("aria-pressed"), Is.EqualTo("false"));
        }
    }

    [Test]
    public async Task TheFiltersStayFoldedUntilTheButtonOpensThemAndCountWhatIsSet()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ServeNoLabels(ctx);
        Serve(ctx, out var requests);

        var component = Render(ctx, [CaseColumn.Date], dates: true, labelFilter: true);

        await component.WaitForElementAsync("a.ec-table-row");

        var button = component.Find("button[aria-controls='cases-filters']");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(button.GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(component.FindAll("#cases-filters"), Is.Empty);
            Assert.That(component.FindAll("input[type=date]"), Is.Empty);
        }

        await button.ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("button[aria-controls='cases-filters']").GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(
                component.FindAll("#cases-filters .ec-filter-name").Select(static name => name.TextContent),
                Is.EqualTo(["Datum spisu od", "do", "Štítek"]));
            Assert.That(
                component.FindAll("#cases-filters input[type=date]").Where(static input => input.HasAttribute("aria-label")),
                Is.Empty,
                "the visible label names the date input; an aria-label would replace it");
        }

        await component.Find("#cases-from").ChangeAsync(new ChangeEventArgs { Value = "2026-08-01" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].From, Is.EqualTo(new DateOnly(2026, 8, 1)));
            Assert.That(component.Find(".ec-filter-count").TextContent, Is.EqualTo("1"));
        }

        await component.Find(".ec-filter-clear").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].From, Is.Null);
            Assert.That(component.FindAll(".ec-filter-count"), Is.Empty, "no active filter, no count");
        }
    }

    [Test]
    public async Task AHostFilterWithAnotherStatusResetsTheSwitch()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, [CaseColumn.Date], statusSwitch: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find("#cases-status-Closed").ClickAsync(new MouseEventArgs());

        component.Render(static parameters => parameters.Add(static list => list.Filter, new CaseListRequest { Take = 20, Status = CaseStatusFilter.Active }));

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#cases-status-Active").GetAttribute("aria-pressed"), Is.EqualTo("true"), "a new address narrows the list and the switch with it");
                Assert.That(requests[^1].Status, Is.EqualTo(CaseStatusFilter.Active));
            }
        });
    }

    private static readonly CaseListItem Row = new()
    {
        CaseId = Guid.CreateVersion7(),
        CaseNumber = "EC/20260821-001",
        Title = "Přestupek",
        Date = new DateOnly(2026, 8, 21),
        Status = CaseStatus.Active,
        Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
    };

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests, int total = 1, IReadOnlyList<CaseListItem>? items = null)
    {
        var captured = new List<CaseListRequest>();
        var response = new CaseListResponse
        {
            Items = items ?? [Row],
            TotalCount = total,
        };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(response);
            });

        ctx.Services.AddSingleton(casesClient);

        requests = captured;
    }

    private static void ServeNoLabels(BunitContext ctx)
    {
        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(labelsClient);
    }

    private static IRenderedComponent<CaseList> Render(
        BunitContext ctx,
        IReadOnlyList<CaseColumn> columns,
        int take = 20,
        bool paging = false,
        bool search = false,
        bool scopeFilter = false,
        bool statusFilter = false,
        bool statusSwitch = false,
        bool dates = false,
        bool labelFilter = false,
        Guid? contactId = null)
    {
        return ctx.Render<CaseList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new CaseListRequest { ContactId = contactId, Take = take })
            .Add(static list => list.ShowPaging, paging)
            .Add(static list => list.ShowSearch, search)
            .Add(static list => list.ShowScopeFilter, scopeFilter)
            .Add(static list => list.ShowStatusFilter, statusFilter)
            .Add(static list => list.ShowStatusSwitch, statusSwitch)
            .Add(static list => list.ShowDates, dates)
            .Add(static list => list.ShowLabelFilter, labelFilter));
    }
}
