using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactListRenderTests
{
    [Test]
    public void TheColumnsRenderInTheOrderTheHostGaveThem()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Kind, ContactColumn.Name, ContactColumn.Address]);

        component.WaitForElement("tbody tr");

        string[] headers = ["Typ", "Kontakt", "Adresa"];

        Assert.That(
            component.FindAll("thead th").Select(static header => header.TextContent.Trim()),
            Is.EqualTo(headers),
            "the host's order of the columns is the order of the headers");
    }

    [Test]
    public void AColumnRendersItsHeaderItsTableCellAndItsCardLineFromOneDefinition()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.DataBoxId]);

        component.WaitForElement("tbody tr");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("thead th").TextContent.Trim(), Is.EqualTo("ID datové schránky"));
            Assert.That(component.Find("tbody td").TextContent, Does.Contain("abc1234"));
            Assert.That(
                component.Find(".card-body.d-lg-none").TextContent,
                Does.Contain("abc1234"),
                "the column that writes the cell writes the card line too");
        }
    }

    [Test]
    public void EveryRowRendersBothTheTableAndTheCardSoOnlyCssChoosesBetweenThem()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Name, ContactColumn.Kind]);

        component.WaitForElement("tbody tr");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".d-none.d-lg-block tbody tr"), Has.Count.EqualTo(1));
            Assert.That(
                component.FindAll(".card-body.d-lg-none > .card"),
                Has.Count.EqualTo(1),
                "the same row renders as a card, and only CSS picks which of the two shows");
        }
    }

    [Test]
    public void ASortableHeaderIsAButtonAndAnUnsortableOneIsPlainText()
    {
        using var ctx = new BunitContext();
        Serve(ctx, out _);

        var component = Render(ctx, [ContactColumn.Name, ContactColumn.Address]);

        component.WaitForElement("tbody tr");

        var headers = component.FindAll("thead th");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headers[0].QuerySelector("button.table-sort"), Is.Not.Null, "a column with a sort key sorts");
            Assert.That(headers[1].QuerySelector("button"), Is.Null, "a column without a sort key carries no button");
        }
    }

    [Test]
    public async Task ClickingTheSortedHeaderFlipsTheDirectionAndAsksForTheFirstPageAgain()
    {
        await using var ctx = new BunitContext();
        Serve(ctx, out var requests, total: 3);

        var component = Render(ctx, [ContactColumn.Name], take: 2, paging: true);

        await component.WaitForElementAsync("tbody tr");
        await component.Find(".ec-listfoot button:last-child").ClickAsync(new MouseEventArgs());

        Assert.That(requests[^1].Skip, Is.EqualTo(2), "the next page asks for the rows behind the first one");

        await component.Find("thead th button").ClickAsync(new MouseEventArgs());

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

        await component.WaitForElementAsync("tbody tr");
        await component.Find(".ec-listfoot button:last-child").ClickAsync(new MouseEventArgs());

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
                Assert.That(component.FindAll("table"), Is.Empty, "a failed load leaves no table behind");
                Assert.That(component.Markup, Does.Contain("Kontakty se nepodařilo načíst"));
            }
        });
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
                    ContactId = Guid.CreateVersion7(),
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
