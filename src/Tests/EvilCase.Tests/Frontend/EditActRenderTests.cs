using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EditActRenderTests
{
    [Test]
    public async Task TheEditFormRenders()
    {
        await using var ctx = new BunitContext();

        Serve(ctx, out var actId);

        // A ValidationMessage outside the page's EditContext throws on render.
        var component = Render(ctx, actId);

        Assert.That(component.FindComponents<InputText>(), Is.Not.Empty);
    }

    [Test]
    public async Task AnEmptyTitleBlocksSaving()
    {
        await using var ctx = new BunitContext();
        var form = ctx.JSInterop.SetupModule("./js/ec-form.js");
        form.SetupVoid("focusFirstInvalid");

        var actsClient = Serve(ctx, out var actId);

        var component = Render(ctx, actId);

        await component.Find("#act-title").ChangeAsync(new ChangeEventArgs { Value = "" });
        await component.Find(".ec-card-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await actsClient.DidNotReceive().EditAct(Arg.Any<Guid>(), Arg.Any<ActEditRequest>(), Arg.Any<CancellationToken>());

        form.VerifyInvoke("focusFirstInvalid");
    }

    private static IActsClient Serve(BunitContext ctx, out Guid actId)
    {
        actId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Překročení rychlosti",
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260812-001",
            Direction = ActDirection.Outgoing,
            Date = new DateOnly(2026, 8, 12),
            Title = "Odpor proti příkazu",
        };

        var actsClient = Substitute.For<IActsClient>();
        actsClient.GetAct(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));

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

        return actsClient;
    }

    private static IRenderedComponent<EditAct> Render(BunitContext ctx, Guid actId)
    {
        return ctx.Render<EditAct>(parameters => parameters.Add(static page => page.ActId, actId));
    }
}
