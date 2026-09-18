using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EditCaseRenderTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(2);

    [Test]
    public async Task TheParentIsPickedFromAHandfulOfMatchesAcrossEveryStatusAndDepth()
    {
        await using var ctx = new BunitContext();

        var served = Serve(ctx);

        var component = Render(ctx, served.Offered[0].CaseId);

        Assert.That(served.Requests, Is.Empty, "an untouched field loads no list at all");

        await component.Find("#case-parent").InputAsync(new ChangeEventArgs { Value = "spis" });

        var request = served.Requests.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Search, Is.EqualTo("spis"), "the server does the searching");
            Assert.That(request.Take, Is.LessThanOrEqualTo(10), "the field takes a handful of matches, never the whole tenant");
            Assert.That(request.Status, Is.EqualTo(CaseStatusFilter.All), "the parent comes from any case of the tenant (SDD-009)");
            Assert.That(request.Scope, Is.EqualTo(CaseListScope.All), "a subordinate case can be a parent too (SDD-009)");
        }
    }

    [Test]
    public async Task TheCaseIsNeverOfferedAsItsOwnParent()
    {
        await using var ctx = new BunitContext();

        var served = Serve(ctx);

        var component = Render(ctx, served.Offered[0].CaseId);

        await component.Find("#case-parent").InputAsync(new ChangeEventArgs { Value = "spis" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)),
            WaitTimeout);

        Assert.That(
            component.Find(".ec-combobox-option").TextContent,
            Does.Contain(served.Offered[1].CaseNumber),
            "a case is never its own parent (SDD-009)");
    }

    [Test]
    public async Task AFailedContactSearchSaysSoInsteadOfClaimingNothingMatches()
    {
        await using var ctx = new BunitContext();

        var served = Serve(ctx);
        served.Contacts
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<ContactListResponse>(new HttpRequestException("the API is down")));

        var component = Render(ctx, served.Offered[0].CaseId);

        await component.Find("#case-contact").InputAsync(new ChangeEventArgs { Value = "úřad" });

        await component.WaitForAssertionAsync(
            () => Assert.That(
                component.Find(".ec-combobox .ec-field-hint").TextContent,
                Does.Contain("nezdařilo"),
                "a search that failed never reads as a search that found nothing"),
            WaitTimeout);
    }

    [Test]
    public async Task SavingSendsTheEditedValues()
    {
        await using var ctx = new BunitContext();

        var served = Serve(ctx);
        var caseId = served.Offered[0].CaseId;

        var component = Render(ctx, caseId);

        await component.Find("#case-title").ChangeAsync(new ChangeEventArgs { Value = "Přestupek — odvolání" });
        await component.Find("#case-status").ChangeAsync(new ChangeEventArgs { Value = nameof(CaseStatus.Closed) });

        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await served.Cases
            .Received(1)
            .EditCase(
                caseId,
                Arg.Is<CaseEditRequest>(static request => request.Title == "Přestupek — odvolání"
                    && request.Status == CaseStatus.Closed
                    && request.CaseNumber == "EC/20260821-001"),
                Arg.Any<CancellationToken>());
    }

    private static (ICasesClient Cases, IContactsClient Contacts, List<CaseListRequest> Requests, IReadOnlyList<CaseListItem> Offered) Serve(BunitContext ctx)
    {
        var captured = new List<CaseListRequest>();
        IReadOnlyList<CaseListItem> items =
        [
            Item("EC/20260821-001", "Přestupek"),
            Item("EC/20260821-002", "Odvolání"),
        ];

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .GetCase(items[0].CaseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseDetail
            {
                CaseId = items[0].CaseId,
                CaseNumber = items[0].CaseNumber,
                Date = items[0].Date,
                Title = items[0].Title,
                Status = items[0].Status,
            }));
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured.Add(call.Arg<CaseListRequest>());

                return Task.FromResult(new CaseListResponse { Items = items, TotalCount = items.Count });
            });
        casesClient
            .EditCase(Arg.Any<Guid>(), Arg.Any<CaseEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContactListResponse { Items = [], TotalCount = 0 }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(contactsClient);

        return (casesClient, contactsClient, captured, items);
    }

    private static CaseListItem Item(string caseNumber, string title)
    {
        return new CaseListItem
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = caseNumber,
            Title = title,
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.Active,
            Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static IRenderedComponent<EditCase> Render(BunitContext ctx, Guid caseId)
    {
        return ctx.Render<EditCase>(parameters => parameters.Add(static page => page.CaseId, caseId));
    }
}
