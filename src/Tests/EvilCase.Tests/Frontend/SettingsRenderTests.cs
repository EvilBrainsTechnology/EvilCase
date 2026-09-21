using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SettingsRenderTests
{
    private static readonly LabelItem InfZLabel = Label("InfZ", LabelColor.Blue);

    private static readonly LabelItem RychlostLabel = Label("rychlost", LabelColor.Red);

    [Test]
    public void TheListShowsEveryLabelWithItsPaletteColour()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx, [InfZLabel, RychlostLabel]);

        var component = Render(ctx);

        component.WaitForElement(".ec-settings-row");

        var rows = component.FindAll(".ec-settings-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(
                rows[0].QuerySelector(".ec-chip")!.GetAttribute("class"),
                Does.Contain("ec-chip-blue"),
                "a label's colour comes from its palette token (SDD-019)");
            Assert.That(rows[0].QuerySelector(".ec-chip")!.TextContent, Does.Contain("InfZ"));
            Assert.That(
                component.FindAll(".ec-settings-row-color").Select(static cell => cell.TextContent),
                Is.EqualTo(["Modrá", "Červená"]));
        }
    }

    [Test]
    public void TheSectionRailNamesTheLabelSection()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx, [InfZLabel]);

        var component = Render(ctx);

        component.WaitForElement(".ec-settings-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-settings-nav-item[aria-current=\"page\"]").TextContent.Trim(), Is.EqualTo("Štítky"));
            Assert.That(component.Find(".ec-page-title").TextContent, Is.EqualTo("Nastavení"));
        }
    }

    [Test]
    public void AnEmptyListOffersFoundingTheFirstLabel()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx, []);

        var component = Render(ctx);

        component.WaitForElement(".ec-empty-text");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find(".ec-empty-text").TextContent,
                Is.EqualTo("Zatím tu není žádný štítek."),
                "an empty list that can be filled carries the call to action (SDD-020)");
            Assert.That(component.FindAll(".ec-empty button"), Is.Not.Empty);
        }
    }

    [Test]
    public async Task AFailedLoadOffersARetryThatLoadsAgain()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(
                static _ => Task.FromException<LabelListResponse>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)),
                static _ => Task.FromResult(new LabelListResponse { Items = [InfZLabel] }));

        ctx.Services.AddSingleton(labelsClient);

        var component = Render(ctx);

        await component.WaitForAssertionAsync(
            () => Assert.That(
                component.Find(".ec-alert").TextContent,
                Does.Contain("nepodařilo načíst"),
                "a failed list offers repeating it (SDD-020)"));

        await component.Find(".ec-card-error .ec-button").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-settings-row"), Has.Count.EqualTo(1)));
    }

    [Test]
    public async Task TheNewLabelButtonOpensTheFormModal()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx, [InfZLabel]);

        var component = Render(ctx);

        await component.WaitForElementAsync(".ec-settings-row");
        await component.Find(".ec-card-header .ec-button-primary").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find(".ec-modal-title").TextContent, Is.EqualTo("Nový štítek"));
    }

    [Test]
    public async Task EditingALabelOpensTheFormFilledWithIt()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx, [InfZLabel]);

        var component = Render(ctx);

        await component.WaitForElementAsync(".ec-settings-row");
        await component.Find(".ec-settings-row:first-child .ec-button-secondary").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-modal-title").TextContent, Is.EqualTo("Upravit štítek"));
            Assert.That(component.Find("#label-name").GetAttribute("value"), Is.EqualTo("InfZ"));
        }
    }

    [Test]
    public async Task DeletingIsConfirmedAndTheSentenceNamesTheCascade()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Serve(ctx, [InfZLabel]);

        var component = Render(ctx);

        await component.WaitForElementAsync(".ec-settings-row");
        await component.Find(".ec-settings-row:first-child .ec-button-danger").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-modal-title").TextContent, Is.EqualTo("Smazat štítek"));
            Assert.That(
                component.Find(".ec-confirm-text").TextContent,
                Does.Contain("InfZ").And.Contain("spisů a úkonů"),
                "the confirmation names what the cascade takes (SDD-019)");
        }
    }

    [Test]
    public async Task ConfirmingTheDeleteCallsTheApiAndReloadsTheList()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var labelsClient = Serve(ctx, [InfZLabel]);

        var component = Render(ctx);

        await component.WaitForElementAsync(".ec-settings-row");
        await component.Find(".ec-settings-row:first-child .ec-button-danger").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-modal-footer .ec-button-danger").ClickAsync(new MouseEventArgs());

        await labelsClient.Received(1).DeleteLabel(InfZLabel.LabelId, Arg.Any<CancellationToken>());
        await labelsClient.Received(2).ListLabels(Arg.Any<CancellationToken>());
    }

    private static ILabelsClient Serve(BunitContext ctx, IReadOnlyList<LabelItem> items)
    {
        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = items }));
        labelsClient
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(labelsClient);

        return labelsClient;
    }

    private static LabelItem Label(string name, LabelColor color)
    {
        return new LabelItem { LabelId = Guid.CreateVersion7(), Name = name, Color = color };
    }

    private static IRenderedComponent<Settings> Render(BunitContext ctx)
    {
        return ctx.Render<Settings>();
    }
}
