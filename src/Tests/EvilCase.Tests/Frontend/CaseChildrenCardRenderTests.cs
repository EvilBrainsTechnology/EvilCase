using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseChildrenCardRenderTests
{
    [Test]
    public async Task OnlyTheOpenChildrenShowUntilTheToggleShowsTheClosedOnes()
    {
        await using var ctx = new BunitContext();

        Serve(
            ctx,
            [
                Child("Předžalobní výzva", CaseStatus.Active),
                Child("Oznámení přestupku", CaseStatus.WaitingOnAuthority),
                Child("Žádost o informace", CaseStatus.Closed),
            ]);

        var component = Render(ctx, Guid.CreateVersion7());

        await component.WaitForElementAsync(".ec-child-title");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-child-title").Select(static title => title.TextContent), Is.EqualTo(["Předžalobní výzva", "Oznámení přestupku"]));
            Assert.That(component.Find(".ec-child-counts").TextContent, Is.EqualTo("2 otevřené · 1 uzavřený"), "spaces keep the parts apart where the dot is hidden from a screen reader");
            Assert.That(component.FindAll(".ec-child-counts .ec-sep"), Has.Count.EqualTo(1), "the counts take the one separator of the app");
            Assert.That(component.Find(".ec-child-toggle").TextContent, Is.EqualTo("Zobrazit i uzavřené (1)"));
            Assert.That(component.Find(".ec-child-toggle").GetAttribute("aria-expanded"), Is.EqualTo("false"));
            Assert.That(component.Find(".ec-child-toggle").GetAttribute("aria-controls"), Is.EqualTo("case-children"));
            Assert.That(component.FindAll("#case-children"), Has.Count.EqualTo(1));
        }

        await component.Find(".ec-child-toggle").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-child-title"), Has.Count.EqualTo(3));
            Assert.That(component.Find(".ec-child-toggle").TextContent, Is.EqualTo("Skrýt uzavřené"));
            Assert.That(component.Find(".ec-child-toggle").GetAttribute("aria-expanded"), Is.EqualTo("true"));
        }
    }

    [Test]
    public async Task ARowCarriesItsStatusAndDateOnTheSecondLine()
    {
        await using var ctx = new BunitContext();

        var child = Child("Předžalobní výzva", CaseStatus.Active);

        Serve(ctx, [child]);

        var component = Render(ctx, Guid.CreateVersion7());

        await component.WaitForElementAsync(".ec-child-title");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-child-status").TextContent.Trim(), Is.EqualTo("Aktivní · 2. 10. 2025"));
            Assert.That(component.Find(".ec-child-title").GetAttribute("title"), Is.EqualTo("Předžalobní výzva"));
            Assert.That(component.Find("a.ec-child-row").GetAttribute("href"), Is.EqualTo($"/cases/{child.CaseId}"));
        }
    }

    [Test]
    public async Task WithNoClosedChildThereIsNoToggle()
    {
        await using var ctx = new BunitContext();

        Serve(ctx, [Child("Předžalobní výzva", CaseStatus.Active), Child("Oznámení přestupku", CaseStatus.Active)]);

        var component = Render(ctx, Guid.CreateVersion7());

        await component.WaitForElementAsync(".ec-child-title");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-child-toggle"), Is.Empty);
            Assert.That(component.Find(".ec-child-counts").TextContent, Is.EqualTo("2 otevřené · 0 uzavřených"));
        }
    }

    [Test]
    public async Task TheCardAsksOnceForEveryChildOfTheCase()
    {
        await using var ctx = new BunitContext();

        var casesClient = Serve(ctx, [Child("Předžalobní výzva", CaseStatus.Active), Child("Žádost o informace", CaseStatus.Closed)]);
        var parentCaseId = Guid.CreateVersion7();
        var component = Render(ctx, parentCaseId);

        await component.WaitForElementAsync(".ec-child-toggle");
        await component.Find(".ec-child-toggle").ClickAsync(new MouseEventArgs());

        await casesClient
            .Received(1)
            .ListCases(
                Arg.Is<CaseListRequest>(request => request.ParentCaseId == parentCaseId && request.Status == CaseStatusFilter.All),
                Arg.Any<CancellationToken>());
    }

    private static CaseListItem Child(string title, CaseStatus status)
    {
        return new CaseListItem
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20251002-001",
            Title = title,
            Status = status,
            Date = new DateOnly(2025, 10, 2),
            Changed = new DateTime(2025, 10, 2, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static ICasesClient Serve(BunitContext ctx, IReadOnlyList<CaseListItem> children)
    {
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseListResponse { Items = children, TotalCount = children.Count }));

        ctx.Services.AddSingleton(casesClient);

        return casesClient;
    }

    private static IRenderedComponent<CaseChildrenCard> Render(BunitContext ctx, Guid parentCaseId)
    {
        return ctx.Render<CaseChildrenCard>(parameters => parameters.Add(static card => card.ParentCaseId, parentCaseId));
    }
}
