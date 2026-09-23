using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.Domain.Cases;
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
    public void EveryColumnTrackComesFromAToken()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [CaseColumn.Date, CaseColumn.Case, CaseColumn.Status, CaseColumn.Labels, CaseColumn.CaseNumber]);

        Assert.That(
            component.Find(".ec-table").GetAttribute("style"),
            Does.Not.Match("[0-9]+px"),
            "a column width is a token in ec-tokens.css, never a size written in the component");
    }

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests, int total = 1)
    {
        var captured = new List<CaseListRequest>();
        var response = new CaseListResponse
        {
            Items =
            [
                new CaseListItem
                {
                    CaseId = Guid.CreateVersion7(),
                    CaseNumber = "EC/20260821-001",
                    Title = "Přestupek",
                    Date = new DateOnly(2026, 8, 21),
                    Status = CaseStatus.Active,
                    Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
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

    private static IRenderedComponent<CaseList> Render(
        BunitContext ctx,
        IReadOnlyList<CaseColumn> columns,
        int take = 20,
        bool paging = false,
        bool search = false,
        bool scopeFilter = false,
        bool statusFilter = false,
        Guid? contactId = null)
    {
        return ctx.Render<CaseList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new CaseListRequest { ContactId = contactId, Take = take })
            .Add(static list => list.ShowPaging, paging)
            .Add(static list => list.ShowSearch, search)
            .Add(static list => list.ShowScopeFilter, scopeFilter)
            .Add(static list => list.ShowStatusFilter, statusFilter));
    }
}
