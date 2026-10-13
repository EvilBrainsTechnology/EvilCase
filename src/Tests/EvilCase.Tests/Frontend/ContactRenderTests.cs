using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactRenderTests
{
    [Test]
    public void TheCasesAndTheActsOfAContactBothReadFromTheNewestDate()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests);

        Render(ctx, contactId, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseRequests[0].Sort, Is.EqualTo(CaseSortKey.Date));
            Assert.That(caseRequests[0].SortDirection, Is.EqualTo(ListSortDirection.Descending), "the cases of a contact read from the newest date (SDD-011)");
            Assert.That(actRequests[0].Sort, Is.EqualTo(ActSortKey.Date));
            Assert.That(actRequests[0].SortDirection, Is.EqualTo(ListSortDirection.Descending), "the acts of a contact read from the newest date (SDD-011)");
        }
    }

    [Test]
    public void BothOccurrenceListsAskForTheContactSOwnRows()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests);

        Render(ctx, contactId, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseRequests[0].ContactId, Is.EqualTo(contactId));
            Assert.That(actRequests[0].ContactId, Is.EqualTo(contactId));
        }
    }

    [Test]
    public void AClosedOrASubordinateCaseOfTheContactIsAnOccurrenceToo()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests);

        Render(ctx, contactId, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseRequests[0].Status, Is.EqualTo(CaseStatusFilter.All), "a closed case of the contact is an occurrence (SDD-011)");
            Assert.That(caseRequests[0].Scope, Is.EqualTo(CaseListScope.All), "a subordinate case of the contact is an occurrence (SDD-011)");
        }
    }

    [Test]
    public void TheActsOfTheContactAreHeadedByTheDateOfTheAct()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, act: IncomingAct());

        var component = Render(ctx, contactId, caseRequests, actRequests);

        component.WaitForAssertion(() =>
            Assert.That(
                component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
                Does.Contain("Datum úkonu"),
                "the acts of a contact head their date column with 'Datum úkonu' (#549)"));
    }

    [Test]
    public void TheFactsCarryTheDataBoxAndTheAddressEachWithACopyButtonAndNoCounts()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, dataBoxId: "vzorov1", address: "náměstí Míru 1, 100 00 Vzorov");

        var component = Render(ctx, contactId, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-detail-fact-name").Select(static name => name.TextContent),
                Is.EqualTo(["ID datové schránky", "Adresa"]),
                "the counts live in the list headers");
            Assert.That(component.Find("button[aria-label='Zkopírovat ID datové schránky']"), Is.Not.Null);
            Assert.That(component.Find("button[aria-label='Zkopírovat adresu']"), Is.Not.Null);
        }
    }

    [Test]
    public void AMissingDataBoxAndAddressShowADashWithoutACopyButton()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-detail-fact").Select(static fact => fact.QuerySelector(":scope > span:last-child")!.TextContent), Is.EqualTo(["—", "—"]));
            Assert.That(component.FindAll(".ec-detail-facts .ec-copy"), Is.Empty);
        }
    }

    [Test]
    public void BothListsCarryTheirOwnFilterToolbarWithDistinctIds()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, caseTotal: 3, actTotal: 5);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#contact-cases-search"), Is.Not.Null);
                Assert.That(component.Find("#contact-cases-status"), Is.Not.Null);
                Assert.That(component.Find("#contact-acts-search"), Is.Not.Null);
                Assert.That(component.FindAll("#contact-acts-direction-all, #contact-acts-direction-incoming, #contact-acts-direction-outgoing"), Has.Count.EqualTo(3));
            }
        });
    }

    [Test]
    public void AContactWithoutOccurrencesShowsTwoEmptyCardsWithoutToolbars()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll(".ec-empty"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll(".ec-table-bar"), Is.Empty);
            }
        });
    }

    [Test]
    public void TheHeaderShowsTheKindAndTheMonogramOfTheContact()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, name: "Městský úřad Vzorov", kind: ContactKind.Authority);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Markup, Does.Contain("Úřad"));
            Assert.That(component.Find(".ec-contact-monogram-lg").TextContent.Trim(), Is.EqualTo("MÚ"));
        }
    }

    [Test]
    public async Task AFilterThatMatchesNothingKeepsTheToolbarAndSaysTheFilterMatchesNothing()
    {
        await using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, act: IncomingAct(), actTotal: 1);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        await (await component.WaitForElementAsync("#contact-acts-direction-outgoing")).ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("#contact-acts-search"), Has.Count.EqualTo(1), "a set filter keeps the toolbar");
                Assert.That(
                    component.FindAll(".ec-empty-text").Select(static text => text.TextContent),
                    Is.EqualTo(["Kontakt nefiguruje v žádném spisu.", "Filtru neodpovídá žádný úkon."]));
            }
        });
    }

    [Test]
    public async Task TheEditButtonAndTheMenuDeleteEachOpenTheirModal()
    {
        await using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, name: "Městský úřad");

        var component = Render(ctx, contactId, caseRequests, actRequests);

        Assert.That(component.FindAll(".ec-page-actions .ec-button-danger"), Is.Empty, "delete lives in the menu");

        await component.Find("#contact-edit").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find("dialog").TextContent, Does.Contain("Upravit kontakt"));

        await component.Find(".ec-modal-header .ec-button").ClickAsync(new MouseEventArgs());
        await component.Find("button[aria-label='Další akce s kontaktem']").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll("[role=menuitem]").Select(static item => item.TextContent.Trim()), Is.EqualTo(["Zkopírovat odkaz na kontakt", "Smazat kontakt…"]));

        await component.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("dialog").TextContent, Does.Contain("Smazat kontakt"));
            Assert.That(component.Find("dialog").TextContent, Does.Contain("Městský úřad"));
        }
    }

    [Test]
    public async Task TheMenuCopiesAnAbsoluteLinkToTheContact()
    {
        await using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests);

        var clipboard = (StubClipboardWriter)ctx.Services.GetRequiredService<IClipboardWriter>();
        var component = Render(ctx, contactId, caseRequests, actRequests);

        await component.Find("button[aria-label='Další akce s kontaktem']").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(clipboard.Written, Is.EqualTo([$"http://localhost/contacts/{contactId}"]));
            Assert.That(component.Find(".ec-page > span[role=status]").TextContent, Is.EqualTo("Odkaz na kontakt zkopírován."));
        }
    }

    private static ActListItem IncomingAct()
    {
        return new ActListItem
        {
            ActId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260821-001",
            CaseTitle = "Přestupek",
            ActNumber = "EC/20260821-001-1",
            Title = "Rozhodnutí",
            Date = new DateOnly(2026, 8, 21),
            Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
            Direction = ActDirection.Incoming,
        };
    }

    private static Guid Serve(
        BunitContext ctx,
        out List<CaseListRequest> caseRequests,
        out List<ActListRequest> actRequests,
        string name = "Městský úřad",
        ContactKind kind = ContactKind.Authority,
        ActListItem? act = null,
        int caseTotal = 0,
        int actTotal = 0,
        string? dataBoxId = null,
        string? address = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contactId = Guid.CreateVersion7();

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient.GetContact(contactId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ContactDetail
        {
            ContactId = contactId,
            Name = name,
            Kind = kind,
            DataBoxId = dataBoxId,
            Address = address,
        }));

        var capturedCases = new List<CaseListRequest>();
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedCases.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(new CaseListResponse { Items = [], TotalCount = caseTotal });
            });

        var capturedActs = new List<ActListRequest>();
        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<ActListRequest>();
                capturedActs.Add(request);

                // The served act is incoming, so an outgoing filter matches nothing.
                var outgoing = request.Direction == ActDirection.Outgoing;

                return Task.FromResult(new ActListResponse { Items = act is null || outgoing ? [] : [act], TotalCount = outgoing ? 0 : actTotal });
            });

        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(contactsClient);
        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(actsClient);

        caseRequests = capturedCases;
        actRequests = capturedActs;

        return contactId;
    }

    private static IRenderedComponent<Contact> Render(
        BunitContext ctx,
        Guid contactId,
        List<CaseListRequest> caseRequests,
        List<ActListRequest> actRequests)
    {
        var component = ctx.Render<Contact>(parameters => parameters.Add(static page => page.ContactId, contactId));

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(caseRequests, Is.Not.Empty, "the contact page loads its cases");
                Assert.That(actRequests, Is.Not.Empty, "the contact page loads its acts");
            }
        });

        return component;
    }
}
