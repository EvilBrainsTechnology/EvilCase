using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
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

        var act = new ActListItem
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

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, act: act);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        component.WaitForAssertion(() =>
            Assert.That(
                component.FindAll(".ec-table-header").Select(static header => header.TextContent.Trim().TrimEnd('↑', '↓').Trim()),
                Does.Contain("Datum úkonu"),
                "the acts of a contact head their date column with 'Datum úkonu' (#549)"));
    }

    [Test]
    public void TheHeaderCountsComeFromTheOccurrenceLists()
    {
        using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, caseTotal: 3, actTotal: 5);

        var component = Render(ctx, contactId, caseRequests, actRequests);

        component.WaitForAssertion(() =>
        {
            var facts = component.FindAll(".ec-detail-fact").ToList();
            var caseFact = facts.First(static fact => string.Equals(fact.QuerySelector(".ec-detail-fact-name")!.TextContent, "Spisy", StringComparison.Ordinal));
            var actFact = facts.First(static fact => string.Equals(fact.QuerySelector(".ec-detail-fact-name")!.TextContent, "Úkony", StringComparison.Ordinal));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(caseFact.QuerySelector(".ec-detail-fact-value")!.TextContent, Is.EqualTo("3"));
                Assert.That(actFact.QuerySelector(".ec-detail-fact-value")!.TextContent, Is.EqualTo("5"));
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
    public async Task TheEditAndTheDeleteButtonEachOpenTheirModal()
    {
        await using var ctx = new BunitContext();

        var contactId = Serve(ctx, out var caseRequests, out var actRequests, name: "Městský úřad");

        var component = Render(ctx, contactId, caseRequests, actRequests);

        await component.Find("#contact-edit").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find("dialog").TextContent, Does.Contain("Upravit kontakt"));

        await component.Find(".ec-modal-header .ec-button").ClickAsync(new MouseEventArgs());
        await component.Find("#contact-delete").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("dialog").TextContent, Does.Contain("Smazat kontakt"));
            Assert.That(component.Find("dialog").TextContent, Does.Contain("Městský úřad"));
        }
    }

    private static Guid Serve(
        BunitContext ctx,
        out List<CaseListRequest> caseRequests,
        out List<ActListRequest> actRequests,
        string name = "Městský úřad",
        ContactKind kind = ContactKind.Authority,
        ActListItem? act = null,
        int caseTotal = 0,
        int actTotal = 0)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contactId = Guid.CreateVersion7();

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient.GetContact(contactId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ContactDetail
        {
            ContactId = contactId,
            Name = name,
            Kind = kind,
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
                capturedActs.Add(call.Arg<ActListRequest>());

                return Task.FromResult(new ActListResponse { Items = act is null ? [] : [act], TotalCount = actTotal });
            });

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
