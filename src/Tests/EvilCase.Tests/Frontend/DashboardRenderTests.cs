using AngleSharp.Dom;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DashboardRenderTests
{
    private static readonly string[] ExpectedCounts = ["6", "1", "9"];

    private static readonly string[] ExpectedLabels = ["Aktivní", "Čeká na úřad", "Uzavřené"];

    private static readonly string[] CaseTileHeaders = ["Změněno ↓", "Datum spisu", "Spis", "Stav", "Štítky", "Spisová značka"];

    private static readonly string[] ActTileHeaders = ["Změněno ↓", "Datum", "Spis", "Úkon", "Směr", "Číslo jednací"];

    [Test]
    public void BothListTilesReadFromTheLastChange()
    {
        using var ctx = new BunitContext();

        Serve(ctx, out var caseRequests, out var actRequests);
        Render(ctx, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseRequests[0].Sort, Is.EqualTo(CaseSortKey.Changed));
            Assert.That(caseRequests[0].SortDirection, Is.EqualTo(ListSortDirection.Descending), "the case tile shows the cases changed last (SDD-015)");
            Assert.That(actRequests[0].Sort, Is.EqualTo(ActSortKey.Changed));
            Assert.That(actRequests[0].SortDirection, Is.EqualTo(ListSortDirection.Descending), "the act tile shows the acts changed last (SDD-015)");
        }
    }

    [Test]
    public void TheCaseTileShowsACaseOfEveryStatus()
    {
        using var ctx = new BunitContext();

        Serve(ctx, out var caseRequests, out var actRequests);
        Render(ctx, caseRequests, actRequests);

        Assert.That(caseRequests[0].Status, Is.EqualTo(CaseStatusFilter.All), "the case tile shows the cases changed last whatever their status (SDD-015)");
    }

    [Test]
    public void EachListTileAsksForFiveRows()
    {
        using var ctx = new BunitContext();

        Serve(ctx, out var caseRequests, out var actRequests);
        Render(ctx, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseRequests[0].Take, Is.EqualTo(5), "a list tile holds at most five rows (SDD-015)");
            Assert.That(actRequests[0].Take, Is.EqualTo(5), "a list tile holds at most five rows (SDD-015)");
        }
    }

    [Test]
    public void EachTileShowsTheCountOfItsStatus()
    {
        using var ctx = new BunitContext();

        var counts = new CaseStatusCounts { Active = 6, WaitingOnAuthority = 1, Closed = 9 };
        Serve(ctx, out var caseRequests, out var actRequests, countCases: () => Task.FromResult(counts));
        var component = Render(ctx, caseRequests, actRequests);

        var values = component.FindAll(".ec-stat-value");
        var labels = component.FindAll(".ec-stat-label");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(values.Select(static value => value.TextContent), Is.EqualTo(ExpectedCounts));
            Assert.That(labels.Select(static label => label.TextContent), Is.EqualTo(ExpectedLabels));
        }
    }

    [Test]
    public void TheTilesHoldTheirHeightWhileTheCountsLoad()
    {
        using var ctx = new BunitContext();

        var neverCompletes = new TaskCompletionSource<CaseStatusCounts>();
        Serve(ctx, out var caseRequests, out var actRequests, countCases: async () => await neverCompletes.Task);

        var component = ctx.Render<Home>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-stat"), Has.Count.EqualTo(3));
            Assert.That(
                component.FindAll(".ec-stat-value").Select(static value => value.TextContent),
                Is.All.EqualTo("—"),
                "loading holds the height so the tiles do not jump (SDD-020)");
            Assert.That(component.Find(".ec-tiles").GetAttribute("aria-busy"), Is.EqualTo("true"));
        }
    }

    [Test]
    public async Task AFailedCountLeavesBothListTilesAndOffersARetry()
    {
        await using var ctx = new BunitContext();

        var counts = new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 };
        var callCount = 0;

        Serve(
            ctx,
            out var caseRequests,
            out var actRequests,
            countCases: () =>
            {
                callCount++;

                if (callCount == 1)
                    throw new HttpRequestException();

                return Task.FromResult(counts);
            });

        var component = Render(ctx, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-card-error"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll(".ec-stat"), Is.Empty);
            Assert.That(component.FindAll(".ec-table"), Has.Count.EqualTo(2));
        }

        await component.Find(".ec-card-error button").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-stat"), Has.Count.EqualTo(3));
    }

    [Test]
    public void ATenantWithNoCaseLeadsToTheFirstCase()
    {
        using var ctx = new BunitContext();

        var counts = new CaseStatusCounts { Active = 0, WaitingOnAuthority = 0, Closed = 0 };
        Serve(ctx, out _, out _, countCases: () => Task.FromResult(counts));

        var component = ctx.Render<Home>();

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-empty"), Is.Not.Empty));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find(".ec-empty").QuerySelector("a[href='/cases/new']"),
                Is.Not.Null,
                "a tenant with no case leads to creating the first one (SDD-015)");
            Assert.That(component.FindAll(".ec-stat"), Is.Empty);
            Assert.That(component.FindAll(".ec-table"), Is.Empty);
        }
    }

    [Test]
    public void NeitherListTileShowsATotalCountOrAPager()
    {
        using var ctx = new BunitContext();

        Serve(
            ctx,
            out var caseRequests,
            out var actRequests,
            caseItems: FiveCases(),
            caseTotal: 40,
            actItems: FiveActs(),
            actTotal: 40);
        var component = Render(ctx, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-card-count"),
                Is.Empty,
                "a tile that lists a few rows shows no total count (SDD-015)");
            Assert.That(component.FindAll(".ec-card-footer"), Is.Empty);
        }
    }

    [Test]
    public void EachListTileShowsTheColumnsOfTheDesign()
    {
        using var ctx = new BunitContext();

        Serve(
            ctx,
            out var caseRequests,
            out var actRequests,
            caseItems: FiveCases(),
            caseTotal: 5,
            actItems: FiveActs(),
            actTotal: 5);
        var component = Render(ctx, caseRequests, actRequests);

        var tables = component.FindAll(".ec-table");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Headers(tables[0]), Is.EqualTo(CaseTileHeaders), "the case tile carries the columns of docs/design/prehled.html");
            Assert.That(Headers(tables[1]), Is.EqualTo(ActTileHeaders), "the act tile carries no labels column (docs/design/prehled.html)");
        }
    }

    [Test]
    public void OnlyTheCaseTileCarriesAHeaderLink()
    {
        using var ctx = new BunitContext();

        Serve(ctx, out var caseRequests, out var actRequests);
        var component = Render(ctx, caseRequests, actRequests);

        var actions = component.FindAll(".ec-card-header-actions");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].QuerySelector("a[href='/cases']"), Is.Not.Null);
        }
    }

    [Test]
    public void TheDashboardCarriesNoTablerClass()
    {
        using var ctx = new BunitContext();

        Serve(ctx, out var caseRequests, out var actRequests);
        var component = Render(ctx, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => component.Find(".ec-page"), Throws.Nothing);
            Assert.That(
                component.FindAll(".card, .btn, .empty, .row, .page-header, .page-body"),
                Is.Empty,
                "a migrated screen carries no class of a foreign library (SDD-020)");
        }
    }

    private static IEnumerable<string> Headers(IElement table)
    {
        return table
            .QuerySelectorAll(".ec-table-header")
            .Select(static header => header.TextContent.Trim());
    }

    private static List<CaseListItem> FiveCases()
    {
        var items = new List<CaseListItem>();

        for (var index = 0; index < 5; index++)
        {
            items.Add(new CaseListItem
            {
                CaseId = Guid.CreateVersion7(),
                CaseNumber = $"EC/20260821-{index.ToString("000", CultureInfo.InvariantCulture)}",
                Title = "Přestupek",
                Date = new DateOnly(2026, 8, 21),
                Status = CaseStatus.Active,
                Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
            });
        }

        return items;
    }

    private static List<ActListItem> FiveActs()
    {
        var items = new List<ActListItem>();

        for (var index = 0; index < 5; index++)
        {
            items.Add(new ActListItem
            {
                ActId = Guid.CreateVersion7(),
                CaseId = Guid.CreateVersion7(),
                CaseNumber = $"EC/20260821-{index.ToString("000", CultureInfo.InvariantCulture)}",
                CaseTitle = "Přestupek",
                ActNumber = $"U/20260821-{index.ToString("000", CultureInfo.InvariantCulture)}",
                Title = "Výzva",
                Date = new DateOnly(2026, 8, 21),
                Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
            });
        }

        return items;
    }

    private static void Serve(
        BunitContext ctx,
        out List<CaseListRequest> caseRequests,
        out List<ActListRequest> actRequests,
        Func<Task<CaseStatusCounts>>? countCases = null,
        IReadOnlyList<CaseListItem>? caseItems = null,
        int caseTotal = 0,
        IReadOnlyList<ActListItem>? actItems = null,
        int actTotal = 0)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var capturedCases = new List<CaseListRequest>();
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .CountCases(Arg.Any<CancellationToken>())
            .Returns(async _ => await (countCases ?? DefaultCounts)());
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedCases.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(new CaseListResponse { Items = caseItems ?? [], TotalCount = caseTotal });
            });

        var capturedActs = new List<ActListRequest>();
        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedActs.Add(call.Arg<ActListRequest>());

                return Task.FromResult(new ActListResponse { Items = actItems ?? [], TotalCount = actTotal });
            });

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(actsClient);

        caseRequests = capturedCases;
        actRequests = capturedActs;
    }

    private static Task<CaseStatusCounts> DefaultCounts()
    {
        return Task.FromResult(new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 });
    }

    private static IRenderedComponent<Home> Render(BunitContext ctx, List<CaseListRequest> caseRequests, List<ActListRequest> actRequests)
    {
        var component = ctx.Render<Home>();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(caseRequests, Is.Not.Empty, "the dashboard loads its case tile");
                Assert.That(actRequests, Is.Not.Empty, "the dashboard loads its act tile");
            }
        });

        return component;
    }
}
