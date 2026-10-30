using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class NewCaseRenderTests
{
    [Test]
    public void TheFormForARootCaseRenders()
    {
        using var ctx = new BunitContext();

        Serve(ctx, Guid.CreateVersion7());

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/cases/new");

        var component = ctx.Render<NewCase>();

        Assert.That(component.FindComponents<InputText>(), Is.Not.Empty);
    }

    [Test]
    public void TheFormForAChildCaseRenders()
    {
        using var ctx = new BunitContext();

        var parentId = Guid.CreateVersion7();

        Serve(ctx, parentId);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo($"/cases/new?parent={parentId}");

        var component = ctx.Render<NewCase>();

        Assert.That(component.FindComponents<InputText>(), Is.Not.Empty);
    }

    private static void Serve(BunitContext ctx, Guid parentId)
    {
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .GetCase(parentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseDetail
            {
                CaseId = parentId,
                CaseNumber = "EC/20260821-001",
                Date = new DateOnly(2026, 8, 21),
                Title = "Přestupek",
                Status = CaseStatus.Active,
            }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());
    }
}
