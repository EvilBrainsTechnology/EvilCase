using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Models;
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
    public async Task TheCaseCardLinksTheCaseAsTheActDetailDoes()
    {
        await using var ctx = new BunitContext();

        var (_, _, _, _, _) = Serve(ctx, out var caseId);

        var component = Render(ctx, caseId);

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-form-aside a.ec-case-link"), Is.Not.Null), WaitTimeout);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-form-aside a.ec-case-link").GetAttribute("href"), Is.EqualTo($"/cases/{caseId}"));
            Assert.That(component.FindAll(".ec-form-aside .ec-case-link-title"), Has.Count.EqualTo(1), "one look for the case card on every screen");
        }
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
            .CreateAct(Arg.Is<CreateActRequest>(request => request.CaseId == caseId), Arg.Any<CancellationToken>());
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
                Arg.Is<CreateActRequest>(request => request.CaseId == caseId && request.Title == "Odpor proti příkazu" && request.Direction == null && request.ContactId == null),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PickingTheCaseInTheFormFilesTheActIntoIt()
    {
        await using var ctx = new BunitContext();

        var (actsClient, _, _, _, _) = Serve(ctx, out var caseId);

        var component = Render(ctx, caseId: null);

        await component.Find("#act-case").InputAsync(new ChangeEventArgs { Value = "rychlosti" });
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-combobox-option"), Is.Not.Empty), WaitTimeout);
        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find("#act-case").GetAttribute("value"), Is.EqualTo("Překročení rychlosti")),
            WaitTimeout);

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "Odpor proti příkazu" });
        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await actsClient
            .Received(1)
            .CreateAct(Arg.Is<CreateActRequest>(request => request.CaseId == caseId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheFormWithoutACaseRefusesToFileUntilOneIsPicked()
    {
        await using var ctx = new BunitContext();

        var (actsClient, _, _, _, _) = Serve(ctx, out _);

        var component = Render(ctx, caseId: null);

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "Odpor proti příkazu" });
        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".validation-message").TextContent, Does.Contain("Vyberte spis"), "an act is filed into a case");
            Assert.That(actsClient.ReceivedCalls(), Is.Empty, "nothing is filed without a case");
        }
    }

    private static (IActsClient Acts, ICasesClient Cases, IContactsClient Contacts, ILabelsClient Labels, ContactListItem CaseContact) Serve(BunitContext ctx, out Guid caseId)
    {
        caseId = Guid.CreateVersion7();

        var caseContact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Městský úřad Vzorov" };
        var otherContact = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Krajský soud ve Vzorově" };

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .CreateAct(Arg.Any<CreateActRequest>(), Arg.Any<CancellationToken>())
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

        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ListedCase(caseId)));

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

    private static CaseListResponse ListedCase(Guid caseId)
    {
        return new CaseListResponse
        {
            Items =
            [
                new CaseListItem
                {
                    CaseId = caseId,
                    CaseNumber = "EC/20260807-001",
                    Title = "Překročení rychlosti",
                    Date = new DateOnly(2026, 8, 7),
                    Status = CaseStatus.Active,
                    Changed = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc),
                },
            ],
            TotalCount = 1,
        };
    }

    private static IRenderedComponent<NewAct> Render(BunitContext ctx, Guid? caseId)
    {
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(ActRoutes.New(caseId));

        return ctx.Render<NewAct>();
    }
}
