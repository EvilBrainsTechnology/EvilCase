using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.App.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DashboardRenderTests
{
    [Test]
    public void TheDashboardRenders()
    {
        using var ctx = new BunitContext();

        Serve(ctx, new CaseStatusCounts { Active = 6, WaitingOnAuthority = 1, Closed = 9 });

        var component = ctx.Render<Home>();

        component.WaitForAssertion(() => Assert.That(component.FindComponents<CaseList>(), Has.Count.EqualTo(1)));
    }

    [Test]
    public void TheDashboardOfATenantWithNoCaseRenders()
    {
        using var ctx = new BunitContext();

        Serve(ctx, new CaseStatusCounts { Active = 0, WaitingOnAuthority = 0, Closed = 0 });

        var component = ctx.Render<Home>();

        component.WaitForAssertion(() => Assert.That(component.FindComponents<EcEmptyState>(), Has.Count.EqualTo(1)));
    }

    private static void Serve(BunitContext ctx, CaseStatusCounts counts)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .CountCases(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(counts));
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(actsClient);
    }
}
