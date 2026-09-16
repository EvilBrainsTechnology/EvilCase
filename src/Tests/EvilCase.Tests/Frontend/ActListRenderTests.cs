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

        component.WaitForElement("tbody tr");

        string[] headers = ["Úkon", "Směr", "Datum"];

        Assert.That(
            component.FindAll("thead th").Select(static header => header.TextContent.Trim()),
            Is.EqualTo(headers),
            "the host's order of the columns is the order of the headers");
    }

    [Test]
    public void AColumnRendersItsHeaderItsTableCellAndItsCardLineFromOneDefinition()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.ActNumber]);

        component.WaitForElement("tbody tr");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("thead th").TextContent.Trim(), Is.EqualTo("Číslo jednací"));
            Assert.That(component.Find("tbody td").TextContent, Does.Contain("EC/20260821-001/1"));
            Assert.That(
                component.Find(".card-body.d-lg-none").TextContent,
                Does.Contain("EC/20260821-001/1"),
                "the column that writes the cell writes the card line too");
        }
    }

    [Test]
    public void EveryRowRendersBothTheTableAndTheCardSoOnlyCssChoosesBetweenThem()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Date, ActColumn.Act]);

        component.WaitForElement("tbody tr");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".d-none.d-lg-block tbody tr"), Has.Count.EqualTo(1));
            Assert.That(
                component.FindAll(".card-body.d-lg-none > .card"),
                Has.Count.EqualTo(1),
                "the same row renders as a card, and only CSS picks which of the two shows");
        }
    }

    [Test]
    public void ASortableHeaderIsAButtonAndAnUnsortableOneIsPlainText()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ActColumn.Date, ActColumn.Contact]);

        component.WaitForElement("tbody tr");

        var headers = component.FindAll("thead th");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers[0].QuerySelector("button.table-sort"), Is.Not.Null, "a column with a sort key sorts");
            Assert.That(headers[1].QuerySelector("button"), Is.Null, "a column without a sort key carries no button");
        }
    }

    [Test]
    public async Task ClickingTheSortedHeaderFlipsTheDirectionAndAsksForTheFirstPageAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [ActColumn.Date], take: 2, paging: true);

        await component.WaitForElementAsync("tbody tr");
        await component.Find(".ec-listfoot button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("thead th button").ClickAsync(new MouseEventArgs());

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

        await component.WaitForElementAsync("tbody tr");
        await component.Find("thead th button").ClickAsync(new MouseEventArgs());

        Assert.That(
            requests.Select(static request => request.CaseId),
            Is.All.EqualTo(caseId),
            "the toolbar narrows what the host's filter left, never widens it");
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
                Assert.That(component.FindAll("table"), Is.Empty, "a failed load leaves no table behind");
                Assert.That(component.Markup, Does.Contain("Úkony se nepodařilo načíst"));
            }
        });
    }

    private static void Serve(BunitContext ctx, out List<ActListRequest> requests, int total = 1)
    {
        var captured = new List<ActListRequest>();
        var caseId = Guid.CreateVersion7();
        var response = new ActListResponse
        {
            Items =
            [
                new ActListItem
                {
                    ActId = Guid.CreateVersion7(),
                    CaseId = caseId,
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
        Guid? caseId = null)
    {
        return ctx.Render<ActList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new ActListRequest { CaseId = caseId, SortDirection = ListSortDirection.Ascending, Take = take })
            .Add(static list => list.ShowPaging, paging));
    }
}
