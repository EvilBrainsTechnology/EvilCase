using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Services;

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

    private static Guid Serve(BunitContext ctx, out List<CaseListRequest> caseRequests, out List<ActListRequest> actRequests)
    {
        // The page's TabBlazor components reach for the browser on their first render.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contactId = Guid.CreateVersion7();

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient.GetContact(contactId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ContactDetail
        {
            ContactId = contactId,
            Name = "Městský úřad",
            Kind = ContactKind.Authority,
        }));

        var capturedCases = new List<CaseListRequest>();
        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedCases.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 });
            });

        var capturedActs = new List<ActListRequest>();
        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedActs.Add(call.Arg<ActListRequest>());

                return Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 });
            });

        ctx.Services.AddSingleton(contactsClient);
        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(Substitute.For<IModalService>());

        caseRequests = capturedCases;
        actRequests = capturedActs;

        return contactId;
    }

    private static void Render(
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
    }
}
