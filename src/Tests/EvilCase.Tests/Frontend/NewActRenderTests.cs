using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class NewActRenderTests
{
    [Test]
    public void TheFormForAnActOfACaseRenders()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId);

        var component = Render(ctx, caseId);

        component.WaitForAssertion(() => Assert.That(component.FindComponents<InputText>(), Is.Not.Empty));
    }

    [Test]
    public void TheFormForAnActWithoutACaseRenders()
    {
        using var ctx = new BunitContext();

        Serve(ctx, Guid.CreateVersion7());

        var component = Render(ctx, caseId: null);

        Assert.That(component.FindComponents<InputText>(), Is.Not.Empty);
    }

    private static void Serve(BunitContext ctx, Guid caseId)
    {
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .GetCase(caseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseDetail
            {
                CaseId = caseId,
                CaseNumber = "EC/20260807-001",
                Date = new DateOnly(2026, 8, 7),
                Title = "Překročení rychlosti",
                Status = CaseStatus.Active,
            }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(Substitute.For<IActsClient>());
        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());
        ctx.Services.AddSingleton(labelsClient);
    }

    private static IRenderedComponent<NewAct> Render(BunitContext ctx, Guid? caseId)
    {
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(ActRoutes.New(caseId));

        return ctx.Render<NewAct>();
    }
}
