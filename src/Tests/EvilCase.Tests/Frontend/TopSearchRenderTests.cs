using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class TopSearchRenderTests
{
    private static readonly Guid CaseId = Guid.NewGuid();

    private static readonly Guid ActId = Guid.NewGuid();

    private static readonly Guid ContactId = Guid.NewGuid();

    private static readonly Guid LabelId = Guid.NewGuid();

    [Test]
    public async Task OneCharacterSearchesNothing()
    {
        await using var ctx = new BunitContext();
        var client = Arrange(ctx, Everything());

        var component = ctx.Render<TopSearch>();
        await component.Find(".ec-topsearch-input").InputAsync(new ChangeEventArgs { Value = "p" });

        Assert.That(component.Find(".ec-sr-state").TextContent, Does.Contain("Napište alespoň 2 znaky"));

        await client.DidNotReceive().Search(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnActiveRowIsTheFirstAndTheArrowsMoveAcrossGroupsAndWrap()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, Everything());

        var component = ctx.Render<TopSearch>();
        var input = component.Find(".ec-topsearch-input");
        await input.InputAsync(new ChangeEventArgs { Value = "poj" });
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("[role=option]"), Has.Count.EqualTo(5)));

        var first = component.FindAll("[role=option]")[0].Id;
        Assert.That(input.GetAttribute("aria-activedescendant"), Is.EqualTo(first), "the first row is active when the results arrive");

        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.That(component.FindAll("[role=option]")[1].GetAttribute("aria-selected"), Is.EqualTo("true"), "the arrow moves to the next row, across groups");

        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.That(component.FindAll("[role=option]")[4].GetAttribute("aria-selected"), Is.EqualTo("true"), "the arrow up from the first row wraps to the last");

        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.That(component.FindAll("[role=option]")[0].GetAttribute("aria-selected"), Is.EqualTo("true"), "the arrow down from the last row wraps to the first");
    }

    [Test]
    public async Task EnterOpensTheActiveRowAndClosesTheList()
    {
        await using var ctx = new BunitContext();
        Arrange(ctx, Everything());

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<TopSearch>();
        var input = component.Find(".ec-topsearch-input");
        await input.InputAsync(new ChangeEventArgs { Value = "poj" });
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("[role=option]"), Has.Count.EqualTo(5)));

        await input.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(navigation.Uri, Does.EndWith($"/acts/{ActId}"));
            Assert.That(component.FindAll("[role=listbox]"), Is.Empty);
        }
    }

    [Test]
    public async Task AFailedSearchAfterAFoundOneLeavesNoRowForEnterToOpen()
    {
        await using var ctx = new BunitContext();
        var client = Arrange(ctx, Nothing());
        var found = Everything();
        client
            .Search(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(found), static _ => throw new HttpRequestException());

        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<TopSearch>();
        var input = component.Find(".ec-topsearch-input");
        await input.InputAsync(new ChangeEventArgs { Value = "poj" });
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("[role=option]"), Is.Not.Empty));
        await input.InputAsync(new ChangeEventArgs { Value = "pojx" });
        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-sr-state").TextContent, Does.Contain("Hledání se nepodařilo")));

        await input.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(navigation.Uri, Does.Not.Contain("/cases/"), "Enter opens no row the failure hid");
            Assert.That(component.Find(".ec-topsearch-input").GetAttribute("aria-activedescendant"), Is.Null, "no hidden row stays active");
        }
    }

    private static ISearchClient Arrange(BunitContext ctx, SearchResponse response)
    {
        var client = Substitute.For<ISearchClient>();
        client
            .Search(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupModule("./js/ec-top-search.js").Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(client);

        return client;
    }

    private static SearchGroup<TItem> Group<TItem>(int total, params TItem[] items)
    {
        return new SearchGroup<TItem> { Items = items, TotalCount = total };
    }

    private static SearchResponse Nothing()
    {
        return new SearchResponse
        {
            Cases = Group<CaseListItem>(0),
            Acts = Group<ActListItem>(0),
            Contacts = Group<ContactListItem>(0),
            Files = Group<FileSearchItem>(0),
            Comments = Group<CommentSearchItem>(0),
        };
    }

    private static SearchResponse Everything()
    {
        return new SearchResponse
        {
            Cases = Group(
                5,
                new CaseListItem
                {
                    CaseId = CaseId,
                    CaseNumber = "EC/20251002-001",
                    Title = "Uplatnění pojištění právní ochrany",
                    Date = new DateOnly(2025, 10, 2),
                    Status = CaseStatus.Active,
                    Changed = DateTime.UtcNow,
                    Labels = [new LabelItem { LabelId = LabelId, Name = "Priorita", Color = LabelColor.Pink }],
                }),
            Acts = Group(
                1,
                new ActListItem
                {
                    ActId = ActId,
                    CaseId = CaseId,
                    CaseNumber = "EC/20251002-001",
                    CaseTitle = "Uplatnění pojištění právní ochrany",
                    ActNumber = "EC/20251002-001/02",
                    Direction = ActDirection.Outgoing,
                    Title = "Výzva pojišťovně k plnění",
                    Date = new DateOnly(2025, 10, 6),
                    Changed = DateTime.UtcNow,
                }),
            Contacts = Group(
                1,
                new ContactListItem
                {
                    ContactId = ContactId,
                    Kind = ContactKind.Authority,
                    Name = "Pojišťovna Alfa, a. s.",
                    Address = "Hlavní 12, 110 00 Praha 1",
                    DataBoxId = "ab1cd2e",
                }),
            Files = OneFile(),
            Comments = OneComment(),
        };
    }

    private static SearchGroup<FileSearchItem> OneFile()
    {
        return Group(
            1,
            new FileSearchItem
            {
                FileId = Guid.NewGuid(),
                FileName = "pojistna-smlouva-2025.pdf",
                SizeBytes = 421_888,
                ActId = ActId,
                OwnerTitle = "Výzva pojišťovně k plnění",
            });
    }

    private static SearchGroup<CommentSearchItem> OneComment()
    {
        return Group(
            1,
            new CommentSearchItem
            {
                CommentId = Guid.NewGuid(),
                Body = "Pojišťovna potvrdila přijetí výzvy telefonicky.",
                Created = DateTime.UtcNow,
                CaseId = CaseId,
                OwnerTitle = "Uplatnění pojištění právní ochrany",
            });
    }
}
