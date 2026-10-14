using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActsRenderTests
{
    [Test]
    public async Task ThePageListsTheActsAcrossCasesInTheAgreedColumns()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/acts");

        var component = ctx.Render<App.Pages.Acts>();

        await component.WaitForAssertionAsync(() =>
        {
            string[] headers = ["Datum úkonu", "Úkon a spis", "Směr", "Kontakt", "Číslo jednací"];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
                    Is.EqualTo(headers));
                Assert.That(requests[0].CaseId, Is.Null, "the page lists acts of every case");
                Assert.That(requests[0].Take, Is.EqualTo(20));
                Assert.That(requests[0].Sort, Is.EqualTo(ActSortKey.Date));
                Assert.That(requests[0].SortDirection, Is.EqualTo(ListSortDirection.Descending), "the newest act comes first");
                Assert.That(requests[0].CaseStatus, Is.EqualTo(CaseStatusFilter.Open), "the list starts on the open cases");
                Assert.That(component.Find("#acts-open-cases").HasAttribute("checked"), Is.True);
                Assert.That(component.Find("h1").TextContent, Does.Contain("Úkony"));
            }
        });
    }

    [Test]
    public async Task TheQueryParametersNarrowTheListAndSetTheToolbar()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/acts?q=výzva&direction=outgoing&open=false");

        var component = ctx.Render<App.Pages.Acts>();

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(requests[0].Search, Is.EqualTo("výzva"));
                Assert.That(requests[0].Direction, Is.EqualTo(ActDirection.Outgoing));
                Assert.That(requests[0].CaseStatus, Is.EqualTo(CaseStatusFilter.All));
                Assert.That(component.Find("#acts-direction-outgoing").GetAttribute("aria-pressed"), Is.EqualTo("true"));
                Assert.That(component.Find("#acts-open-cases").HasAttribute("checked"), Is.False, "open=false leaves the box unchecked");
                Assert.That(component.Find("input[type=\"search\"]").GetAttribute("value"), Is.EqualTo("výzva"));
            }
        });
    }

    [Test]
    public async Task UnknownQueryValuesFallBackToTheDefaults()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/acts?direction=sideways&open=maybe");

        _ = ctx.Render<App.Pages.Acts>();

        await Assert.ThatAsync(
            () => Task.FromResult(requests.Count),
            Is.GreaterThan(0).After(2000, 20));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[0].Direction, Is.Null);
            Assert.That(requests[0].CaseStatus, Is.EqualTo(CaseStatusFilter.Open));
        }
    }

    [Test]
    public async Task ANewDirectionInTheAddressResetsTheToolbar()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _);

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/acts?direction=incoming");

        var component = ctx.Render<App.Pages.Acts>();

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find("#acts-direction-incoming").GetAttribute("aria-pressed"), Is.EqualTo("true")));

        navigation.NavigateTo("/acts?direction=outgoing");

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find("#acts-direction-outgoing").GetAttribute("aria-pressed"), Is.EqualTo("true")));
    }

    [Test]
    public async Task ANewOpenValueInTheAddressResetsTheBox()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/acts");

        var component = ctx.Render<App.Pages.Acts>();

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find("#acts-open-cases").HasAttribute("checked"), Is.True));

        navigation.NavigateTo("/acts?open=false");

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#acts-open-cases").HasAttribute("checked"), Is.False);
                Assert.That(requests[^1].CaseStatus, Is.EqualTo(CaseStatusFilter.All));
            }
        });
    }

    private static void Serve(BunitContext ctx, out List<ActListRequest> requests)
    {
        var captured = new List<ActListRequest>();
        var response = new ActListResponse
        {
            Items =
            [
                new ActListItem
                {
                    ActId = Guid.CreateVersion7(),
                    CaseId = Guid.CreateVersion7(),
                    CaseNumber = "EC/20260821-001",
                    CaseTitle = "Přestupek",
                    ActNumber = "EC/20260821-001/20260821-002",
                    Direction = ActDirection.Incoming,
                    Title = "Oznámení",
                    Date = new DateOnly(2026, 8, 21),
                    Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            TotalCount = 1,
        };

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<ActListRequest>());

                return Task.FromResult(response);
            });

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(actsClient);

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));
        ctx.Services.AddSingleton(labelsClient);

        requests = captured;
    }
}
