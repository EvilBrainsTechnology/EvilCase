using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActRenderTests
{
    [Test]
    public void TheActRenders()
    {
        using var ctx = FrontendContext.Create();

        var actId = Guid.CreateVersion7();

        Serve(ctx, actId, Detail(actId, status: null));

        var component = ctx.Render<Act>(parameters => parameters.Add(static page => page.ActId, actId));

        // A second render diffs the keyed cards again, which threw when two of them shared a key.
        component.Render();

        Assert.That(component.FindComponents<CommentsCard>(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task MarkingAsSentWritesTheStatusAndShowsIt()
    {
        await using var ctx = FrontendContext.Create();

        var actId = Guid.CreateVersion7();
        var actsClient = Serve(ctx, actId, Detail(actId, ActStatus.InProgress));

        var component = ctx.Render<Act>(parameters => parameters.Add(static page => page.ActId, actId));

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
        await component.FindAll("[role=menuitem]").Single(static item => item.TextContent.Contains("Označit jako odesláno", StringComparison.Ordinal)).ClickAsync(new MouseEventArgs());

        await actsClient.Received(1).SetActStatus(actId, Arg.Is<ActStatusRequest>(static request => request.Status == ActStatus.Sent), Arg.Any<CancellationToken>());

        var header = component.Find(".ec-page-header-row").TextContent;

        await component.Find("button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());

        var items = component.FindAll("[role=menuitem]").Select(static item => item.TextContent).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(header, Does.Contain("Odesláno").And.Not.Contain("Rozpracováno"), "the header shows the new status at once");
            Assert.That(items, Has.None.Contain("Označit jako odesláno"), "a sent act offers no marking as sent");
        }
    }

    private static ActDetail Detail(Guid actId, ActStatus? status)
    {
        return new ActDetail
        {
            ActId = actId,
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260807-002",
            Status = status,
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };
    }

    private static IActsClient Serve(BunitContext ctx, Guid actId, ActDetail detail)
    {
        var actsClient = Substitute.For<IActsClient>();
        actsClient.GetAct(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        var actFilesClient = Substitute.For<IActFilesClient>();
        actFilesClient.ListActFiles(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var actCommentsClient = Substitute.For<IActCommentsClient>();
        actCommentsClient.ListActComments(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(Substitute.For<IActLabelsClient>());
        ctx.Services.AddSingleton(actCommentsClient);
        ctx.Services.AddSingleton(actFilesClient);
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return actsClient;
    }
}
