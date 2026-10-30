using Bunit;
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
