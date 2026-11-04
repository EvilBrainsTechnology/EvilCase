using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
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

    [Test]
    public async Task PickingOutgoingPrefillsInProgress()
    {
        await using var ctx = new BunitContext();

        var component = RenderForCase(ctx);

        await component.Find("#act-direction-outgoing").ClickAsync(new MouseEventArgs());

        Assert.That(Pressed(component), Is.EqualTo(["act-status-in-progress"]), "an outgoing act with no status yet starts in progress");
    }

    [Test]
    public async Task PickingOutgoingKeepsAStatusAlreadyPicked()
    {
        await using var ctx = new BunitContext();

        var component = RenderForCase(ctx);

        await component.Find("#act-status-sent").ClickAsync(new MouseEventArgs());
        await component.Find("#act-direction-outgoing").ClickAsync(new MouseEventArgs());

        Assert.That(Pressed(component), Is.EqualTo(["act-status-sent"]), "the prefill never overwrites a status the user chose");
    }

    [Test]
    public async Task PickingIncomingLeavesTheStatusEmpty()
    {
        await using var ctx = new BunitContext();

        var component = RenderForCase(ctx);

        await component.Find("#act-direction-incoming").ClickAsync(new MouseEventArgs());

        Assert.That(Pressed(component), Is.Empty, "only an outgoing act gets a status prefilled");
    }

    private static IRenderedComponent<NewAct> RenderForCase(BunitContext ctx)
    {
        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId);

        var component = Render(ctx, caseId);

        component.WaitForAssertion(() => Assert.That(component.FindAll("#act-direction-outgoing"), Is.Not.Empty));

        return component;
    }

    private static List<string?> Pressed(IRenderedComponent<NewAct> component)
    {
        return [.. component.FindAll("[aria-label='Stav úkonu'] [aria-pressed=true]").Select(static segment => segment.Id)];
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
        navigation.NavigateTo(caseId is null ? "/acts/new" : $"/acts/new?case={caseId}");

        return ctx.Render<NewAct>();
    }
}
