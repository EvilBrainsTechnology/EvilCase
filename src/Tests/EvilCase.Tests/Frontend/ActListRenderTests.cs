using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.Domain.Acts;
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

        var component = Render(ctx, [ActColumn.Act, ActColumn.Direction, ActColumn.Date]);

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

        var component = Render(ctx, [ActColumn.ActNumber]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-header").TextContent.Trim(), Is.EqualTo("Číslo jednací"));
            Assert.That(component.Find(".ec-table-cell").TextContent, Does.Contain("EC/20260821-001/1"));
        }
    }

    [Test]
    public void EveryRowRendersOnceAndOnlyCssReflowsItOnANarrowWidth()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Date, ActColumn.Act]);

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

        var component = Render(ctx, [ActColumn.Act]);

        await component.WaitForElementAsync("a.ec-table-row");

        var row = component.Find("a.ec-table-row");

        Assert.That(row.GetAttribute("href"), Is.EqualTo($"/cases/{CaseId}/act/{ActId}"));
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
            [ActColumn.Changed, ActColumn.Date, ActColumn.Case, ActColumn.Act, ActColumn.Direction, ActColumn.Contact, ActColumn.Labels, ActColumn.ActNumber, ActColumn.ExternalNumber]);

        Assert.That(
            component.Find(".ec-table").GetAttribute("style"),
            Does.Not.Match("[0-9]+px"),
            "a column width is a token in ec-tokens.css, never a size written in the component");
    }

    private static readonly Guid CaseId = Guid.CreateVersion7();

    private static readonly Guid ActId = Guid.CreateVersion7();

    private static void Serve(BunitContext ctx, out List<ActListRequest> requests, int total = 1)
    {
        var captured = new List<ActListRequest>();
        var response = new ActListResponse
        {
            Items =
            [
                new ActListItem
                {
                    ActId = ActId,
                    CaseId = CaseId,
                    CaseNumber = "EC/20260821-001",
                    CaseTitle = "Přestupek",
                    ActNumber = "EC/20260821-001/1",
                    Direction = ActDirection.Incoming,
                    Title = "Oznámení",
                    Date = new DateOnly(2026, 8, 21),
                    Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
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

    private static IRenderedComponent<ActList> Render(
        BunitContext ctx,
        IReadOnlyList<ActColumn> columns,
        int take = 20,
        bool paging = false,
        bool directionFilter = false,
        Guid? caseId = null)
    {
        return ctx.Render<ActList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new ActListRequest { CaseId = caseId, SortDirection = ListSortDirection.Ascending, Take = take })
            .Add(static list => list.ShowPaging, paging)
            .Add(static list => list.ShowDirectionFilter, directionFilter));
    }
}
