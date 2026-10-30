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

namespace EvilBrains.EvilCase.Tests.Frontend;

public class LabelFieldRenderTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(2);

    private static readonly LabelItem Deadline = new() { LabelId = Guid.CreateVersion7(), Name = "Hlídat lhůtu", Color = LabelColor.Orange };

    private static readonly LabelItem Appeal = new() { LabelId = Guid.CreateVersion7(), Name = "Odvolání", Color = LabelColor.Blue };

    private static readonly LabelItem Court = new() { LabelId = Guid.CreateVersion7(), Name = "Soud", Color = LabelColor.Purple };

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
