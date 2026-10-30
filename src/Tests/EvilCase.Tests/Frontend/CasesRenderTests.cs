using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components.Lists;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CasesRenderTests
{
    [Test]
    public void TheListRenders()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(labelsClient);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases?q=rychlost&status=Closed&scope=All");

        var component = ctx.Render<App.Pages.Cases>();

        Assert.That(component.FindComponents<CaseList>(), Has.Count.EqualTo(1));
    }
}
