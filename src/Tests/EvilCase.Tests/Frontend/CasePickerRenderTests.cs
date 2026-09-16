using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CasePickerRenderTests
{
    [Test]
    public async Task ThePickerAsksTheServerForTheMatchesInsteadOfLoadingEveryCase()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests);

        var component = Render(ctx, Guid.CreateVersion7());

        Assert.That(requests, Is.Empty, "an untouched picker loads no list at all");

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "přestupek" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests.Single().Search, Is.EqualTo("přestupek"), "the server does the searching");
            Assert.That(requests.Single().Take, Is.LessThanOrEqualTo(10), "the picker takes a handful of matches, never the whole tenant");
            Assert.That(component.Markup, Does.Contain("EC/20260821-001"));
        }
    }

    [Test]
    public async Task ThePickerNeverOffersTheCaseItselfAsItsOwnParent()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _, out var offered);

        var component = Render(ctx, offered[0].CaseId);

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "spis" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Markup, Does.Not.Contain(offered[0].CaseNumber), "a case is never its own parent");
            Assert.That(component.Markup, Does.Contain(offered[1].CaseNumber));
        }
    }

    [Test]
    public async Task ChoosingAMatchAnswersWithItAndTheSelectionIsGivenBack()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _, out var offered);

        CaseListItem? chosen = null;
        var component = Render(ctx, Guid.CreateVersion7(), item => chosen = item);

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "spis" });
        await component.Find(".ec-picker-menu button").ClickAsync(new MouseEventArgs());

        Assert.That(chosen, Is.SameAs(offered[0]));
    }

    [Test]
    public async Task ThePickerTakesTheParentAwayAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out _, out var offered);

        CaseListItem? chosen = offered[0];
        var component = Render(ctx, Guid.CreateVersion7(), item => chosen = item, selected: offered[0]);

        await component.Find("#case-parent-clear").ClickAsync(new MouseEventArgs());

        Assert.That(chosen, Is.Null, "a case that hangs under another one can be taken out of it");
    }

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests)
    {
        Serve(ctx, out requests, out _);
    }

    private static void Serve(BunitContext ctx, out List<CaseListRequest> requests, out IReadOnlyList<CaseListItem> offered)
    {
        var captured = new List<CaseListRequest>();
        IReadOnlyList<CaseListItem> items =
        [
            Item("EC/20260821-001", "Přestupek"),
            Item("EC/20260821-002", "Odvolání"),
        ];

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(new CaseListResponse { Items = items, TotalCount = items.Count });
            });

        ctx.Services.AddSingleton(casesClient);

        requests = captured;
        offered = items;
    }

    private static CaseListItem Item(string caseNumber, string title)
    {
        return new CaseListItem
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = caseNumber,
            Title = title,
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.Active,
            Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static IRenderedComponent<CasePicker> Render(
        BunitContext ctx,
        Guid excludedCaseId,
        Action<CaseListItem?>? selectedChanged = null,
        CaseListItem? selected = null)
    {
        return ctx.Render<CasePicker>(parameters =>
        {
            parameters
                .Add(static picker => picker.InputId, "case-parent")
                .Add(static picker => picker.ExcludedCaseId, excludedCaseId)
                .Add(static picker => picker.Selected, selected);

            if (selectedChanged is not null)
                parameters.Add(static picker => picker.SelectedChanged, selectedChanged);
        });
    }
}
