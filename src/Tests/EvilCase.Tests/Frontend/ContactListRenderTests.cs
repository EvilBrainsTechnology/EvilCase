using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactListRenderTests
{
    private static readonly Guid ServedContactId = Guid.CreateVersion7();

    [Test]
    public void TheColumnsRenderInTheOrderTheHostGaveThem()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Kind, ContactColumn.Name, ContactColumn.Address]);

        component.WaitForElement("a.ec-table-row");

        string[] headers = ["Typ", "Kontakt", "Adresa"];

        Assert.That(
            component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
            Is.EqualTo(headers),
            "the host's order of the columns is the order of the headers");
    }

    [Test]
    public void AColumnRendersItsHeaderAndItsCellFromOneDefinition()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.DataBoxId]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-table-header").TextContent.Trim(), Is.EqualTo("ID datové schránky"));
            Assert.That(component.Find(".ec-table-cell").TextContent, Does.Contain("abc1234"));
        }
    }

    [Test]
    public void EveryRowRendersOnceAndOnlyCssReflowsItOnANarrowWidth()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Kind, ContactColumn.Name]);

        component.WaitForElement("a.ec-table-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("a.ec-table-row"), Has.Count.EqualTo(1), "the same row reflows on a narrow width; only CSS chooses how");
            Assert.That(
                component.FindAll("a.ec-table-row")[0].QuerySelectorAll(".ec-table-cell")[1].ClassList,
                Does.Contain("ec-table-cell-primary"),
                "the Name column takes its own line where the row reflows");
        }
    }

    [Test]
    public void ASortableHeaderIsAButtonAndAnUnsortableOneIsPlainText()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Name, ContactColumn.Address]);

        component.WaitForElement("a.ec-table-row");

        var headers = component.FindAll(".ec-table-header");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers[0].QuerySelector("button.ec-sort"), Is.Not.Null, "a column with a sort key sorts");
            Assert.That(headers[1].QuerySelector("button"), Is.Null, "a column without a sort key carries no button");
        }
    }

    [Test]
    public async Task ClickingTheSortedHeaderFlipsTheDirectionAndAsksForTheFirstPageAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [ContactColumn.Name], take: 2, paging: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find(".ec-table-header button").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].SortDirection, Is.Not.EqualTo(requests[0].SortDirection), "a click on the sorted column turns it around");
            Assert.That(requests[^1].Skip, Is.Zero, "a change of the order starts the list over");
        }
    }

    [Test]
    public async Task ChangingTheSearchResetsTheListToItsFirstPage()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [ContactColumn.Name], take: 2, paging: true, search: true);

        await component.WaitForElementAsync("a.ec-table-row");
        await component.Find(".ec-card-footer button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "úřad" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests[^1].Search, Is.EqualTo("úřad"));
            Assert.That(requests[^1].Skip, Is.Zero, "a new search starts the list over");
        }
    }

    [Test]
    public void TheFailureStateReplacesTheTableAndNamesWhatFailed()
    {
        using var ctx = new BunitContext();

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<ContactListResponse>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        ctx.Services.AddSingleton(contactsClient);

        var component = Render(ctx, [ContactColumn.Name]);

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("a.ec-table-row"), Is.Empty, "a failed load leaves no row behind");
                Assert.That(component.Markup, Does.Contain("Seznam kontaktů se nepodařilo načíst"));
                Assert.That(component.FindAll(".ec-card-error button"), Has.Count.EqualTo(1), "a failed load offers a retry (SDD-020)");
            }
        });
    }

    [Test]
    public void EveryColumnCarriesAHeaderAndTheKindReadsAsTextInTheRow()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Name, ContactColumn.Kind, ContactColumn.DataBoxId, ContactColumn.Address]);

        component.WaitForElement("a.ec-table-row");

        var row = component.Find("a.ec-table-row").TextContent;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim()), Does.Contain("Typ"));
            Assert.That(component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim()), Does.Contain("ID datové schránky"));
            Assert.That(component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim()), Does.Contain("Adresa"));
            Assert.That(row, Does.Contain("Úřad"), "the kind reads as text in the row, at every width");
        }
    }

    [Test]
    public void TheRowLinksToTheContactDetail()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Name]);

        component.WaitForElement("a.ec-table-row");

        Assert.That(component.Find("a.ec-table-row").GetAttribute("href"), Is.EqualTo($"/contacts/{ServedContactId}"));
    }

    private static void Serve(BunitContext ctx, out List<ContactListRequest> requests, int total = 1)
    {
        var captured = new List<ContactListRequest>();
        var response = new ContactListResponse
        {
            Items =
            [
                new ContactListItem
                {
                    ContactId = ServedContactId,
                    Kind = ContactKind.Authority,
                    Name = "Městský úřad",
                    DataBoxId = "abc1234",
                    Address = "Náměstí 1",
                },
            ],
            TotalCount = total,
        };

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<ContactListRequest>());

                return Task.FromResult(response);
            });

        ctx.Services.AddSingleton(contactsClient);

        requests = captured;
    }

    private static IRenderedComponent<ContactList> Render(
        BunitContext ctx,
        IReadOnlyList<ContactColumn> columns,
        int take = 20,
        bool paging = false,
        bool search = false)
    {
        return ctx.Render<ContactList>(parameters => parameters
            .Add(static list => list.Columns, columns)
            .Add(static list => list.Filter, new ContactListRequest { Take = take })
            .Add(static list => list.ShowPaging, paging)
            .Add(static list => list.ShowSearch, search));
    }
}
