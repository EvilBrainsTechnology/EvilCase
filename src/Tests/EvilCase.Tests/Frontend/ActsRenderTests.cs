using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components.Lists;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActsRenderTests
{
    [Test]
    public void TheListRenders()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/acts?q=odpor&direction=Incoming&open=false");

        var component = ctx.Render<App.Pages.Acts>();

        Assert.That(component.FindComponents<ActList>(), Has.Count.EqualTo(1));
    }

    [TestCase("inprogress", ActStatusFilter.InProgress)]
    [TestCase("none", ActStatusFilter.None)]
    [TestCase("unknown", ActStatusFilter.All)]
    public async Task TheStatusInTheAddressNarrowsTheList(string query, ActStatusFilter expected)
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var actsClient = Serve(ctx);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/acts?status={query}");

        ctx.Render<App.Pages.Acts>();

        await actsClient.Received().ListActs(Arg.Is<ActListRequest>(request => request.Status == expected), Arg.Any<CancellationToken>());
    }

    private static IActsClient Serve(BunitContext ctx)
    {
        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(labelsClient);

        return actsClient;
    }
}
