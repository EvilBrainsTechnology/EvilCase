using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EditActRenderTests
{
    [Test]
    public async Task TheFormCarriesBothNumbersOfTheAct()
    {
        await using var ctx = new BunitContext();

        var (_, detail) = Serve(ctx, out var caseId, out var actId);

        var component = Render(ctx, caseId, actId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.Find("#act-number").GetAttribute("value"),
                Is.EqualTo(detail.ActNumber),
                "the act number is edited here (SDD-010)");
            Assert.That(
                component.Find("#act-external-number").GetAttribute("value"),
                Is.EqualTo(detail.ExternalActNumber),
                "the number another authority gave it is edited here (SDD-010)");
        }
    }

    [Test]
    public async Task TheActSDirectionIsThePressedSegment()
    {
        await using var ctx = new BunitContext();

        Serve(ctx, out var caseId, out var actId);

        var component = Render(ctx, caseId, actId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("#act-direction-outgoing").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(component.Find("#act-direction-incoming").GetAttribute("aria-pressed"), Is.EqualTo("false"));
        }
    }

    [Test]
    public async Task ADifferingCaseContactIsNamedInAWarning()
    {
        await using var ctx = new BunitContext();

        var (_, detail) = Serve(ctx, out var caseId, out var actId);

        var component = Render(ctx, caseId, actId);

        Assert.That(component.Find(".ec-warning").TextContent, Does.Contain(detail.CaseContact!.Name));
    }

    [Test]
    public async Task SavingSendsTheEditedValues()
    {
        await using var ctx = new BunitContext();

        var (actsClient, detail) = Serve(ctx, out var caseId, out var actId);

        var component = Render(ctx, caseId, actId);

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "Odpor proti příkazu — doplnění" });
        await component.Find("#act-external-number").ChangeAsync(new ChangeEventArgs { Value = "MUV-2026/9999" });

        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await actsClient
            .Received(1)
            .EditAct(
                caseId,
                actId,
                Arg.Is<ActEditRequest>(request => request.Title == "Odpor proti příkazu — doplnění"
                    && request.ActNumber == detail.ActNumber
                    && request.ExternalActNumber == "MUV-2026/9999"
                    && request.Date == detail.Date
                    && request.Direction == ActDirection.Outgoing),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnEmptyTitleBlocksSavingAndSaysSo()
    {
        await using var ctx = new BunitContext();

        var (actsClient, _) = Serve(ctx, out var caseId, out var actId);

        var component = Render(ctx, caseId, actId);

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "" });
        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await actsClient.DidNotReceive().EditAct(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ActEditRequest>(), Arg.Any<CancellationToken>());

        Assert.That(
            component.FindAll(".ec-field-error").Select(static node => node.TextContent),
            Has.Some.Contain("Zadejte název úkonu"),
            "the form validates against the page's EditContext before it sends anything");
    }

    private static (IActsClient Acts, ActDetail Detail) Serve(BunitContext ctx, out Guid caseId, out Guid actId)
    {
        caseId = Guid.CreateVersion7();
        actId = Guid.CreateVersion7();

        var contactA = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Městský úřad Vzorov" };
        var contactB = new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Krajský soud ve Vzorově" };

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Překročení rychlosti",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260812-001",
            ExternalActNumber = "MUV-2026/1234",
            Direction = ActDirection.Outgoing,
            Date = new DateOnly(2026, 8, 12),
            Title = "Odpor proti příkazu",
            Contact = contactB,
            CaseContact = contactA,
        };

        var actsClient = Substitute.For<IActsClient>();
        actsClient.GetAct(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));
        actsClient
            .EditAct(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ActEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContactListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(contactsClient);
        ctx.Services.AddSingleton(labelsClient);

        return (actsClient, detail);
    }

    private static IRenderedComponent<EditAct> Render(BunitContext ctx, Guid caseId, Guid actId)
    {
        return ctx.Render<EditAct>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId));
    }
}
