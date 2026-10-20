using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelFieldRenderTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(2);

    private static readonly LabelItem Deadline = new() { LabelId = Guid.CreateVersion7(), Name = "Hlídat lhůtu", Color = LabelColor.Orange };

    private static readonly LabelItem Appeal = new() { LabelId = Guid.CreateVersion7(), Name = "Odvolání", Color = LabelColor.Blue };

    private static readonly LabelItem Court = new() { LabelId = Guid.CreateVersion7(), Name = "Soud", Color = LabelColor.Purple };

    [Test]
    public async Task TheOfferHoldsOnlyTheLabelsNotYetSelectedWithoutCheckboxesOrCounts()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _, selected: [Deadline]);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.FindAll(".ec-label-option").Select(static option => option.TextContent.Trim()), Is.EqualTo(["Odvolání", "Soud"]));
                    Assert.That(component.FindAll("input[type=checkbox]"), Is.Empty);
                    Assert.That(component.Find(".ec-label-menu-foot").TextContent, Does.Contain("↑↓ pohyb · Enter přidat · Esc zavřít"));
                    Assert.That(component.Find(".ec-label-manage").GetAttribute("href"), Is.EqualTo("/settings"));
                }
            },
            WaitTimeout);
    }

    [Test]
    public async Task TheOfferFollowsCzechAlphabeticalOrderIgnoringCase()
    {
        await using var ctx = new BunitContext();

        var names = new[] { "Zuzana", "Řehoř", "zeman", "Čížek", "Adam", "čeněk" };
        var labels = names.Select(static name => new LabelItem { LabelId = Guid.CreateVersion7(), Name = name, Color = LabelColor.Blue }).ToList();

        var component = Render(ctx, out _, available: labels);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option").Select(static option => option.TextContent.Trim()), Is.EqualTo(["Adam", "čeněk", "Čížek", "Řehoř", "zeman", "Zuzana"])),
            WaitTimeout);
    }

    [Test]
    public async Task TypingNarrowsTheOfferAndANonMatchSaysSo()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _, create: false);

        await component.Find("#labels").InputAsync(new ChangeEventArgs { Value = "odv" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1)),
            WaitTimeout);

        await component.Find("#labels").InputAsync(new ChangeEventArgs { Value = "zzz" });

        Assert.That(component.Find(".ec-label-empty").TextContent, Is.EqualTo("Žádný další štítek neodpovídá."));
    }

    [Test]
    public async Task AnEntityCarryingEveryLabelSaysThereIsNothingToAdd()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _, selected: [Deadline, Appeal, Court]);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-label-empty").TextContent, Is.EqualTo("Všechny štítky už jsou přidané.")),
            WaitTimeout);
    }

    [Test]
    public async Task ClickingALabelAddsItAndTheBoxStaysOpenForTheNext()
    {
        await using var ctx = new BunitContext();

        IReadOnlyList<LabelItem>? raised = null;

        var component = Render(ctx, out _, selected: [], onChanged: labels => raised = labels);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(3)),
            WaitTimeout);

        await component.FindAll(".ec-label-option")[1].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(raised!.Select(static label => label.Name), Is.EqualTo(["Odvolání"]));
            Assert.That(component.FindAll("#labels"), Has.Count.EqualTo(1), "the box stays open");
        }
    }

    [Test]
    public async Task TheArrowsMoveAndEnterAddsTheHighlightedLabel()
    {
        await using var ctx = new BunitContext();

        IReadOnlyList<LabelItem>? raised = null;

        var component = Render(ctx, out _, selected: [], onChanged: labels => raised = labels);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(3)),
            WaitTimeout);

        await component.Find("#labels").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.That(component.Find(".ec-label-option-active").TextContent, Does.Contain("Odvolání"));

        await component.Find("#labels").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.That(raised!.Select(static label => label.Name), Is.EqualTo(["Odvolání"]));
    }

    [Test]
    public async Task EscapeClosesTheOffer()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-menu"), Has.Count.EqualTo(1)),
            WaitTimeout);

        await component.Find("#labels").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(component.FindAll(".ec-label-menu"), Is.Empty);
    }

    [Test]
    public async Task InlineShowsTheAddButtonUntilItIsClickedOrTheShortcutFires()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _, inline: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-label-add").TextContent, Does.Contain("Štítek"));
            Assert.That(component.FindAll("#labels"), Is.Empty);
        }

        await component.InvokeAsync(component.Instance.OpenFromShortcut);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-label-add"), Is.Empty);
            Assert.That(component.FindAll("#labels"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task EscapeInlineReturnsTheFocusToTheAddButton()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _, inline: true);

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());
        await component.Find("#labels").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        var buttonId = component.Find(".ec-label-add").GetAttribute("blazor:elementReference");

        var focused = ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 3);

        Assert.That(((ElementReference)focused[2].Arguments[0]!).Id, Is.EqualTo(buttonId));
    }

    [Test]
    public async Task PickingALabelInlineClosesTheBoxAndReturnsTheFocusToTheAddButton()
    {
        await using var ctx = new BunitContext();

        IReadOnlyList<LabelItem>? raised = null;

        var component = Render(ctx, out _, selected: [], onChanged: labels => raised = labels, inline: true);

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(3)),
            WaitTimeout);

        await component.FindAll(".ec-label-option")[1].ClickAsync(new MouseEventArgs());

        var buttonId = component.Find(".ec-label-add").GetAttribute("blazor:elementReference");

        var focused = ctx.JSInterop.VerifyFocusAsyncInvoke(calledTimes: 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(raised!.Select(static label => label.Name), Is.EqualTo(["Odvolání"]));
            Assert.That(component.FindAll(".ec-label-menu"), Is.Empty);
            Assert.That(((ElementReference)focused[2].Arguments[0]!).Id, Is.EqualTo(buttonId));
        }
    }

    [Test]
    public async Task AFormFieldLeavesThePageShortcutListening()
    {
        await using var ctx = new BunitContext();

        var module = ctx.JSInterop.SetupModule("./js/ec-label-field.js");
        module.Mode = JSRuntimeMode.Loose;

        var component = Render(ctx, out _);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(3)),
            WaitTimeout);

        await component.Find("#labels").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        await ctx.DisposeComponentsAsync();

        using (Assert.EnterMultipleScope())
        {
            module.VerifyInvoke("scrollActiveIntoView");
            module.VerifyNotInvoke("stop", "the L listener belongs to the inline field on the same page");
        }
    }

    [Test]
    public async Task AFilterFieldWithNoLabelsDoesNotSuggestFoundingOne()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out var labelsClient, create: false);

        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-label-empty").TextContent, Is.EqualTo("Zatím není žádný štítek.")),
            WaitTimeout);
    }

    [Test]
    public async Task TextMatchingNoLabelOffersFoundingOneWithThePaletteAndAssignsItAtOnce()
    {
        await using var ctx = new BunitContext();

        var created = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Pojištění", Color = LabelColor.Green };

        IReadOnlyList<LabelItem>? raised = null;

        var component = Render(ctx, out var labelsClient, selected: [Deadline], onChanged: labels => raised = labels);

        labelsClient.CreateLabel(Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(created));

        await component.Find("#labels").InputAsync(new ChangeEventArgs { Value = "Pojištění" });

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(".ec-label-create-button").TextContent, Does.Contain("Založit štítek „Pojištění“"));
                    Assert.That(component.FindAll(".ec-label-swatches .ec-swatch"), Has.Count.EqualTo(13));
                }
            },
            WaitTimeout);

        await component.FindAll(".ec-label-swatches .ec-swatch")[9].ClickAsync(new MouseEventArgs());
        await component.Find(".ec-label-create-button").ClickAsync(new MouseEventArgs());

        await labelsClient
            .Received(1)
            .CreateLabel(
                Arg.Is<LabelEditRequest>(static request => request.Name == "Pojištění" && request.Color == LabelColor.Green),
                Arg.Any<CancellationToken>());

        Assert.That(raised!.Select(static label => label.Name), Is.EqualTo(["Hlídat lhůtu", "Pojištění"]));
    }

    [Test]
    public async Task AnExactNameMatchOffersNoFoundingAndAFilterFieldNeverDoes()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _);

        await component.Find("#labels").InputAsync(new ChangeEventArgs { Value = "soud" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1)),
            WaitTimeout);

        Assert.That(component.FindAll(".ec-label-create"), Is.Empty, "a name that exists is added, not founded");

        await component.Find("#labels").InputAsync(new ChangeEventArgs { Value = "Nový" });

        Assert.That(component.FindAll(".ec-label-create"), Has.Count.EqualTo(1));

        component.Render(static parameters => parameters.Add(static field => field.AllowCreate, value: false));

        Assert.That(component.FindAll(".ec-label-create"), Is.Empty, "a filter has nothing to found a label for");
    }

    [Test]
    public async Task ATakenNameIsReportedAndNothingIsAssigned()
    {
        await using var ctx = new BunitContext();

        var raised = false;

        var component = Render(ctx, out var labelsClient, onChanged: _ => raised = true);

        labelsClient
            .CreateLabel(Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<LabelItem>(new ApiException(HttpStatusCode.Conflict, responseBody: null)));

        await component.Find("#labels").InputAsync(new ChangeEventArgs { Value = "Nový" });
        await component.Find(".ec-label-create-button").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-label-menu .ec-field-error").TextContent, Does.Contain("už existuje"));
            Assert.That(raised, Is.False);
        }
    }

    [Test]
    public async Task TheCrossOnAChipRaisesTheSelectionWithoutTheLabel()
    {
        await using var ctx = new BunitContext();

        IReadOnlyList<LabelItem>? raised = null;

        var component = Render(ctx, out _, selected: [Deadline, Appeal], onChanged: labels => raised = labels);

        await component.Find("button[aria-label='Odebrat štítek Hlídat lhůtu']").ClickAsync(new MouseEventArgs());

        Assert.That(raised!.Select(static label => label.Name), Is.EqualTo(["Odvolání"]));
    }

    [Test]
    public async Task TheSheetCarriesTheTitleASearchAndTheDoneButton()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, out _);

        await component.Find("#labels").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(component.Find(".ec-label-sheet-title").TextContent, Is.EqualTo("Přidat štítek"));
                    Assert.That(component.Find(".ec-label-sheet-done").TextContent, Is.EqualTo("Hotovo"));
                    Assert.That(component.FindAll(".ec-label-sheet-search"), Has.Count.EqualTo(1));
                }
            },
            WaitTimeout);

        await component.Find(".ec-label-sheet-done").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-label-menu"), Is.Empty);
    }

    private static IRenderedComponent<LabelField> Render(
        BunitContext ctx,
        out ILabelsClient labelsClient,
        IReadOnlyList<LabelItem>? selected = null,
        Action<IReadOnlyList<LabelItem>>? onChanged = null,
        bool inline = false,
        bool create = true,
        IReadOnlyList<LabelItem>? available = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [.. (available ?? [Deadline, Appeal, Court]).Select(static label => LabelListItems.Of(label))] }));

        ctx.Services.AddSingleton(labelsClient);

        var captured = onChanged ?? (static _ => { });

        return ctx.Render<LabelField>(parameters => parameters
            .Add(static field => field.InputId, "labels")
            .Add(static field => field.Selected, selected ?? [])
            .Add(static field => field.Inline, inline)
            .Add(static field => field.AllowCreate, create)
            .Add(static field => field.SelectedChanged, EventCallback.Factory.Create(ctx, captured)));
    }
}
