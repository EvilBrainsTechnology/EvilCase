using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using Microsoft.AspNetCore.Components;
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
        await component.Find("#cases-root-only").ChangeAsync(new ChangeEventArgs { Value = false });

        await component.WaitForAssertionAsync(() => Assert.That(
            component.Find("input[aria-label=\"Hledat ve spisech\"]").GetAttribute("value"),
            Is.EqualTo("lhůta"),
            "only a changed search in the address reseeds the list's own search box"));
    }

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests)
    {
        var captured = new List<CaseListRequest>();
        var response = new CaseListResponse { Items = [], TotalCount = 0 };

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
