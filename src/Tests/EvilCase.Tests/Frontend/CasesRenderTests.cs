using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CasesRenderTests
{
    [Test]
    public async Task TheSearchFromTheAddressNarrowsTheList()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases?q=rychlost");

        var component = ctx.Render<App.Pages.Cases>();

        await component.WaitForAssertionAsync(
            () => Assert.That(requests[0].Search, Is.EqualTo("rychlost"), "the bar's search opens the case list narrowed by the text"));
    }

    [Test]
    public async Task ThePageListsTheCasesWithTheChangeMomentLast()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, withRow: true);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases");

        var component = ctx.Render<App.Pages.Cases>();

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
                    Is.EqualTo(["Datum spisu", "Spis", "Stav", "Změněno"]));
                Assert.That(requests[0].Sort, Is.EqualTo(CaseSortKey.Date));
                Assert.That(requests[0].SortDirection, Is.EqualTo(ListSortDirection.Descending), "the newest case comes first");
            }
        });
    }

    [Test]
    public async Task TheSearchFromTheAddressFillsTheToolbarBox()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases?q=rychlost");

        var component = ctx.Render<App.Pages.Cases>();

        await component.WaitForAssertionAsync(() => Assert.That(
            component.Find("input[type=\"search\"]").GetAttribute("value"),
            Is.EqualTo("rychlost"),
            "the list shows what it was narrowed by"));
    }

    [Test]
    public async Task NoSearchInTheAddressLeavesTheListUnnarrowed()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases");

        var component = ctx.Render<App.Pages.Cases>();

        await component.WaitForAssertionAsync(() => Assert.That(requests[0].Search, Is.Null));
    }

    [Test]
    public async Task AnotherFilterKeepsWhatTheListsOwnSearchBoxHolds()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases?q=rychlost");

        var component = ctx.Render<App.Pages.Cases>();
        var box = component.Find("input[aria-label=\"Hledat ve spisech\"]");

        await box.InputAsync(new ChangeEventArgs { Value = "lhůta" });
        await component.Find(".ec-check-box").ChangeAsync(new ChangeEventArgs { Value = false });

        await component.WaitForAssertionAsync(() => Assert.That(
            component.Find("input[aria-label=\"Hledat ve spisech\"]").GetAttribute("value"),
            Is.EqualTo("lhůta"),
            "only a changed search in the address reseeds the list's own search box"));
    }

    [Test]
    public async Task TheStatusAndScopeFromTheAddressNarrowTheListAndSetTheToolbar()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases?status=Closed&scope=All");

        var component = ctx.Render<App.Pages.Cases>();

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(requests[0].Status, Is.EqualTo(CaseStatusFilter.Closed));
                Assert.That(requests[0].Scope, Is.EqualTo(CaseListScope.All));
                Assert.That(component.Find("#cases-status-Closed").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find(".ec-check-box").HasAttribute("checked"), Is.False, "a list that includes the sub-cases leaves the scope box unchecked");
            }
        });
    }

    [Test]
    public async Task UnknownStatusAndScopeInTheAddressFallBackToTheDefaults()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases?status=Nonsense&scope=99");

        _ = ctx.Render<App.Pages.Cases>();

        await Assert.ThatAsync(
            () => Task.FromResult(requests.Count),
            Is.GreaterThan(0).After(2000, 20));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[0].Status, Is.EqualTo(CaseStatusFilter.Open));
            Assert.That(requests[0].Scope, Is.EqualTo(CaseListScope.RootOnly));
        }
    }

    [TestCase("/cases?q=lhuta&status=all&scope=all")]
    [TestCase("/cases?q=rychlost&status=Closed&scope=All")]
    public async Task ANavigationToTheListResetsEveryFilterToTheAddress(string target)
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(target);

        var component = ctx.Render<App.Pages.Cases>();

        await component.WaitForElementAsync("#cases-status-All");
        await component.Find("#cases-status-Active").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-check-box").ChangeAsync(new ChangeEventArgs { Value = true });
        await component.Find("button[aria-controls=\"cases-filters\"]").ClickAsync(new MouseEventArgs());
        await component.Find("#cases-from").ChangeAsync(new ChangeEventArgs { Value = "2026-01-01" });

        await component.WaitForAssertionAsync(() => Assert.That(requests[^1].From, Is.EqualTo(new DateOnly(2026, 1, 1))));

        navigation.NavigateTo(target);

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(requests[^1].From, Is.Null, "the date range is off after the navigation");
                Assert.That(requests[^1].Status, Is.EqualTo(target.Contains("status=all", StringComparison.Ordinal) ? CaseStatusFilter.All : CaseStatusFilter.Closed));
                Assert.That(requests[^1].Scope, Is.EqualTo(CaseListScope.All));
                Assert.That(component.Find("#cases-status-Active").GetAttribute("aria-pressed"), Is.EqualTo("false"));
                Assert.That(component.Find(".ec-check-box").HasAttribute("checked"), Is.False);
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

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests, bool withRow = false)
    {
        var captured = new List<CaseListRequest>();
        var response = withRow ? new CaseListResponse { Items = [Row], TotalCount = 1 } : new CaseListResponse { Items = [], TotalCount = 0 };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(response);
            });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(casesClient);

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));
        ctx.Services.AddSingleton(labelsClient);

        requests = captured;
    }
}
