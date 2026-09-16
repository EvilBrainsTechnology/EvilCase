using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DashboardRenderTests
{
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

    private static void Serve(BunitContext ctx, out List<CaseListRequest> caseRequests, out List<ActListRequest> actRequests)
    {
        // The page's TabBlazor components reach for the browser on their first render.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var capturedCases = new List<CaseListRequest>();
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .CountCases(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseStatusCounts { Active = 1, WaitingOnAuthority = 0, Closed = 0 }));
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedCases.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 });
            });

        var capturedActs = new List<ActListRequest>();
        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedActs.Add(call.Arg<ActListRequest>());

                return Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 });
            });

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(actsClient);

        caseRequests = capturedCases;
        actRequests = capturedActs;
    }

    private static void Render(BunitContext ctx, List<CaseListRequest> caseRequests, List<ActListRequest> actRequests)
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
    }
}
