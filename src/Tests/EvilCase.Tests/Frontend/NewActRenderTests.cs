using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class NewActRenderTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(2);

    [Test]
    public async Task PickingADirectionFillsTheContactFromTheCase()
    {
        await using var ctx = new BunitContext();

        var (_, _, _, _, caseContact) = Serve(ctx, out var caseId);

        var component = Render(ctx, caseId);

        await component.WaitForAssertionAsync(() => Assert.That(component.Find("#act-direction-outgoing"), Is.Not.Null), WaitTimeout);
        await component.Find("#act-direction-outgoing").ClickAsync(new MouseEventArgs());

        Assert.That(
            component.Find("#act-contact").GetAttribute("value"),
            Is.EqualTo(caseContact.Name),
            "the direction prefills the act's contact from the case (SDD-010)");
    }

    [Test]
    public async Task ClickingTheChosenDirectionAgainClearsIt()
    {
        await using var ctx = new BunitContext();

        var (_, _, _, _, _) = Serve(ctx, out var caseId);

        var component = Render(ctx, caseId);

        await component.WaitForAssertionAsync(() => Assert.That(component.Find("#act-direction-outgoing"), Is.Not.Null), WaitTimeout);
        await component.Find("#act-direction-outgoing").ClickAsync(new MouseEventArgs());
        await component.Find("#act-direction-outgoing").ClickAsync(new MouseEventArgs());

        Assert.That(
            component.Find("#act-direction-outgoing").GetAttribute("aria-pressed"),
            Is.EqualTo("false"),
            "an act without a direction stays reachable (SDD-010)");
    }

    [Test]
    public async Task ADifferingContactIsWarnedAboutAndStillSaves()
    {
        await using var ctx = new BunitContext();

        var (actsClient, _, _, _, caseContact) = Serve(ctx, out var caseId);

        var component = Render(ctx, caseId);

        await component.Find("#act-contact").InputAsync(new ChangeEventArgs { Value = "soud" });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-combobox-option"), Is.Not.Empty), WaitTimeout);
        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        // A direction is required alongside a contact (SDD-010's "fill in both, or neither").
        await component.Find("#act-direction-outgoing").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-warning"), Is.Not.Empty), WaitTimeout);

        Assert.That(component.Find(".ec-warning").TextContent, Does.Contain(caseContact.Name));

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "Odpor proti příkazu" });
        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await actsClient
            .Received(1)
            .CreateAct(caseId, Arg.Any<CreateActRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FilingSendsTheFormValues()
    {
        await using var ctx = new BunitContext();

        var (actsClient, _, _, _, _) = Serve(ctx, out var caseId);

        var component = Render(ctx, caseId);

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "Odpor proti příkazu" });
        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await actsClient
            .Received(1)
            .CreateAct(
                caseId,
                Arg.Is<CreateActRequest>(static request => request.Title == "Odpor proti příkazu" && request.Direction == null && request.ContactId == null),
                Arg.Any<CancellationToken>());
    }

    private static (IActsClient Acts, ICasesClient Cases, IContactsClient Contacts, ILabelsClient Labels, ContactListItem CaseContact) Serve(BunitContext ctx, out Guid caseId)
    {
        caseId = Guid.CreateVersion7();

        var caseContact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Městský úřad Vzorov" };
        var otherContact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Krajský soud ve Vzorově" };

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .CreateAct(Arg.Any<Guid>(), Arg.Any<CreateActRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListItem
            {
                ActId = Guid.CreateVersion7(),
                CaseId = caseId,
                CaseNumber = "EC/20260807-001",
                CaseTitle = "Překročení rychlosti",
                ActNumber = "EC/20260807-001/20260812-001",
                Title = "Odpor proti příkazu",
                Date = new DateOnly(2026, 8, 12),
                Changed = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
            }));

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .GetCase(caseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseDetail
            {
                CaseId = caseId,
                CaseNumber = "EC/20260807-001",
                Date = new DateOnly(2026, 8, 7),
                Title = "Překročení rychlosti",
                Status = CaseStatus.Active,
                Contact = caseContact,
            }));

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContactListResponse { Items = [otherContact], TotalCount = 1 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(contactsClient);
        ctx.Services.AddSingleton(labelsClient);

        return (actsClient, casesClient, contactsClient, labelsClient, caseContact);
    }

    private static IRenderedComponent<NewAct> Render(BunitContext ctx, Guid caseId)
    {
        return ctx.Render<NewAct>(parameters => parameters.Add(static page => page.CaseId, caseId));
    }
}
